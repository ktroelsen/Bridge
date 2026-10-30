namespace Bridge.Engine;

/// <summary>Suits in bridge rank order (clubs lowest).</summary>
public enum Suit { Clubs, Diamonds, Hearts, Spades }

/// <summary>Ranks with their face value, so Two = 2 and Ace = 14.</summary>
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

public readonly record struct Card(Suit Suit, Rank Rank)
{
    /// <summary>High card points: ace 4, king 3, queen 2, jack 1.</summary>
    public int Hcp => Rank switch
    {
        Rank.Ace => 4,
        Rank.King => 3,
        Rank.Queen => 2,
        Rank.Jack => 1,
        _ => 0,
    };

    /// <summary>All 52 cards in a fixed order (suit by suit, low to high).</summary>
    public static IReadOnlyList<Card> Deck { get; } =
        [.. from suit in Enum.GetValues<Suit>() from rank in Enum.GetValues<Rank>() select new Card(suit, rank)];

    /// <summary>Short notation such as "SA", "HT" or "C2" (PBN style).</summary>
    public override string ToString() => $"{SuitLetter(Suit)}{RankLetter(Rank)}";

    public static char SuitLetter(Suit suit) => "CDHS"[(int)suit];

    public static char RankLetter(Rank rank) => "23456789TJQKA"[(int)rank - 2];
}
