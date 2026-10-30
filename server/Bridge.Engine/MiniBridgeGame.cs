namespace Bridge.Engine;

public enum GamePhase { Redeal, ChooseContract, Play, Done }

/// <summary>
/// A mini-bridge game against the bot, rebuilt from the deal number, the human's contract
/// choice and the human's cards. The human sits South and also plays North when North-South
/// declare (the declarer plays the dummy). The bot plays every other seat.
/// </summary>
public sealed class MiniBridgeGame
{
    private MiniBridgeGame(Deal deal, Seat? declarer, Contract? contract, CardPlay? play)
    {
        Deal = deal;
        Declarer = declarer;
        Contract = contract;
        Play = play;
    }

    public Deal Deal { get; }

    public Seat? Declarer { get; }

    public Contract? Contract { get; }

    public CardPlay? Play { get; }

    public GamePhase Phase => Declarer is null ? GamePhase.Redeal
        : Play is null ? GamePhase.ChooseContract
        : Play.IsComplete ? GamePhase.Done
        : GamePhase.Play;

    public bool HumanDeclares => Declarer?.IsNorthSouth() == true;

    public bool HumanControls(Seat seat) => seat == Seat.South || (HumanDeclares && seat == Seat.North);

    /// <summary>The score for North-South (when the game is over).</summary>
    public int? ScoreNorthSouth
    {
        get
        {
            if (Phase != GamePhase.Done) return null;
            var declarerNs = Contract!.Declarer.IsNorthSouth();
            var vulnerable = Deal.Vulnerability == Vulnerability.Both
                || Deal.Vulnerability == (declarerNs ? Vulnerability.NorthSouth : Vulnerability.EastWest);
            var score = Scoring.Score(Contract, vulnerable, Play!.DeclarerTricks);
            return declarerNs ? score : -score;
        }
    }

    public static MiniBridgeGame Replay(int number, Strain? strain, bool game, IReadOnlyList<Card> humanCards)
    {
        var deal = Deal.FromNumber(number);
        var declarer = MiniBridge.Declarer(deal);
        if (declarer is not { } d)
        {
            if (humanCards.Count > 0) throw new InvalidPlayException("Spillet skal gives om (20–20).");
            return new MiniBridgeGame(deal, null, null, null);
        }

        Contract contract;
        if (d.IsNorthSouth())
        {
            if (strain is not { } s)
            {
                if (humanCards.Count > 0) throw new InvalidPlayException("Vælg kontrakt, før der spilles.");
                return new MiniBridgeGame(deal, d, null, null);
            }
            contract = MiniBridge.ContractFor(d, s, game);
        }
        else
        {
            contract = MiniBridge.BotContract(deal, d);
        }

        var play = new CardPlay(deal, contract);
        var result = new MiniBridgeGame(deal, d, contract, play);
        var next = 0;
        while (!play.IsComplete)
        {
            var seat = play.NextToPlay;
            if (result.HumanControls(seat))
            {
                if (next == humanCards.Count) break;
                play.Play(humanCards[next++]);
            }
            else
            {
                play.Play(EasyBot.Choose(play.ViewFor(seat)));
            }
        }
        if (next < humanCards.Count) throw new InvalidPlayException("For mange kort.");
        return result;
    }
}
