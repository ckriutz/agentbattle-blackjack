public class Round
{
    private readonly Deck _deck;
    private readonly Player _dealer;
    private readonly IReadOnlyList<Player> _players;
    private readonly int _minBet;

    public Round(Deck deck, Player dealer, IReadOnlyList<Player> players, int minBet = 5)
    {
        _deck = deck ?? throw new ArgumentNullException(nameof(deck));
        _dealer = dealer ?? throw new ArgumentNullException(nameof(dealer));
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _minBet = minBet;
    }

    public void Play(int roundNumber, bool hitSoft17 = true)
    {
        Console.WriteLine();
        Console.WriteLine($"=== New Round #{roundNumber} ===");

        _dealer.ClearForNewRound();
        foreach (var player in _players)
        {
            player.ClearForNewRound();
        }

        var activePlayers = CollectBets();
        if (activePlayers.Count == 0)
        {
            Console.WriteLine("No active players with valid bets.");
            return;
        }

        // Initial deal
        foreach (var player in activePlayers)
        {
            player.Receive(_deck.Draw());
            player.Receive(_deck.Draw());
        }
        _dealer.Receive(_deck.Draw());
        _dealer.Receive(_deck.Draw());

        // Show initial hands (hide dealer hole card)
        var dealerUpCard = _dealer.Hand.Cards.Count > 0 ? _dealer.Hand.Cards[0] : default;
        Console.WriteLine($"Dealer shows: {dealerUpCard} ??");
        foreach (var player in activePlayers)
            Console.WriteLine($"{player.Name}: {player.Hand}");

        // Players' turns
        foreach (var player in activePlayers)
        {
            if (player.Hand.IsBlackjack)
            {
                Console.WriteLine($"{player.Name} has Blackjack.");
                continue;
            }

            while (!player.Hand.IsBust)
            {
                var action = player.Act();
                if (action == PlayerAction.DoubleDown)
                {
                    if (player.TryDoubleDown())
                    {
                        var drawn = _deck.Draw();
                        player.Receive(drawn);
                        Console.WriteLine($"{player.Name} doubles down, draws {drawn} -> {player.Hand} (Total Bet: ${player.CurrentBet})");
                    }
                    else
                    {
                        var drawn = _deck.Draw();
                        player.Receive(drawn);
                        Console.WriteLine($"{player.Name} hits, draws {drawn} -> {player.Hand}");
                    }

                    break;
                }
                if (action == PlayerAction.Hit)
                {
                    var drawn = _deck.Draw();
                    player.Receive(drawn);
                    Console.WriteLine($"{player.Name} hits, draws {drawn} -> {player.Hand}");
                    continue;
                }

                Console.WriteLine($"{player.Name} stands -> {player.Hand}");
                break;
            }

            if (player.Hand.IsBust)
                Console.WriteLine($"{player.Name} busts -> {player.Hand}");
        }

        // Dealer turn (reveal hole card)
        Console.WriteLine($"Dealer reveals: {_dealer.Hand}");

        if (_dealer.Hand.IsBlackjack)
        {
            Console.WriteLine("Dealer has Blackjack.");
        }
        else
        {
            while (true)
            {
                int value = _dealer.Hand.BestValue;
                bool soft = _dealer.Hand.IsSoft;

                bool shouldHit = value < 17 || (hitSoft17 && value == 17 && soft);
                if (!shouldHit) break;

                var drawn = _deck.Draw();
                _dealer.Receive(drawn);
                Console.WriteLine($"Dealer hits, draws {drawn} -> {_dealer.Hand}");

                if (_dealer.Hand.IsBust)
                {
                    Console.WriteLine($"Dealer busts -> {_dealer.Hand}");
                    break;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Results:");

        foreach (var player in activePlayers)
        {
            var outcome = Evaluate(player);
            switch (outcome)
            {
                case Outcome.BlackjackWin:
                    player.WinBlackjack();
                    break;
                case Outcome.Win:
                    player.WinBet();
                    break;
                case Outcome.Push:
                    player.PushBet();
                    break;
                case Outcome.Lose:
                    player.LoseBet();
                    break;
            }

            Console.WriteLine($"{player.Name}: {player.Hand} vs Dealer {_dealer.Hand} => {outcome} (Balance: ${player.Balance})");
        }
    }

    private List<Player> CollectBets()
    {
        var active = new List<Player>();
        foreach (var player in _players)
        {
            if (player.Balance < _minBet)
            {
                Console.WriteLine($"{player.Name} cannot meet the minimum bet and sits out.");
                continue;
            }

            int bet = RandomBet(player.Balance);
            player.PlaceBet(bet);
            Console.WriteLine($"{player.Name} bets ${bet} (Remaining Balance: ${player.Balance})");
            active.Add(player);
        }

        return active;
    }

    private int RandomBet(int balance)
    {
        int maxUnits = balance / _minBet;
        int betUnits = Random.Shared.Next(1, maxUnits + 1);
        return betUnits * _minBet;
    }

    public enum Outcome { Win, Lose, Push, BlackjackWin }

    public Outcome Evaluate(Player player)
    {
        if (player.Hand.IsBlackjack)
        {
            if (_dealer.Hand.IsBlackjack) return Outcome.Push;
            return Outcome.BlackjackWin;
        }
        if (_dealer.Hand.IsBlackjack) return Outcome.Lose;
        if (player.Hand.IsBust) return Outcome.Lose;
        if (_dealer.Hand.IsBust) return Outcome.Win;

        var p = player.Hand.BestValue;
        var d = _dealer.Hand.BestValue;

        if (p > d) return Outcome.Win;
        if (p < d) return Outcome.Lose;

        return Outcome.Push;
    }
}