namespace Bridge.Engine;

/// <summary>
/// SplitMix64: tiny, fast and fully specified, so a seed gives the same numbers on every
/// platform and .NET version (System.Random makes no such promise). Deal codes depend on it.
/// </summary>
public sealed class Rng(ulong seed)
{
    private ulong _state = seed;

    public ulong Next()
    {
        var z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>An unbiased number in [0, n) (rejection sampling).</summary>
    public int NextBelow(int n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        var limit = ulong.MaxValue - ulong.MaxValue % (ulong)n;
        while (true)
        {
            var value = Next();
            if (value < limit) return (int)(value % (ulong)n);
        }
    }
}
