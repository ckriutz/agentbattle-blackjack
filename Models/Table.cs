public class Table
{
    public Player Dealer { get; }
    public List<Player> Players { get; }
    public GameLogger Logger { get; }

    private readonly int _deckCount;
    private readonly bool _shuffle;

    public Table(int deckCount = 1, bool shuffle = true, GameLogger? logger = null)
    {
        _deckCount = deckCount;
        _shuffle = shuffle;
        Dealer = Player.Dealer();
        Players = new List<Player>();
        Logger = logger ?? new GameLogger();
    }

    public Player AddPlayer(string? name = null)
    {
        var player = new Player(name ?? $"Player {Players.Count + 1}");
        Players.Add(player);
        return player;
    }

    public async Task PlayRoundAsync(int roundNumber, bool hitSoft17 = true, int minBet = 5)
    {
        var round = new Round(new Deck(_deckCount, _shuffle), Dealer, Players, minBet, Logger);
        await round.PlayAsync(roundNumber, hitSoft17);
    }

    public async Task PlayUntilOneRemainingAsync(int minBet = 5, bool hitSoft17 = true, int maxRounds = 50)
    {
        Logger.Log("=== Game Start ===");
        foreach (var player in Players)
        {
            Logger.Log($"{player.Name} joins the game with balance: ${player.Balance}");
        }

        int roundNumber = 1;
        while (true)
        {
            RemoveBankruptPlayers(minBet);

            if (Players.Count == 0)
            {
                Logger.Log("No players left with sufficient balance.");
                return;
            }

            if (Players.Count == 1)
            {
                var winner = Players[0];
                Logger.Log($"Winner: {winner.Name} (Balance: ${winner.Balance})");
                return;
            }

            // Max rounds reached - highest balance wins
            if (roundNumber > maxRounds)
            {
                var winner = Players.OrderByDescending(p => p.Balance).First();
                Logger.LogEmpty();
                Logger.Log("=== MAX ROUNDS REACHED ===");
                Logger.Log($"Winner by highest balance: {winner.Name} (Balance: ${winner.Balance})");

                Logger.LogEmpty();
                Logger.Log("Final Standings:");
                foreach (var player in Players.OrderByDescending(p => p.Balance))
                {
                    Logger.Log($"  {player.Name}: ${player.Balance}");
                }
                return;
            }

            await PlayRoundAsync(roundNumber, hitSoft17, minBet);
            roundNumber++;
        }
    }

    private void RemoveBankruptPlayers(int minBet)
    {
        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (Players[i].Balance < minBet)
            {
                Logger.Log($"{Players[i].Name} is eliminated (Balance: ${Players[i].Balance}).");
                Players.RemoveAt(i);
            }
        }
    }
}