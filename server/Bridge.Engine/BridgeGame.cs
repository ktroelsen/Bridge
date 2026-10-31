using Bridge.Engine.Bidding;

namespace Bridge.Engine;

/// <summary>One call in the game with what it means, and for the human's calls what the system would have bid.</summary>
public sealed record CallRecord(Seat Seat, Call Call, string Explanation, BidChoice? System);

/// <summary>
/// A full game against the bots: bidding, then play. Rebuilt from the deal number and the
/// human's calls and cards. The human sits South and bids for South only; in the play they
/// also play North when North-South declare.
/// </summary>
public sealed class BridgeGame
{
    private readonly List<CallRecord> _calls = [];

    private BridgeGame(Deal deal)
    {
        Deal = deal;
        Auction = new Auction(deal.Dealer);
    }

    public Deal Deal { get; }

    public Auction Auction { get; }

    public IReadOnlyList<CallRecord> Calls => _calls;

    public Contract? Contract { get; private set; }

    public CardPlay? Play { get; private set; }

    public GamePhase Phase => !Auction.IsComplete ? GamePhase.Bidding
        : Auction.IsPassedOut ? GamePhase.PassedOut
        : Play!.IsComplete ? GamePhase.Done
        : GamePhase.Play;

    public bool HumanDeclares => Contract?.Declarer.IsNorthSouth() == true;

    public bool HumanControls(Seat seat) => seat == Seat.South || (Play is not null && HumanDeclares && seat == Seat.North);

    /// <summary>The system's call for South right now (the hint), while South is to bid.</summary>
    public BidChoice? Hint =>
        Phase == GamePhase.Bidding && Auction.NextSeat == Seat.South
            ? Bidder.Choose(Deal.Dealer, Auction.Calls, Seat.South, Deal[Seat.South])
            : null;

    public int? ScoreNorthSouth
    {
        get
        {
            if (Phase == GamePhase.PassedOut) return 0;
            if (Phase != GamePhase.Done) return null;
            var declarerNs = Contract!.Declarer.IsNorthSouth();
            var vulnerable = Deal.Vulnerability == Vulnerability.Both
                || Deal.Vulnerability == (declarerNs ? Vulnerability.NorthSouth : Vulnerability.EastWest);
            var score = Scoring.Score(Contract, vulnerable, Play!.DeclarerTricks);
            return declarerNs ? score : -score;
        }
    }

    public static BridgeGame Replay(int number, IReadOnlyList<Call> humanCalls, IReadOnlyList<Card> humanCards, BotLevel level = BotLevel.Easy)
    {
        var game = new BridgeGame(Deal.FromNumber(number));
        var deal = game.Deal;
        var auction = game.Auction;

        var nextCall = 0;
        while (!auction.IsComplete)
        {
            var seat = auction.NextSeat;
            var before = auction.Calls.ToList();
            if (seat == Seat.South)
            {
                if (nextCall == humanCalls.Count) break;
                var call = humanCalls[nextCall++];
                var system = Bidder.Choose(deal.Dealer, before, seat, deal[seat]);
                auction.Add(call);
                game._calls.Add(new CallRecord(seat, call, Bidder.Explain(deal.Dealer, before, seat, call), system));
            }
            else
            {
                var choice = Bidder.Choose(deal.Dealer, before, seat, deal[seat]);
                auction.Add(choice.Call);
                // Shown to the human: what the call means in the system, not the bot's own reasoning (that would reveal its hand).
                game._calls.Add(new CallRecord(seat, choice.Call, Bidder.Explain(deal.Dealer, before, seat, choice.Call), null));
            }
        }
        if (nextCall < humanCalls.Count) throw new InvalidPlayException("For mange meldinger.");
        if (!auction.IsComplete || auction.IsPassedOut)
        {
            if (humanCards.Count > 0) throw new InvalidPlayException("Der spilles ikke kort endnu.");
            return game;
        }

        game.Contract = auction.Contract();
        var play = game.Play = new CardPlay(deal, game.Contract!);
        // What each player's calls showed, for the bots' picture of the hidden hands.
        var shown = Enum.GetValues<Seat>().ToDictionary(s => s, s => Bidder.Model(deal.Dealer, auction.Calls, s));
        var nextCard = 0;
        while (!play.IsComplete)
        {
            var seat = play.NextToPlay;
            if (game.HumanControls(seat))
            {
                if (nextCard == humanCards.Count) break;
                play.Play(humanCards[nextCard++]);
            }
            else
            {
                play.Play(PlayBot.Choose(play, seat, level, s => shown[s]));
            }
        }
        if (nextCard < humanCards.Count) throw new InvalidPlayException("For mange kort.");
        return game;
    }
}
