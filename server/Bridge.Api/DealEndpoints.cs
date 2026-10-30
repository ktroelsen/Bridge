using Bridge.Engine;

namespace Bridge.Api;

public record CardDto(Suit Suit, Rank Rank);

public record HandDto(Seat Seat, int Hcp, bool Balanced, IReadOnlyList<CardDto> Cards);

public record DealDto(int Number, int Board, Seat Dealer, Vulnerability Vulnerability, IReadOnlyList<HandDto> Hands);

public static class DealEndpoints
{
    public const int MinRandom = 100_000;
    public const int MaxRandom = 999_999;

    /// <summary>
    /// GET /api/deal?number=… shows all four hands of a deal (open cards, for learning).
    /// Without a number a random deal is made. Games against the bot will get their own
    /// endpoint that only reveals the cards the player may see.
    /// </summary>
    public static void MapDealEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/deal", (int? number) =>
        {
            var n = number ?? Random.Shared.Next(MinRandom, MaxRandom + 1);
            if (n is < Deal.MinNumber or > Deal.MaxNumber)
                return Results.BadRequest(new { error = "Ugyldigt spilnummer." });
            return Results.Ok(ToDto(Deal.FromNumber(n)));
        });
    }

    public static DealDto ToDto(Deal deal) => new(
        deal.Number,
        deal.Board,
        deal.Dealer,
        deal.Vulnerability,
        [.. Enum.GetValues<Seat>().Select(seat =>
        {
            var hand = deal[seat];
            return new HandDto(seat, hand.Hcp, hand.IsBalanced, [.. hand.Cards.Select(c => new CardDto(c.Suit, c.Rank))]);
        })]);
}
