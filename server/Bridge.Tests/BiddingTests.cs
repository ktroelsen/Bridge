using Bridge.Engine;
using Bridge.Engine.Bidding;

namespace Bridge.Tests;

public class BiddingTests
{
    /// <summary>The system's call for a hand after the given calls (dealer North, so the next seat follows from the count).</summary>
    private static BidChoice Bid(string hand, params string[] before)
    {
        var calls = new List<AuctionCall>();
        var seat = Seat.North;
        foreach (var c in before)
        {
            calls.Add(new AuctionCall(seat, Call.Parse(c)));
            seat = seat.Next();
        }
        return Bidder.Choose(Seat.North, calls, seat, Pbn.Hand(hand));
    }

    [Theory]
    [InlineData("AQ3.KJ4.Q873.K92", "1N")]      // 15 balanced
    [InlineData("A3.KQ874.Q87.K92", "1H")]      // 14, five hearts
    [InlineData("AK874.KJ874.3.A2", "1S")]      // 5-5 majors: spades
    [InlineData("AQ84.KJ74.Q83.92", "1D")]      // 4-4-3-2, no five-card major
    [InlineData("AQ84.KJ7.Q83.932", "1C")]      // 3-3 in the minors
    [InlineData("AKQ4.AKJ7.KQ3.A2", "2C")]      // 26
    [InlineData("AK4.KQ7.AQ83.K92", "2N")]      // 21 balanced
    [InlineData("KQJ9874.3.93.842", "3S")]      // weak with seven spades
    [InlineData("K984.Q73.J83.Q92", "P")]       // 8
    public void Openings(string hand, string call) => Assert.Equal(Call.Parse(call), Bid(hand).Call);

    [Theory]
    [InlineData("1H", "K84.Q73.J832.Q92", "2H")]   // 8, three hearts
    [InlineData("1H", "K84.Q73.AJ32.Q92", "3H")]   // 12, three hearts
    [InlineData("1H", "K84.Q73.AJ32.KQ2", "4H")]   // 15, three hearts
    [InlineData("1H", "KJ84.73.J832.Q92", "1S")]   // four spades, no support
    [InlineData("1H", "Q84.73.Q8732.J92", "P")]    // 5: too weak to respond
    [InlineData("1C", "K984.QJ73.83.Q92", "1H")]   // 4-4 majors: hearts first
    [InlineData("1N", "K9843.Q73.83.J92", "2H")]   // transfer to spades
    [InlineData("1N", "984.KQ973.83.J92", "2D")]   // transfer to hearts
    [InlineData("1N", "K984.Q73.A83.J92", "2C")]   // Stayman with 10 and four spades
    [InlineData("1N", "K98.Q73.A832.J92", "3N")]   // 10 flat
    [InlineData("1N", "J98.Q73.8632.J92", "P")]    // 4
    [InlineData("2C", "J98.Q73.8632.J92", "2D")]   // waiting
    public void Responses(string opening, string hand, string call)
    {
        // North opens, East passes, South responds.
        Assert.Equal(Call.Parse(call), Bid(hand, opening, "P").Call);
    }

    [Theory]
    [InlineData("AQ3.KJ84.Q87.K92", "1N", "2C", "2H")]   // Stayman: four hearts
    [InlineData("AQ84.KJ8.Q87.K92", "1N", "2C", "2S")]   // Stayman: four spades
    [InlineData("AQ8.KJ8.Q874.K92", "1N", "2C", "2D")]   // Stayman: no major
    [InlineData("AQ8.KJ8.Q874.K92", "1N", "2D", "2H")]   // completes the transfer
    [InlineData("A3.AQ874.KQ7.K92", "1H", "2H", "3H")]   // 18: invites
    [InlineData("A3.AQ874.K87.932", "1H", "1S", "1N")]   // 13 balanced: 1UT
    [InlineData("3.AQ874.K873.K92", "1H", "1S", "2D")]   // new lower suit
    public void Opener_rebids(string hand, string opening, string response, string call) =>
        Assert.Equal(Call.Parse(call), Bid(hand, opening, "P", response, "P").Call);

    [Fact]
    public void Opener_must_not_pass_a_new_suit()
    {
        // 1♥ - 1♠: opener with a minimum and no spade support still bids.
        Assert.NotEqual(Call.Pass, Bid("3.AQ874.K873.K92", "1H", "P", "1S", "P").Call);
    }

    [Theory]
    [InlineData("KQJ84.73.K83.Q92", "1D", "1S")]     // overcall
    [InlineData("KQ84.AJ73.3.Q982", "1D", "X")]      // takeout double
    [InlineData("AQ3.KJ4.Q873.K92", "1H", "1N")]     // 15 balanced
    [InlineData("984.73.Q873.Q982", "1H", "P")]
    public void Overcalls(string hand, string opening, string call) =>
        // North opens, East calls.
        Assert.Equal(Call.Parse(call), Bid(hand, opening).Call);

    [Fact]
    public void Explanations_describe_the_call()
    {
        var calls = new List<AuctionCall>();
        Assert.Contains("mindst 5 ♥", Bidder.Explain(Seat.North, calls, Seat.North, Call.Parse("1H")));
        Assert.Contains("15–17 HP", Bidder.Explain(Seat.North, calls, Seat.North, Call.Parse("1N")));
        calls.Add(new AuctionCall(Seat.North, Call.Parse("1N")));
        calls.Add(new AuctionCall(Seat.East, Call.Pass));
        Assert.Contains("Stayman", Bidder.Explain(Seat.North, calls, Seat.South, Call.Parse("2C")));
        Assert.Contains("Overføring", Bidder.Explain(Seat.North, calls, Seat.South, Call.Parse("2D")));
    }

    [Fact]
    public void Model_collects_what_a_player_has_shown()
    {
        var calls = new List<AuctionCall> { new(Seat.North, Call.Parse("1N")), new(Seat.East, Call.Pass) };
        var model = Bidder.Model(Seat.North, calls, Seat.North);
        Assert.Equal(15, model.HcpMin);
        Assert.Equal(17, model.HcpMax);
        Assert.True(model.Balanced);
    }

    [Fact]
    public void Four_bots_always_finish_the_auction()
    {
        var levels = new int[8];
        for (var n = 1; n <= 1500; n++)
        {
            var deal = Deal.FromNumber(n);
            var auction = new Auction(deal.Dealer);
            while (!auction.IsComplete)
            {
                var seat = auction.NextSeat;
                auction.Add(Bidder.Choose(deal.Dealer, auction.Calls, seat, deal[seat]).Call);
                Assert.True(auction.Calls.Count < 40, $"Deal {n} doesn't end: {string.Join(" ", auction.Calls.Select(c => c.Call))}");
            }
            levels[auction.Contract()?.Level ?? 0]++;
        }
        // Most deals end in a sensible contract, few are passed out or reach the 6-7 level.
        Assert.InRange(levels[0], 0, 250);
        Assert.InRange(levels[6] + levels[7], 0, 60);
    }
}
