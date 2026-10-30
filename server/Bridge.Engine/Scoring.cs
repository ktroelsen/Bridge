namespace Bridge.Engine;

/// <summary>Duplicate scoring, as used in clubs and tournaments.</summary>
public static class Scoring
{
    /// <summary>The score for the declarer's side (negative when the contract goes down).</summary>
    public static int Score(Contract contract, bool vulnerable, int tricks)
    {
        var multiplier = contract.Penalty switch { Penalty.Doubled => 2, Penalty.Redoubled => 4, _ => 1 };
        var over = tricks - contract.TricksNeeded;
        if (over < 0) return -Undertricks(-over, contract.Penalty, vulnerable);

        var trickScore = TrickValue(contract.Strain, contract.Level) * multiplier;
        var score = trickScore;
        score += trickScore >= 100 ? (vulnerable ? 500 : 300) : 50;
        if (contract.Level == 6) score += vulnerable ? 750 : 500;
        if (contract.Level == 7) score += vulnerable ? 1500 : 1000;
        score += contract.Penalty switch { Penalty.Doubled => 50, Penalty.Redoubled => 100, _ => 0 };
        score += contract.Penalty switch
        {
            Penalty.None => over * (contract.Strain is Strain.Clubs or Strain.Diamonds ? 20 : 30),
            Penalty.Doubled => over * (vulnerable ? 200 : 100),
            _ => over * (vulnerable ? 400 : 200),
        };
        return score;
    }

    /// <summary>Points for the contracted tricks: 20 for minors, 30 for majors, 40 + 30 for no trump.</summary>
    public static int TrickValue(Strain strain, int level) => strain switch
    {
        Strain.Clubs or Strain.Diamonds => 20 * level,
        Strain.Hearts or Strain.Spades => 30 * level,
        _ => 40 + 30 * (level - 1),
    };

    private static int Undertricks(int down, Penalty penalty, bool vulnerable)
    {
        if (penalty == Penalty.None) return down * (vulnerable ? 100 : 50);
        var doubled = 0;
        for (var i = 1; i <= down; i++)
            doubled += vulnerable ? (i == 1 ? 200 : 300) : (i == 1 ? 100 : i <= 3 ? 200 : 300);
        return penalty == Penalty.Redoubled ? doubled * 2 : doubled;
    }
}
