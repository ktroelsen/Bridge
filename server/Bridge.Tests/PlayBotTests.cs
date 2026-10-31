using Bridge.Engine;
using Bridge.Engine.Bidding;

namespace Bridge.Tests;

public class PlayBotTests
{
    /// <summary>Plays the first five tricks with the easy bot, the rest at <paramref name="level"/>.</summary>
    private static List<Card> PlayOut(int number, BotLevel level)
    {
        var deal = Deal.FromNumber(number);
        var declarer = MiniBridge.Declarer(deal)!.Value;
        var play = new CardPlay(deal, MiniBridge.BotContract(deal, declarer));
        while (play.Played.Count < 20) play.Play(EasyBot.Choose(play.ViewFor(play.NextToPlay)));
        while (!play.IsComplete)
            play.Play(PlayBot.Choose(play, play.NextToPlay, level, s => Shows.Hcp(deal[s].Hcp, deal[s].Hcp)));
        return [.. play.Tricks.SelectMany(t => t.Plays).Select(p => p.Card)];
    }

    [Theory]
    [InlineData(BotLevel.Medium)]
    [InlineData(BotLevel.Hard)]
    public void Same_position_gives_the_same_card_without_the_cache(BotLevel level)
    {
        // Games are replayed on every request, so the bot must be deterministic.
        foreach (var n in new[] { 3, 8 })
        {
            var first = PlayOut(n, level);
            PlayBot.ClearCache();
            Assert.Equal(first, PlayOut(n, level));
        }
    }

    [Fact]
    public void Medium_declarer_takes_at_least_as_many_tricks_as_easy_on_average()
    {
        int easy = 0, hard = 0;
        foreach (var n in new[] { 1, 2, 3, 4, 6, 7, 8, 9 })
        {
            var deal = Deal.FromNumber(n);
            if (MiniBridge.Declarer(deal) is not { } d) continue;
            var contract = MiniBridge.BotContract(deal, d);
            foreach (var level in new[] { BotLevel.Easy, BotLevel.Medium })
            {
                var play = new CardPlay(deal, contract);
                while (!play.IsComplete)
                {
                    var seat = play.NextToPlay;
                    var mine = seat == contract.Declarer || seat == contract.Dummy;
                    play.Play(PlayBot.Choose(play, seat, mine ? level : BotLevel.Easy));
                }
                if (level == BotLevel.Easy) easy += play.DeclarerTricks;
                else hard += play.DeclarerTricks;
            }
        }
        Assert.True(hard >= easy, $"Medium {hard} < Easy {easy}");
    }

    [Fact]
    public void Full_games_with_the_medium_bot_finish()
    {
        var calls = new List<Call>();
        var game = BridgeGame.Replay(5, calls, [], BotLevel.Medium);
        while (game.Phase == GamePhase.Bidding)
        {
            calls.Add(game.Hint!.Call);
            game = BridgeGame.Replay(5, calls, [], BotLevel.Medium);
        }
        var cards = new List<Card>();
        while (game.Phase == GamePhase.Play)
        {
            cards.Add(game.Play!.LegalCards()[0]);
            game = BridgeGame.Replay(5, calls, cards, BotLevel.Medium);
        }
        Assert.True(game.Phase is GamePhase.Done or GamePhase.PassedOut);
    }
}
