public class Round
{
    private readonly Deck _deck;
    private readonly Player _dealer;
    private readonly IReadOnlyList<Player> _players;
    private readonly int _minBet;
    private readonly GameLogger _logger;

    public Round(Deck deck, Player dealer, IReadOnlyList<Player> players, int minBet, GameLogger logger)
    {
        _deck = deck ?? throw new ArgumentNullException(nameof(deck));
        _dealer = dealer ?? throw new ArgumentNullException(nameof(dealer));
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _minBet = minBet;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PlayAsync(int roundNumber, bool hitSoft17 = true)
    {
        _logger.LogEmpty();
        _logger.Log($"=== New Round #{roundNumber} ===");

        _dealer.ClearForNewRound();
        foreach (var player in _players)
        {
            player.ClearForNewRound();
        }

        var activePlayers = await CollectBetsAsync();
        if (activePlayers.Count == 0)
        {
            _logger.Log("No active players with valid bets.");
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
        var dealerUpCard = _dealer.Hand.Cards[0];

        _logger.Log($"Dealer shows: {dealerUpCard} ??");
        foreach (var player in activePlayers)
            _logger.Log($"{player.Name}: {player.Hand}");

        // Players' turns
        foreach (var player in activePlayers)
        {
            if (player.Hand.IsBlackjack)
            {
                _logger.Log($"{player.Name} has Blackjack.");
                continue;
            }

            await PlayPlayerTurnAsync(player, dealerUpCard, activePlayers);
        }

        // Dealer turn
        PlayDealerTurn(hitSoft17);

        // Results
        ResolveResults(activePlayers);
    }

    private async Task PlayPlayerTurnAsync(Player player, Card dealerUpCard, List<Player> allPlayers)
    {
        while (!player.Hand.IsBust && !player.Hand.IsBlackjack)
        {
            var context = BuildActionContext(player, dealerUpCard, allPlayers);
            var decision = await player.DecideActionAsync(context);

            _logger.Log($"{player.Name} [{player.ModelName}]: {decision.Action}");
            _logger.Log($"  Reasoning: \"{decision.Reasoning}\"");

            if (decision.Action == PlayerAction.DoubleDown)
            {
                var handBefore = player.Hand.ToString();
                var valueBefore = player.Hand.BestValue;

                if (player.TryDoubleDown())
                {
                    var drawn = _deck.Draw();
                    player.Receive(drawn);
                    player.RecordAction(PlayerAction.DoubleDown, decision.Reasoning, drawn);
                    _logger.Log($"  Doubled down, drew {drawn} -> {player.Hand} (Bet: ${player.CurrentBet})");
                }
                else
                {
                    var drawn = _deck.Draw();
                    player.Receive(drawn);
                    player.RecordAction(PlayerAction.Hit, decision.Reasoning, drawn);
                    _logger.Log($"  (Couldn't double) Hit, drew {drawn} -> {player.Hand}");
                }
                break;
            }

            if (decision.Action == PlayerAction.Hit)
            {
                var drawn = _deck.Draw();
                player.RecordAction(PlayerAction.Hit, decision.Reasoning, drawn);
                player.Receive(drawn);
                _logger.Log($"  Drew {drawn} -> {player.Hand}");

                if (player.Hand.IsBust)
                    _logger.Log($"  {player.Name} BUSTS!");
                continue;
            }

            // Stand
            player.RecordAction(PlayerAction.Stand, decision.Reasoning, null);
            _logger.Log($"  Stands with {player.Hand}");
            break;
        }
    }

    private ActionContext BuildActionContext(Player player, Card dealerUpCard, List<Player> allPlayers)
    {
        var otherPlayers = allPlayers
        .Where(p => p != player)
        .Select(p => new OpponentHandInfo(p.Name, p.Hand.ToString(), p.Hand.BestValue, p.CurrentBet, p.Hand.IsBust)).ToList();

        return new ActionContext(
            player.Name,
            player.Hand.ToString(),
            player.Hand.BestValue,
            player.Hand.IsSoft,
            dealerUpCard,
            player.CurrentBet,
            player.Balance,
            player.CanDoubleDown,
            otherPlayers,
            player.RoundHistory
        );
    }

    private async Task<List<Player>> CollectBetsAsync()
    {
        var active = new List<Player>();
        var placedBets = new Dictionary<Player, int>();

        foreach (var player in _players)
        {
            // Needs at least min bet to play.
            if (player.Balance < _minBet)
            {
                _logger.Log($"{player.Name} cannot meet minimum bet, sits out.");
                continue;
            }

            // Build context with bets placed so far
            var opponents = placedBets
                .Select(kvp => new OpponentInfo(kvp.Key.Name, kvp.Key.Balance + kvp.Value, kvp.Value))
                .Concat(_players.Where(p => p != player && !placedBets.ContainsKey(p))
                .Select(p => new OpponentInfo(p.Name, p.Balance, 0)))
                .ToList();

            var context = new BetContext(player.Name, player.Balance, _minBet, opponents);
            var decision = await player.DecideBetAsync(context);

            player.PlaceBet(decision.Amount);
            placedBets[player] = decision.Amount;

            _logger.Log($"{player.Name} [{player.ModelName}] bets ${decision.Amount}");
            _logger.Log($"  Reasoning: \"{decision.Reasoning}\"");

            active.Add(player);
        }

        return active;
    }

    private void PlayDealerTurn(bool hitSoft17)
    {
        _logger.LogEmpty();
        _logger.Log($"Dealer reveals: {_dealer.Hand}");

        if (_dealer.Hand.IsBlackjack)
        {
            _logger.Log("Dealer has Blackjack!");
            return;
        }

        while (true)
        {
            int dealerValue = _dealer.Hand.BestValue;
            bool isSoft = _dealer.Hand.IsSoft;

            // Dealer stands on 17+ (or 18+ if hitSoft17 is false and hand is soft)
            if (dealerValue > 17)
                break;
            if (dealerValue == 17 && (!hitSoft17 || !isSoft))
                break;

            var drawn = _deck.Draw();
            _dealer.Receive(drawn);
            _logger.Log($"Dealer draws {drawn} -> {_dealer.Hand}");

            if (_dealer.Hand.IsBust)
            {
                _logger.Log("Dealer BUSTS!");
                break;
            }
        }

        if (!_dealer.Hand.IsBust)
            _logger.Log($"Dealer stands with {_dealer.Hand}");
    }

    private void ResolveResults(List<Player> activePlayers)
    {
        _logger.LogEmpty();
        _logger.Log("=== Results ===");

        foreach (var player in activePlayers)
        {
            var outcome = Evaluate(player);
            int betAmount = player.CurrentBet; // Capture bet before it's zeroed out
            
            switch (outcome)
            {
                case Outcome.BlackjackWin:
                    player.WinBlackjack();
                    _logger.Log($"{player.Name}: BLACKJACK! Wins ${betAmount + (betAmount * 3) / 2} (New balance: ${player.Balance})");
                    break;
                case Outcome.Win:
                    player.WinBet();
                    _logger.Log($"{player.Name}: WIN! Wins ${betAmount} (New balance: ${player.Balance})");
                    break;
                case Outcome.Push:
                    player.PushBet();
                    _logger.Log($"{player.Name}: PUSH (New balance: ${player.Balance})");
                    break;
                case Outcome.Lose:
                    player.LoseBet();
                    _logger.Log($"{player.Name}: LOSE (New balance: ${player.Balance})");
                    break;
            }
        }
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