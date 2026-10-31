using System.Numerics;

namespace Bridge.Engine.Analysis;

/// <summary>
/// Double dummy solver: how many tricks each side takes when all four hands are known and
/// everybody plays perfectly. Alpha-beta search on "can North-South take at least n tricks?",
/// with three standard tricks: cards that are equal in value (e.g. K-Q in the same hand, or K-J
/// once the queen is gone) are searched once, positions at the start of a trick are cached by
/// their relative ranks, and bounds stop the search as soon as the answer is clear.
/// </summary>
public sealed class DoubleDummy
{
    private const int SuitBits = 16;
    private const ulong SuitMask = 0x1FFF;

    // Hands as 64-bit masks: suit s uses bits 16s..16s+12, bit r is rank r+2 (two = 0, ace = 12).
    private readonly ulong[] _hands = new ulong[4];
    private readonly int _trump; // -1 for no trump
    // The current trick.
    private int _leader;
    private int _count;
    private readonly int[] _trick = new int[4];
    // Moves per search depth (at most 52 cards, 13 moves per node).
    private readonly int[][] _moves = [.. Enumerable.Range(0, 53).Select(_ => new int[13])];
    // Partition search cache: positions at the start of a trick, grouped by leader and suit lengths.
    // An entry holds a bound and which top cards it depends on; any position with the same lengths
    // and the same owners of those top cards has the same bound.
    private readonly Dictionary<(ulong Lengths, int Leader), Bucket> _cache = [];
    private int _entries;
    private const int MaxEntries = 3_000_000;

    private record struct Entry(ulong A, ulong B, sbyte Bound, bool Lower);

    /// <summary>The entries for one leader and set of suit lengths; the same pattern is stored once with its best bound.</summary>
    private sealed class Bucket
    {
        public readonly List<Entry> Entries = [];
        public readonly Dictionary<(ulong, ulong, bool), int> Index = [];
    }
    // Moves that caused a cutoff before, per seat: tried first next time.
    private readonly int[,] _history = new int[4, 64];

    public long Nodes { get; private set; }

    /// <summary>When set, the search gives up with a <see cref="TimeoutException"/> after this time.</summary>
    public DateTime? Deadline { get; set; }

    /// <summary>A solver for the position in a card play (the current trick may be part played).</summary>
    public DoubleDummy(CardPlay play)
    {
        _trump = play.Trump is { } t ? (int)t : -1;
        SetPosition(play);
    }

    /// <summary>
    /// Moves the solver to another position of the same deal and contract. The cache is kept:
    /// what was learned about one position of a deal helps with the others.
    /// </summary>
    public void SetPosition(CardPlay play)
    {
        if ((play.Trump is { } t ? (int)t : -1) != _trump) throw new ArgumentException("Samme trumf kræves.", nameof(play));
        Array.Clear(_hands);
        foreach (var seat in Enum.GetValues<Seat>())
            foreach (var card in play.Remaining(seat))
                _hands[(int)seat] |= Bit(Code(card));
        var trick = play.CurrentTrick;
        _leader = (int)trick.Leader;
        _count = trick.IsComplete ? 0 : trick.Plays.Count;
        if (trick.IsComplete) _leader = (int)play.NextToPlay;
        for (var i = 0; i < _count; i++) _trick[i] = Code(trick.Plays[i].Card);
    }

    private static int Code(Card card) => (int)card.Suit * SuitBits + (int)card.Rank - 2;

    private static Card FromCode(int code) => new((Suit)(code / SuitBits), (Rank)(code % SuitBits + 2));

    private static ulong Bit(int code) => 1UL << code;

    private int ToPlay => (_leader + _count) & 3;

    private static bool IsNs(int seat) => (seat & 1) == 0;

    private int RemainingTricks => (BitOperations.PopCount(_hands[0] | _hands[1] | _hands[2] | _hands[3]) + _count + 3) / 4;

    /// <summary>Tricks North-South take from here on (counting the current trick).</summary>
    public int NorthSouthTricks()
    {
        int lo = 0, hi = RemainingTricks;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (Search(mid, 0, out _)) lo = mid;
            else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>
    /// For each card the player to move may play: the tricks North-South take from here if they
    /// play it (cards of equal value give the same number).
    /// </summary>
    public IReadOnlyDictionary<Card, int> CardValues()
    {
        var seat = ToPlay;
        var result = new Dictionary<Card, int>();
        var legal = LegalMask(seat);
        var count = Generate(seat, 52);
        var moves = _moves[52].Take(count).ToArray();
        foreach (var move in moves)
        {
            var won = Play(move, out var undo, out _);
            var value = won + NorthSouthTricks();
            Unplay(move, undo);
            // Equal cards (same suit, nothing in between that someone else holds) get the same value.
            foreach (var code in Equivalents(seat, move, legal)) result[FromCode(code)] = value;
        }
        return result;
    }

    private IEnumerable<int> Equivalents(int seat, int move, ulong legal)
    {
        yield return move;
        var suit = move / SuitBits;
        var present = AllPresent();
        for (var code = move - 1; code >= suit * SuitBits; code--)
        {
            var b = Bit(code);
            if ((legal & b) != 0) yield return code;
            else if ((present & b) != 0) yield break;
        }
    }

    private ulong AllPresent()
    {
        var present = _hands[0] | _hands[1] | _hands[2] | _hands[3];
        for (var i = 0; i < _count; i++) present |= Bit(_trick[i]);
        return present;
    }

    private ulong LegalMask(int seat)
    {
        var hand = _hands[seat];
        if (_count == 0) return hand;
        var led = _trick[0] / SuitBits;
        var follow = hand & (SuitMask << (led * SuitBits));
        return follow != 0 ? follow : hand;
    }

    /// <summary>
    /// Can North-South take at least <paramref name="needed"/> of the remaining tricks?
    /// <paramref name="win"/> gets the cards whose rank decided the answer (the "winning ranks").
    /// </summary>
    private bool Search(int needed, int depth, out ulong win)
    {
        if ((++Nodes & 0xFFF) == 0 && Deadline is { } deadline && DateTime.UtcNow > deadline)
            throw new TimeoutException("Analysen tog for lang tid.");
        win = 0;
        if (needed <= 0) return true;
        var remaining = RemainingTricks;
        if (needed > remaining) return false;

        var atStart = _count == 0;
        ulong lengths = 0;
        if (atStart)
        {
            lengths = Lengths();
            if (Lookup(lengths, needed, out var hit, out win)) return hit;
            // Sure tricks for the side on lead give a bound without searching.
            var quick = QuickTricks(out var quickWin);
            if (IsNs(_leader))
            {
                if (quick >= needed)
                {
                    win = quickWin;
                    return true;
                }
            }
            else if (needed > remaining - quick)
            {
                win = quickWin;
                return false;
            }
        }

        var seat = ToPlay;
        var ns = IsNs(seat);
        var count = Generate(seat, depth);
        var moves = _moves[depth];
        var result = !ns;
        for (var i = 0; i < count; i++)
        {
            var move = moves[i];
            var won = Play(move, out var undo, out var trickWin);
            var ok = Search(needed - won, depth + 1, out var childWin);
            Unplay(move, undo);
            childWin |= trickWin;
            if (ok == ns)
            {
                result = ns;
                win = childWin;
                _history[seat, move] += remaining * remaining + 1;
                break;
            }
            win |= childWin;
        }

        if (atStart) Store(lengths, result ? needed : needed - 1, result, win);
        return result;
    }

    private ulong Lengths()
    {
        ulong key = 0;
        for (var seat = 0; seat < 4; seat++)
            for (var suit = 0; suit < 4; suit++)
                key = (key << 4) | (uint)BitOperations.PopCount((_hands[seat] >> (suit * SuitBits)) & SuitMask);
        return key;
    }

    /// <summary>Who holds each card left in a suit, highest first, 2 bits per card; and how many there are.</summary>
    private (ulong Owners, int Count) Owners(int suit)
    {
        ulong h0 = _hands[0], h1 = _hands[1], h2 = _hands[2], h3 = _hands[3];
        var bits = ((h0 | h1 | h2 | h3) >> (suit * SuitBits)) & SuitMask;
        ulong owners = 0;
        var n = 0;
        while (bits != 0)
        {
            var r = 63 - BitOperations.LeadingZeroCount(bits);
            bits &= ~(1UL << r);
            var bit = 1UL << (suit * SuitBits + r);
            owners = (owners << 2) | (uint)((h0 & bit) != 0 ? 0 : (h1 & bit) != 0 ? 1 : (h2 & bit) != 0 ? 2 : 3);
            n++;
        }
        return (owners, n);
    }

    /// <summary>The k highest cards left in a suit, as a mask.</summary>
    private ulong TopCards(int suit, int k)
    {
        var bits = ((_hands[0] | _hands[1] | _hands[2] | _hands[3]) >> (suit * SuitBits)) & SuitMask;
        ulong mask = 0;
        while (k-- > 0 && bits != 0)
        {
            var r = 63 - BitOperations.LeadingZeroCount(bits);
            bits &= ~(1UL << r);
            mask |= 1UL << (suit * SuitBits + r);
        }
        return mask;
    }

    /// <summary>Packs, per suit, how many top cards matter and who holds them.</summary>
    private (ulong A, ulong B) Pack(ulong win)
    {
        ulong a = 0, b = 0;
        for (var suit = 0; suit < 4; suit++)
        {
            var (owners, n) = Owners(suit);
            var winBits = (win >> (suit * SuitBits)) & SuitMask;
            var k = 0;
            if (winBits != 0)
            {
                var lowest = BitOperations.TrailingZeroCount(winBits);
                var all = ((_hands[0] | _hands[1] | _hands[2] | _hands[3]) >> (suit * SuitBits)) & SuitMask;
                k = BitOperations.PopCount(all >> lowest);
            }
            var top = k == 0 ? 0UL : owners >> (2 * (n - k));
            var part = (uint)k | (top << 4);
            if (suit < 2) a |= part << (suit * 30);
            else b |= part << ((suit - 2) * 30);
        }
        return (a, b);
    }

    private bool Lookup(ulong lengths, int needed, out bool result, out ulong win)
    {
        result = false;
        win = 0;
        if (!_cache.TryGetValue((lengths, _leader), out var bucket)) return false;
        var list = bucket.Entries;
        Span<ulong> owners = stackalloc ulong[4];
        Span<int> counts = stackalloc int[4];
        for (var suit = 0; suit < 4; suit++) (owners[suit], counts[suit]) = Owners(suit);
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            if (e.Lower ? e.Bound < needed : e.Bound >= needed) continue;
            var match = true;
            for (var suit = 0; suit < 4 && match; suit++)
            {
                var part = (suit < 2 ? e.A >> (suit * 30) : e.B >> ((suit - 2) * 30)) & 0x3FFFFFFF;
                var k = (int)(part & 0xF);
                if (k == 0) continue;
                match = k <= counts[suit] && (owners[suit] >> (2 * (counts[suit] - k))) == part >> 4;
            }
            if (!match) continue;
            result = e.Lower;
            for (var suit = 0; suit < 4; suit++)
            {
                var part = (suit < 2 ? e.A >> (suit * 30) : e.B >> ((suit - 2) * 30)) & 0x3FFFFFFF;
                win |= TopCards(suit, (int)(part & 0xF));
            }
            return true;
        }
        return false;
    }

    private void Store(ulong lengths, int bound, bool lower, ulong win)
    {
        if (_entries >= MaxEntries)
        {
            _cache.Clear();
            _entries = 0;
        }
        var key = (lengths, _leader);
        if (!_cache.TryGetValue(key, out var bucket)) _cache[key] = bucket = new Bucket();
        var (a, b) = Pack(win);
        if (bucket.Index.TryGetValue((a, b, lower), out var at))
        {
            // Keep the stronger bound for the same pattern.
            var e = bucket.Entries[at];
            if (lower ? bound > e.Bound : bound < e.Bound) bucket.Entries[at] = e with { Bound = (sbyte)bound };
            return;
        }
        bucket.Index[(a, b, lower)] = bucket.Entries.Count;
        bucket.Entries.Add(new Entry(a, b, (sbyte)bound, lower));
        _entries++;
    }

    /// <summary>
    /// Tricks the player on lead can take at once by playing their top cards from the top, keeping
    /// the lead. In a trump contract a side suit only counts while both opponents can follow (or
    /// have no trumps left).
    /// </summary>
    private int QuickTricks(out ulong win)
    {
        win = 0;
        var me = _leader;
        ulong lho = _hands[(me + 1) & 3], rho = _hands[(me + 3) & 3], partner = _hands[(me + 2) & 3];
        var others = lho | rho | partner;
        var oppTrumps = _trump >= 0 && (((lho | rho) >> (_trump * SuitBits)) & SuitMask) != 0;
        var total = 0;
        for (var suit = 0; suit < 4; suit++)
        {
            var mine = (_hands[me] >> (suit * SuitBits)) & SuitMask;
            if (mine == 0) continue;
            var rest = (others >> (suit * SuitBits)) & SuitMask;
            // Count my cards from the top that are higher than every card the others hold.
            var top = 0;
            for (var r = 12; r >= 0; r--)
            {
                var bit = 1UL << r;
                if ((mine & bit) != 0) top++;
                else if ((rest & bit) != 0) break;
            }
            if (top == 0) continue;
            win |= TopCards(suit, top);
            if (_trump >= 0 && suit != _trump && oppTrumps)
            {
                var lhoLen = BitOperations.PopCount((lho >> (suit * SuitBits)) & SuitMask);
                var rhoLen = BitOperations.PopCount((rho >> (suit * SuitBits)) & SuitMask);
                top = Math.Min(top, Math.Min(lhoLen, rhoLen));
            }
            total += top;
        }
        return Math.Min(total, RemainingTricks);
    }

    /// <summary>Plays a card; returns 1 if it completed a trick that North-South won.</summary>
    private int Play(int code, out Undo undo, out ulong trickWin)
    {
        undo = new Undo(_leader, _count, _trick[0], _trick[1], _trick[2]);
        trickWin = 0;
        var seat = ToPlay;
        _hands[seat] &= ~Bit(code);
        _trick[_count++] = code;
        if (_count < 4) return 0;
        var index = WinningIndex();
        // The winning card's rank mattered if it beat another card of its suit.
        var winCard = _trick[index];
        for (var i = 0; i < 4; i++)
            if (i != index && _trick[i] / SuitBits == winCard / SuitBits) trickWin = Bit(winCard);
        var winner = (_leader + index) & 3;
        _leader = winner;
        _count = 0;
        return IsNs(winner) ? 1 : 0;
    }

    /// <summary>What a played card changed: the trick's leader and size, and its first cards (a later trick reuses the array).</summary>
    private readonly record struct Undo(int Leader, int Count, int T0, int T1, int T2);

    private void Unplay(int code, Undo undo)
    {
        _leader = undo.Leader;
        _count = undo.Count;
        _trick[0] = undo.T0;
        _trick[1] = undo.T1;
        _trick[2] = undo.T2;
        _hands[ToPlay] |= Bit(code);
    }

    private int WinningIndex()
    {
        var best = 0;
        for (var i = 1; i < 4; i++)
        {
            int a = _trick[i], w = _trick[best];
            int sa = a / SuitBits, sw = w / SuitBits;
            if (sa == sw ? a > w : sa == _trump) best = i;
        }
        return best;
    }

    /// <summary>The distinct moves (one per group of equal cards), best guesses first.</summary>
    private int Generate(int seat, int depth)
    {
        var legal = LegalMask(seat);
        var present = AllPresent();
        var moves = _moves[depth];
        var n = 0;
        Span<int> scores = stackalloc int[13];

        // The card currently winning the trick, if any.
        var winning = -1;
        var partnerWinning = false;
        if (_count > 0)
        {
            var index = WinningIndex();
            winning = _trick[index];
            var winner = (_leader + index) & 3;
            partnerWinning = winner != seat && (winner & 1) == (seat & 1);
        }

        for (var suit = 3; suit >= 0; suit--)
        {
            var bits = (legal >> (suit * SuitBits)) & SuitMask;
            if (bits == 0) continue;
            var others = ((present & ~_hands[seat]) >> (suit * SuitBits)) & SuitMask;
            var prev = -1;
            for (var r = 12; r >= 0; r--)
            {
                if ((bits & (1UL << r)) == 0) continue;
                // Equal to the card just above it in this hand when nobody else holds a card in between.
                if (prev >= 0 && (others & (((1UL << prev) - 1) & ~((1UL << (r + 1)) - 1))) == 0)
                {
                    prev = r;
                    continue;
                }
                prev = r;
                var code = suit * SuitBits + r;
                moves[n] = code;
                scores[n] = Score(code, winning, partnerWinning, suit, seat) * 1000 + Math.Min(_history[seat, code], 999);
                n++;
            }
        }

        // Insertion sort by score, highest first.
        for (var i = 1; i < n; i++)
        {
            int m = moves[i], s = scores[i], j = i - 1;
            while (j >= 0 && scores[j] < s)
            {
                moves[j + 1] = moves[j];
                scores[j + 1] = scores[j];
                j--;
            }
            moves[j + 1] = m;
            scores[j + 1] = s;
        }
        return n;
    }

    private int Score(int code, int winning, bool partnerWinning, int suit, int seat)
    {
        var rank = code % SuitBits;
        if (winning < 0) return rank + (suit == _trump ? -5 : 0); // leading: high cards first
        var beats = suit == winning / SuitBits ? code > winning : suit == _trump && winning / SuitBits != _trump;
        if (partnerWinning) return beats ? -rank - 20 : 20 - rank;   // partner has it: play low
        return beats ? 40 - rank : -rank;                              // win as cheaply as possible, else play low
    }

}
