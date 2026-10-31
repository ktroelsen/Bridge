namespace Bridge.Engine.Analysis;

/// <summary>A card that gave away tricks: with <see cref="Better"/> the side would have taken <see cref="Cost"/> more.</summary>
public sealed record CardMistake(int Trick, Seat Seat, Card Card, int Cost, IReadOnlyList<Card> Better);

/// <summary>
/// The double dummy review of a played deal: how many tricks the declarer takes with perfect
/// play from the start, and every card that cost tricks (assuming perfect play afterwards).
/// </summary>
public sealed record PlayReview(int DeclarerTricksPerfect, int DeclarerTricks, IReadOnlyList<CardMistake> Mistakes, bool Complete);

/// <summary>The best cards in a position, by double dummy, and the tricks the player's side then takes in total.</summary>
public sealed record BestCards(IReadOnlyList<Card> Cards, int SideTricks, IReadOnlyDictionary<Card, int> Values);

public static class PlayAnalysis
{
    /// <summary>Reviews a whole played deal. Stops when <paramref name="budget"/> runs out (then <see cref="PlayReview.Complete"/> is false).</summary>
    public static PlayReview Review(CardPlay finished, TimeSpan budget)
    {
        var cards = finished.Tricks.SelectMany(t => t.Plays).Select(p => p.Card).ToList();
        var replay = new CardPlay(finished.Deal, finished.Contract);
        var solver = new DoubleDummy(replay) { Deadline = DateTime.UtcNow + budget };
        var declarerNs = finished.Contract.Declarer.IsNorthSouth();
        var mistakes = new List<CardMistake>();
        int? perfect = null;
        var complete = true;

        try
        {
            foreach (var card in cards)
            {
                solver.SetPosition(replay);
                var best = Best(replay, solver);
                perfect ??= declarerNs == replay.NextToPlay.IsNorthSouth() ? best.SideTricks : 13 - best.SideTricks;
                var cost = best.SideTricks - best.Values[card];
                if (cost > 0)
                    mistakes.Add(new CardMistake(replay.Tricks.Count, replay.NextToPlay, card, cost, best.Cards));
                replay.Play(card);
            }
        }
        catch (TimeoutException)
        {
            complete = false;
        }

        return new PlayReview(perfect ?? 0, finished.DeclarerTricks, mistakes, complete);
    }

    /// <summary>The best cards for the player to move.</summary>
    public static BestCards Best(CardPlay play, TimeSpan budget)
    {
        var solver = new DoubleDummy(play) { Deadline = DateTime.UtcNow + budget };
        return Best(play, solver);
    }

    private static BestCards Best(CardPlay play, DoubleDummy solver)
    {
        // Values are North-South tricks from here; turned into the player's side's tricks for the whole deal.
        var values = solver.CardValues().ToDictionary(kv => kv.Key, kv => SideValue(play, kv.Value));
        var top = values.Values.Max();
        var best = values.Where(kv => kv.Value == top).Select(kv => kv.Key)
            .OrderByDescending(c => c.Suit).ThenByDescending(c => c.Rank).ToList();
        return new BestCards(best, top, values);
    }

    /// <summary>North-South tricks from the current position, as tricks for the whole deal for the side to move.</summary>
    private static int SideValue(CardPlay play, int nsFromHere)
    {
        var ns = play.NextToPlay.IsNorthSouth();
        var remaining = 13 - play.Tricks.Count(t => t.IsComplete);
        var side = ns ? nsFromHere : remaining - nsFromHere;
        return side + play.TricksWon(ns);
    }
}
