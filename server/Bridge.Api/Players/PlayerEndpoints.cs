using System.Text.RegularExpressions;
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
