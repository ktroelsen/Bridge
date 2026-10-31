using Bridge.Engine;
using Bridge.Engine.Analysis;

namespace Bridge.Api;

public record PlayHintDto(IReadOnlyList<CardDto> Cards, int SideTricks);

/// <summary>Who made a mistake, from the player's point of view.</summary>
public enum MistakeBy { You, Partner, Opponents }

public record MistakeDto(int Trick, Seat Seat, MistakeBy By, CardDto Card, int Cost, IReadOnlyList<CardDto> Better);

public record ReviewDto(int DeclarerTricksPerfect, int DeclarerTricks, bool Complete, IReadOnlyList<MistakeDto> Mistakes);

/// <summary>
/// Double dummy help: the best card in the play (hint) and the review of a finished deal. Both
/// rebuild the game from the same request as the game endpoints.
/// </summary>
public static class AnalysisEndpoints
{
    private static readonly TimeSpan HintBudget = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan ReviewBudget = TimeSpan.FromSeconds(20);

    public static void MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/game/hint", (GameRequest request) =>
            Run(() => BridgeGame.Replay(Number(request.Number), Calls(request.Calls), Cards(request.Cards), request.Level), g => g.Play, g => g.HumanControls, Hint));
        app.MapPost("/api/game/review", (GameRequest request) =>
            Run(() => BridgeGame.Replay(Number(request.Number), Calls(request.Calls), Cards(request.Cards), request.Level), g => g.Play, g => g.HumanControls, Review));
        app.MapPost("/api/minibridge/hint", (MiniBridgeRequest request) =>
            Run(() => MiniBridgeGame.Replay(Number(request.Number), request.Contract?.Strain, request.Contract?.Game ?? false, Cards(request.Cards), request.Level),
                g => g.Play, g => g.HumanControls, Hint));
        app.MapPost("/api/minibridge/review", (MiniBridgeRequest request) =>
            Run(() => MiniBridgeGame.Replay(Number(request.Number), request.Contract?.Strain, request.Contract?.Game ?? false, Cards(request.Cards), request.Level),
                g => g.Play, g => g.HumanControls, Review));
    }

    private static int Number(int? number) => number ?? throw new InvalidPlayException("Spilnummeret mangler.");

    private static List<Call> Calls(List<string>? calls) => (calls ?? []).Select(Call.Parse).ToList();

    private static List<Card> Cards(List<CardDto>? cards) => (cards ?? []).Select(c => new Card(c.Suit, c.Rank)).ToList();

    private static CardDto ToDto(Card c) => new(c.Suit, c.Rank);

    private static IResult Run<TGame>(Func<TGame> replay, Func<TGame, CardPlay?> play, Func<TGame, Func<Seat, bool>> human,
        Func<CardPlay, Func<Seat, bool>, IResult> analyse)
    {
        try
        {
            var game = replay();
            return play(game) is { } p ? analyse(p, human(game)) : Results.BadRequest(new { error = "Kortspillet er ikke begyndt." });
        }
        catch (Exception ex) when (ex is InvalidPlayException or FormatException)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult Hint(CardPlay play, Func<Seat, bool> human)
    {
        if (play.IsComplete || !human(play.NextToPlay)) return Results.BadRequest(new { error = "Det er ikke din tur." });
        try
        {
            var best = PlayAnalysis.Best(play, HintBudget);
            return Results.Ok(new PlayHintDto([.. best.Cards.Select(ToDto)], best.SideTricks));
        }
        catch (TimeoutException)
        {
            return Results.Problem("Analysen tog for lang tid.", statusCode: 503);
        }
    }

    private static IResult Review(CardPlay play, Func<Seat, bool> human)
    {
        if (!play.IsComplete) return Results.BadRequest(new { error = "Spillet er ikke slut." });
        var review = PlayAnalysis.Review(play, ReviewBudget);
        MistakeBy By(Seat s) => human(s) ? MistakeBy.You : s.IsNorthSouth() ? MistakeBy.Partner : MistakeBy.Opponents;
        return Results.Ok(new ReviewDto(
            review.DeclarerTricksPerfect,
            review.DeclarerTricks,
            review.Complete,
            [.. review.Mistakes.Select(m => new MistakeDto(m.Trick, m.Seat, By(m.Seat), ToDto(m.Card), m.Cost, [.. m.Better.Select(ToDto)]))]));
    }
}
