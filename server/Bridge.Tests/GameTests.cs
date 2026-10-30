using System.Net;
using System.Net.Http.Json;
using Bridge.Api;
using Bridge.Engine;

namespace Bridge.Tests;

public class GameTests(TestFactory factory) : IClassFixture<TestFactory>
{
    private static async Task<GameDto> Post(HttpClient client, GameRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/game", request, TestFactory.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GameDto>(TestFactory.Json))!;
    }

    [Fact]
    public async Task Whole_games_can_be_played_following_the_hints()
    {
        var client = factory.CreateClient();
        for (var n = 1; n <= 12; n++)
        {
            var calls = new List<string>();
            var cards = new List<CardDto>();
            var game = await Post(client, new GameRequest(n, calls, cards));
            while (game.Phase is GamePhase.Bidding or GamePhase.Play)
            {
                if (game.Phase == GamePhase.Bidding)
                {
                    Assert.NotNull(game.Hint);
                    Assert.Contains(game.Hint!.Call, game.LegalCalls);
                    Assert.DoesNotContain(game.Hands, h => h.Seat != Seat.South);
                    calls.Add(game.Hint.Call);
                }
                else
                {
                    cards.Add(game.LegalCards[0]);
                }
                game = await Post(client, new GameRequest(n, calls, cards));
            }
            Assert.NotNull(game.ScoreNorthSouth);
            Assert.NotNull(game.FullDeal);
            Assert.All(game.Auction, c => Assert.False(string.IsNullOrWhiteSpace(c.Explanation)));
            // Following the system gives no feedback.
            Assert.All(game.Auction, c => Assert.Null(c.SystemCall));
            if (game.Phase == GamePhase.Done) Assert.Equal(13, game.TricksNorthSouth + game.TricksEastWest);
        }
    }

    [Fact]
    public async Task A_call_that_differs_from_the_system_gets_feedback()
    {
        var client = factory.CreateClient();
        var number = Enumerable.Range(1, 500).First(n => Deal.FromNumber(n).Dealer == Seat.South && Deal.FromNumber(n)[Seat.South].Hcp >= 13);
        var game = await Post(client, new GameRequest(number, ["7N"], []));
        var mine = game.Auction.First(c => c.Seat == Seat.South);
        Assert.Equal("7N", mine.Call);
        Assert.NotNull(mine.SystemCall);
        Assert.NotNull(mine.SystemExplanation);
    }

    [Fact]
    public async Task Only_visible_hands_are_sent_during_play()
    {
        var client = factory.CreateClient();
        for (var n = 20; n < 40; n++)
        {
            var calls = new List<string>();
            var game = await Post(client, new GameRequest(n, calls, []));
            while (game.Phase == GamePhase.Bidding)
            {
                calls.Add(game.Hint!.Call);
                game = await Post(client, new GameRequest(n, calls, []));
            }
            if (game.Phase != GamePhase.Play) continue;
            var c = game.Contract!;
            var allowed = new HashSet<Seat> { Seat.South, c.Declarer.Partner() };
            if (c.Declarer == Seat.North) allowed.Add(Seat.North);
            Assert.All(game.Hands, h => Assert.Contains(h.Seat, allowed));
            Assert.Null(game.FullDeal);
        }
    }

    [Theory]
    [InlineData("1Z")]
    [InlineData("8C")]
    public async Task Bad_calls_are_rejected(string call)
    {
        var number = Enumerable.Range(1, 500).First(n => Deal.FromNumber(n).Dealer == Seat.South);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/game", new GameRequest(number, [call], []), TestFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Illegal_call_is_rejected()
    {
        // South deals: double is not allowed as the first call.
        var number = Enumerable.Range(1, 500).First(n => Deal.FromNumber(n).Dealer == Seat.South);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/game", new GameRequest(number, ["X"], []), TestFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
