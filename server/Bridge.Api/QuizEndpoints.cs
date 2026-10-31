using Bridge.Engine;
using Bridge.Engine.Analysis;
using Bridge.Engine.Bidding;

namespace Bridge.Api;

public record QuizCallDto(Seat Seat, string Call, string Explanation);

public record BiddingQuestionDto(
    int Number,
    Seat Dealer,
    IReadOnlyList<QuizCallDto> Auction,
    IReadOnlyList<CardDto> Hand,
    int Hcp,
    IReadOnlyList<string> LegalCalls,
    HintDto Answer);

public record CardValueDto(CardDto Card, int Tricks);

public record PlayQuestionDto(
    int Number,
    ContractDto Contract,
    Seat ToPlay,
    int Trick,
    int TricksNorthSouth,
    int TricksEastWest,
    IReadOnlyList<VisibleHandDto> Hands,
    IReadOnlyList<CardDto> LegalCards,
    IReadOnlyList<CardDto> Best,
    int BestTricks,
    IReadOnlyList<CardValueDto> Values);

public static class QuizEndpoints
{
    /// <summary>GET /api/quiz/bidding?kind=opening|response|ntResponse: a fresh "what do you bid?" question.</summary>
    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/quiz/play?stage=early|middle|defend: a play problem with all hands open.
        app.MapGet("/api/quiz/play", (string? stage) =>
        {
            var (from, to) = stage switch { "middle" => (6, 9), "defend" => (2, 6), _ => (3, 4) };
            PlayQuestion q;
            try
            {
                q = PlayQuiz.Make(from, to, Random.Shared, TimeSpan.FromSeconds(12), defend: stage == "defend");
            }
            catch (TimeoutException)
            {
                return Results.Problem("Kunne ikke finde en opgave. Prøv igen.", statusCode: 503);
            }
            var play = q.Play;
            var c = play.Contract;
            static CardDto Dto(Card card) => new(card.Suit, card.Rank);
            return Results.Ok(new PlayQuestionDto(
                q.Number,
                new ContractDto(c.Level, c.Strain, c.Declarer, c.Penalty),
                play.NextToPlay,
                play.Tricks.Count,
                play.TricksWon(true),
                play.TricksWon(false),
                [.. Enum.GetValues<Seat>().Select(s => new VisibleHandDto(s, [.. play.Remaining(s).Select(Dto)]))],
                [.. play.LegalCards().Select(Dto)],
                [.. q.Best.Cards.Select(Dto)],
                q.Best.SideTricks,
                [.. q.Best.Values.Select(kv => new CardValueDto(Dto(kv.Key), kv.Value))]));
        });

        app.MapGet("/api/quiz/bidding", (string kind) =>
        {
            if (!Enum.TryParse<QuizKind>(kind, ignoreCase: true, out var quizKind) || !Enum.IsDefined(quizKind))
                return Results.BadRequest(new { error = "Ukendt slags spørgsmål." });
            var q = BiddingQuiz.Make(quizKind, Random.Shared);
            return Results.Ok(new BiddingQuestionDto(
                q.Number,
                q.Dealer,
                [.. q.Before.Select(c => new QuizCallDto(c.Seat, c.Call.ToString(), c.Explanation))],
                [.. q.Hand.Cards.Select(c => new CardDto(c.Suit, c.Rank))],
                q.Hand.Hcp,
                [.. q.Legal.Select(c => c.ToString())],
                new HintDto(q.Answer.Call.ToString(), q.Answer.Explanation)));
        });
    }
}
