using Bridge.Engine;

namespace Bridge.Api;

public record ContractChoiceDto(Strain Strain, bool Game);

public record MiniBridgeRequest(int? Number, ContractChoiceDto? Contract, List<CardDto>? Cards, Practice Practice = Practice.Any);

public record SeatHcpDto(Seat Seat, int Hcp);

public record VisibleHandDto(Seat Seat, IReadOnlyList<CardDto> Cards);

public record PlayedCardDto(Seat Seat, CardDto Card);

public record TrickDto(Seat Leader, IReadOnlyList<PlayedCardDto> Plays, Seat? Winner);

public record ContractDto(int Level, Strain Strain, Seat Declarer, Penalty Penalty = Penalty.None);

public record MiniBridgeDto(
    int Number,
    int Board,
    Seat Dealer,
    Vulnerability Vulnerability,
    GamePhase Phase,
    IReadOnlyList<SeatHcpDto> Hcp,
    Seat? Declarer,
    Seat? Dummy,
    ContractDto? Contract,
    IReadOnlyList<VisibleHandDto> Hands,
    IReadOnlyList<TrickDto> Tricks,
    Seat? ToPlay,
    IReadOnlyList<CardDto> LegalCards,
    int TricksNorthSouth,
    int TricksEastWest,
    int? ScoreNorthSouth,
    DealDto? FullDeal);

public static class MiniBridgeEndpoints
{
    /// <summary>
    /// POST /api/minibridge rebuilds the game from the deal number, the contract choice and the
    /// human's cards, lets the bot play up to the human's next card, and returns only what the
    /// human may see. No server state: the client sends the request again with one more card.
    /// </summary>
    public static void MapMiniBridgeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/minibridge", (MiniBridgeRequest request) =>
        {
            var number = request.Number ?? NewNumber(request.Practice);
            if (number is < Deal.MinNumber or > Deal.MaxNumber)
                return Results.BadRequest(new { error = "Ugyldigt spilnummer." });
            if (request.Cards is { Count: > 26 })
                return Results.BadRequest(new { error = "For mange kort." });

            try
            {
                var cards = (request.Cards ?? []).Select(c => new Card(c.Suit, c.Rank)).ToList();
                var game = MiniBridgeGame.Replay(number, request.Contract?.Strain, request.Contract?.Game ?? false, cards);
                return Results.Ok(ToDto(game));
            }
            catch (InvalidPlayException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    /// <summary>A random deal number for the practice kind (never a 20–20 redeal).</summary>
    private static int NewNumber(Practice practice)
    {
        while (true)
        {
            var n = Random.Shared.Next(DealEndpoints.MinRandom, DealEndpoints.MaxRandom + 1);
            if (MiniBridge.Matches(Deal.FromNumber(n), practice)) return n;
        }
    }

    private static CardDto ToDto(Card c) => new(c.Suit, c.Rank);

    public static MiniBridgeDto ToDto(MiniBridgeGame game)
    {
        var deal = game.Deal;
        var play = game.Play;
        var done = game.Phase == GamePhase.Done;
        var dummy = game.Declarer?.Partner();

        // South always; the dummy is face up from the start in mini-bridge; North too when the
        // human declares (it is then either the declarer or the dummy).
        var visible = Enum.GetValues<Seat>()
            .Where(s => !done && game.Phase != GamePhase.Redeal
                && (s == Seat.South || s == dummy || (game.HumanDeclares && s == Seat.North)))
            .Select(s => new VisibleHandDto(s, [.. (play?.Remaining(s) ?? deal[s].Cards).Select(ToDto)]))
            .ToList();

        var toPlay = play is { IsComplete: false } ? play.NextToPlay : (Seat?)null;

        return new MiniBridgeDto(
            deal.Number,
            deal.Board,
            deal.Dealer,
            deal.Vulnerability,
            game.Phase,
            [.. Enum.GetValues<Seat>().Select(s => new SeatHcpDto(s, deal[s].Hcp))],
            game.Declarer,
            dummy,
            game.Contract is { } c ? new ContractDto(c.Level, c.Strain, c.Declarer) : null,
            visible,
            play is null ? [] : [.. play.Tricks.Where(t => t.Plays.Count > 0).Select(t =>
                new TrickDto(t.Leader, [.. t.Plays.Select(p => new PlayedCardDto(p.Seat, ToDto(p.Card)))], play.Winner(t)))],
            toPlay,
            toPlay is { } seat && game.HumanControls(seat) ? [.. play!.LegalCards().Select(ToDto)] : [],
            play?.TricksWon(true) ?? 0,
            play?.TricksWon(false) ?? 0,
            game.ScoreNorthSouth,
            done || game.Phase == GamePhase.Redeal ? DealEndpoints.ToDto(deal) : null);
    }
}
