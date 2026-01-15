using System;

// Models/Suit.cs
public enum Suit { Clubs, Diamonds, Hearts, Spades }

// Models/Rank.cs
public enum Rank
{
    Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace
}

public static class SuitExtensions
{
    public static string ToSymbol(this Suit suit) => suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?"
    };
}

public static class RankExtensions
{
    public static string ToShortString(this Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.King => "K",
        Rank.Queen => "Q",
        Rank.Jack => "J",
        _ => ((int)rank).ToString()
    };

    // For blackjack scoring
    public static IReadOnlyList<int> BlackjackValues(this Rank rank) => rank switch
    {
        Rank.Ace => new[] { 1, 11 },
        Rank.King or Rank.Queen or Rank.Jack => new[] { 10 },
        _ => new[] { (int)rank }
    };
}

// Models/Card.cs
public readonly record struct Card(Rank Rank, Suit Suit)
{
    public override string ToString()
    {
        return $"{Rank.ToShortString()}{Suit.ToSymbol()}";
    }
}