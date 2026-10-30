namespace Bridge.Engine.Bidding;

/// <summary>
/// A simple natural beginner system, close to what Danish clubs teach: five-card majors,
/// 1UT = 15–17 balanced, 2♣ strong, Stayman and Jacoby transfers over 1UT, simple overcalls
/// and the takeout double. Rules are tried in order; the first that fits the hand is bid.
/// </summary>
public static class BeginnerSystem
{
    private static readonly Suit[] Majors = [Suit.Spades, Suit.Hearts];
    private static readonly Suit[] AllSuits = [Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs];

    public static IReadOnlyList<BidRule> Rules { get; } = [.. Opening(), .. Responses(), .. OpenerRebids(), .. ResponderRebids(), .. Competition()];

    private static Call C(string text) => Call.Parse(text);

    private static string Name(Suit s) => s switch
    {
        Suit.Spades => "spar",
        Suit.Hearts => "hjerter",
        Suit.Diamonds => "ruder",
        _ => "klør",
    };

    private static bool Is(Call? call, string text) => call is { } c && c == C(text);

    private static bool IsMajor(Suit s) => s is Suit.Hearts or Suit.Spades;

    private static bool HasFiveCardMajor(Hand h) => h.Length(Suit.Hearts) >= 5 || h.Length(Suit.Spades) >= 5;

    private static bool HasFourCardMajor(Hand h) => h.Length(Suit.Hearts) >= 4 || h.Length(Suit.Spades) >= 4;

    /// <summary>The longest suit (ties: the higher-ranking).</summary>
    private static Suit Longest(Hand h, IEnumerable<Suit>? among = null) =>
        (among ?? AllSuits).OrderByDescending(h.Length).ThenByDescending(s => s).First();

    // ---------------------------------------------------------------- opening

    private static IEnumerable<BidRule> Opening()
    {
        const Situation S = Situation.Opening;
        yield return new("open-2c", S, _ => C("2C"), _ => Shows.Hcp(22),
            Text: _ => "Stærk åbning. Siger intet om klør, og makker må ikke passe");
        yield return new("open-2nt", S, _ => C("2N"), _ => Shows.Hcp(20, 21).Bal(), Text: _ => "Åbning 2UT");
        yield return new("open-1nt", S, _ => C("1N"), _ => Shows.Hcp(15, 17).Bal(), Text: _ => "Åbning 1UT");
        yield return new("open-1s", S, _ => C("1S"), _ => Shows.Hcp(12, 21).Len(Suit.Spades, 5),
            When: (_, h) => h.Length(Suit.Spades) >= h.Length(Suit.Hearts), Text: _ => "Åbning");
        yield return new("open-1h", S, _ => C("1H"), _ => Shows.Hcp(12, 21).Len(Suit.Hearts, 5), Text: _ => "Åbning");
        yield return new("open-1d", S, _ => C("1D"), _ => Shows.Hcp(12, 21).Len(Suit.Diamonds, 3).Len(Suit.Hearts, 0, 4).Len(Suit.Spades, 0, 4),
            When: (_, h) => h.Length(Suit.Diamonds) > h.Length(Suit.Clubs) || (h.Length(Suit.Diamonds) == h.Length(Suit.Clubs) && h.Length(Suit.Clubs) >= 4),
            Text: _ => "Åbning uden 5-korts højfarve");
        yield return new("open-1c", S, _ => C("1C"), _ => Shows.Hcp(12, 21).Len(Suit.Clubs, 3).Len(Suit.Hearts, 0, 4).Len(Suit.Spades, 0, 4),
            Text: _ => "Åbning uden 5-korts højfarve");
        foreach (var suit in AllSuits)
            yield return new($"open-3{suit}", S, _ => Call.Bid(3, suit), _ => Shows.Hcp(6, 10).Len(suit, 7),
                Text: _ => "Spærremelding: en lang farve og en svag hånd");
        yield return new("open-pass", S, _ => Call.Pass, _ => Shows.Hcp(0, 11), Text: _ => "Pas");
    }

    // ---------------------------------------------------------------- responses

    private static IEnumerable<BidRule> Responses()
    {
        const Situation S = Situation.Response;

        // To 1 in a major.
        foreach (var major in Majors)
        {
            bool Opened(BidContext c) => c.Opening == Call.Bid(1, major);
            yield return new($"resp-1{major}-4", S, _ => Call.Bid(4, major), _ => Shows.Hcp(13, 15).Len(major, 3), Opened,
                Text: _ => $"Støtte og udgang: mindst 3 {Name(major)}");
            yield return new($"resp-1{major}-3", S, _ => Call.Bid(3, major), _ => Shows.Hcp(10, 12).Len(major, 3), Opened,
                Text: _ => "Inviterer til udgang med støtte");
            yield return new($"resp-1{major}-2", S, _ => Call.Bid(2, major), _ => Shows.Hcp(6, 9).Len(major, 3), Opened,
                Text: _ => "Enkel støtte");
        }
        yield return new("resp-1h-1s", S, _ => C("1S"), _ => Shows.Hcp(6).Len(Suit.Spades, 4), c => Is(c.Opening, "1H"),
            Text: _ => "Ny farve. Makker skal melde igen");
        yield return new("resp-1s-2h", S, _ => C("2H"), _ => Shows.Hcp(10).Len(Suit.Hearts, 5), c => Is(c.Opening, "1S"),
            Text: _ => "Ny farve på 2-trinnet. Makker skal melde igen");

        // To 1 in a minor: show a four-card major first (hearts first with 4-4).
        bool MinorOpened(BidContext c) => Is(c.Opening, "1C") || Is(c.Opening, "1D");
        yield return new("resp-1m-1h", S, _ => C("1H"), _ => Shows.Hcp(6).Len(Suit.Hearts, 4), MinorOpened,
            When: (_, h) => !(h.Length(Suit.Spades) >= 5 && h.Length(Suit.Spades) > h.Length(Suit.Hearts)),
            Text: _ => "Ny farve. Makker skal melde igen");
        yield return new("resp-1m-1s", S, _ => C("1S"), _ => Shows.Hcp(6).Len(Suit.Spades, 4), MinorOpened,
            Text: _ => "Ny farve. Makker skal melde igen");
        yield return new("resp-1c-1d", S, _ => C("1D"), _ => Shows.Hcp(6).Len(Suit.Diamonds, 4).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3),
            c => Is(c.Opening, "1C"), Text: _ => "Ny farve uden 4-korts højfarve. Makker skal melde igen");
        foreach (var minor in new[] { Suit.Diamonds, Suit.Clubs })
        {
            bool Opened(BidContext c) => c.Opening == Call.Bid(1, minor);
            var support = minor == Suit.Diamonds ? 4 : 5;
            yield return new($"resp-1{minor}-3", S, _ => Call.Bid(3, minor), _ => Shows.Hcp(10, 12).Len(minor, support).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3), Opened,
                Text: _ => "Inviterer med støtte");
            yield return new($"resp-1{minor}-2", S, _ => Call.Bid(2, minor), _ => Shows.Hcp(6, 9).Len(minor, support).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3), Opened,
                Text: _ => "Enkel støtte");
        }
        yield return new("resp-1d-2c", S, _ => C("2C"), _ => Shows.Hcp(10).Len(Suit.Clubs, 4).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3),
            c => Is(c.Opening, "1D"), Text: _ => "Ny farve på 2-trinnet. Makker skal melde igen");
        yield return new("resp-1s-2d", S, _ => C("2D"), _ => Shows.Hcp(10).Len(Suit.Diamonds, 4).Len(Suit.Spades, 0, 2),
            c => Is(c.Opening, "1S") || Is(c.Opening, "1H"), When: (_, h) => h.Length(Suit.Diamonds) >= h.Length(Suit.Clubs),
            Text: _ => "Ny farve på 2-trinnet. Makker skal melde igen");
        yield return new("resp-1M-2c", S, _ => C("2C"), _ => Shows.Hcp(10).Len(Suit.Clubs, 4).Len(Suit.Spades, 0, 2),
            c => Is(c.Opening, "1S") || Is(c.Opening, "1H") || Is(c.Opening, "1D"),
            Text: _ => "Ny farve på 2-trinnet. Makker skal melde igen");

        // Notrump responses to a suit opening.
        bool SuitOpened(BidContext c) => c.Opening is { Level: 1 } o && o.Suit is not null;
        yield return new("resp-3nt", S, _ => C("3N"), c => NoFitShows(c, Shows.Hcp(13, 15).Bal()), SuitOpened, Text: _ => "Udgang i uden trumf");
        yield return new("resp-2nt", S, _ => C("2N"), c => NoFitShows(c, Shows.Hcp(11, 12).Bal()), SuitOpened, Text: _ => "Inviterer til 3UT");
        yield return new("resp-1nt", S, _ => C("1N"), _ => Shows.Hcp(6, 10), SuitOpened, Text: _ => "Svag hånd uden støtte eller en farve, der kan meldes på 1-trinnet");

        // To 1UT.
        bool NtOpened(BidContext c) => Is(c.Opening, "1N");
        yield return new("resp-1nt-2d", S, _ => C("2D"), _ => Shows.Anything.Len(Suit.Hearts, 5), NtOpened,
            When: (_, h) => h.Length(Suit.Hearts) >= h.Length(Suit.Spades), Text: _ => "Overføring: viser hjerter. Makker skal melde 2♥");
        yield return new("resp-1nt-2h", S, _ => C("2H"), _ => Shows.Anything.Len(Suit.Spades, 5), NtOpened,
            Text: _ => "Overføring: viser spar. Makker skal melde 2♠");
        yield return new("resp-1nt-2c", S, _ => C("2C"), _ => Shows.Hcp(8), NtOpened, When: (_, h) => HasFourCardMajor(h) && !HasFiveCardMajor(h),
            Text: _ => "Stayman: spørger, om makker har 4 kort i en højfarve");
        yield return new("resp-1nt-3nt", S, _ => C("3N"), _ => Shows.Hcp(10, 15).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3), NtOpened, Text: _ => "Udgang");
        yield return new("resp-1nt-2nt", S, _ => C("2N"), _ => Shows.Hcp(8, 9).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3), NtOpened, Text: _ => "Inviterer til 3UT");

        // To 2UT.
        foreach (var major in Majors)
            yield return new($"resp-2nt-4{major}", S, _ => Call.Bid(4, major), _ => Shows.Hcp(5).Len(major, 6), c => Is(c.Opening, "2N"), Text: _ => "Udgang");
        yield return new("resp-2nt-3nt", S, _ => C("3N"), _ => Shows.Hcp(5, 10), c => Is(c.Opening, "2N"), Text: _ => "Udgang");

        // To 2♣: a waiting bid with any hand.
        yield return new("resp-2c-2d", S, _ => C("2D"), _ => Shows.Anything, c => Is(c.Opening, "2C"),
            Text: _ => "Ventemelding: siger intet om hånden, men makker skal melde igen");

        // To a 3-level preempt.
        yield return new("resp-pre-4M", S, c => c.OpeningSuit is { } s && IsMajor(s) ? Call.Bid(4, s) : null,
            c => Shows.Hcp(15).Len(c.OpeningSuit!.Value, 2), c => c.Opening is { Level: 3 } && c.OpeningSuit is not null, Text: _ => "Udgang");
        yield return new("resp-pre-3nt", S, _ => C("3N"), _ => Shows.Hcp(16).Bal(), c => c.Opening is { Level: 3 } && c.OpeningSuit is not null, Text: _ => "Udgang i uden trumf");

        yield return new("resp-pass", S, _ => Call.Pass, c => Is(c.Opening, "1N") ? Shows.Hcp(0, 7) : Is(c.Opening, "2N") ? Shows.Hcp(0, 4) : Shows.Hcp(0, 5),
            c => !Is(c.Opening, "2C"), Text: _ => "Pas");
    }

    /// <summary>No three-card support for a major opening and no four-card major that could be bid at the 1-level.</summary>
    private static Shows NoFitShows(BidContext c, Shows shows)
    {
        var s = shows;
        if (c.OpeningSuit is { } o && IsMajor(o)) s = s.Len(o, 0, 2);
        if (c.OpeningSuit is Suit.Clubs or Suit.Diamonds) s = s.Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3);
        if (c.OpeningSuit == Suit.Hearts) s = s.Len(Suit.Spades, 0, 3);
        return s;
    }

    // ---------------------------------------------------------------- opener's rebid

    private static IEnumerable<BidRule> OpenerRebids()
    {
        const Situation S = Situation.OpenerRebid;
        Suit Own(BidContext c) => c.OpeningSuit!.Value;
        Call Resp(BidContext c) => c.Partners[0];
        bool SuitOpening(BidContext c) => c.Opening is { Level: 1 } o && o.Suit is not null;

        // Partner raised my suit.
        bool Raised(BidContext c, int level) => SuitOpening(c) && Resp(c) == Call.Bid(level, Own(c).ToStrain());
        yield return new("reb-raise2-game", S, c => IsMajor(Own(c)) ? Call.Bid(4, Own(c)) : C("3N"), _ => Shows.Hcp(19), c => Raised(c, 2), Text: _ => "Udgang");
        yield return new("reb-raise2-invite", S, c => Call.Bid(3, Own(c)), _ => Shows.Hcp(16, 18), c => Raised(c, 2) && IsMajor(Own(c)),
            Text: _ => "Inviterer: makker melder udgang med en god støtte (8–9 HP)");
        yield return new("reb-raise3-game", S, c => IsMajor(Own(c)) ? Call.Bid(4, Own(c)) : C("3N"), _ => Shows.Hcp(14), c => Raised(c, 3), Text: _ => "Udgang");

        // Partner bid a new suit at the 1-level.
        bool NewSuit1(BidContext c) => SuitOpening(c) && Resp(c) is { Level: 1 } r && r.Suit is not null && r.Suit != Own(c);
        Suit P(BidContext c) => Resp(c).Suit!.Value;
        yield return new("reb-1x-raise4", S, c => IsMajor(P(c)) ? Call.Bid(4, P(c)) : null, c => Shows.Hcp(19, 21).Len(P(c), 4), NewSuit1, Text: _ => "Støtte og udgang");
        yield return new("reb-1x-raise3", S, c => Call.Bid(3, P(c)), c => Shows.Hcp(16, 18).Len(P(c), 4), NewSuit1, Text: _ => "Støtte med en god hånd. Inviterer");
        yield return new("reb-1x-raise2", S, c => Call.Bid(2, P(c)), c => Shows.Hcp(12, 15).Len(P(c), 4), NewSuit1, Text: _ => "Støtte");
        yield return new("reb-1x-1s", S, _ => C("1S"), _ => Shows.Hcp(12, 18).Len(Suit.Spades, 4), NewSuit1, Text: _ => "Ny farve på 1-trinnet");
        yield return new("reb-1x-1nt", S, _ => C("1N"), _ => Shows.Hcp(12, 14).Bal(), NewSuit1, Text: _ => "Jævn minimumshånd");
        yield return new("reb-1x-2nt", S, _ => C("2N"), _ => Shows.Hcp(18, 19).Bal(), NewSuit1, Text: _ => "Jævn stærk hånd");
        yield return new("reb-1x-jump", S, c => Call.Bid(3, Own(c)), c => Shows.Hcp(16, 18).Len(Own(c), 6), NewSuit1, Text: _ => "Lang farve og en god hånd. Inviterer");
        yield return new("reb-1x-own", S, c => Call.Bid(2, Own(c)), c => Shows.Hcp(12, 15).Len(Own(c), 6), NewSuit1, Text: _ => "Lang farve, minimumshånd");
        foreach (var suit in AllSuits)
            yield return new($"reb-1x-2{suit}", S, _ => Call.Bid(2, suit), _ => Shows.Hcp(12, 18).Len(suit, 4),
                c => NewSuit1(c) && suit < Own(c) && suit != P(c), Text: _ => "Ny farve. Viser mindst 4 kort");

        // Partner bid a new suit at the 2-level (10+, forcing).
        bool NewSuit2(BidContext c) => SuitOpening(c) && Resp(c) is { Level: 2 } r && r.Suit is not null && r.Suit != Own(c);
        yield return new("reb-2x-raise", S, c => IsMajor(P(c)) ? Call.Bid(3, P(c)) : null, c => Shows.Hcp(12).Len(P(c), 3), NewSuit2, Text: _ => "Støtte");
        yield return new("reb-2x-2nt", S, _ => C("2N"), _ => Shows.Hcp(12, 14).Bal(), NewSuit2, Text: _ => "Jævn minimumshånd");
        yield return new("reb-2x-3nt", S, _ => C("3N"), _ => Shows.Hcp(18, 19).Bal(), NewSuit2, Text: _ => "Jævn stærk hånd, udgang");
        yield return new("reb-2x-own", S, c => c.Cheapest(Own(c).ToStrain(), 3), c => Shows.Hcp(12).Len(Own(c), 6), NewSuit2, Text: _ => "Lang farve");
        yield return new("reb-2x-raise-minor", S, c => c.Cheapest(P(c).ToStrain(), 3), c => Shows.Hcp(12).Len(P(c), 4), NewSuit2, Text: _ => "Støtte");

        // Partner bid 1UT (6-10).
        bool Nt1(BidContext c) => SuitOpening(c) && Resp(c) == C("1N");
        yield return new("reb-1nt-3nt", S, _ => C("3N"), _ => Shows.Hcp(19, 21).Bal(), Nt1, Text: _ => "Udgang");
        yield return new("reb-1nt-2nt", S, _ => C("2N"), _ => Shows.Hcp(18, 18).Bal(), Nt1, Text: _ => "Inviterer til 3UT");
        yield return new("reb-1nt-own", S, c => Call.Bid(2, Own(c)), c => Shows.Hcp(12, 15).Len(Own(c), 6), Nt1, Text: _ => "Lang farve, minimumshånd");
        foreach (var suit in AllSuits)
            yield return new($"reb-1nt-2{suit}", S, _ => Call.Bid(2, suit), _ => Shows.Hcp(12, 18).Len(suit, 4),
                c => Nt1(c) && suit < Own(c), Text: _ => "Ny farve. Viser mindst 4 kort");
        yield return new("reb-1nt-pass", S, _ => Call.Pass, _ => Shows.Hcp(12, 17), Nt1, Text: _ => "Pas");

        // Partner bid 2UT (11-12) or 3UT.
        yield return new("reb-2nt-3nt", S, _ => C("3N"), _ => Shows.Hcp(13), c => SuitOpening(c) && Is(Resp(c), "2N"), Text: _ => "Udgang");

        // After 1UT: Stayman and transfers.
        bool NtOpen(BidContext c) => Is(c.Opening, "1N");
        yield return new("reb-stay-2h", S, _ => C("2H"), _ => Shows.Hcp(15, 17).Len(Suit.Hearts, 4), c => NtOpen(c) && Is(Resp(c), "2C"), Text: _ => "Svar på Stayman: 4 hjerter (kan også have 4 spar)");
        yield return new("reb-stay-2s", S, _ => C("2S"), _ => Shows.Hcp(15, 17).Len(Suit.Spades, 4).Len(Suit.Hearts, 0, 3), c => NtOpen(c) && Is(Resp(c), "2C"), Text: _ => "Svar på Stayman: 4 spar, ikke 4 hjerter");
        yield return new("reb-stay-2d", S, _ => C("2D"), _ => Shows.Hcp(15, 17).Len(Suit.Hearts, 0, 3).Len(Suit.Spades, 0, 3), c => NtOpen(c) && Is(Resp(c), "2C"), Text: _ => "Svar på Stayman: ingen 4-korts højfarve");
        yield return new("reb-transfer-h", S, _ => C("2H"), _ => Shows.Hcp(15, 17), c => NtOpen(c) && Is(Resp(c), "2D"), Text: _ => "Fuldfører overføringen");
        yield return new("reb-transfer-s", S, _ => C("2S"), _ => Shows.Hcp(15, 17), c => NtOpen(c) && Is(Resp(c), "2H"), Text: _ => "Fuldfører overføringen");
        yield return new("reb-1nt-2nt-3nt", S, _ => C("3N"), _ => Shows.Hcp(16, 17), c => NtOpen(c) && Is(Resp(c), "2N"), Text: _ => "Accepterer invitationen");

        // After 2♣ - 2♦.
        bool Strong(BidContext c) => Is(c.Opening, "2C") && Is(Resp(c), "2D");
        yield return new("reb-2c-3nt", S, _ => C("3N"), _ => Shows.Hcp(25).Bal(), Strong, Text: _ => "Jævn, meget stærk hånd");
        yield return new("reb-2c-2nt", S, _ => C("2N"), _ => Shows.Hcp(22, 24).Bal(), Strong, Text: _ => "Jævn stærk hånd");
        foreach (var suit in AllSuits)
            yield return new($"reb-2c-{suit}", S, c => c.Cheapest(suit.ToStrain(), 3), _ => Shows.Hcp(22).Len(suit, 5), Strong,
                When: (_, h) => Longest(h) == suit, Text: _ => "Viser sin lange farve");

        // A new suit by responder is forcing: opener must not pass (unless the opponents bid in between).
        bool Forcing(BidContext c) => SuitOpening(c) && Resp(c).Suit is { } r && r != Own(c) && !c.RightHandActed;
        foreach (var suit in AllSuits)
            yield return new($"reb-2x-new{suit}", S, c => c.Cheapest(suit.ToStrain(), 2), _ => Shows.Hcp(12).Len(suit, 4),
                c => NewSuit2(c) && suit != Own(c) && suit != P(c), Text: _ => "Ny farve. Viser mindst 4 kort");
        yield return new("reb-forced-own", S, c => c.Cheapest(Own(c).ToStrain(), 3), c => Shows.Hcp(12).Len(Own(c), 5), Forcing,
            Text: _ => "Gentager sin farve");
        yield return new("reb-forced-nt", S, c => c.Cheapest(Strain.NoTrump, Resp(c).Level), _ => Shows.Hcp(12, 14), Forcing,
            Text: _ => "Uden trumf");
    }

    // ---------------------------------------------------------------- responder's rebid

    private static IEnumerable<BidRule> ResponderRebids()
    {
        const Situation S = Situation.ResponderRebid;
        bool NtStayman(BidContext c) => Is(c.Opening, "1N") && c.Mine.Count == 1 && Is(c.Mine[0], "2C");
        Call Rebid(BidContext c) => c.Partners[1];

        foreach (var major in Majors)
        {
            bool Shown(BidContext c) => NtStayman(c) && Rebid(c) == Call.Bid(2, major);
            yield return new($"stay-{major}-4", S, _ => Call.Bid(4, major), _ => Shows.Hcp(10).Len(major, 4), Shown, Text: _ => "Fit og udgang");
            yield return new($"stay-{major}-3", S, _ => Call.Bid(3, major), _ => Shows.Hcp(8, 9).Len(major, 4), Shown, Text: _ => "Fit. Inviterer til udgang");
        }
        yield return new("stay-3nt", S, _ => C("3N"), _ => Shows.Hcp(10), NtStayman, Text: _ => "Udgang. Ingen fit i højfarve");
        yield return new("stay-2nt", S, _ => C("2N"), _ => Shows.Hcp(8, 9), NtStayman, Text: _ => "Inviterer til 3UT. Ingen fit i højfarve");

        foreach (var (transfer, major) in new[] { ("2D", Suit.Hearts), ("2H", Suit.Spades) })
        {
            bool After(BidContext c) => Is(c.Opening, "1N") && c.Mine.Count == 1 && Is(c.Mine[0], transfer);
            yield return new($"tr-{major}-4", S, _ => Call.Bid(4, major), _ => Shows.Hcp(10).Len(major, 6), After, Text: _ => "Udgang med 6 kort i farven");
            yield return new($"tr-{major}-3nt", S, _ => C("3N"), _ => Shows.Hcp(10, 15).Len(major, 5, 5), After, Text: _ => "Udgang. Makker vælger mellem 3UT og 4 i højfarven");
            yield return new($"tr-{major}-3", S, _ => Call.Bid(3, major), _ => Shows.Hcp(8, 9).Len(major, 6), After, Text: _ => "Inviterer med 6 kort");
            yield return new($"tr-{major}-2nt", S, _ => C("2N"), _ => Shows.Hcp(8, 9).Len(major, 5, 5), After, Text: _ => "Inviterer med 5 kort");
            yield return new($"tr-{major}-pass", S, _ => Call.Pass, _ => Shows.Hcp(0, 7), After, Text: _ => "Svag hånd: spiller 2 i højfarven");
        }
    }

    // ---------------------------------------------------------------- competition

    private static IEnumerable<BidRule> Competition()
    {
        // Overcalls and the takeout double.
        const Situation O = Situation.Overcall;
        bool OneLevelOpening(BidContext c) => c.LastBid is { Call.Level: <= 2 } b && b.Seat != c.Partner && c.Opening?.Suit is not null;
        yield return new("ov-1nt", O, _ => C("1N"), _ => Shows.Hcp(15, 18).Bal(), c => c.Opening?.Level == 1 && c.Opening?.Suit is not null,
            Text: _ => "Indmelding 1UT");
        foreach (var suit in AllSuits)
        {
            yield return new($"ov-1{suit}", O, c => c.Cheapest(suit.ToStrain(), 1), _ => Shows.Hcp(8, 16).Len(suit, 5),
                c => OneLevelOpening(c) && !c.OpponentSuits.Contains(suit), When: (_, h) => Longest(h) == suit, Text: _ => "Indmelding");
            yield return new($"ov-2{suit}", O, c => c.Cheapest(suit.ToStrain(), 2), _ => Shows.Hcp(11, 16).Len(suit, 5),
                c => OneLevelOpening(c) && !c.OpponentSuits.Contains(suit), When: (_, h) => Longest(h) == suit, Text: _ => "Indmelding på 2-trinnet");
        }
        yield return new("ov-double", O, _ => Call.Double, c => TakeoutShows(c), OneLevelOpening,
            When: (c, h) => c.OpponentSuits.All(s => h.Length(s) <= 2),
            Text: _ => "Oplysningsdobling: åbningshånd og støtte til de umeldte farver. Makker skal melde");
        yield return new("ov-pass", O, _ => Call.Pass, _ => Shows.Anything, Text: _ => "Pas");

        // Answering partner's takeout double: the best unbid suit.
        const Situation A = Situation.Advance;
        bool Doubled(BidContext c) => c.Partners[0].Kind == CallKind.Double;
        IEnumerable<Suit> Unbid(BidContext c) => AllSuits.Where(s => !c.OpponentSuits.Contains(s));
        foreach (var suit in AllSuits)
        {
            bool Best(BidContext c, Hand h) => Unbid(c).Contains(suit) && Longest(h, Unbid(c).OrderByDescending(IsMajor)) == suit;
            yield return new($"adv-x-game{suit}", A, _ => IsMajor(suit) ? Call.Bid(4, suit) : null, _ => Shows.Hcp(12).Len(suit, 4), Doubled, Best, _ => "Udgang efter makkers dobling");
            yield return new($"adv-x-jump{suit}", A, c => c.Jump(suit.ToStrain()) is { Level: <= 3 } j ? j : null, _ => Shows.Hcp(9, 11).Len(suit, 4),
                c => Doubled(c) && !c.RightHandActed, Best, _ => "Spring: 9–11 HP");
            yield return new($"adv-x-{suit}", A, c => c.Cheapest(suit.ToStrain(), 3), c => c.RightHandActed ? Shows.Hcp(6, 11) : Shows.Hcp(0, 8), Doubled, Best,
                _ => "Svar på makkers oplysningsdobling: bedste umeldte farve");
        }
        yield return new("adv-x-3nt", A, _ => C("3N"), _ => Shows.Hcp(12), Doubled, Text: _ => "Udgang");

        // Supporting partner's overcall.
        bool Overcalled(BidContext c) => c.Partners[0].IsBid && c.PartnerSuit is not null;
        Suit Ps(BidContext c) => c.PartnerSuit!.Value;
        yield return new("adv-ov-game", A, c => IsMajor(Ps(c)) ? Call.Bid(4, Ps(c)) : null, c => Shows.Hcp(13).Len(Ps(c), 3), Overcalled, Text: _ => "Støtte og udgang");
        yield return new("adv-ov-jump", A, c => c.Jump(Ps(c).ToStrain()) is { Level: <= 3 } j ? j : null, c => Shows.Hcp(11, 12).Len(Ps(c), 3), Overcalled, Text: _ => "Støtte. Inviterer");
        yield return new("adv-ov-raise", A, c => c.Cheapest(Ps(c).ToStrain(), 3), c => Shows.Hcp(7, 10).Len(Ps(c), 3), Overcalled, Text: _ => "Støtte");
        yield return new("adv-pass", A, _ => Call.Pass, _ => Shows.Anything, c => !Doubled(c) || c.RightHandActed, Text: _ => "Pas");
    }

    private static Shows TakeoutShows(BidContext c)
    {
        var s = Shows.Hcp(12);
        foreach (var suit in AllSuits.Where(x => !c.OpponentSuits.Contains(x))) s = s.Len(suit, 3);
        return s;
    }
}

public static class SuitExtensions
{
    public static Strain ToStrain(this Suit suit) => (Strain)suit;
}
