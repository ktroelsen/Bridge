using System.Text.Json;
using System.Text.RegularExpressions;
using Bridge.Engine;
using Bridge.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Bridge.Api.Players;

/// <summary>
/// Players and their lesson progress. A player is identified by name only; anyone who knows a
/// name can continue as it, which is fine for a family/friends app.
/// </summary>
public static partial class PlayerEndpoints
{
    public const int MaxNameLength = 30;
    public const int MaxLessonIdLength = 40;

    public static void MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var players = app.MapGroup("/api/players");
        MapGameRecords(players);

        players.MapPost("/", async (PlayerRequest request, AppDbContext db) =>
        {
            var player = await GetOrCreateAsync(db, request.Name);
            return player is null ? BadName() : Results.Ok(new PlayerDto(player.Name));
        });

        players.MapGet("/{name}/progress", async (string name, AppDbContext db) =>
        {
            var player = await FindAsync(db, name);
            if (player is null) return Results.Ok(Array.Empty<LessonProgressDto>());
            var rows = await db.LessonProgress.Where(p => p.PlayerId == player.Id).OrderBy(p => p.CompletedAt).ToListAsync();
            return Results.Ok(rows.Select(ToDto));
        });

        // Records a finished lesson. Only the best score is kept, so repeating a lesson never lowers it.
        players.MapPut("/{name}/progress/{lessonId}", async (string name, string lessonId, LessonResultRequest request, AppDbContext db) =>
        {
            if (!LessonId().IsMatch(lessonId) || lessonId.Length > MaxLessonIdLength)
                return Results.BadRequest(new { error = "Ukendt lektion." });
            if (request.Score is < 0 or > 100)
                return Results.BadRequest(new { error = "Scoren skal være 0-100." });
            var player = await GetOrCreateAsync(db, name);
            if (player is null) return BadName();

            var row = await db.LessonProgress.FirstOrDefaultAsync(p => p.PlayerId == player.Id && p.LessonId == lessonId);
            if (row is null)
            {
                row = new LessonProgress { PlayerId = player.Id, LessonId = lessonId, BestScore = request.Score, CompletedAt = DateTime.UtcNow };
                db.LessonProgress.Add(row);
            }
            else if (request.Score > row.BestScore)
            {
                row.BestScore = request.Score;
                row.CompletedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(row));
        });
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Finished games: the server replays the request and records the real result.</summary>
    private static void MapGameRecords(RouteGroupBuilder players)
    {
        players.MapPost("/{name}/games/mini", async (string name, MiniBridgeRequest request, AppDbContext db) =>
        {
            MiniBridgeGame game;
            try
            {
                game = MiniBridgeGame.Replay(request.Number ?? 0, request.Contract?.Strain, request.Contract?.Game ?? false,
                    (request.Cards ?? []).Select(c => new Card(c.Suit, c.Rank)).ToList(), request.Level);
            }
            catch (Exception ex) when (ex is InvalidPlayException or ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            if (game.Phase != GamePhase.Done) return Results.BadRequest(new { error = "Spillet er ikke slut." });
            return await Save(db, name, "mini", request.Number!.Value, request.Level, JsonSerializer.Serialize(request with { Practice = Practice.Any }, Json),
                game.Contract, game.Play!.DeclarerTricks, game.ScoreNorthSouth ?? 0);
        });

        players.MapPost("/{name}/games/full", async (string name, GameRequest request, AppDbContext db) =>
        {
            BridgeGame game;
            try
            {
                game = BridgeGame.Replay(request.Number ?? 0, (request.Calls ?? []).Select(Call.Parse).ToList(),
                    (request.Cards ?? []).Select(c => new Card(c.Suit, c.Rank)).ToList(), request.Level);
            }
            catch (Exception ex) when (ex is InvalidPlayException or FormatException or ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            if (game.Phase is not (GamePhase.Done or GamePhase.PassedOut)) return Results.BadRequest(new { error = "Spillet er ikke slut." });
            return await Save(db, name, "full", request.Number!.Value, request.Level, JsonSerializer.Serialize(request, Json),
                game.Contract, game.Play?.DeclarerTricks ?? 0, game.ScoreNorthSouth ?? 0);
        });

        players.MapGet("/{name}/stats", async (string name, AppDbContext db) =>
        {
            var player = await FindAsync(db, name);
            if (player is null) return Results.Ok(new StatsDto(0, 0, [], []));
            var games = await db.Games.Where(g => g.PlayerId == player.Id).OrderByDescending(g => g.FinishedAt).ToListAsync();
            var played = games.Where(g => g.Contract != "").ToList();
            var byLevel = played.GroupBy(g => g.Level).OrderBy(g => g.Key switch { "easy" => 0, "medium" => 1, _ => 2 }).Select(g => new StatsGroupDto(
                g.Key,
                g.Count(),
                g.Count(x => x.OurContract),
                g.Count(x => x.OurContract && x.Made),
                g.Count(x => !x.OurContract),
                g.Count(x => !x.OurContract && !x.Made),
                g.Sum(x => x.ScoreNorthSouth))).ToList();
            return Results.Ok(new StatsDto(games.Count, games.Count - played.Count, byLevel, [.. games.Take(30).Select(ToDto)]));
        });
    }

    private static async Task<IResult> Save(AppDbContext db, string name, string mode, int number, BotLevel level, string requestJson,
        Contract? contract, int declarerTricks, int score)
    {
        var player = await GetOrCreateAsync(db, name);
        if (player is null) return BadName();
        // Saving the same finished game twice (a retry, a reload) returns the first record.
        var existing = await db.Games.FirstOrDefaultAsync(g => g.PlayerId == player.Id && g.Mode == mode && g.RequestJson == requestJson);
        if (existing is not null) return Results.Ok(ToDto(existing));
        var record = new GameRecord
        {
            PlayerId = player.Id,
            Mode = mode,
            Number = number,
            Level = JsonNamingPolicy.CamelCase.ConvertName(level.ToString()),
            RequestJson = requestJson,
            Contract = contract is null ? "" : Call.Bid(contract.Level, contract.Strain).ToString(),
            Declarer = contract is null ? "" : JsonNamingPolicy.CamelCase.ConvertName(contract.Declarer.ToString()),
            OurContract = contract?.Declarer.IsNorthSouth() ?? false,
            DeclarerTricks = declarerTricks,
            Made = contract is not null && declarerTricks >= contract.TricksNeeded,
            ScoreNorthSouth = score,
            FinishedAt = DateTime.UtcNow,
        };
        db.Games.Add(record);
        await db.SaveChangesAsync();
        return Results.Ok(ToDto(record));
    }

    private static GameRecordDto ToDto(GameRecord g) =>
        new(g.Id, g.Mode, g.Number, g.Level, g.Contract, g.Declarer, g.OurContract, g.DeclarerTricks, g.Made, g.ScoreNorthSouth, g.FinishedAt);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex LessonId();

    private static LessonProgressDto ToDto(LessonProgress p) => new(p.LessonId, p.BestScore, p.CompletedAt);

    private static IResult BadName() =>
        Results.BadRequest(new { error = $"Navnet skal være 1-{MaxNameLength} tegn." });

    private static string? Clean(string? name)
    {
        var trimmed = string.Join(' ', (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return trimmed.Length is >= 1 and <= MaxNameLength ? trimmed : null;
    }

    private static async Task<Player?> FindAsync(AppDbContext db, string name)
    {
        var clean = Clean(name);
        if (clean is null) return null;
        var normalized = clean.ToUpperInvariant();
        return await db.Players.FirstOrDefaultAsync(p => p.NormalizedName == normalized);
    }

    private static async Task<Player?> GetOrCreateAsync(AppDbContext db, string name)
    {
        var clean = Clean(name);
        if (clean is null) return null;
        var existing = await FindAsync(db, clean);
        if (existing is not null) return existing;
        var player = new Player { Name = clean, NormalizedName = clean.ToUpperInvariant(), CreatedAt = DateTime.UtcNow };
        db.Players.Add(player);
        try
        {
            await db.SaveChangesAsync();
            return player;
        }
        catch (DbUpdateException)
        {
            // Created at the same moment by another request.
            db.Entry(player).State = EntityState.Detached;
            return await FindAsync(db, clean);
        }
    }
}
