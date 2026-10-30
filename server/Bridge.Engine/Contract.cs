namespace Bridge.Engine;

/// <summary>What is played: a trump suit or no trump. The suits share their values with <see cref="Suit"/>.</summary>
public enum Strain { Clubs, Diamonds, Hearts, Spades, NoTrump }

public enum Penalty { None, Doubled, Redoubled }

public sealed record Contract(int Level, Strain Strain, Seat Declarer, Penalty Penalty = Penalty.None)
{
    public Suit? Trump => Strain == Strain.NoTrump ? null : (Suit)Strain;

    public Seat Dummy => Declarer.Partner();

    /// <summary>Tricks the declarer needs: the level plus the six of the "book".</summary>
    public int TricksNeeded => Level + 6;

    /// <summary>The lowest level that scores game in a strain: 3UT, 4♥/4♠, 5♣/5♦.</summary>
    public static int GameLevel(Strain strain) => strain switch
    {
        Strain.NoTrump => 3,
        Strain.Hearts or Strain.Spades => 4,
        _ => 5,
    };
}
