namespace Bridge.Engine;

public enum CallKind { Pass, Bid, Double, Redouble }

/// <summary>A call in the auction: pass, a bid (level 1-7 and a strain), double or redouble.</summary>
public readonly record struct Call(CallKind Kind, int Level = 0, Strain Strain = Strain.Clubs)
{
    public static readonly Call Pass = new(CallKind.Pass);
    public static readonly Call Double = new(CallKind.Double);
    public static readonly Call Redouble = new(CallKind.Redouble);

    public static Call Bid(int level, Strain strain)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 7);
        return new Call(CallKind.Bid, level, strain);
    }

    public static Call Bid(int level, Suit suit) => Bid(level, (Strain)suit);

    public bool IsBid => Kind == CallKind.Bid;

    /// <summary>Bids in order: 1♣ = 0, 1♦ = 1 … 7UT = 34.</summary>
    public int Rank => (Level - 1) * 5 + (int)Strain;

    public Suit? Suit => IsBid && Strain != Strain.NoTrump ? (Suit)Strain : null;

    /// <summary>Short notation such as "1H", "3N", "P", "X", "XX".</summary>
    public override string ToString() => Kind switch
    {
        CallKind.Pass => "P",
        CallKind.Double => "X",
        CallKind.Redouble => "XX",
        _ => $"{Level}{"CDHSN"[(int)Strain]}",
    };

    public static Call Parse(string text) => text.ToUpperInvariant() switch
    {
        "P" => Pass,
        "X" => Double,
        "XX" => Redouble,
        var t when t.Length == 2 && t[0] is >= '1' and <= '7' && "CDHSN".Contains(t[1]) =>
            Bid(t[0] - '0', (Strain)"CDHSN".IndexOf(t[1])),
        _ => throw new FormatException($"Ukendt melding: {text}"),
    };
}
