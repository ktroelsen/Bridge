using Bridge.Engine;
using Bridge.Engine.Analysis;

namespace Bridge.Tests;

public class DoubleDummyTests
{
    /// <summary>Plain minimax over every legal card: slow, but obviously right. North-South tricks from here.</summary>
    private static int BruteForce(CardPlay play)
    {
        if (play.IsComplete) return 0;
        var ns = play.NextToPlay.IsNorthSouth();
        var best = ns ? -1 : 99;
        foreach (var card in play.LegalCards())
        {
            var copy = Clone(play);
            var tricksBefore = copy.TricksWon(true);
            copy.Play(card);
            var value = copy.TricksWon(true) - tricksBefore + BruteForce(copy);
            best = ns ? Math.Max(best, value) : Math.Min(best, value);
        }
        return best;
    }

    private static CardPlay Clone(CardPlay play)
    {
        var copy = new CardPlay(play.Deal, play.Contract);
        foreach (var t in play.Tricks)
            foreach (var p in t.Plays)
                copy.Play(p.Card);
        return copy;
    }

    /// <summary>A deal played by the easy bot until <paramref name="left"/> cards are left in each hand.</summary>
    private static CardPlay Ending(int number, Strain strain, int left)
    {
        var play = new CardPlay(Deal.FromNumber(number), new Contract(1, strain, (Seat)(number % 4)));
        while (play.Played.Count < 52 - 4 * left) play.Play(EasyBot.Choose(play.ViewFor(play.NextToPlay)));
        return play;
    }

    [Theory]
    [InlineData(Strain.NoTrump)]
    [InlineData(Strain.Spades)]
    [InlineData(Strain.Clubs)]
    public void Matches_brute_force_on_small_endings(Strain strain)
    {
        for (var n = 1; n <= 40; n++)
        {
            var play = Ending(n, strain, 3);
            Assert.Equal(BruteForce(play), new DoubleDummy(play).NorthSouthTricks());
        }
    }

    [Fact]
    public void Matches_brute_force_in_the_middle_of_a_trick()
    {
        for (var n = 1; n <= 30; n++)
        {
            var play = Ending(n, Strain.Hearts, 3);
            if (play.IsComplete) continue;
            play.Play(play.LegalCards()[0]);
            if (play.IsComplete) continue;
            Assert.Equal(BruteForce(play), new DoubleDummy(play).NorthSouthTricks());
            var values = new DoubleDummy(play).CardValues();
            Assert.Equal(play.LegalCards().Count, values.Count);
            var ns = play.NextToPlay.IsNorthSouth();
            Assert.Equal(BruteForce(play), ns ? values.Values.Max() : values.Values.Min());
        }
    }

    [Fact]
    public void Thirteen_top_tricks()
    {
        // North-South hold all the top cards: 13 tricks in no trump whoever leads.
        var deal = Pbn.Deal(
            north: "AKQJ.AKQ.AKQ.AKQ", east: "T987.JT9.JT9.JT9", south: "6543.876.876.876", west: "2.5432.5432.5432");
        Assert.Equal(13, new DoubleDummy(new CardPlay(deal, new Contract(7, Strain.NoTrump, Seat.South))).NorthSouthTricks());
    }

    [Fact]
    public void Trumps_ruff_the_long_suit()
    {
        // In no trump North-South run 13 spade tricks... no: North has all spades, East-West all hearts and both minors.
        var deal = Pbn.Deal(
            north: "AKQJT98765432...", east: ".AKQJT98765432..", south: "..AKQJT98765432.", west: "...AKQJT98765432");
        // East leads against South's contract: in no trump East takes all 13 hearts.
        Assert.Equal(0, new DoubleDummy(new CardPlay(deal, new Contract(1, Strain.NoTrump, Seat.North))).NorthSouthTricks());
        // With spades as trumps North ruffs the first heart and takes the rest.
        Assert.Equal(13, new DoubleDummy(new CardPlay(deal, new Contract(1, Strain.Spades, Seat.North))).NorthSouthTricks());
    }
}

public class PlayAnalysisTests
{
    private static CardPlay PlayedByBots(int number, Strain strain, Seat declarer)
    {
        var play = new CardPlay(Deal.FromNumber(number), new Contract(2, strain, declarer));
        while (!play.IsComplete) play.Play(EasyBot.Choose(play.ViewFor(play.NextToPlay)));
        return play;
    }

    [Theory]
    [InlineData(3, Strain.NoTrump, Seat.South)]
    [InlineData(9, Strain.Spades, Seat.East)]
    [InlineData(12, Strain.Hearts, Seat.North)]
    public void Mistakes_add_up_to_the_difference_from_perfect_play(int number, Strain strain, Seat declarer)
    {
        var play = PlayedByBots(number, strain, declarer);
        var review = PlayAnalysis.Review(play, TimeSpan.FromSeconds(60));
        Assert.True(review.Complete);
        var declarerNs = declarer.IsNorthSouth();
        var lost = review.Mistakes.Where(m => m.Seat.IsNorthSouth() == declarerNs).Sum(m => m.Cost);
        var gifts = review.Mistakes.Where(m => m.Seat.IsNorthSouth() != declarerNs).Sum(m => m.Cost);
        Assert.Equal(review.DeclarerTricks, review.DeclarerTricksPerfect - lost + gifts);
        Assert.All(review.Mistakes, m => Assert.DoesNotContain(m.Card, m.Better));
    }

    [Fact]
    public void Best_cards_are_legal_and_reach_the_best_value()
    {
        var play = new CardPlay(Deal.FromNumber(5), new Contract(3, Strain.NoTrump, Seat.South));
        for (var i = 0; i < 6; i++) play.Play(EasyBot.Choose(play.ViewFor(play.NextToPlay)));
        var best = PlayAnalysis.Best(play, TimeSpan.FromSeconds(30));
        Assert.NotEmpty(best.Cards);
        Assert.All(best.Cards, c => Assert.Contains(c, play.LegalCards()));
        Assert.All(best.Cards, c => Assert.Equal(best.SideTricks, best.Values[c]));
        Assert.Equal(play.LegalCards().Count, best.Values.Count);
    }

    [Fact]
    public void Gives_up_when_out_of_time()
    {
        var play = PlayedByBots(4, Strain.NoTrump, Seat.South);
        var review = PlayAnalysis.Review(play, TimeSpan.Zero);
        Assert.False(review.Complete);
    }
}

public class ContractTableTests
{
    [Fact]
    public void Table_matches_single_solves_and_finds_the_best_contract()
    {
        // North-South hold everything in spades and hearts: 13 tricks in either major.
        var deal = Pbn.Deal(
            north: "AKQJT98.765432..", east: "..AKQJT98765432.", south: "765432.AKQJT98..", west: "...AKQJT98765432");
        var spades = ContractTable.ForStrain(deal, Strain.Spades, TimeSpan.FromSeconds(30));
        Assert.Equal([13, 0, 13, 0], spades);
        var guessFree = new DoubleDummy(new CardPlay(deal, new Contract(1, Strain.Hearts, Seat.East))).NorthSouthTricks();
        Assert.Equal(13 - guessFree, ContractTable.ForStrain(deal, Strain.Hearts, TimeSpan.FromSeconds(30))[(int)Seat.East]);

        var table = ContractTable.For(deal, TimeSpan.FromSeconds(60));
        Assert.True(table.Complete);
        Assert.Equal(7, table.NorthSouth!.Contract.Level);
        Assert.True(table.NorthSouth.Contract.Strain is Strain.Spades or Strain.Hearts);
    }

    [Fact]
    public void A_guess_gives_the_same_answer()
    {
        for (var n = 1; n <= 6; n++)
        {
            var play = new CardPlay(Deal.FromNumber(n), new Contract(1, Strain.Hearts, Seat.South));
            for (var i = 0; i < 24; i++) play.Play(EasyBot.Choose(play.ViewFor(play.NextToPlay)));
            var exact = new DoubleDummy(play).NorthSouthTricks();
            foreach (var guess in new[] { 0, 3, 7, 13 })
                Assert.Equal(exact, new DoubleDummy(play).NorthSouthTricks(guess));
        }
    }
}
