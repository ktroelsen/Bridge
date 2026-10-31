using System.Net.Http.Json;
using Bridge.Api;
using Bridge.Engine;
using Bridge.Engine.Bidding;

namespace Bridge.Tests;

public class QuizTests(TestFactory factory) : IClassFixture<TestFactory>
{
    [Theory]
    [InlineData(QuizKind.Opening)]
    [InlineData(QuizKind.Response)]
    [InlineData(QuizKind.NtResponse)]
    [InlineData(QuizKind.Rebid)]
    [InlineData(QuizKind.NtOpener)]
    [InlineData(QuizKind.Overcall)]
    [InlineData(QuizKind.Advance)]
    public void Questions_are_answered_by_the_system_and_vary(QuizKind kind)
    {
        var random = new Random(7);
        var answers = new HashSet<string>();
        for (var i = 0; i < 40; i++)
        {
            var q = BiddingQuiz.Make(kind, random);
            Assert.Contains(q.Answer.Call, q.Legal);
            Assert.Equal(q.Answer.Call, Bidder.Choose(q.Dealer, [.. q.Before.Select(c => new AuctionCall(c.Seat, c.Call))], Seat.South, q.Hand).Call);
            if (kind == QuizKind.Opening) Assert.Empty(q.Before);
            if (kind is QuizKind.Response or QuizKind.NtResponse)
            {
                Assert.Equal(Call.Pass, q.Before[1].Call);
                Assert.Equal(kind == QuizKind.NtResponse, q.Before[0].Call == Call.Parse("1N"));
            }
            // South's own earlier calls are the system's, too.
            Assert.All(q.Before.Where(c => c.Seat == Seat.South), c => Assert.Equal(Seat.South, c.Seat));
            if (kind is QuizKind.Rebid or QuizKind.NtOpener) Assert.Equal(Seat.South, q.Before[0].Seat);
            answers.Add(q.Answer.Call.ToString());
        }
        Assert.True(answers.Count >= (kind == QuizKind.NtOpener ? 3 : 4), $"Too few different answers: {string.Join(", ", answers)}");
    }

    [Fact]
    public async Task Quiz_endpoint_returns_a_question()
    {
        var q = (await factory.CreateClient().GetFromJsonAsync<BiddingQuestionDto>("/api/quiz/bidding?kind=response", TestFactory.Json))!;
        Assert.Equal(13, q.Hand.Count);
        Assert.Equal(2, q.Auction.Count);
        Assert.Contains(q.Answer.Call, q.LegalCalls);
        Assert.False(string.IsNullOrWhiteSpace(q.Answer.Explanation));
    }
}

public class PlayQuizTests(TestFactory factory) : IClassFixture<TestFactory>
{
    [Theory]
    [InlineData(3, 4)]
    [InlineData(6, 9)]
    public void Problems_have_north_south_on_lead_and_a_wrong_card(int from, int to)
    {
        var random = new Random(3);
        for (var i = 0; i < 5; i++)
        {
            var q = Bridge.Engine.Analysis.PlayQuiz.Make(from, to, random, TimeSpan.FromSeconds(30));
            Assert.True(q.Play.NextToPlay.IsNorthSouth());
            Assert.True(q.Play.Contract.Declarer.IsNorthSouth());
            Assert.Empty(q.Play.CurrentTrick.Plays);
            Assert.InRange(q.Play.Tricks.Count, from, to);
            Assert.True(q.Best.Values.Values.Min() < q.Best.SideTricks);
            Assert.All(q.Best.Cards, c => Assert.Contains(c, q.Play.LegalCards()));
        }
    }

    [Fact]
    public async Task Play_quiz_endpoint_returns_a_problem()
    {
        var q = (await factory.CreateClient().GetFromJsonAsync<PlayQuestionDto>("/api/quiz/play?stage=early", TestFactory.Json))!;
        Assert.Equal(4, q.Hands.Count);
        Assert.NotEmpty(q.Best);
        Assert.Equal(q.LegalCards.Count, q.Values.Count);
    }
}
