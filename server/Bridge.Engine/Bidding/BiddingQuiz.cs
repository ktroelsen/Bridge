namespace Bridge.Engine.Bidding;

public enum QuizKind
{
    /// <summary>South deals: what do you open?</summary>
    Opening,
    /// <summary>Partner opened 1 in a suit, East passed: what do you respond?</summary>
    Response,
    /// <summary>Partner opened 1UT, East passed: what do you respond?</summary>
    NtResponse,
}

/// <summary>A "what do you bid?" question: South's hand, the calls so far and the system's answer.</summary>
public sealed record BiddingQuestion(int Number, Seat Dealer, IReadOnlyList<CallRecord> Before, Hand Hand, BidChoice Answer, IReadOnlyList<Call> Legal);

/// <summary>
/// Makes bidding questions from random deals, answered by the same rules the bot bids with.
/// Each question first picks a kind of answer (say "a raise") and then deals until the system's
/// answer is of that kind, so the questions cover the whole topic and are not mostly passes.
/// </summary>
public static class BiddingQuiz
{
    private sealed record Category(string Name, int Weight, Func<Call, Call?, bool> Matches);

    private static readonly Category[] OpeningCategories =
    [
        new("1nt", 3, (c, _) => c == Call.Parse("1N")),
        new("1major", 4, (c, _) => c.Level == 1 && c.Strain is Strain.Hearts or Strain.Spades),
        new("1minor", 3, (c, _) => c.Level == 1 && c.Strain is Strain.Clubs or Strain.Diamonds),
        new("pass", 2, (c, _) => c == Call.Pass),
        new("2nt", 1, (c, _) => c == Call.Parse("2N")),
        new("2c", 1, (c, _) => c == Call.Parse("2C")),
        new("preempt", 1, (c, _) => c.Level == 3 && c.Strain != Strain.NoTrump),
    ];

    private static readonly Category[] ResponseCategories =
    [
        new("raise", 4, (c, opening) => c.IsBid && c.Strain == opening!.Value.Strain),
        new("new-suit", 3, (c, opening) => c.IsBid && c.Strain != opening!.Value.Strain && c.Strain != Strain.NoTrump),
        new("notrump", 2, (c, _) => c.IsBid && c.Strain == Strain.NoTrump),
        new("pass", 2, (c, _) => c == Call.Pass),
    ];

    private static readonly Category[] NtCategories =
    [
        new("transfer", 3, (c, _) => c == Call.Parse("2D") || c == Call.Parse("2H")),
        new("stayman", 3, (c, _) => c == Call.Parse("2C")),
        new("notrump", 2, (c, _) => c == Call.Parse("2N") || c == Call.Parse("3N")),
        new("pass", 1, (c, _) => c == Call.Pass),
    ];

    public static BiddingQuestion Make(QuizKind kind, Random random)
    {
        var categories = kind switch { QuizKind.Opening => OpeningCategories, QuizKind.Response => ResponseCategories, _ => NtCategories };
        var target = Pick(categories, random);
        BiddingQuestion? fallback = null;
        for (var attempt = 0; attempt < 20_000; attempt++)
        {
            var number = random.Next(Deal.MinNumber, 10_000_000);
            if (Try(kind, number) is not { } question) continue;
            fallback ??= question;
            var opening = question.Before.FirstOrDefault(c => c.Call.IsBid)?.Call;
            if (target.Matches(question.Answer.Call, opening)) return question;
        }
        return fallback ?? throw new InvalidOperationException("Kunne ikke lave et spørgsmål.");
    }

    /// <summary>The question for one deal number, if the deal fits the kind.</summary>
    public static BiddingQuestion? Try(QuizKind kind, int number)
    {
        var deal = Deal.FromNumber(number);
        // The quiz always has South deal (openings) or North deal (responses), whatever the board says.
        var dealer = kind == QuizKind.Opening ? Seat.South : Seat.North;
        var auction = new Auction(dealer);
        var before = new List<CallRecord>();

        void Bot(Seat seat)
        {
            var calls = auction.Calls.ToList();
            var choice = Bidder.Choose(dealer, calls, seat, deal[seat]);
            auction.Add(choice.Call);
            before.Add(new CallRecord(seat, choice.Call, Bidder.Explain(dealer, calls, seat, choice.Call), null));
        }

        if (kind != QuizKind.Opening)
        {
            Bot(Seat.North);
            var opening = auction.Calls[0].Call;
            var wanted = kind == QuizKind.NtResponse ? opening == Call.Parse("1N") : opening is { Level: 1 } o && o.Suit is not null;
            if (!wanted) return null;
            Bot(Seat.East);
            if (auction.Calls[1].Call != Call.Pass) return null;
        }

        var answer = Bidder.Choose(dealer, auction.Calls, Seat.South, deal[Seat.South]);
        return new BiddingQuestion(number, dealer, before, deal[Seat.South], answer, [.. auction.LegalCalls()]);
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
