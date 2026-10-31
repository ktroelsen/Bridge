using System.Collections.Concurrent;
using Bridge.Engine.Bidding;

namespace Bridge.Engine;

public enum BotLevel { Easy, Medium, Hard }

/// <summary>
/// The card-play bot at three levels. Easy is <see cref="EasyBot"/>'s rules of thumb. Medium and
/// Hard sample layouts of the hidden cards that fit what the bot knows (its own hand, the dummy,
/// the played cards, who has shown out of a suit and what the bidding showed), solve each layout
/// double dummy and play the card that most often makes (or beats) the contract.
///
/// Games are replayed on every request, so a bot must always choose the same card in the same
/// position: the samples are seeded by the position and each solve is limited by a node count,
/// never by time. Decisions are cached so a replay doesn't redo the work.
/// </summary>
public static class PlayBot
{
    private sealed record Settings(int FromTrick, int Samples, long NodesPerSample);

    private static readonly Dictionary<BotLevel, Settings> Levels = new()
    {
        [BotLevel.Medium] = new(4, 8, 150_000),
        [BotLevel.Hard] = new(1, 20, 400_000),
    };

    private static readonly ConcurrentDictionary<string, Card> Cache = new();
    private const int MaxCache = 100_000;

    /// <summary>Forgets cached decisions (for tests: the bot must decide the same without the cache).</summary>
    public static void ClearCache() => Cache.Clear();

    /// <summary>
    /// The card for <paramref name="seat"/> (the player to move). <paramref name="shown"/> gives what
    /// each player's calls (or announced points) showed about their hand.
    /// </summary>
    public static Card Choose(CardPlay play, Seat seat, BotLevel level, Func<Seat, Shows>? shown = null)
    {
        var view = play.ViewFor(seat);
        var easy = EasyBot.Choose(view);
        if (level == BotLevel.Easy || view.Legal.Count == 1 || play.Played.Count == 0) return easy;
        var settings = Levels[level];
        if (play.Tricks.Count < settings.FromTrick) return easy;

        var key = Key(play, level);
        if (Cache.TryGetValue(key, out var cached)) return cached;
        var card = MonteCarlo(play, view, settings, shown, easy);
        if (Cache.Count > MaxCache) Cache.Clear();
        Cache[key] = card;
        return card;
    }

    private static string Key(CardPlay play, BotLevel level) =>
        $"{level}|{string.Join(" ", Enum.GetValues<Seat>().Select(s => string.Concat(play.Deal[s].Cards)))}|{play.Contract}|" +
        string.Concat(play.Tricks.SelectMany(t => t.Plays).Select(p => p.Card));

    private static Card MonteCarlo(CardPlay play, PlayView view, Settings settings, Func<Seat, Shows>? shown, Card easy)
    {
        var seat = view.Seat;
        var ns = seat.IsNorthSouth();
        var contract = play.Contract;
        var declaringSide = contract.Declarer.IsNorthSouth() == ns;
        var goal = declaringSide ? contract.TricksNeeded : 14 - contract.TricksNeeded;
        var wonSoFar = play.TricksWon(ns);
        var remaining = 13 - play.Tricks.Count(t => t.IsComplete);

        var legal = view.Legal;
        var made = new Dictionary<Card, int>();
        var tricks = new Dictionary<Card, int>();
        foreach (var c in legal) (made[c], tricks[c]) = (0, 0);

        // Deterministic: the same position gives the same samples.
        var random = new Random(StableSeed(Key(play, BotLevel.Hard)));
        var solved = 0;
        for (var i = 0; i < settings.Samples; i++)
        {
            if (Sample(play, view, shown, random) is not { } layout) continue;
            var position = Replay(layout, play);
            var solver = new Analysis.DoubleDummy(position) { MaxNodes = settings.NodesPerSample };
            IReadOnlyDictionary<Card, int> values;
            try
            {
                values = solver.CardValues();
            }
            catch (TimeoutException)
            {
                continue;
            }
            solved++;
            foreach (var (card, nsTricks) in values)
            {
                if (!made.ContainsKey(card)) continue;
                var side = wonSoFar + (ns ? nsTricks : remaining - nsTricks);
                tricks[card] += side;
                if (side >= goal) made[card]++;
            }
        }
        if (solved == 0) return easy;

        // Most often makes (or beats) the contract, then most tricks; the easy bot's card wins ties.
        var best = legal.OrderByDescending(c => made[c]).ThenByDescending(c => tricks[c]).First();
        return made[easy] == made[best] && tricks[easy] == tricks[best] ? easy : best;
    }

    private static int StableSeed(string text)
    {
        // string.GetHashCode is randomised per process; this one is stable.
        unchecked
        {
            var h = 17;
            foreach (var ch in text) h = h * 31 + ch;
            return h;
        }
    }

    /// <summary>The same play with the sampled hands: deal them out and replay the played cards.</summary>
    private static CardPlay Replay(Hand[] layout, CardPlay play)
    {
        var deal = Deal.FromHands(layout, play.Deal.Board);
        var copy = new CardPlay(deal, play.Contract);
        foreach (var p in play.Tricks.SelectMany(t => t.Plays)) copy.Play(p.Card);
        return copy;
    }

    /// <summary>
    /// A layout of all four original hands that fits what the player knows, or null if none was
    /// found. Bidding constraints are dropped if they can't be met.
    /// </summary>
    private static Hand[]? Sample(CardPlay play, PlayView view, Func<Seat, Shows>? shown, Random random)
    {
        var plays = play.Tricks.SelectMany(t => t.Plays).ToList();
        var playedBy = Enum.GetValues<Seat>().ToDictionary(s => s, s => plays.Where(p => p.Seat == s).Select(p => p.Card).ToList());
        var hidden = Enum.GetValues<Seat>().Where(s => !view.Visible.ContainsKey(s)).ToList();

        // Who has shown out of a suit.
        var voids = hidden.ToDictionary(s => s, _ => new HashSet<Suit>());
        foreach (var trick in play.Tricks.Where(t => t.Plays.Count > 0))
        {
            var led = trick.Plays[0].Card.Suit;
            foreach (var p in trick.Plays.Skip(1))
                if (p.Card.Suit != led && voids.TryGetValue(p.Seat, out var v)) v.Add(led);
        }

        var known = new HashSet<Card>(play.Played);
        foreach (var cards in view.Visible.Values) known.UnionWith(cards);
        var unknown = Card.Deck.Where(c => !known.Contains(c)).ToList();
        var need = hidden.ToDictionary(s => s, s => 13 - playedBy[s].Count);

        for (var attempt = 0; attempt < 400; attempt++)
        {
            var useBidding = attempt < 300;
            var deck = unknown.OrderBy(_ => random.Next()).ToList();
            var dealt = hidden.ToDictionary(s => s, _ => new List<Card>());
            var ok = true;
            foreach (var card in deck)
            {
                var open = hidden.Where(s => dealt[s].Count < need[s] && !voids[s].Contains(card.Suit)).ToList();
                if (open.Count == 0)
                {
                    ok = false;
                    break;
                }
                // Weighted by the room left, so both hands fill evenly.
                var room = open.Sum(s => need[s] - dealt[s].Count);
                var pick = random.Next(room);
                var to = open.First(s => (pick -= need[s] - dealt[s].Count) < 0);
                dealt[to].Add(card);
            }
            if (!ok) continue;

            var hands = Enum.GetValues<Seat>().Select(s =>
                new Hand(hidden.Contains(s) ? dealt[s].Concat(playedBy[s]) : view.Visible[s].Concat(playedBy[s]))).ToArray();
            if (useBidding && shown is not null && hidden.Any(s => !shown(s).Matches(hands[(int)s]))) continue;
            return hands;
        }
        return null;
    }
}
