namespace Bridge.Engine.Bidding;

/// <summary>
/// What a call promises about the hand: a point range, suit lengths and maybe a balanced shape.
/// The same description is used to check a hand (the bot bidding), to explain a call in Danish
/// and to build a picture of a player's hand from their calls.
/// </summary>
public sealed record Shows
{
    public int HcpMin { get; init; }
    public int HcpMax { get; init; } = 37;
    public int[] MinLength { get; init; } = [0, 0, 0, 0];
    public int[] MaxLength { get; init; } = [13, 13, 13, 13];
    public bool? Balanced { get; init; }

    public static readonly Shows Anything = new();

    public static Shows Hcp(int min, int max = 37) => new() { HcpMin = min, HcpMax = max };

    public Shows Len(Suit suit, int min, int max = 13)
    {
        int[] mins = [.. MinLength], maxs = [.. MaxLength];
        mins[(int)suit] = min;
        maxs[(int)suit] = max;
        return this with { MinLength = mins, MaxLength = maxs };
    }

    public Shows Bal() => this with { Balanced = true };

    public bool Matches(Hand hand) =>
        hand.Hcp >= HcpMin && hand.Hcp <= HcpMax &&
        Enum.GetValues<Suit>().All(s => hand.Length(s) >= MinLength[(int)s] && hand.Length(s) <= MaxLength[(int)s]) &&
        (Balanced is null || hand.IsBalanced == Balanced);

    /// <summary>Everything two calls show together (the narrower bounds win).</summary>
    public Shows And(Shows other) => new()
    {
        HcpMin = Math.Max(HcpMin, other.HcpMin),
        HcpMax = Math.Max(Math.Max(HcpMin, other.HcpMin), Math.Min(HcpMax, other.HcpMax)),
        MinLength = [.. MinLength.Zip(other.MinLength, Math.Max)],
        MaxLength = [.. MaxLength.Zip(other.MaxLength, Math.Min)],
        Balanced = Balanced ?? other.Balanced,
    };

    private static readonly string[] Symbols = ["♣", "♦", "♥", "♠"];

    /// <summary>Danish, e.g. "12–21 HP, mindst 5 ♥".</summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (HcpMin > 0 && HcpMax < 37) parts.Add($"{HcpMin}–{HcpMax} HP");
        else if (HcpMin > 0) parts.Add($"{HcpMin}+ HP");
        else if (HcpMax < 37) parts.Add($"højst {HcpMax} HP");
        foreach (var s in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            int min = MinLength[(int)s], max = MaxLength[(int)s];
            if (min > 0 && max < 13) parts.Add(min == max ? $"{min} {Symbols[(int)s]}" : $"{min}–{max} {Symbols[(int)s]}");
            else if (min > 0) parts.Add($"mindst {min} {Symbols[(int)s]}");
            else if (max < 13) parts.Add(max == 0 ? $"ingen {Symbols[(int)s]}" : $"højst {max} {Symbols[(int)s]}");
        }
        if (Balanced == true) parts.Add("jævn fordeling");
        return string.Join(", ", parts);
    }

    public static string Symbol(Suit suit) => Symbols[(int)suit];
}
