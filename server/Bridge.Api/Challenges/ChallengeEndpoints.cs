using System.Security.Cryptography;
using System.Text.Json;
using Bridge.Api.Data;
using Bridge.Api.Players;
using Bridge.Engine;
using Microsoft.EntityFrameworkCore;

namespace Bridge.Api.Challenges;

public sealed record CreateChallengeRequest(string Name, string Mode, BotLevel Level, int Games);

/// <summary>One player's result on a deal. Score and contract are left out while the viewer hasn't played the deal.</summary>
public sealed record BoardResultDto(string Name, string? Contract, string? Declarer, int? DeclarerTricks, bool? Made, int? ScoreNorthSouth);

public sealed record BoardDto(int Index, int Number, bool PlayedByViewer, IReadOnlyList<BoardResultDto> Results);

/// <summary>A player's standing over the deals both they and the viewer have played.</summary>
public sealed record StandingDto(string Name, int Played, int Compared, int TotalScore, double? Percent);

public sealed record ChallengeDto(
    string Code, string Mode, BotLevel Level, string CreatedBy, DateTime CreatedAt,
    IReadOnlyList<BoardDto> Boards, IReadOnlyList<StandingDto> Standings);

public sealed record ChallengeSummaryDto(string Code, string Mode, BotLevel Level, string CreatedBy, DateTime CreatedAt, int Games, int PlayedByMe, int Players);

/// <summary>
/// Challenges: the same series of deals played by friends against the bots, each on their own
/// time, compared deal by deal with matchpoints (as in duplicate bridge).
/// </summary>
public static class ChallengeEndpoints
{
    public static readonly int[] Sizes = [4, 8, 12];
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static void MapChallengeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/challenges", async (CreateChallengeRequest request, AppDbContext db) =>
        {
            if (request.Mode is not ("mini" or "full")) return Results.BadRequest(new { error = "Ukendt spilform." });
            if (!Sizes.Contains(request.Games)) return Results.BadRequest(new { error = $"Vælg {string.Join(", ", Sizes)} spil." });
            var player = await PlayerEndpoints.GetOrCreateAsync(db, request.Name);
            if (player is null) return PlayerEndpoints.BadName();

            var numbers = new List<int>();
            while (numbers.Count < request.Games)
            {
                var n = Random.Shared.Next(DealEndpoints.MinRandom, DealEndpoints.MaxRandom + 1);
                // Mini-bridge deals where both pairs have 20 points are dealt again, so they can't be played.
                if (numbers.Contains(n) || (request.Mode == "mini" && MiniBridge.Declarer(Deal.FromNumber(n)) is null)) continue;
                numbers.Add(n);
            }

            var challenge = new Challenge
            {
                CreatedById = player.Id,
                Mode = request.Mode,
                Level = LevelName(request.Level),
                NumbersJson = JsonSerializer.Serialize(numbers),
                CreatedAt = DateTime.UtcNow,
            };
            for (var attempt = 0; ; attempt++)
            {
                challenge.Code = NewCode();
                if (!await db.Challenges.AnyAsync(c => c.Code == challenge.Code)) break;
                if (attempt > 20) return Results.Problem("Kunne ikke lave en kode.");
            }
            db.Challenges.Add(challenge);
            await db.SaveChangesAsync();
            return Results.Ok(await ToDto(db, challenge, player.Name));
        });

        app.MapGet("/api/challenges/{code}", async (string code, string? name, AppDbContext db) =>
        {
            var challenge = await Find(db, code);
            return challenge is null ? Results.NotFound(new { error = "Udfordringen findes ikke." }) : Results.Ok(await ToDto(db, challenge, name));
        });

        app.MapGet("/api/players/{name}/challenges", async (string name, AppDbContext db) =>
        {
            var player = await PlayerEndpoints.FindAsync(db, name);
            if (player is null) return Results.Ok(Array.Empty<ChallengeSummaryDto>());
            var playedIn = db.Games.Where(g => g.PlayerId == player.Id && g.ChallengeId != null).Select(g => g.ChallengeId);
            var challenges = await db.Challenges.Include(c => c.CreatedBy)
                .Where(c => c.CreatedById == player.Id || playedIn.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAt).Take(50).ToListAsync();
            var ids = challenges.Select(c => c.Id).ToList();
            var games = await db.Games.Where(g => g.ChallengeId != null && ids.Contains(g.ChallengeId.Value))
                .Select(g => new { g.ChallengeId, g.PlayerId }).ToListAsync();
            return Results.Ok(challenges.Select(c => new ChallengeSummaryDto(
                c.Code, c.Mode, ParseLevel(c.Level), c.CreatedBy.Name, c.CreatedAt, Numbers(c).Count,
                games.Count(g => g.ChallengeId == c.Id && g.PlayerId == player.Id),
                games.Where(g => g.ChallengeId == c.Id).Select(g => g.PlayerId).Append(c.CreatedById).Distinct().Count())));
        });
    }

    public static async Task<Challenge?> Find(AppDbContext db, string code)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        return await db.Challenges.Include(c => c.CreatedBy).FirstOrDefaultAsync(c => c.Code == normalized);
    }

    public static List<int> Numbers(Challenge c) => JsonSerializer.Deserialize<List<int>>(c.NumbersJson) ?? [];

    public static string LevelName(BotLevel level) => JsonNamingPolicy.CamelCase.ConvertName(level.ToString());

    private static BotLevel ParseLevel(string level) => Enum.TryParse<BotLevel>(level, ignoreCase: true, out var l) ? l : BotLevel.Easy;

    private static string NewCode() =>
        string.Concat(Enumerable.Range(0, 6).Select(_ => CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]));

    private static async Task<ChallengeDto> ToDto(AppDbContext db, Challenge challenge, string? viewerName)
    {
        var numbers = Numbers(challenge);
        var games = await db.Games.Include(g => g.Player).Where(g => g.ChallengeId == challenge.Id).ToListAsync();
        var viewer = viewerName is null ? null : await PlayerEndpoints.FindAsync(db, viewerName);
        var viewerBoards = games.Where(g => viewer is not null && g.PlayerId == viewer.Id).Select(g => g.Number).ToHashSet();

        var boards = numbers.Select((n, i) =>
        {
            var played = viewerBoards.Contains(n);
            var results = games.Where(g => g.Number == n).OrderByDescending(g => g.ScoreNorthSouth).Select(g => played
                ? new BoardResultDto(g.Player.Name, g.Contract, g.Declarer, g.DeclarerTricks, g.Made, g.ScoreNorthSouth)
                : new BoardResultDto(g.Player.Name, null, null, null, null, null)).ToList();
            return new BoardDto(i + 1, n, played, results);
        }).ToList();

        // Matchpoints over the deals the viewer has played: on each deal a player scores 1 for
        // every other player they beat and ½ for every tie.
        var players = games.Select(g => g.Player).Append(challenge.CreatedBy).DistinctBy(p => p.Id).ToList();
        var standings = players.Select(p =>
        {
            double points = 0, max = 0;
            var compared = 0;
            var total = 0;
            foreach (var n in viewerBoards)
            {
                var mine = games.FirstOrDefault(g => g.Number == n && g.PlayerId == p.Id);
                if (mine is null) continue;
                compared++;
                total += mine.ScoreNorthSouth;
                foreach (var other in games.Where(g => g.Number == n && g.PlayerId != p.Id))
                {
                    max += 1;
                    points += mine.ScoreNorthSouth > other.ScoreNorthSouth ? 1 : mine.ScoreNorthSouth == other.ScoreNorthSouth ? 0.5 : 0;
                }
            }
            return new StandingDto(p.Name, games.Count(g => g.PlayerId == p.Id), compared, total, max > 0 ? Math.Round(100 * points / max, 1) : null);
        }).OrderByDescending(s => s.Percent ?? -1).ThenByDescending(s => s.TotalScore).ToList();

        return new ChallengeDto(challenge.Code, challenge.Mode, ParseLevel(challenge.Level), challenge.CreatedBy.Name, challenge.CreatedAt, boards, standings);
    }
}
