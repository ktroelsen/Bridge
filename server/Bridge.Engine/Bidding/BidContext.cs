namespace Bridge.Engine.Bidding;

public enum Situation
{
    /// <summary>Nobody has bid yet.</summary>
    Opening,
    /// <summary>Partner opened; my first call.</summary>
    Response,
    /// <summary>I opened, partner responded; my second call.</summary>
    OpenerRebid,
    /// <summary>Partner opened, I responded, partner bid again; my second call.</summary>
    ResponderRebid,
    /// <summary>The opponents opened and our side hasn't bid yet.</summary>
    Overcall,
    /// <summary>Partner overcalled or doubled the opening; my first call.</summary>
    Advance,
    /// <summary>Partner asked for aces with 4UT (Blackwood): my answer.</summary>
    BlackwoodAnswer,
    /// <summary>I asked for aces with 4UT and partner answered: how high to go.</summary>
    AfterBlackwood,
    Other,
}

/// <summary>The auction seen from one player who is about to call.</summary>
public sealed class BidContext
{
    private readonly Auction _auction;

    public BidContext(Seat dealer, IEnumerable<AuctionCall> before, Seat me)
    {
        _auction = new Auction(dealer);
        foreach (var c in before) _auction.Add(c.Call);
        if (_auction.NextSeat != me) throw new ArgumentException("Det er ikke denne spillers tur.", nameof(me));
        Me = me;
        Calls = _auction.Calls;
        FirstBid = Calls.FirstOrDefault(c => c.Call.IsBid) is { Call.IsBid: true } f ? f : null;
        Mine = [.. Calls.Where(c => c.Seat == me && c.Call.Kind != CallKind.Pass).Select(c => c.Call)];
        Partners = [.. Calls.Where(c => c.Seat == Partner && c.Call.Kind != CallKind.Pass).Select(c => c.Call)];
        OpponentsActed = Calls.Any(c => c.Seat.IsNorthSouth() != me.IsNorthSouth() && c.Call.Kind != CallKind.Pass);
        Situation = FindSituation();
    }

    public Seat Me { get; }

    public Seat Partner => Me.Partner();

    public IReadOnlyList<AuctionCall> Calls { get; }

    public AuctionCall? FirstBid { get; }

    /// <summary>The opening bid, if any.</summary>
    public Call? Opening => FirstBid?.Call;

    /// <summary>My earlier calls (passes left out).</summary>
    public IReadOnlyList<Call> Mine { get; }

    /// <summary>Partner's calls (passes left out).</summary>
    public IReadOnlyList<Call> Partners { get; }

    public bool OpponentsActed { get; }

    public Situation Situation { get; }

    public AuctionCall? LastBid => _auction.LastBid;

    /// <summary>Whether the last bid was made by our side.</summary>
    public bool OurContract => LastBid is { } b && b.Seat.IsNorthSouth() == Me.IsNorthSouth();

    /// <summary>Whether the player on my right made a call other than pass just before me.</summary>
    public bool RightHandActed => Calls.Count > 0 && Calls[^1].Call.Kind != CallKind.Pass;

    public bool Legal(Call call) => _auction.IsLegal(call);

    /// <summary>The cheapest legal bid in a strain, if it is at most <paramref name="maxLevel"/>.</summary>
    public Call? Cheapest(Strain strain, int maxLevel = 7)
    {
        for (var level = 1; level <= maxLevel; level++)
            if (Legal(Call.Bid(level, strain))) return Call.Bid(level, strain);
        return null;
    }

    /// <summary>The cheapest bid one level higher than necessary (a jump).</summary>
    public Call? Jump(Strain strain) => Cheapest(strain) is { } c && c.Level < 7 ? Call.Bid(c.Level + 1, strain) : null;

    public Suit? OpeningSuit => Opening?.Suit;

    /// <summary>The suit of partner's first bid (opening or overcall), if a suit.</summary>
    public Suit? PartnerSuit => Partners.FirstOrDefault(c => c.IsBid).Suit;

    /// <summary>Suits the opponents have bid.</summary>
    public IEnumerable<Suit> OpponentSuits =>
        Calls.Where(c => c.Seat.IsNorthSouth() != Me.IsNorthSouth() && c.Call.Suit is not null).Select(c => c.Call.Suit!.Value).Distinct();

    /// <summary>
    /// The trump suit agreed before a Blackwood 4UT: the last suit our side bid before it.
    /// Null when 4UT wasn't Blackwood (no suit bid first, e.g. after 1UT it would ask about points).
    /// </summary>
    public Suit? BlackwoodSuit { get; private set; }

    private Situation FindSituation()
    {
        // Blackwood: 4UT by our side right after our side bid a suit.
        var ours = Calls.Where(c => c.Seat.IsNorthSouth() == Me.IsNorthSouth() && c.Call.IsBid).ToList();
        var askAt = ours.FindLastIndex(c => c.Call == Call.Parse("4N"));
        if (askAt > 0 && ours[askAt - 1].Call.Suit is { } agreed)
        {
            BlackwoodSuit = agreed;
            var asker = ours[askAt].Seat;
            // Partner's 4UT is our side's latest bid, so I haven't answered yet.
            if (asker == Partner && askAt == ours.Count - 1) return Situation.BlackwoodAnswer;
            if (asker == Me && askAt == ours.Count - 2 && ours[^1].Seat == Partner)
                return Situation.AfterBlackwood;
        }

        if (FirstBid is not { } first) return Situation.Opening;
        if (first.Seat == Partner)
        {
            if (Mine.Count == 0) return Situation.Response;
            if (Mine.Count == 1 && Partners.Count == 2) return Situation.ResponderRebid;
            return Situation.Other;
        }
        if (first.Seat == Me)
            return Mine.Count == 1 && Partners.Count == 1 ? Situation.OpenerRebid : Situation.Other;
        // The opponents opened.
        if (Mine.Count == 0 && Partners.Count == 0) return Situation.Overcall;
        if (Mine.Count == 0 && Partners.Count == 1) return Situation.Advance;
        return Situation.Other;
    }
}
