// Models/Deck.cs
public class Deck
{
    private readonly List<Card> _cards;
    private int _index;

    public Deck(int deckCount = 1, bool shuffle = true)
    {
        _cards = Enumerable.Range(0, deckCount)
            .SelectMany(_ => AllCards())
            .ToList();
        if (shuffle) Shuffle();
    }

    public bool HasCards => _index < _cards.Count;
    public int Remaining => _cards.Count - _index;

    public Card Draw() =>
        HasCards ? _cards[_index++] : throw new InvalidOperationException("Deck is empty");

    public void Shuffle()
    {
        var rng = Random.Shared;
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        _index = 0;
    }

    private static IEnumerable<Card> AllCards() =>
        from suit in Enum.GetValues<Suit>()
        from rank in Enum.GetValues<Rank>()
        select new Card(rank, suit);
}