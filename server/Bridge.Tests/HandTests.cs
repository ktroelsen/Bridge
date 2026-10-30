using Bridge.Engine;

namespace Bridge.Tests;

public class HandTests
{
    private static Hand Parse(string pbn) => Pbn.Hand(pbn);

    [Theory]
    [InlineData("AKQ2.J52.T93.842", 10)]
    [InlineData("AKQJ.AKQ.AKQ.AKQ", 37)]
    [InlineData("T987.T98.T98.T98", 0)]
    public void Hcp_counts_honours(string pbn, int hcp) => Assert.Equal(hcp, Parse(pbn).Hcp);

    [Theory]
    [InlineData("AKQ2.J52.T93.842", true)]   // 4-3-3-3
    [InlineData("AKQ2.J52.T932.84", true)]   // 4-4-3-2
    [InlineData("AKQ32.J52.T93.84", true)]   // 5-3-3-2
    [InlineData("AKQ2.J5.T932.842", true)]   // 4-4-3-2 (other suits)
    [InlineData("AKQ32.J5.T932.84", false)]  // two doubletons
    [InlineData("AKQ32.J52.T9876.", false)]  // void
    [InlineData("AKQ32.J5.T9876.4", false)]  // singleton
    public void Balanced_hands(string pbn, bool balanced) => Assert.Equal(balanced, Parse(pbn).IsBalanced);

    [Fact]
    public void Hand_is_sorted_spades_first_high_first()
    {
        var hand = Parse("2QKA.25J.T93.842");
        Assert.Equal("SA SK SQ S2 HJ H5 H2 DT D9 D3 C8 C4 C2", string.Join(" ", hand.Cards));
    }

    [Fact]
    public void Hand_must_have_13_different_cards() =>
        Assert.Throws<ArgumentException>(() => new Hand(Card.Deck.Take(12)));
}
