namespace Bridge.Engine.Bidding;

/// <summary>The call the system makes, with the Danish explanation of what it shows.</summary>
public sealed record BidChoice(Call Call, string Explanation, string? RuleId);

/// <summary>
/// Bids with <see cref="BeginnerSystem"/>: the first rule that fits the hand. When no rule
/// covers the situation, the bot places the contract from what partner has shown: game with
/// 25+ points together, an invitation with 23-24, otherwise a part score in a fit.
/// </summary>
public static class Bidder
{
    public static BidChoice Choose(Seat dealer, IReadOnlyList<AuctionCall> before, Seat seat, Hand hand)
    {
        var ctx = new BidContext(dealer, before, seat);
        foreach (var rule in BeginnerSystem.Rules)
        {
            if (!rule.AppliesTo(ctx) || rule.CallIn(ctx) is not { } call || !rule.Fits(ctx, hand)) continue;
            return new BidChoice(call, rule.Explain(ctx), rule.Id);
        }
        return Place(ctx, hand, Model(dealer, before, seat.Partner()));
    }

    /// <summary>What a call means in the system, as Danish text for the player.</summary>
    public static string Explain(Seat dealer, IReadOnlyList<AuctionCall> before, Seat seat, Call call)
    {
        var ctx = new BidContext(dealer, before, seat);
        if (RuleFor(ctx, call) is { } rule) return rule.Explain(ctx);
        if (call == Call.Parse("4N") && IsBlackwood(ctx)) return "Blackwood: spørger, hvor mange esser makker har";
        return call.Kind switch
        {
            CallKind.Pass => "Pas",
            CallKind.Double => "Dobling",
            CallKind.Redouble => "Redobling",
            _ when IsGame(call) => "Udgang",
            _ when ctx.OurContract || ctx.Partners.Count > 0 => "Naturlig melding: vil spille her",
            _ => "Naturlig melding",
        };
    }

    /// <summary>What a player has shown with all their calls so far.</summary>
    public static Shows Model(Seat dealer, IReadOnlyList<AuctionCall> calls, Seat seat)
    {
        var shows = Shows.Anything;
        for (var i = 0; i < calls.Count; i++)
        {
            if (calls[i].Seat != seat) continue;
            var ctx = new BidContext(dealer, calls.Take(i), seat);
            if (RuleFor(ctx, calls[i].Call) is { } rule) shows = shows.And(rule.Shows(ctx));
        }
        return shows;
    }

    /// <summary>Whether 4UT now would be Blackwood: our side's latest bid is a suit.</summary>
    private static bool IsBlackwood(BidContext ctx) =>
        ctx.LastBid is { } last && last.Seat.IsNorthSouth() == ctx.Me.IsNorthSouth() && last.Call.Suit is not null
        && ctx.Calls.All(c => c.Call != Call.Parse("4N"));

    private static BidRule? RuleFor(BidContext ctx, Call call) =>
        BeginnerSystem.Rules.FirstOrDefault(r => r.AppliesTo(ctx) && r.CallIn(ctx) == call);

    private static bool IsGame(Call call) => call.IsBid && call.Level >= Contract.GameLevel(call.Strain);

    private static readonly Suit[] SuitsMajorsFirst = [Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs];

    private static BidChoice Place(BidContext ctx, Hand hand, Shows partner)
    {
        BidChoice Pass(string why) => new(Call.Pass, why, "place-pass");

        // Partner hasn't said anything: leave the bidding to the system rules.
        if (ctx.Partners.Count == 0 || ctx.Partners.All(c => c.Kind == CallKind.Pass))
            return Pass("Pas");

        var min = hand.Hcp + partner.HcpMin;
        var max = hand.Hcp + partner.HcpMax;
        int Together(Suit s) => hand.Length(s) + partner.MinLength[(int)s];
        var fit = SuitsMajorsFirst.Where(s => Together(s) >= 8).OrderByDescending(s => s is Suit.Hearts or Suit.Spades).ThenByDescending(Together).Cast<Suit?>().FirstOrDefault();
        var majorFit = fit is Suit.Hearts or Suit.Spades;
        var fitText = fit is { } f ? $" og mindst {Together(f)} {Shows.Symbol(f)} tilsammen" : "";

        Call? target;
        string why;
        if (min >= 33 && fit is not null && IsBlackwood(ctx) && ctx.Legal(Call.Parse("4N")))
            return new BidChoice(Call.Parse("4N"), $"Blackwood: I har mindst {min} HP tilsammen{fitText}, så der kan være slem. Spørger efter esser", "place-blackwood");
        if (min >= 33)
        {
            target = Call.Bid(min >= 37 ? 7 : 6, majorFit ? fit!.Value.ToStrain() : Strain.NoTrump);
            why = $"Slem: I har mindst {min} HP tilsammen{fitText}";
        }
        else if (min >= 25 && (majorFit || fit is null || min < 29))
        {
            target = majorFit ? Call.Bid(4, fit!.Value) : Call.Bid(3, Strain.NoTrump);
            why = $"Udgang: I har mindst {min} HP tilsammen{fitText}";
        }
        else if (min >= 29 && fit is { } minor)
        {
            target = Call.Bid(5, minor);
            why = $"Udgang: I har mindst {min} HP tilsammen{fitText}";
        }
        else if (min >= 23 && max >= 25)
        {
            target = majorFit ? Call.Bid(3, fit!.Value) : Call.Bid(2, Strain.NoTrump);
            why = $"Inviterer til udgang: I har {min}–{Math.Min(max, 40)} HP tilsammen{fitText}";
        }
        else
        {
            // Part score: stay where we are if it's our contract, otherwise compete in a fit.
            if (ctx.OurContract) return Pass($"Pas: nok til delkontrakt, men ikke til udgang (I har {min}–{Math.Min(max, 40)} HP)");
            if (fit is { } s && min >= 20 && ctx.Cheapest(s.ToStrain(), 3) is { } compete)
                return new BidChoice(compete, $"Konkurrerer: I har en fit{fitText}", "place-compete");
            return Pass("Pas");
        }

        // Already there (or higher): no need to bid again.
        if (ctx.OurContract && ctx.LastBid is { } last && last.Call.Level >= target.Value.Level && (last.Call.Strain == target.Value.Strain || IsGame(last.Call)))
            return Pass($"Pas: kontrakten er god nok ({why[0..1].ToLowerInvariant()}{why[1..]})");
        if (ctx.Legal(target.Value)) return new BidChoice(target.Value, why, "place");
        // The target bid is too low now; settle for the cheapest bid in the same strain if it isn't too high.
        if (ctx.Cheapest(target.Value.Strain, Math.Max(target.Value.Level, Contract.GameLevel(target.Value.Strain))) is { } higher && !ctx.OurContract)
            return new BidChoice(higher, why, "place");
        return Pass("Pas");
    }
}
