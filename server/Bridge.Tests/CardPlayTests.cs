using Bridge.Engine;

namespace Bridge.Tests;

public class CardPlayTests
{
    // North and South hold the majors, East all the diamonds and West all the clubs.
    private static readonly Deal Deal = Pbn.Deal(
        north: "AKQJT98.765432..",
        east: "..AKQJT98765432.",
        south: "765432.AKQJT98..",
        west: "...AKQJT98765432");

    [Fact]
    public void Opening_lead_is_left_of_declarer()
    {
        var play = new CardPlay(Deal.FromNumber(7), new Contract(1, Strain.NoTrump, Seat.South));
        Assert.Equal(Seat.West, play.NextToPlay);
        Assert.False(play.DummyVisible);
    }

    [Fact]
    public void Must_follow_suit()
    {
        var play = new CardPlay(Deal, new Contract(1, Strain.Spades, Seat.North));
        // East leads a diamond; South has none and may play anything, West must follow.
        play.Play(Pbn.Card("DA"));
        Assert.Equal(Seat.South, play.NextToPlay);
        Assert.Equal(13, play.LegalCards().Count);
        play.Play(Pbn.Card("S2"));
        Assert.Equal(Seat.West, play.NextToPlay);
        Assert.Equal(13, play.LegalCards().Count);   // West has only clubs
        play.Play(Pbn.Card("CA"));
        Assert.Equal(Seat.North, play.NextToPlay);
        Assert.Throws<InvalidPlayException>(() => play.Play(Pbn.Card("DK")));   // not North's card
    }

    [Fact]
    public void Trump_wins_and_winner_leads_next()
    {
        var play = new CardPlay(Deal, new Contract(1, Strain.Spades, Seat.North));
        play.Play(Pbn.Card("DA"));   // East
        play.Play(Pbn.Card("S2"));   // South ruffs
        play.Play(Pbn.Card("CA"));   // West
        play.Play(Pbn.Card("H7"));   // North
        Assert.Equal(Seat.South, play.Winner(play.Tricks[0]));
        Assert.Equal(Seat.South, play.NextToPlay);
        Assert.Equal(1, play.TricksWon(northSouth: true));
    }

    [Fact]
    public void Highest_of_led_suit_wins_in_no_trump()
    {
        var play = new CardPlay(Deal, new Contract(1, Strain.NoTrump, Seat.North));
        play.Play(Pbn.Card("D5"));   // East
        play.Play(Pbn.Card("S2"));   // South discards
        play.Play(Pbn.Card("CA"));   // West discards
        play.Play(Pbn.Card("SA"));   // North discards a high spade
        Assert.Equal(Seat.East, play.Winner(play.Tricks[0]));
    }

    [Fact]
    public void Card_not_in_hand_is_rejected()
    {
        var play = new CardPlay(Deal, new Contract(1, Strain.NoTrump, Seat.North));
        Assert.Throws<InvalidPlayException>(() => play.Play(Pbn.Card("SA")));
    }

    [Fact]
    public void Bot_view_hides_other_hands()
    {
        var play = new CardPlay(Deal.FromNumber(99), new Contract(1, Strain.NoTrump, Seat.South));
        var west = play.ViewFor(Seat.West);
        Assert.Equal([Seat.West], west.Visible.Keys);   // before the opening lead: only its own hand
        play.Play(EasyBot.Choose(west));
        var north = play.ViewFor(Seat.North);
        Assert.Equal([Seat.North, Seat.South], north.Visible.Keys.Order());   // dummy sees declarer's hand
        var east = play.ViewFor(Seat.East);
        Assert.Equal([Seat.North, Seat.East], east.Visible.Keys.Order());   // defender sees own hand and dummy
    }
}
