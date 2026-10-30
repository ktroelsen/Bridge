using Bridge.Engine;

namespace Bridge.Tests;

public class AuctionTests
{
    private static Auction Auction(Seat dealer, params string[] calls)
    {
        var a = new Auction(dealer);
        foreach (var c in calls) a.Add(Call.Parse(c));
        return a;
    }

    [Fact]
    public void Bids_must_go_up()
    {
        var a = Auction(Seat.North, "1H");
        Assert.False(a.IsLegal(Call.Parse("1D")));
        Assert.False(a.IsLegal(Call.Parse("1H")));
        Assert.True(a.IsLegal(Call.Parse("1S")));
        Assert.True(a.IsLegal(Call.Parse("1N")));
        Assert.True(a.IsLegal(Call.Parse("2C")));
    }

    [Fact]
    public void Double_only_opponents_bid_and_redouble_only_a_double()
    {
        var a = Auction(Seat.North, "1H");
        Assert.True(a.IsLegal(Call.Double));      // East doubles North
        Assert.False(a.IsLegal(Call.Redouble));
        a.Add(Call.Pass);                          // East
        Assert.False(a.IsLegal(Call.Double));     // South can't double partner
        a.Add(Call.Pass);                          // South
        Assert.True(a.IsLegal(Call.Double));      // West doubles North
        a.Add(Call.Double);
        Assert.True(a.IsLegal(Call.Redouble));    // North redoubles
        Assert.Throws<InvalidPlayException>(() => a.Add(Call.Double));
    }

    [Fact]
    public void Three_passes_end_the_auction_and_first_to_name_the_strain_declares()
    {
        var a = Auction(Seat.North, "1H", "P", "2H", "P", "4H", "X", "P", "P", "P");
        Assert.True(a.IsComplete);
        var c = a.Contract()!;
        Assert.Equal(4, c.Level);
        Assert.Equal(Strain.Hearts, c.Strain);
        Assert.Equal(Seat.North, c.Declarer);
        Assert.Equal(Penalty.Doubled, c.Penalty);
    }

    [Fact]
    public void Four_passes_pass_the_deal_out()
    {
        var a = Auction(Seat.East, "P", "P", "P");
        Assert.False(a.IsComplete);
        a.Add(Call.Pass);
        Assert.True(a.IsPassedOut);
        Assert.Null(a.Contract());
    }

    [Theory]
    [InlineData("1C", 0)]
    [InlineData("1N", 4)]
    [InlineData("2C", 5)]
    [InlineData("7N", 34)]
    public void Bids_have_a_rank(string call, int rank) => Assert.Equal(rank, Call.Parse(call).Rank);
}
