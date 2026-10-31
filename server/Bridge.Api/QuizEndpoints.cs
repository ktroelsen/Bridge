using Bridge.Engine;
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

public static class QuizEndpoints
{
    /// <summary>GET /api/quiz/bidding?kind=opening|response|ntResponse: a fresh "what do you bid?" question.</summary>
    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
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
