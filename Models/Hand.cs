// Models/Hand.cs
public class Hand
{
    private readonly List<Card> _cards = new();
    public IReadOnlyList<Card> Cards => _cards;
    public int Count => _cards.Count;

    public Card Add(Card card) { _cards.Add(card); return card; }

    public void Clear() => _cards.Clear();

    public int BestValue
    {
        get
        {
            int minTotal = _cards.Sum(c => c.Rank.BlackjackValues()[0]); // all Aces as 1
            int aces = _cards.Count(c => c.Rank == Rank.Ace);
            int total = minTotal;
            while (aces > 0 && total + 10 <= 21)
            {
                total += 10; // upgrade one Ace from 1 to 11
                aces--;
            }
            return total;
        }
    }

    public bool IsSoft
    {
        get
        {
            int minTotal = _cards.Sum(c => c.Rank.BlackjackValues()[0]);
            return _cards.Any(c => c.Rank == Rank.Ace) && BestValue != minTotal;
        }
    }

    public bool IsBust => BestValue > 21;
    public bool IsBlackjack => _cards.Count == 2 && BestValue == 21;

    public override string ToString()
    {
        if (_cards.Count == 0) return "(empty)";

        var cards = string.Join(" ", _cards);

        if (IsBlackjack) return $"{cards} (Blackjack)";
        if (IsBust) return $"{cards} (BUST {BestValue})";

        return $"{cards} ({BestValue}{(IsSoft ? " soft" : "")})";
    }
}