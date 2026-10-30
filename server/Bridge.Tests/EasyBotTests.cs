using Bridge.Engine;

namespace Bridge.Tests;

public class EasyBotTests
{
    // West leads against South's 3UT. West has K-Q-J in hearts.
    private static readonly Deal Deal = Pbn.Deal(
        north: "AK32.32.KQ32.432",
        east: "T98.T987.T98.T98",
        south: "QJ7.A65.AJ7.AKQ7",
        west: "654.KQJ4.654.J65");

    [Fact]
    public void Leads_top_of_a_sequence()
    {
        var play = new CardPlay(Deal, new Contract(3, Strain.NoTrump, Seat.South));
        Assert.Equal(Pbn.Card("HK"), EasyBot.Choose(play.ViewFor(Seat.West)));
    }

    [Fact]
    public void Second_hand_plays_low_and_fourth_hand_wins_cheaply()
    {
        var play = new CardPlay(Deal, new Contract(3, Strain.NoTrump, Seat.South));
        play.Play(Pbn.Card("HK"));                                           // West
        Assert.Equal(Pbn.Card("H2"), EasyBot.Choose(play.ViewFor(Seat.North)));  // second hand low
        play.Play(Pbn.Card("H2"));
        Assert.Equal(Pbn.Card("H7"), EasyBot.Choose(play.ViewFor(Seat.East)));   // partner winning: low
        play.Play(Pbn.Card("H7"));
        Assert.Equal(Pbn.Card("HA"), EasyBot.Choose(play.ViewFor(Seat.South)));  // only card that wins
    }

    [Fact]
    public void Declarer_draws_trumps_with_the_top_trump()
    {
        var play = new CardPlay(Deal, new Contract(4, Strain.Spades, Seat.North));
        // East leads, South (dummy) and West follow low, North wins and must lead.
        play.Play(Pbn.Card("D8"));
        play.Play(Pbn.Card("D7"));
        play.Play(Pbn.Card("D4"));
        play.Play(Pbn.Card("DK"));
        Assert.Equal(Seat.North, play.NextToPlay);
        Assert.Equal(Pbn.Card("SA"), EasyBot.Choose(play.ViewFor(Seat.North)));
    }

    [Fact]
    public void Ruffs_when_void_and_opponents_win()
    {
        var deal = Pbn.Deal(
            north: "AKQJT98765432...",
            east: ".AKQJT98765432..",
            south: "..AKQJT98765432.",
            west: "...AKQJT98765432");
        // Not a realistic deal, but simple: hearts are trumps and West declares, so East is dummy.
        var play = new CardPlay(deal, new Contract(1, Strain.Hearts, Seat.West));
        play.Play(Pbn.Card("S2"));   // North leads
        Assert.Equal(Pbn.Card("H2"), EasyBot.Choose(play.ViewFor(Seat.East)));   // cheapest ruff
    }
}
