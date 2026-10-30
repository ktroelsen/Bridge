using Bridge.Engine;

namespace Bridge.Tests;

public class MiniBridgeTests
{
    [Fact]
    public void Pair_with_most_points_declares_and_stronger_partner_is_declarer()
    {
        for (var n = 1; n <= 300; n++)
        {
            var deal = Deal.FromNumber(n);
            var ns = deal[Seat.North].Hcp + deal[Seat.South].Hcp;
            var declarer = MiniBridge.Declarer(deal);
            if (ns == 20) { Assert.Null(declarer); continue; }
            Assert.Equal(ns > 20, declarer!.Value.IsNorthSouth());
            Assert.True(deal[declarer.Value].Hcp >= deal[declarer.Value.Partner()].Hcp);
        }
    }

    [Fact]
    public void Tie_in_the_pair_goes_to_first_speaker_from_dealer()
    {
        // North and South have 12 each; board 2 has East as dealer, so South speaks before North.
        var deal = Pbn.Deal(
            north: "AK32.Q32.J32.Q43", east: "Q54.K54.Q54.T652", south: "J76.A76.K76.KJ87", west: "T98.JT98.AT98.A9", board: 2);
        Assert.Equal(12, deal[Seat.North].Hcp);
        Assert.Equal(12, deal[Seat.South].Hcp);
        Assert.Equal(Seat.South, MiniBridge.Declarer(deal));
    }

    [Theory]
    [InlineData(Strain.NoTrump, true, 3)]
    [InlineData(Strain.Hearts, true, 4)]
    [InlineData(Strain.Clubs, true, 5)]
    [InlineData(Strain.Spades, false, 1)]
    public void Contract_choice_is_part_score_or_game(Strain strain, bool game, int level) =>
        Assert.Equal(level, MiniBridge.ContractFor(Seat.South, strain, game).Level);

    [Fact]
    public void Bot_declarer_without_major_fit_plays_no_trump_with_25_points()
    {
        var deal = Pbn.Deal(
            north: "T98.JT987.AT98.A", east: "AKQ2.K32.J32.432", south: "J76.Q54.Q54.JT65", west: "543.A6.K76.KQ987");
        // East-West: 25 points; spades 4+3 = 7 (no fit), clubs 3+5 = 8 → 3UT on points.
        var contract = MiniBridge.BotContract(deal, Seat.East);
        Assert.Equal(Strain.NoTrump, contract.Strain);
        Assert.Equal(3, contract.Level);
    }

    [Fact]
    public void Redeal_has_no_declarer()
    {
        var number = Enumerable.Range(1, 2000).First(n => MiniBridge.Declarer(Deal.FromNumber(n)) is null);
        var game = MiniBridgeGame.Replay(number, null, false, []);
        Assert.Equal(GamePhase.Redeal, game.Phase);
    }

    [Fact]
    public void Human_declarer_must_choose_contract_first()
    {
        var number = Enumerable.Range(1, 2000).First(n => MiniBridge.Declarer(Deal.FromNumber(n))?.IsNorthSouth() == true);
        Assert.Equal(GamePhase.ChooseContract, MiniBridgeGame.Replay(number, null, false, []).Phase);
        Assert.Equal(GamePhase.Play, MiniBridgeGame.Replay(number, Strain.NoTrump, false, []).Phase);
    }

    /// <summary>Plays whole games where the human's cards are chosen by the bot too.</summary>
    [Fact]
    public void Whole_games_complete_with_legal_bot_play()
    {
        var played = 0;
        for (var n = 1; played < 150; n++)
        {
            if (MiniBridge.Declarer(Deal.FromNumber(n)) is not { } declarer) continue;
            Strain? strain = declarer.IsNorthSouth() ? (Strain)(n % 5) : null;
            var human = new List<Card>();
            MiniBridgeGame game;
            while (true)
            {
                game = MiniBridgeGame.Replay(n, strain, n % 2 == 0, human);
                if (game.Phase == GamePhase.Done) break;
                Assert.Equal(GamePhase.Play, game.Phase);
                Assert.True(game.HumanControls(game.Play!.NextToPlay));
                human.Add(EasyBot.Choose(game.Play.ViewFor(game.Play.NextToPlay)));
            }
            Assert.Equal(13, game.Play!.TricksWon(true) + game.Play.TricksWon(false));
            Assert.NotNull(game.ScoreNorthSouth);
            played++;
        }
    }

    [Fact]
    public void Too_many_cards_are_rejected()
    {
        var number = Enumerable.Range(1, 2000).First(n => MiniBridge.Declarer(Deal.FromNumber(n))?.IsNorthSouth() == false);
        var game = MiniBridgeGame.Replay(number, null, false, []);
        var legal = game.Play!.LegalCards()[0];
        var other = Deal.FromNumber(number)[Seat.South].Cards.First(c => c != legal);
        Assert.Throws<InvalidPlayException>(() => MiniBridgeGame.Replay(number, null, false, [legal, other, other]));
    }
}
