using Bridge.Engine;

namespace Bridge.Tests;

/// <summary>Hands and deals written PBN style: spades.hearts.diamonds.clubs, e.g. "AKQ2.J52.T93.842".</summary>
public static class Pbn
{
    private static readonly Suit[] Order = [Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs];

    public static Hand Hand(string pbn) =>
        new(pbn.Split('.').SelectMany((s, i) => s.Select(r => new Card(Order[i], (Rank)("23456789TJQKA".IndexOf(r) + 2)))));

    public static Deal Deal(string north, string east, string south, string west, int board = 1) =>
        Engine.Deal.FromHands([Hand(north), Hand(east), Hand(south), Hand(west)], board);

    public static Card Card(string text) =>
        new((Suit)"CDHS".IndexOf(text[0]), (Rank)("23456789TJQKA".IndexOf(text[1]) + 2));
}
