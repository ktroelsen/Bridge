namespace Bridge.Engine;

public readonly record struct PlayedCard(Seat Seat, Card Card);

/// <summary>One trick: up to four cards, starting with the leader and going clockwise.</summary>
public sealed class Trick(Seat leader)
{
    private readonly List<PlayedCard> _plays = [];

    public Seat Leader { get; } = leader;

    public IReadOnlyList<PlayedCard> Plays => _plays;

    public bool IsComplete => _plays.Count == 4;

    public Suit? LedSuit => _plays.Count > 0 ? _plays[0].Card.Suit : null;

    public Seat NextSeat => (Seat)(((int)Leader + _plays.Count) % 4);

    internal void Add(Card card) => _plays.Add(new PlayedCard(NextSeat, card));

    /// <summary>The card currently winning: the highest trump, otherwise the highest card in the led suit.</summary>
    public PlayedCard? Winning(Suit? trump)
    {
        if (_plays.Count == 0) return null;
        var best = _plays[0];
        foreach (var play in _plays.Skip(1))
            if (Beats(play.Card, best.Card, trump))
                best = play;
        return best;
    }

    /// <summary>Whether <paramref name="card"/> beats <paramref name="current"/> (the card winning so far).</summary>
    public static bool Beats(Card card, Card current, Suit? trump)
    {
        if (card.Suit == current.Suit) return card.Rank > current.Rank;
        return card.Suit == trump;
    }
}
