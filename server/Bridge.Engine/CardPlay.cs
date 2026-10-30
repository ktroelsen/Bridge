namespace Bridge.Engine;

/// <summary>
/// The card play of one deal: 13 tricks, follow suit when you can, and the winner of a trick
/// leads to the next. The player on the declarer's left makes the opening lead.
/// </summary>
public sealed class CardPlay
{
    private readonly List<Card>[] _remaining;
    private readonly List<Trick> _tricks = [];
    private readonly HashSet<Card> _played = [];

    public CardPlay(Deal deal, Contract contract)
    {
        Deal = deal;
        Contract = contract;
        _remaining = [.. Enum.GetValues<Seat>().Select(s => deal[s].Cards.ToList())];
        _tricks.Add(new Trick(contract.Declarer.Next()));
    }

    public Deal Deal { get; }

    public Contract Contract { get; }

    public Suit? Trump => Contract.Trump;

    /// <summary>All tricks so far; the last one may be incomplete.</summary>
    public IReadOnlyList<Trick> Tricks => _tricks;

    public Trick CurrentTrick => _tricks[^1];

    public IReadOnlySet<Card> Played => _played;

    public bool IsComplete => _tricks.Count == 13 && CurrentTrick.IsComplete;

    /// <summary>The dummy is put down when the opening lead has been made.</summary>
    public bool DummyVisible => _played.Count > 0;

    public Seat NextToPlay => CurrentTrick.NextSeat;

    public IReadOnlyList<Card> Remaining(Seat seat) => _remaining[(int)seat];

    public IReadOnlyList<Card> LegalCards()
    {
        if (IsComplete) return [];
        var hand = Remaining(NextToPlay);
        var led = CurrentTrick.LedSuit;
        if (led is { } suit && hand.Any(c => c.Suit == suit))
            return [.. hand.Where(c => c.Suit == suit)];
        return hand;
    }

    public void Play(Card card)
    {
        if (IsComplete) throw new InvalidPlayException("Spillet er slut.");
        var seat = NextToPlay;
        if (!Remaining(seat).Contains(card)) throw new InvalidPlayException($"{card} er ikke på hånden.");
        if (!LegalCards().Contains(card)) throw new InvalidPlayException("Du skal bekende kulør.");

        _remaining[(int)seat].Remove(card);
        _played.Add(card);
        CurrentTrick.Add(card);
        if (CurrentTrick.IsComplete && _tricks.Count < 13)
            _tricks.Add(new Trick(CurrentTrick.Winning(Trump)!.Value.Seat));
    }

    public Seat? Winner(Trick trick) => trick.IsComplete ? trick.Winning(Trump)!.Value.Seat : null;

    public int TricksWon(bool northSouth) =>
        _tricks.Count(t => t.IsComplete && Winner(t)!.Value.IsNorthSouth() == northSouth);

    public int DeclarerTricks => TricksWon(Contract.Declarer.IsNorthSouth());

    /// <summary>
    /// What the player at <paramref name="seat"/> can see: their own cards, the dummy once it is
    /// down, and (for the declarer playing from dummy, or the dummy's partner) the declarer's
    /// hand. Bots decide from this view only, so they never see hidden cards.
    /// </summary>
    public PlayView ViewFor(Seat seat)
    {
        var visible = new Dictionary<Seat, IReadOnlyList<Card>> { [seat] = Remaining(seat) };
        var declarerSide = seat == Contract.Declarer || seat == Contract.Dummy;
        if (DummyVisible || declarerSide)
            visible[Contract.Dummy] = Remaining(Contract.Dummy);
        if (declarerSide)
            visible[Contract.Declarer] = Remaining(Contract.Declarer);
        return new PlayView(seat, Contract, seat == NextToPlay ? LegalCards() : [], visible, _tricks, _played);
    }
}

/// <summary>Everything one player may know at their turn.</summary>
public sealed record PlayView(
    Seat Seat,
    Contract Contract,
    IReadOnlyList<Card> Legal,
    IReadOnlyDictionary<Seat, IReadOnlyList<Card>> Visible,
    IReadOnlyList<Trick> Tricks,
    IReadOnlySet<Card> Played)
{
    public Suit? Trump => Contract.Trump;

    public Trick CurrentTrick => Tricks[^1];

    public IReadOnlyList<Card> Hand => Visible[Seat];

    public bool IsDeclarerSide => Seat == Contract.Declarer || Seat == Contract.Dummy;

    /// <summary>Cards held by this player's side that this player can see.</summary>
    public IEnumerable<Card> OwnSideCards =>
        Visible.Where(kv => kv.Key == Seat || kv.Key == Seat.Partner()).SelectMany(kv => kv.Value);
}
