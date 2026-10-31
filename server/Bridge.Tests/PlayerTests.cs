using System.Net;
using System.Net.Http.Json;
using Bridge.Api.Players;
using Bridge.Engine;

namespace Bridge.Tests;

public class PlayerTests(TestFactory factory) : IClassFixture<TestFactory>
{
    [Fact]
    public async Task Player_name_is_case_insensitive_and_trimmed()
    {
        var client = factory.CreateClient();
        var a = await (await client.PostAsJsonAsync("/api/players", new PlayerRequest("  Kasper   T "))).Content.ReadFromJsonAsync<PlayerDto>();
        var b = await (await client.PostAsJsonAsync("/api/players", new PlayerRequest("kasper t"))).Content.ReadFromJsonAsync<PlayerDto>();
        Assert.Equal("Kasper T", a!.Name);
        Assert.Equal("Kasper T", b!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0123456789012345678901234567890")]
    public async Task Bad_names_are_rejected(string name)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/players", new PlayerRequest(name));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Lesson_progress_keeps_the_best_score()
    {
        var client = factory.CreateClient();
        Assert.Empty((await client.GetFromJsonAsync<List<LessonProgressDto>>("/api/players/Ida/progress"))!);

        (await client.PutAsJsonAsync("/api/players/Ida/progress/trumf", new LessonResultRequest(70))).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/players/Ida/progress/trumf", new LessonResultRequest(40))).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/players/ida/progress/point", new LessonResultRequest(100))).EnsureSuccessStatusCode();

        var progress = (await client.GetFromJsonAsync<List<LessonProgressDto>>("/api/players/IDA/progress"))!;
        Assert.Equal(2, progress.Count);
        Assert.Equal(70, progress.Single(p => p.LessonId == "trumf").BestScore);
        Assert.Equal(100, progress.Single(p => p.LessonId == "point").BestScore);
    }

    [Theory]
    [InlineData("Trumf", 50)]
    [InlineData("trumf..x", 50)]
    [InlineData("trumf", 101)]
    [InlineData("trumf", -1)]
    public async Task Bad_progress_is_rejected(string lessonId, int score)
    {
        var response = await factory.CreateClient().PutAsJsonAsync($"/api/players/Ida/progress/{lessonId}", new LessonResultRequest(score));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class GameRecordTests(TestFactory factory) : IClassFixture<TestFactory>
{
    private static async Task<List<Bridge.Api.CardDto>> FinishMini(HttpClient client, int number, Bridge.Api.ContractChoiceDto? contract)
    {
        var cards = new List<Bridge.Api.CardDto>();
        while (true)
        {
            var r = await client.PostAsJsonAsync("/api/minibridge", new Bridge.Api.MiniBridgeRequest(number, contract, cards), TestFactory.Json);
            var g = (await r.Content.ReadFromJsonAsync<Bridge.Api.MiniBridgeDto>(TestFactory.Json))!;
            if (g.Phase == Bridge.Engine.GamePhase.Done) return cards;
            cards.Add(g.LegalCards[0]);
        }
    }

    [Fact]
    public async Task Finished_games_are_saved_once_and_counted()
    {
        var client = factory.CreateClient();
        var defending = Enumerable.Range(1, 500).First(n => Bridge.Engine.MiniBridge.Declarer(Bridge.Engine.Deal.FromNumber(n))?.IsNorthSouth() == false);
        var declaring = Enumerable.Range(1, 500).First(n => Bridge.Engine.MiniBridge.Declarer(Bridge.Engine.Deal.FromNumber(n))?.IsNorthSouth() == true);
        var choice = new Bridge.Api.ContractChoiceDto(Bridge.Engine.Strain.NoTrump, false);

        var cards1 = await FinishMini(client, defending, null);
        var request1 = new Bridge.Api.MiniBridgeRequest(defending, null, cards1);
        var first = await (await client.PostAsJsonAsync("/api/players/Bo/games/mini", request1, TestFactory.Json)).Content.ReadFromJsonAsync<GameRecordDto>(TestFactory.Json);
        var again = await (await client.PostAsJsonAsync("/api/players/Bo/games/mini", request1, TestFactory.Json)).Content.ReadFromJsonAsync<GameRecordDto>(TestFactory.Json);
        Assert.Equal(first!.Id, again!.Id);
        Assert.False(first.OurContract);

        var cards2 = await FinishMini(client, declaring, choice);
        (await client.PostAsJsonAsync("/api/players/Bo/games/mini", new Bridge.Api.MiniBridgeRequest(declaring, choice, cards2), TestFactory.Json)).EnsureSuccessStatusCode();

        var stats = (await client.GetFromJsonAsync<StatsDto>("/api/players/bo/stats", TestFactory.Json))!;
        Assert.Equal(2, stats.Games);
        var easy = Assert.Single(stats.ByLevel);
        Assert.Equal("easy", easy.Level);
        Assert.Equal(1, easy.Declared);
        Assert.Equal(1, easy.Defended);
        Assert.Equal(2, stats.Recent.Count);
    }

    [Fact]
    public async Task Unfinished_games_are_not_saved()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/players/Bo/games/full", new Bridge.Api.GameRequest(7, [], []), TestFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
