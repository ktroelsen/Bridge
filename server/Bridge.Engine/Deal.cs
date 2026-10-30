namespace Bridge.Engine;

/// <summary>
/// A deal fully determined by its number: the cards come from a seeded shuffle, and dealer and
/// vulnerability follow the standard duplicate board cycle of 16.
/// </summary>
public sealed class Deal
{
    public const int MinNumber = 1;
    public const int MaxNumber = 99_999_999;

    // Vulnerability of boards 1-16 (the standard duplicate table).
    private static readonly Vulnerability[] Cycle =
    [
        Vulnerability.None, Vulnerability.NorthSouth, Vulnerability.EastWest, Vulnerability.Both,
        Vulnerability.NorthSouth, Vulnerability.EastWest, Vulnerability.Both, Vulnerability.None,
        Vulnerability.EastWest, Vulnerability.Both, Vulnerability.None, Vulnerability.NorthSouth,
        Vulnerability.Both, Vulnerability.None, Vulnerability.NorthSouth, Vulnerability.EastWest,
    ];

    private readonly Hand[] _hands;

    private Deal(int number, Hand[] hands)
    {
        Number = number;
        _hands = hands;
    }

    public int Number { get; }

    /// <summary>Position in the 16-board cycle, 1-16.</summary>
    public int Board => (Number - 1) % 16 + 1;

    public Seat Dealer => (Seat)((Board - 1) % 4);

    public Vulnerability Vulnerability => Cycle[Board - 1];

    public Hand this[Seat seat] => _hands[(int)seat];

    public static Deal FromNumber(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, MinNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, MaxNumber);

        // Fisher-Yates on the fixed deck order, then 13 cards to each seat from North.
        var deck = Card.Deck.ToArray();
        var rng = new Rng((ulong)number);
        for (var i = deck.Length - 1; i > 0; i--)
        {
            var j = rng.NextBelow(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        var hands = Enumerable.Range(0, 4).Select(s => new Hand(deck.Skip(s * 13).Take(13))).ToArray();
        return new Deal(number, hands);
    }
}
