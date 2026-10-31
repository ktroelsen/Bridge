namespace Bridge.Engine.Bidding;

public enum QuizKind
{
    /// <summary>South deals: what do you open?</summary>
    Opening,
    /// <summary>Partner opened 1 in a suit, East passed: what do you respond?</summary>
    Response,
    /// <summary>Partner opened 1UT, East passed: what do you respond?</summary>
    NtResponse,
    /// <summary>You opened 1 in a suit and partner responded: your second bid.</summary>
    Rebid,
    /// <summary>You opened 1UT and partner used Stayman or a transfer: your answer.</summary>
    NtOpener,
    /// <summary>East opened 1 in a suit: do you overcall, double or pass?</summary>
    Overcall,
    /// <summary>West opened, partner overcalled or doubled, East passed: your call.</summary>
    Advance,
}

/// <summary>A "what do you bid?" question: South's hand, the calls so far and the system's answer.</summary>
public sealed record BiddingQuestion(int Number, Seat Dealer, IReadOnlyList<CallRecord> Before, Hand Hand, BidChoice Answer, IReadOnlyList<Call> Legal);

/// <summary>
/// Makes bidding questions from random deals, answered by the same rules the bot bids with.
/// A scenario says who deals and which calls must come before South's turn (all made by the
/// bots, South's own earlier calls too). Each question first picks a kind of answer (say "a
/// raise") and then deals until the system's answer is of that kind, so the questions cover the
/// whole topic and are not mostly passes.
/// </summary>
public static class BiddingQuiz
{
    /// <summary>A kind of answer. <c>Before</c> holds the calls before South's, in order.</summary>
    private sealed record Category(int Weight, Func<Call, IReadOnlyList<Call>, bool> Matches);

    /// <summary>Who deals, what each call before South's must look like, and the kinds of answers.</summary>
    private sealed record Scenario(Seat Dealer, Func<Call, IReadOnlyList<Call>, bool>[] Before, Category[] Categories);

    private static Call C(string text) => Call.Parse(text);

    private static bool OneOfASuit(Call c) => c is { Kind: CallKind.Bid, Level: 1 } && c.Suit is not null;

    private static bool IsPass(Call c, IReadOnlyList<Call> _) => c == Call.Pass;

    private static readonly Dictionary<QuizKind, Scenario> Scenarios = new()
    {
        [QuizKind.Opening] = new(Seat.South, [],
        [
            new(3, (c, _) => c == C("1N")),
            new(4, (c, _) => c.Level == 1 && c.Strain is Strain.Hearts or Strain.Spades),
            new(3, (c, _) => c.Level == 1 && c.Strain is Strain.Clubs or Strain.Diamonds),
            new(2, (c, _) => c == Call.Pass),
            new(1, (c, _) => c == C("2N")),
            new(1, (c, _) => c == C("2C")),
            new(1, (c, _) => c.Level == 3 && c.Strain != Strain.NoTrump),
        ]),
        [QuizKind.Response] = new(Seat.North, [(c, _) => OneOfASuit(c), IsPass],
        [
            new(4, (c, b) => c.IsBid && c.Strain == b[0].Strain),
            new(3, (c, b) => c.IsBid && c.Strain != b[0].Strain && c.Strain != Strain.NoTrump),
            new(2, (c, _) => c.IsBid && c.Strain == Strain.NoTrump),
            new(2, (c, _) => c == Call.Pass),
        ]),
        [QuizKind.NtResponse] = new(Seat.North, [(c, _) => c == C("1N"), IsPass],
        [
            new(3, (c, _) => c == C("2D") || c == C("2H")),
            new(3, (c, _) => c == C("2C")),
            new(2, (c, _) => c == C("2N") || c == C("3N")),
            new(1, (c, _) => c == Call.Pass),
        ]),
        [QuizKind.Rebid] = new(Seat.South, [(c, _) => OneOfASuit(c), IsPass, (c, _) => c.IsBid, IsPass],
        [
            new(3, (c, b) => c.IsBid && c.Strain == b[2].Strain && c.Strain != Strain.NoTrump),
            new(3, (c, _) => c.IsBid && c.Strain == Strain.NoTrump),
            new(2, (c, b) => c.IsBid && c.Strain == b[0].Strain),
            new(2, (c, b) => c.IsBid && c.Strain != b[0].Strain && c.Strain != b[2].Strain && c.Strain != Strain.NoTrump),
            new(1, (c, _) => c == Call.Pass),
        ]),
        [QuizKind.NtOpener] = new(Seat.South, [(c, _) => c == C("1N"), IsPass, (c, _) => c == C("2C") || c == C("2D") || c == C("2H"), IsPass],
        [
            new(2, (_, b) => b[2] == C("2C")),
            new(1, (_, b) => b[2] != C("2C")),
        ]),
        [QuizKind.Overcall] = new(Seat.East, [(c, _) => OneOfASuit(c)],
        [
            new(3, (c, _) => c.IsBid && c.Strain != Strain.NoTrump),
            new(2, (c, _) => c == Call.Double),
            new(1, (c, _) => c == C("1N")),
            new(2, (c, _) => c == Call.Pass),
        ]),
        [QuizKind.Advance] = new(Seat.West, [(c, _) => OneOfASuit(c), (c, _) => (c.IsBid && c.Suit is not null) || c == Call.Double, IsPass],
        [
            new(3, (c, b) => b[1].IsBid && c.IsBid && c.Strain == b[1].Strain),
            new(3, (c, b) => b[1] == Call.Double && c.IsBid),
            new(1, (c, _) => c == Call.Pass),
        ]),
    };

    public static BiddingQuestion Make(QuizKind kind, Random random)
    {
        var scenario = Scenarios[kind];
        var target = Pick(scenario.Categories, random);
        BiddingQuestion? fallback = null;
        for (var attempt = 0; attempt < 20_000; attempt++)
        {
            if (Try(kind, random.Next(Deal.MinNumber, 10_000_000)) is not { } question) continue;
            fallback ??= question;
            if (target.Matches(question.Answer.Call, [.. question.Before.Select(c => c.Call)])) return question;
        }
        return fallback ?? throw new InvalidOperationException("Kunne ikke lave et spørgsmål.");
    }

    /// <summary>The question for one deal number, if the bots' calls fit the scenario.</summary>
    public static BiddingQuestion? Try(QuizKind kind, int number)
    {
        var scenario = Scenarios[kind];
        var deal = Deal.FromNumber(number);
        var auction = new Auction(scenario.Dealer);
        var before = new List<CallRecord>();

        foreach (var wanted in scenario.Before)
        {
            var seat = auction.NextSeat;
            var calls = auction.Calls.ToList();
            var choice = Bidder.Choose(scenario.Dealer, calls, seat, deal[seat]);
            if (!wanted(choice.Call, [.. before.Select(c => c.Call)])) return null;
            auction.Add(choice.Call);
            before.Add(new CallRecord(seat, choice.Call, Bidder.Explain(scenario.Dealer, calls, seat, choice.Call), null));
        }
        if (auction.NextSeat != Seat.South || auction.IsComplete) return null;

        var answer = Bidder.Choose(scenario.Dealer, auction.Calls, Seat.South, deal[Seat.South]);
        return new BiddingQuestion(number, scenario.Dealer, before, deal[Seat.South], answer, [.. auction.LegalCalls()]);
    }

    private static Category Pick(Category[] categories, Random random)
    {
        var roll = random.Next(categories.Sum(c => c.Weight));
        foreach (var c in categories)
        {
            if (roll < c.Weight) return c;
            roll -= c.Weight;
        }
        return categories[^1];
    }
}
