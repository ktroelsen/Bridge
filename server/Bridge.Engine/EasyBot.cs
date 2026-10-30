namespace Bridge.Engine;

/// <summary>
/// The easy card-play bot: the classic beginner rules. Second hand low, third hand high, win as
/// cheaply as possible, don't overtake partner, ruff when you can't follow. As declarer it draws
/// trumps and cashes its winners. It decides from a <see cref="PlayView"/> only.
/// </summary>
public static class EasyBot
{
    public static Card Choose(PlayView view)
    {
        var legal = view.Legal;
        if (legal.Count == 0) throw new InvalidOperationException("Ingen lovlige kort.");
        if (legal.Count == 1) return legal[0];

        var trick = view.CurrentTrick;
        if (trick.Plays.Count == 0)
            return view.IsDeclarerSide ? DeclarerLead(view) : DefenderLead(view);

        var led = trick.LedSuit!.Value;
        var winning = trick.Winning(view.Trump)!.Value;
        var partnerWinning = winning.Seat == view.Seat.Partner();
        var last = trick.Plays.Count == 3;
        var partnerSafe = partnerWinning && (last || IsMaster(view, winning.Card) || winning.Card.Suit == view.Trump && led != view.Trump);

        if (legal[0].Suit == led)
        {
            if (partnerSafe) return Lowest(legal);
            var winners = legal.Where(c => Trick.Beats(c, winning.Card, view.Trump)).ToList();
            // Second hand low (unless it can take a sure trick with a winner of its own).
            if (trick.Plays.Count == 1)
            {
                var master = winners.FirstOrDefault(c => IsMaster(view, c));
                return master != default && view.Trump != null ? master : Lowest(legal);
            }
            if (winners.Count == 0) return Lowest(legal);
            // Third hand high (the lowest of touching cards); fourth hand as cheap as possible.
            return last ? Lowest(winners) : LowestEquivalent(view, Highest(winners));
        }

        // Can't follow: ruff if it helps, otherwise throw a small card.
        if (!partnerSafe && view.Trump is { } trump)
        {
            var ruffs = legal.Where(c => c.Suit == trump && Trick.Beats(c, winning.Card, trump)).ToList();
            if (ruffs.Count > 0) return Lowest(ruffs);
        }
        return Discard(view);
    }

    private static Card DefenderLead(PlayView view)
    {
        var hand = view.Hand;
        var trump = view.Trump;

        // Return partner's suit: the suit partner led most recently.
        var partnerSuit = view.Tricks
            .Where(t => t.Plays.Count > 0 && t.Leader == view.Seat.Partner())
            .Select(t => (Suit?)t.LedSuit).LastOrDefault();

        // After the opening lead: cash a sure winner in a side suit.
        if (view.Played.Count > 0)
        {
            var cash = hand.Where(c => c.Suit != trump && IsMaster(view, c)).OrderByDescending(c => c.Rank).FirstOrDefault();
            if (cash != default) return cash;
            if (partnerSuit is { } ps && hand.Any(c => c.Suit == ps)) return LeadFromSuit(view, ps);
        }

        var suits = hand.Select(c => c.Suit).Distinct().Where(s => s != trump).ToList();
        if (suits.Count == 0) return Lowest(hand);
        var best = suits.OrderByDescending(s => LeadScore(view, s)).First();
        return LeadFromSuit(view, best);
    }

    /// <summary>How attractive a suit is to lead from (higher is better).</summary>
    private static int LeadScore(PlayView view, Suit suit)
    {
        var cards = view.Hand.Where(c => c.Suit == suit).OrderByDescending(c => c.Rank).ToList();
        var score = 0;
        if (HasSequence(cards)) score += 6;
        if (view.Trump == null) score += 2 * cards.Count;
        else
        {
            if (cards.Count == 1 && view.Hand.Any(c => c.Suit == view.Trump)) score += 3;
            if (cards[0].Rank == Rank.Ace && (cards.Count < 2 || cards[1].Rank != Rank.King)) score -= 4;
        }
        // Don't lead away from a lone king or queen.
        if (cards[0].Rank is Rank.King or Rank.Queen && !HasSequence(cards) && cards.Count <= 3) score -= 3;
        return score;
    }

    /// <summary>Top of a sequence, 4th best from four or more, top from two, low from three with an honour.</summary>
    private static Card LeadFromSuit(PlayView view, Suit suit)
    {
        var cards = view.Hand.Where(c => c.Suit == suit).OrderByDescending(c => c.Rank).ToList();
        if (HasSequence(cards) || cards.Count <= 2) return cards[0];
        if (cards.Count >= 4) return cards[3];
        return cards[0].Rank >= Rank.Jack ? cards[^1] : cards[0];
    }

    /// <summary>Two touching cards at the top, the higher one ten or better (AK, KQ, QJ, JT).</summary>
    private static bool HasSequence(List<Card> cardsHighFirst) =>
        cardsHighFirst.Count >= 2 && cardsHighFirst[0].Rank >= Rank.Ten && cardsHighFirst[0].Rank - cardsHighFirst[1].Rank == 1;

    private static Card DeclarerLead(PlayView view)
    {
        var hand = view.Hand;
        var ours = view.OwnSideCards.ToList();

        // Draw trumps while the opponents may have some and we hold the top trump.
        if (view.Trump is { } trump)
        {
            var theirTrumps = 13 - ours.Count(c => c.Suit == trump) - view.Played.Count(c => c.Suit == trump);
            var topTrump = ours.Where(c => c.Suit == trump).OrderByDescending(c => c.Rank).FirstOrDefault();
            if (theirTrumps > 0 && topTrump != default && IsMaster(view, topTrump) && hand.Any(c => c.Suit == trump))
                return hand.Contains(topTrump) ? topTrump : Lowest(hand.Where(c => c.Suit == trump));
        }

        // Cash a winner from this hand.
        var cash = hand.Where(c => IsMaster(view, c)).OrderBy(c => c.Suit == view.Trump).ThenByDescending(c => c.Rank).FirstOrDefault();
        if (cash != default) return cash;

        // Otherwise lead low in our longest side suit, towards partner's high cards.
        var suit = hand.Select(c => c.Suit).Distinct()
            .OrderBy(s => s == view.Trump)
            .ThenByDescending(s => ours.Count(c => c.Suit == s))
            .First();
        return Lowest(hand.Where(c => c.Suit == suit));
    }

    /// <summary>Throw the smallest card from the suit we care least about.</summary>
    private static Card Discard(PlayView view)
    {
        var legal = view.Legal;
        var pool = legal.Where(c => c.Suit != view.Trump).ToList();
        if (pool.Count == 0) pool = [.. legal];
        return pool
            .OrderBy(c => pool.Count(o => o.Suit == c.Suit && IsMaster(view, o)))   // keep suits with winners
            .ThenBy(c => c.Rank)
            .ThenByDescending(c => pool.Count(o => o.Suit == c.Suit))
            .First();
    }

    /// <summary>
    /// Whether a card is the highest left in its suit, counting cards our side holds (that this
    /// player can see) as safe.
    /// </summary>
    internal static bool IsMaster(PlayView view, Card card)
    {
        var ours = view.OwnSideCards.ToHashSet();
        for (var r = card.Rank + 1; r <= Rank.Ace; r++)
        {
            var higher = new Card(card.Suit, r);
            if (!view.Played.Contains(higher) && !ours.Contains(higher)) return false;
        }
        return true;
    }

    /// <summary>The lowest card in hand worth the same as <paramref name="card"/> (no unplayed card between them).</summary>
    private static Card LowestEquivalent(PlayView view, Card card)
    {
        var result = card;
        for (var r = card.Rank - 1; r >= Rank.Two; r--)
        {
            var lower = new Card(card.Suit, r);
            if (view.Hand.Contains(lower)) result = lower;
            else if (!view.Played.Contains(lower)) break;
        }
        return result;
    }

    private static Card Lowest(IEnumerable<Card> cards) => cards.OrderBy(c => c.Rank).First();

    private static Card Highest(IEnumerable<Card> cards) => cards.OrderByDescending(c => c.Rank).First();
}
