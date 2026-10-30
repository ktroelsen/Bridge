namespace Bridge.Engine;

/// <summary>Thirteen cards, sorted by suit (spades first) and rank (high first).</summary>
public sealed class Hand
{
    public Hand(IEnumerable<Card> cards)
    {
        Cards = [.. cards.OrderByDescending(c => c.Suit).ThenByDescending(c => c.Rank)];
        if (Cards.Count != 13 || Cards.Distinct().Count() != 13)
            throw new ArgumentException("En hånd skal have 13 forskellige kort.", nameof(cards));
    }

    public IReadOnlyList<Card> Cards { get; }

    public int Hcp => Cards.Sum(c => c.Hcp);

    public int Length(Suit suit) => Cards.Count(c => c.Suit == suit);

    public IEnumerable<Card> InSuit(Suit suit) => Cards.Where(c => c.Suit == suit);

    /// <summary>No void, no singleton and at most one doubleton (4-3-3-3, 4-4-3-2, 5-3-3-2).</summary>
    public bool IsBalanced
    {
        get
        {
            var lengths = Enum.GetValues<Suit>().Select(Length).ToList();
            return lengths.All(l => l >= 2) && lengths.Count(l => l == 2) <= 1;
        }
    }
}
