public class Table
{
    public Player Dealer { get; }
    public List<Player> Players { get; }

    private readonly int _deckCount;
    private readonly bool _shuffle;

    public Table(int deckCount = 1, bool shuffle = true)
    {
        _deckCount = deckCount;
        _shuffle = shuffle;
        Dealer = Player.Dealer();
        Players = new List<Player>();
    }

    public Player AddPlayer(string? name = null)
    {
        var player = new Player(name ?? $"Player {Players.Count + 1}");
        Players.Add(player);
        return player;
    }

    public void PlayRound(int roundNumber, bool hitSoft17 = true, int minBet = 5)
    {
        var round = new Round(new Deck(_deckCount, _shuffle), Dealer, Players, minBet);
        round.Play(roundNumber, hitSoft17);
    }

    public void PlayUntilOneRemaining(int minBet = 5, bool hitSoft17 = true)
    {
        Console.WriteLine("=== Game Start ===");
        foreach (var player in Players)
        {
            Console.WriteLine($"{player.Name} joins the game with balance: ${player.Balance}");
        }

        int roundNumber = 1;
        while (true)
        {
            RemoveBankruptPlayers(minBet);

            if (Players.Count == 0)
            {
                Console.WriteLine("No players left with sufficient balance.");
                return;
            }

            if (Players.Count == 1)
            {
                var winner = Players[0];
                Console.WriteLine($"Winner: {winner.Name} (Balance: ${winner.Balance})");
                return;
            }

            PlayRound(roundNumber, hitSoft17, minBet);
            roundNumber++;
        }
    }

    private void RemoveBankruptPlayers(int minBet)
    {
        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (Players[i].Balance < minBet)
            {
                Console.WriteLine($"{Players[i].Name} is eliminated (Balance: ${Players[i].Balance}).");
                Players.RemoveAt(i);
            }
        }
    }
}