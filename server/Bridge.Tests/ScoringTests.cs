using Bridge.Engine;

namespace Bridge.Tests;

public class ScoringTests
{
    [Theory]
    [InlineData(1, Strain.Clubs, Penalty.None, false, 7, 70)]
    [InlineData(2, Strain.Hearts, Penalty.None, false, 8, 110)]
    [InlineData(1, Strain.NoTrump, Penalty.None, false, 8, 120)]
    [InlineData(3, Strain.NoTrump, Penalty.None, false, 9, 400)]
    [InlineData(3, Strain.NoTrump, Penalty.None, true, 9, 600)]
    [InlineData(4, Strain.Spades, Penalty.None, false, 10, 420)]
    [InlineData(4, Strain.Spades, Penalty.None, true, 11, 650)]
    [InlineData(5, Strain.Diamonds, Penalty.None, false, 11, 400)]
    [InlineData(6, Strain.Spades, Penalty.None, false, 12, 980)]
    [InlineData(7, Strain.NoTrump, Penalty.None, true, 13, 2220)]
    [InlineData(2, Strain.Spades, Penalty.Doubled, false, 8, 470)]
    [InlineData(1, Strain.NoTrump, Penalty.Redoubled, false, 7, 560)]
    [InlineData(1, Strain.NoTrump, Penalty.Doubled, false, 9, 380)]
    [InlineData(3, Strain.NoTrump, Penalty.None, false, 8, -50)]
    [InlineData(3, Strain.NoTrump, Penalty.None, true, 7, -200)]
    [InlineData(4, Strain.Spades, Penalty.Doubled, false, 7, -500)]
    [InlineData(4, Strain.Spades, Penalty.Doubled, true, 7, -800)]
    [InlineData(4, Strain.Spades, Penalty.Doubled, false, 6, -800)]
    [InlineData(4, Strain.Spades, Penalty.Redoubled, true, 9, -400)]
    public void Duplicate_scores(int level, Strain strain, Penalty penalty, bool vulnerable, int tricks, int expected) =>
        Assert.Equal(expected, Scoring.Score(new Contract(level, strain, Seat.South, penalty), vulnerable, tricks));
}
