namespace Bridge.Engine;

/// <summary>
/// Mini-bridge: bridge without bidding, the usual first step for beginners. Everyone shows their
/// high card points; the pair with most points declares, and the partner with most points in that
/// pair is the declarer. The dummy goes down at once, and the declarer then chooses trumps (or no
/// trump) and whether to play a part score or a game. At 20–20 the cards are dealt again.
/// </summary>
public static class MiniBridge
{
    /// <summary>The declarer, or null when both pairs have 20 points (redeal).</summary>
    public static Seat? Declarer(Deal deal)
    {
        var ns = deal[Seat.North].Hcp + deal[Seat.South].Hcp;
        if (ns == 20) return null;
        Seat[] pair = ns > 20 ? [Seat.North, Seat.South] : [Seat.East, Seat.West];
        // Most points declares; on a tie, the first to speak (counting from the dealer).
        return pair
            .OrderByDescending(s => deal[s].Hcp)
            .ThenBy(s => ((int)s - (int)deal.Dealer + 4) % 4)
            .First();
    }

    /// <summary>The contract for a declarer's choice: level 1, or the lowest game level.</summary>
    public static Contract ContractFor(Seat declarer, Strain strain, bool game) =>
        new(game ? Contract.GameLevel(strain) : 1, strain, declarer);

    /// <summary>
    /// The bot declarer's choice, from its own hand and the dummy (both are visible to it):
    /// a major with 8+ cards together, else no trump with 25+ points, else a minor with 8+ cards,
    /// else no trump. Game with 25+ points (29+ in a minor).
    /// </summary>
    public static Contract BotContract(Deal deal, Seat declarer)
    {
        var a = deal[declarer];
        var b = deal[declarer.Partner()];
        var hcp = a.Hcp + b.Hcp;
        int Fit(Suit s) => a.Length(s) + b.Length(s);

        var major = new[] { Suit.Spades, Suit.Hearts }.OrderByDescending(Fit).First();
        if (Fit(major) >= 8) return ContractFor(declarer, (Strain)major, hcp >= 25);
        if (hcp >= 25) return ContractFor(declarer, Strain.NoTrump, true);
        var minor = new[] { Suit.Diamonds, Suit.Clubs }.OrderByDescending(Fit).First();
        if (Fit(minor) >= 8) return ContractFor(declarer, (Strain)minor, hcp >= 29);
        return ContractFor(declarer, Strain.NoTrump, false);
    }
}
