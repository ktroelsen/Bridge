namespace Bridge.Engine.Bidding;

/// <summary>
/// One rule of the bidding system: in a situation (and when <see cref="Applies"/> holds for the
/// auction), bid <see cref="Call"/> with a hand that matches <see cref="Shows"/> (and <see cref="When"/>).
/// Because the call and what it shows depend only on the auction, the rule also explains the
/// call when someone else makes it.
/// </summary>
public sealed record BidRule(
    string Id,
    Situation Situation,
    Func<BidContext, Call?> Call,
    Func<BidContext, Shows> Shows,
    Func<BidContext, bool>? Applies = null,
    Func<BidContext, Hand, bool>? When = null,
    Func<BidContext, string>? Text = null)
{
    public bool AppliesTo(BidContext ctx) => ctx.Situation == Situation && (Applies?.Invoke(ctx) ?? true);

    /// <summary>The call this rule would make here, if it is legal.</summary>
    public Call? CallIn(BidContext ctx) => Call(ctx) is { } c && ctx.Legal(c) ? c : null;

    public bool Fits(BidContext ctx, Hand hand) => Shows(ctx).Matches(hand) && (When?.Invoke(ctx, hand) ?? true);

    /// <summary>Danish explanation: the rule's name or purpose, then what the call shows.</summary>
    public string Explain(BidContext ctx)
    {
        var text = Text?.Invoke(ctx);
        var shows = Shows(ctx).Describe();
        if (string.IsNullOrEmpty(text)) return Capitalize(shows);
        return string.IsNullOrEmpty(shows) ? text : $"{text} ({shows})";
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
