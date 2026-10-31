using System.Net;
using System.Net.Http.Json;
using Bridge.Api;
using Bridge.Api.Challenges;
using Bridge.Api.Players;
using Bridge.Engine;

namespace Bridge.Tests;

public class ChallengeTests(TestFactory factory) : IClassFixture<TestFactory>
{
    /// <summary>Plays a mini-bridge deal to the end: no-trump part score if North-South declare, first legal card every time.</summary>
    private static async Task<MiniBridgeRequest> Finish(HttpClient client, int number, ContractChoiceDto? choice = null)
    {
        var deal = Deal.FromNumber(number);
        var contract = MiniBridge.Declarer(deal)!.Value.IsNorthSouth() ? choice ?? new ContractChoiceDto(Strain.NoTrump, false) : null;
        var cards = new List<CardDto>();
        while (true)
        {
            var request = new MiniBridgeRequest(number, contract, cards);
            var game = (await (await client.PostAsJsonAsync("/api/minibridge", request, TestFactory.Json)).Content.ReadFromJsonAsync<MiniBridgeDto>(TestFactory.Json))!;
            if (game.Phase == GamePhase.Done) return request;
            cards.Add(game.LegalCards[0]);
        }
    }

    private static async Task<ChallengeDto> Get(HttpClient client, string code, string viewer) =>
        (await client.GetFromJsonAsync<ChallengeDto>($"/api/challenges/{code}?name={viewer}", TestFactory.Json))!;

    [Fact]
    public async Task Friends_play_the_same_deals_and_are_compared()
    {
        var client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/challenges", new CreateChallengeRequest("Anna", "mini", BotLevel.Easy, 4), TestFactory.Json))
            .Content.ReadFromJsonAsync<ChallengeDto>(TestFactory.Json);
        Assert.Equal(4, created!.Boards.Count);
        Assert.Equal(6, created.Code.Length);
        var code = created.Code;
        var first = created.Boards[0].Number;

        // Anna plays deal 1; Bent hasn't, so he sees that Anna played it but not her score.
        var anna = await Finish(client, first);
        (await client.PostAsJsonAsync($"/api/players/Anna/games/mini?challenge={code}", anna, TestFactory.Json)).EnsureSuccessStatusCode();
        var forBent = await Get(client, code.ToLowerInvariant(), "Bent");
        var hidden = Assert.Single(forBent.Boards[0].Results);
        Assert.Equal("Anna", hidden.Name);
        Assert.Null(hidden.ScoreNorthSouth);

        // Bent plays it with a different contract choice; now both see both scores.
        var bent = await Finish(client, first, new ContractChoiceDto(Strain.Spades, true));
        (await client.PostAsJsonAsync($"/api/players/Bent/games/mini?challenge={code}", bent, TestFactory.Json)).EnsureSuccessStatusCode();
        var forAnna = await Get(client, code, "Anna");
        Assert.All(forAnna.Boards[0].Results, r => Assert.NotNull(r.ScoreNorthSouth));
        var a = forAnna.Standings.Single(s => s.Name == "Anna");
        var b = forAnna.Standings.Single(s => s.Name == "Bent");
        Assert.Equal(100, a.Percent!.Value + b.Percent!.Value);

        // Only the first result counts: playing the deal again doesn't change it.
        var again = await Finish(client, first, new ContractChoiceDto(Strain.Clubs, false));
        var response = await client.PostAsJsonAsync($"/api/players/Anna/games/mini?challenge={code}", again, TestFactory.Json);
        var record = (await response.Content.ReadFromJsonAsync<GameRecordDto>(TestFactory.Json))!;
        Assert.Equal(forAnna.Boards[0].Results.Single(r => r.Name == "Anna").ScoreNorthSouth, record.ScoreNorthSouth);

        var mine = (await client.GetFromJsonAsync<List<ChallengeSummaryDto>>("/api/players/bent/challenges", TestFactory.Json))!;
        var summary = Assert.Single(mine);
        Assert.Equal(1, summary.PlayedByMe);
        Assert.Equal(2, summary.Players);
    }

    [Fact]
    public async Task A_deal_outside_the_challenge_is_rejected()
    {
        var client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/challenges", new CreateChallengeRequest("Carl", "mini", BotLevel.Easy, 4), TestFactory.Json))
            .Content.ReadFromJsonAsync<ChallengeDto>(TestFactory.Json);
        var other = Enumerable.Range(1, 500).First(n => MiniBridge.Declarer(Deal.FromNumber(n)) is not null && created!.Boards.All(b => b.Number != n));
        var request = await Finish(client, other);
        var response = await client.PostAsJsonAsync($"/api/players/Carl/games/mini?challenge={created!.Code}", request, TestFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("mini", 5)]
    [InlineData("poker", 4)]
    public async Task Bad_challenges_are_rejected(string mode, int games)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/challenges", new CreateChallengeRequest("Dora", mode, BotLevel.Easy, games), TestFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_code_is_not_found()
    {
        var response = await factory.CreateClient().GetAsync("/api/challenges/ZZZZZZ");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
