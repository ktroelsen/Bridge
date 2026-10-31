namespace Bridge.Engine.Analysis;

/// <summary>A play problem: all four hands open, North-South on lead, find the card that takes most tricks.</summary>
public sealed record PlayQuestion(int Number, CardPlay Play, BestCards Best);

/// <summary>
/// Makes play problems: a mini-bridge deal (the bot's choice of contract) played by the bots up
/// to a trick that North-South lead. For declarer problems North-South declare; for defence
/// problems East-West declare and South is on lead. The double dummy solver
/// gives the answer. Only positions where some card loses a trick are used, so there is
/// something to find.
/// </summary>
public static class PlayQuiz
{
    private static readonly TimeSpan TryBudget = TimeSpan.FromSeconds(2);

    public static PlayQuestion Make(int fromTrick, int toTrick, Random random, TimeSpan budget, bool defend = false)
    {
        var deadline = DateTime.UtcNow + budget;
        while (DateTime.UtcNow < deadline)
        {
            var number = random.Next(Deal.MinNumber, 10_000_000);
            var target = random.Next(fromTrick, toTrick + 1);
            // A cap per deal, so one hard deal can't use up all the time.
            var left = deadline - DateTime.UtcNow;
            if (Try(number, target, left < TryBudget ? left : TryBudget, defend) is { } question) return question;
        }
        throw new TimeoutException("Kunne ikke finde en opgave i tide.");
    }

    /// <summary>The problem for a deal at the start of trick <paramref name="trick"/> (1-13), if it makes a good one.</summary>
    public static PlayQuestion? Try(int number, int trick, TimeSpan budget, bool defend = false)
    {
        var deal = Deal.FromNumber(number);
        if (MiniBridge.Declarer(deal) is not { } declarer || declarer.IsNorthSouth() == defend) return null;
        var play = new CardPlay(deal, MiniBridge.BotContract(deal, declarer));
        while (play.Tricks.Count < trick || play.CurrentTrick.Plays.Count > 0)
        {
            if (play.IsComplete) return null;
            play.Play(EasyBot.Choose(play.ViewFor(play.NextToPlay)));
        }
        if (!play.NextToPlay.IsNorthSouth() || play.LegalCards().Count < 2) return null;
        // In defence the human only plays South.
        if (defend && play.NextToPlay != Seat.South) return null;
        try
        {
            var best = PlayAnalysis.Best(play, budget);
            // Something must be at stake: at least one card gives fewer tricks.
            return best.Values.Values.Min() < best.SideTricks ? new PlayQuestion(number, play, best) : null;
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
}
