namespace Bridge.Engine.Analysis;

/// <summary>A contract one side can make with perfect play, and its score for that side.</summary>
public sealed record BestContract(Contract Contract, int Tricks, int Score);

/// <summary>
/// Tricks each player takes as declarer in each strain with perfect play (the double dummy
/// table), and the best contract for each side from it.
/// </summary>
public sealed record ContractTable(int[,] Tricks, BestContract? NorthSouth, BestContract? EastWest, bool Complete)
{
    /// <summary>Tricks for <paramref name="declarer"/> in <paramref name="strain"/> (-1 if not worked out in time).</summary>
    public int this[Seat declarer, Strain strain] => Tricks[(int)declarer, (int)strain];

    public static ContractTable For(Deal deal, TimeSpan budget)
    {
        var tricks = new int[4, 5];
        var deadline = DateTime.UtcNow + budget;
        var complete = true;
        foreach (var strain in Enum.GetValues<Strain>())
        {
            var row = ForStrain(deal, strain, deadline - DateTime.UtcNow);
            for (var s = 0; s < 4; s++) tricks[s, (int)strain] = row[s];
            complete &= row.All(t => t >= 0);
        }
        return FromTricks(deal, tricks, complete);
    }

    /// <summary>The table from tricks per declarer and strain (-1 where unknown).</summary>
    public static ContractTable FromTricks(Deal deal, int[,] tricks, bool complete) =>
        new(tricks, Best(deal, tricks, northSouth: true), Best(deal, tricks, northSouth: false), complete);

    /// <summary>Tricks for each declarer (North, East, South, West) in one strain; -1 where time ran out.</summary>
    public static int[] ForStrain(Deal deal, Strain strain, TimeSpan budget)
    {
        int[] result = [-1, -1, -1, -1];
        // One solver for the strain: the four declarers only change who leads, so the cache helps,
        // and each result is a good first guess for the next.
        DoubleDummy? solver = null;
        int? guess = null;
        try
        {
            foreach (var declarer in Enum.GetValues<Seat>())
            {
                var play = new CardPlay(deal, new Contract(1, strain, declarer));
                if (solver is null) solver = new DoubleDummy(play) { Deadline = DateTime.UtcNow + budget };
                else solver.SetPosition(play);
                var ns = solver.NorthSouthTricks(guess);
                guess = ns;
                result[(int)declarer] = declarer.IsNorthSouth() ? ns : 13 - ns;
            }
        }
        catch (TimeoutException)
        {
        }
        return result;
    }

    /// <summary>The highest scoring contract a side makes. Bid as low as gives the same score: game, or slam when there are 12+ tricks.</summary>
    private static BestContract? Best(Deal deal, int[,] tricks, bool northSouth)
    {
        var vulnerable = deal.Vulnerability == Vulnerability.Both
            || deal.Vulnerability == (northSouth ? Vulnerability.NorthSouth : Vulnerability.EastWest);
        BestContract? best = null;
        foreach (var declarer in Enum.GetValues<Seat>().Where(s => s.IsNorthSouth() == northSouth))
            foreach (var strain in Enum.GetValues<Strain>())
            {
                var t = tricks[(int)declarer, (int)strain];
                if (t < 7) continue; // also -1: unknown
                var level = t >= 12 ? t - 6 : Math.Min(t - 6, Contract.GameLevel(strain));
                var contract = new Contract(level, strain, declarer);
                var score = Scoring.Score(contract, vulnerable, t);
                // Ties go to the cheaper-to-reach strain order of preference: no trump, majors, minors.
                if (best is null || score > best.Score || (score == best.Score && Preference(strain) > Preference(best.Contract.Strain)))
                    best = new BestContract(contract, t, score);
            }
        return best;
    }

    private static int Preference(Strain s) => s switch { Strain.NoTrump => 3, Strain.Spades or Strain.Hearts => 2, _ => 1 };
}
