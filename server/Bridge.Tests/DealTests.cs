using Bridge.Engine;

namespace Bridge.Tests;

public class DealTests
{
    [Fact]
    public void Deal_uses_every_card_once()
    {
        var deal = Deal.FromNumber(482913);
        var cards = Enum.GetValues<Seat>().SelectMany(s => deal[s].Cards).ToList();
        Assert.Equal(52, cards.Distinct().Count());
        Assert.All(Enum.GetValues<Seat>(), s => Assert.Equal(13, deal[s].Cards.Count));
    }

    [Fact]
    public void Same_number_gives_same_deal()
    {
        var a = Deal.FromNumber(123456);
        var b = Deal.FromNumber(123456);
        Assert.All(Enum.GetValues<Seat>(), s => Assert.Equal(a[s].Cards, b[s].Cards));
        Assert.NotEqual(a[Seat.North].Cards, Deal.FromNumber(123457)[Seat.North].Cards);
    }

    [Fact]
    public void Deal_numbers_never_change()
    {
        // Golden test: shared deal numbers must give the same cards forever. If this fails,
        // the shuffle changed and every saved or shared deal number is broken.
        var deal = Deal.FromNumber(1);
        Assert.Equal(Golden, string.Join(" ", deal[Seat.North].Cards));
    }

    private const string Golden = "ST S6 H9 DA DQ D9 D8 D7 D6 D5 CQ CT C4";

    [Fact]
    public void Total_hcp_is_40()
    {
        for (var n = 1; n <= 200; n++)
        {
            var deal = Deal.FromNumber(n);
            Assert.Equal(40, Enum.GetValues<Seat>().Sum(s => deal[s].Hcp));
        }
    }

    [Theory]
    [InlineData(1, Seat.North, Vulnerability.None)]
    [InlineData(2, Seat.East, Vulnerability.NorthSouth)]
    [InlineData(3, Seat.South, Vulnerability.EastWest)]
    [InlineData(4, Seat.West, Vulnerability.Both)]
    [InlineData(5, Seat.North, Vulnerability.NorthSouth)]
    [InlineData(13, Seat.North, Vulnerability.Both)]
    [InlineData(16, Seat.West, Vulnerability.EastWest)]
    [InlineData(17, Seat.North, Vulnerability.None)]
    public void Dealer_and_vulnerability_follow_the_board_cycle(int number, Seat dealer, Vulnerability vulnerability)
    {
        var deal = Deal.FromNumber(number);
        Assert.Equal(dealer, deal.Dealer);
        Assert.Equal(vulnerability, deal.Vulnerability);
    }

    [Fact]
    public void Shuffle_is_roughly_uniform()
    {
        // How often the ace of spades lands with each seat over many deals.
        var counts = new int[4];
        for (var n = 1; n <= 4000; n++)
        {
            var deal = Deal.FromNumber(n);
            counts[(int)Enum.GetValues<Seat>().First(s => deal[s].Cards.Contains(new Card(Suit.Spades, Rank.Ace)))]++;
        }
        Assert.All(counts, c => Assert.InRange(c, 850, 1150));
    }
}
