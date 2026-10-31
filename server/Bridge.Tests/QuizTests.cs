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
            else
            {
                Assert.Equal(Call.Pass, q.Before[1].Call);
                Assert.Equal(kind == QuizKind.NtResponse, q.Before[0].Call == Call.Parse("1N"));
            }
            answers.Add(q.Answer.Call.ToString());
        }
        Assert.True(answers.Count >= 4, $"Too few different answers: {string.Join(", ", answers)}");
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
