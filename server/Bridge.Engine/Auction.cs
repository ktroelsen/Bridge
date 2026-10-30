namespace Bridge.Engine;

public readonly record struct AuctionCall(Seat Seat, Call Call);

/// <summary>
/// The bidding: calls go clockwise from the dealer. A bid must be higher than the last bid,
/// you may double the opponents' last bid and redouble their double. Three passes after a bid
/// end the auction (four passes at the start: the deal is passed out).
/// </summary>
public sealed class Auction(Seat dealer)
{
    private readonly List<AuctionCall> _calls = [];

    public Seat Dealer { get; } = dealer;

    public IReadOnlyList<AuctionCall> Calls => _calls;

    public Seat NextSeat => (Seat)(((int)Dealer + _calls.Count) % 4);

    public AuctionCall? LastBid => _calls.LastOrDefault(c => c.Call.IsBid) is { Call.IsBid: true } b ? b : null;

    public bool IsComplete =>
        (_calls.Count == 4 && _calls.All(c => c.Call.Kind == CallKind.Pass)) ||
        (_calls.Count >= 4 && _calls.Any(c => c.Call.IsBid) && _calls.TakeLast(3).All(c => c.Call.Kind == CallKind.Pass));

    public bool IsPassedOut => IsComplete && LastBid is null;

    public bool IsLegal(Call call)
    {
        if (IsComplete) return false;
        var lastAction = _calls.LastOrDefault(c => c.Call.Kind != CallKind.Pass);
        var opponents = _calls.Count > 0 && lastAction != default && lastAction.Seat.IsNorthSouth() != NextSeat.IsNorthSouth();
        return call.Kind switch
        {
            CallKind.Pass => true,
            CallKind.Bid => LastBid is not { } last || call.Rank > last.Call.Rank,
            CallKind.Double => opponents && lastAction.Call.IsBid,
            CallKind.Redouble => opponents && lastAction.Call.Kind == CallKind.Double,
            _ => false,
        };
    }

    public void Add(Call call)
    {
        if (!IsLegal(call)) throw new InvalidPlayException(IsComplete ? "Meldingerne er slut." : $"{call} er ikke en lovlig melding.");
        _calls.Add(new AuctionCall(NextSeat, call));
    }

    public IEnumerable<Call> LegalCalls()
    {
        if (IsComplete) yield break;
        yield return Call.Pass;
        if (IsLegal(Call.Double)) yield return Call.Double;
        if (IsLegal(Call.Redouble)) yield return Call.Redouble;
        for (var level = 1; level <= 7; level++)
            foreach (var strain in Enum.GetValues<Strain>())
            {
                var bid = Call.Bid(level, strain);
                if (IsLegal(bid)) yield return bid;
            }
    }

    /// <summary>The final contract, or null when the deal was passed out.</summary>
    public Contract? Contract()
    {
        if (!IsComplete || LastBid is not { } last) return null;
        var side = last.Seat.IsNorthSouth();
        // The declarer is the first player of the winning side to name the strain.
        var declarer = _calls.First(c => c.Call.IsBid && c.Call.Strain == last.Call.Strain && c.Seat.IsNorthSouth() == side).Seat;
        var after = _calls.SkipWhile(c => c != last).Skip(1).Select(c => c.Call.Kind).ToList();
        var penalty = after.Contains(CallKind.Redouble) ? Penalty.Redoubled : after.Contains(CallKind.Double) ? Penalty.Doubled : Penalty.None;
        return new Contract(last.Call.Level, last.Call.Strain, declarer, penalty);
    }
}
