using Bridge.Engine;

namespace Bridge.Api;

public record GameRequest(int? Number, List<string>? Calls, List<CardDto>? Cards);

public record CallDto(Seat Seat, string Call, string Explanation, string? SystemCall, string? SystemExplanation);

public record HintDto(string Call, string Explanation);

public record GameDto(
    int Number,
    int Board,
    Seat Dealer,
    Vulnerability Vulnerability,
    GamePhase Phase,
    int SouthHcp,
    IReadOnlyList<CallDto> Auction,
    IReadOnlyList<string> LegalCalls,
    HintDto? Hint,
    ContractDto? Contract,
    Seat? Dummy,
    IReadOnlyList<VisibleHandDto> Hands,
    IReadOnlyList<TrickDto> Tricks,
    Seat? ToPlay,
    IReadOnlyList<CardDto> LegalCards,
    int TricksNorthSouth,
    int TricksEastWest,
    int? ScoreNorthSouth,
    DealDto? FullDeal);

public static class GameEndpoints
{
    /// <summary>
    /// POST /api/game rebuilds a full game (bidding and play) from the deal number and the
    /// human's calls and cards, lets the bots act up to the human's next turn, and returns only
    /// what the human may see. No server state.
    /// </summary>
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/game", (GameRequest request) =>
        {
            var number = request.Number ?? Random.Shared.Next(DealEndpoints.MinRandom, DealEndpoints.MaxRandom + 1);
            if (number is < Deal.MinNumber or > Deal.MaxNumber)
                return Results.BadRequest(new { error = "Ugyldigt spilnummer." });
            if (request.Calls is { Count: > 40 } || request.Cards is { Count: > 26 })
                return Results.BadRequest(new { error = "For mange meldinger eller kort." });

            try
            {
                var calls = (request.Calls ?? []).Select(Call.Parse).ToList();
                var cards = (request.Cards ?? []).Select(c => new Card(c.Suit, c.Rank)).ToList();
                return Results.Ok(ToDto(BridgeGame.Replay(number, calls, cards)));
            }
            catch (FormatException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidPlayException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static CardDto ToDto(Card c) => new(c.Suit, c.Rank);

    public static GameDto ToDto(BridgeGame game)
    {
        var deal = game.Deal;
        var play = game.Play;
        var contract = game.Contract;
        var over = game.Phase is GamePhase.Done or GamePhase.PassedOut;

        // South always; the dummy once the opening lead is made; North from the start of the play when North is the declarer (the human plays it).
        var visible = Enum.GetValues<Seat>().Where(s =>
            !over && (s == Seat.South
            || (play is not null && s == contract!.Dummy && play.DummyVisible)
            || (play is not null && s == Seat.North && contract!.Declarer == Seat.North)))
            .Select(s => new VisibleHandDto(s, [.. (play?.Remaining(s) ?? deal[s].Cards).Select(ToDto)]))
            .ToList();

        var toPlay = play is { IsComplete: false } ? play.NextToPlay : (Seat?)null;
        var hint = game.Hint;
        var southToBid = game.Phase == GamePhase.Bidding && game.Auction.NextSeat == Seat.South;

        return new GameDto(
            deal.Number,
            deal.Board,
            deal.Dealer,
            deal.Vulnerability,
            game.Phase,
            deal[Seat.South].Hcp,
            [.. game.Calls.Select(c => new CallDto(c.Seat, c.Call.ToString(), c.Explanation,
                c.System is { } s && s.Call != c.Call ? s.Call.ToString() : null,
                c.System is { } s2 && s2.Call != c.Call ? s2.Explanation : null))],
            southToBid ? [.. game.Auction.LegalCalls().Select(c => c.ToString())] : [],
            hint is null ? null : new HintDto(hint.Call.ToString(), hint.Explanation),
            contract is null ? null : new ContractDto(contract.Level, contract.Strain, contract.Declarer, contract.Penalty),
            contract?.Dummy,
            visible,
            play is null ? [] : [.. play.Tricks.Where(t => t.Plays.Count > 0).Select(t =>
                new TrickDto(t.Leader, [.. t.Plays.Select(p => new PlayedCardDto(p.Seat, ToDto(p.Card)))], play.Winner(t)))],
            toPlay,
            toPlay is { } seat && game.HumanControls(seat) ? [.. play!.LegalCards().Select(ToDto)] : [],
            play?.TricksWon(true) ?? 0,
            play?.TricksWon(false) ?? 0,
            game.ScoreNorthSouth,
            over ? DealEndpoints.ToDto(deal) : null);
    }
}
