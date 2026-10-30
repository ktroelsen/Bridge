using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bridge.Api;
using Bridge.Engine;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bridge.Tests;

public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Health_is_ok()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Random_deal_gets_a_number_and_four_hands()
    {
        var deal = (await factory.CreateClient().GetFromJsonAsync<DealDto>("/api/deal", Json))!;
        Assert.InRange(deal.Number, DealEndpoints.MinRandom, DealEndpoints.MaxRandom);
        Assert.Equal(4, deal.Hands.Count);
        Assert.Equal(40, deal.Hands.Sum(h => h.Hcp));
    }

    [Fact]
    public async Task Deal_by_number_matches_the_engine()
    {
        var deal = (await factory.CreateClient().GetFromJsonAsync<DealDto>("/api/deal?number=42", Json))!;
        var expected = Deal.FromNumber(42);
        Assert.Equal(expected.Dealer, deal.Dealer);
        Assert.Equal(expected[Seat.South].Cards.Select(c => new CardDto(c.Suit, c.Rank)), deal.Hands[(int)Seat.South].Cards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100_000_000)]
    public async Task Invalid_number_is_rejected(int number)
    {
        var response = await factory.CreateClient().GetAsync($"/api/deal?number={number}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
