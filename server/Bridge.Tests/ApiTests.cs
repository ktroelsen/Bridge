using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bridge.Api;
using Bridge.Engine;

namespace Bridge.Tests;

public class ApiTests(TestFactory factory) : IClassFixture<TestFactory>
{
    private static readonly JsonSerializerOptions Json = TestFactory.Json;

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

    private async Task<MiniBridgeDto> PostMiniBridge(HttpClient client, MiniBridgeRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/minibridge", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MiniBridgeDto>(Json))!;
    }

    [Fact]
    public async Task Mini_bridge_game_can_be_played_to_the_end()
    {
        var client = factory.CreateClient();
        var game = await PostMiniBridge(client, new MiniBridgeRequest(null, null, null));
        Assert.InRange(game.Number, DealEndpoints.MinRandom, DealEndpoints.MaxRandom);
        Assert.Equal(40, game.Hcp.Sum(h => h.Hcp));

        ContractChoiceDto? contract = null;
        if (game.Phase == GamePhase.ChooseContract)
        {
            contract = new ContractChoiceDto(Strain.NoTrump, false);
            game = await PostMiniBridge(client, new MiniBridgeRequest(game.Number, contract, null));
        }

        var cards = new List<CardDto>();
        while (game.Phase == GamePhase.Play)
        {
            Assert.NotEmpty(game.LegalCards);
            cards.Add(game.LegalCards[0]);
            game = await PostMiniBridge(client, new MiniBridgeRequest(game.Number, contract, cards));
        }

        Assert.Equal(GamePhase.Done, game.Phase);
        Assert.Equal(13, game.TricksNorthSouth + game.TricksEastWest);
        Assert.NotNull(game.ScoreNorthSouth);
        Assert.NotNull(game.FullDeal);
    }

    [Fact]
    public async Task Mini_bridge_only_shows_cards_the_player_may_see()
    {
        var client = factory.CreateClient();
        for (var n = 1; n <= 40; n++)
        {
            var deal = Deal.FromNumber(n);
            if (MiniBridge.Declarer(deal) is not { } declarer) continue;
            var contract = declarer.IsNorthSouth() ? new ContractChoiceDto(Strain.Spades, false) : null;
            var game = await PostMiniBridge(client, new MiniBridgeRequest(n, contract, null));

            Seat[] allowed = declarer.IsNorthSouth() ? [Seat.North, Seat.South] : [Seat.South, declarer.Partner()];
            Assert.All(game.Hands, h => Assert.Contains(h.Seat, allowed));
            Assert.Null(game.FullDeal);
        }
    }

    [Fact]
    public async Task Mini_bridge_rejects_illegal_card()
    {
        var client = factory.CreateClient();
        var number = Enumerable.Range(1, 500).First(n => MiniBridge.Declarer(Deal.FromNumber(n))?.IsNorthSouth() == false);
        var west = Deal.FromNumber(number)[Seat.West].Cards[0];
        var response = await client.PostAsJsonAsync("/api/minibridge",
            new MiniBridgeRequest(number, null, [new CardDto(west.Suit, west.Rank)]), Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(Practice.Declare)]
    [InlineData(Practice.Defend)]
    [InlineData(Practice.TrumpFit)]
    [InlineData(Practice.NoTrump)]
    public async Task Practice_deals_match_their_kind(Practice practice)
    {
        var client = factory.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            var game = await PostMiniBridge(client, new MiniBridgeRequest(null, null, null, practice));
            Assert.True(MiniBridge.Matches(Deal.FromNumber(game.Number), practice));
        }
    }
}
