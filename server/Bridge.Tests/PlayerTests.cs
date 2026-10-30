using System.Net;
using System.Net.Http.Json;
using Bridge.Api.Players;

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
