public class Player
{
    public string Name { get; }
    public Hand Hand { get; }
    public bool IsDealer { get; }
    public int Balance { get; set; } = 50;
    public int CurrentBet { get; private set; }

    // The Strategy is going to match with an object that implements IPlayerStrategy.
    // This could be an AI, or just a basic strategy implementation.
    public IPlayerStrategy? Strategy { get; set; }
    public string ModelName { get; set; } = "BasicStrategy";
    public List<ActionHistoryEntry> RoundHistory { get; } = new List<ActionHistoryEntry>();

    public Player(string name, bool isDealer = false)
    {
        Name = string.IsNullOrWhiteSpace(name) ? (isDealer ? "Dealer" : "Player") : name;
        IsDealer = isDealer;
        Hand = new Hand();
    }

    public static Player Dealer(string name = "Dealer") => new Player(name, isDealer: true);

    public Card Receive(Card card) => Hand.Add(card);
    public void ClearForNewRound()
    {
        Hand.Clear();
        CurrentBet = 0;
        RoundHistory.Clear(); // NEW: Clear history each round
    }

    public void PlaceBet(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Bet must be positive.");
        if (amount > Balance) throw new InvalidOperationException("Bet exceeds available balance.");
        Balance -= amount;
        CurrentBet = amount;
    }

    public bool CanDoubleDown => CurrentBet > 0 && Balance >= CurrentBet && Hand.Cards.Count == 2;

    public bool TryDoubleDown()
    {
        if (!CanDoubleDown) return false;
        Balance -= CurrentBet;
        CurrentBet *= 2;
        return true;
    }

    public void WinBet()
    {
        if (CurrentBet <= 0) return;
        Balance += CurrentBet * 2;
        CurrentBet = 0;
    }

    public void WinBlackjack()
    {
        if (CurrentBet <= 0) return;
        Balance += CurrentBet + (CurrentBet * 3) / 2;
        CurrentBet = 0;
    }

    public void PushBet()
    {
        if (CurrentBet <= 0) return;
        Balance += CurrentBet;
        CurrentBet = 0;
    }

    public void LoseBet()
    {
        CurrentBet = 0;
    }
    
    public bool HasCards => Hand.Cards.Count > 0;

    public override string ToString() => $"{Name}: {Hand}";

    // When the player does things, we record them here for future AI decisions.
    public void RecordAction(PlayerAction action, string reasoning, Card? cardReceived)
    {
        RoundHistory.Add(new ActionHistoryEntry(Hand.ToString(), Hand.BestValue, action, cardReceived, reasoning));
    }

    public async Task<BetDecision> DecideBetAsync(BetContext context)
    {
        if (Strategy == null)
        {
            var amount = RandomBet(context.MinBet, context.Balance);
            return new BetDecision(amount, "Basic strategy: random bet");
        }
        return await Strategy.DecideBetAsync(context);
    }

    public async Task<ActionDecision> DecideActionAsync(ActionContext context)
    {
        if (Strategy == null)
        {
            var action = BasicStrategyAction();
            return new ActionDecision(action, "Basic strategy fallback");
        }
        return await Strategy.DecideActionAsync(context);
    }

    // This is a generic strategy if we need it.
    private PlayerAction BasicStrategyAction()
    {
        if (Hand.IsBust || Hand.IsBlackjack) return PlayerAction.Stand;
        if (IsDealer) return PlayerAction.Stand;

        if (CanDoubleDown && Hand.BestValue >= 9 && Hand.BestValue <= 11)
            return PlayerAction.DoubleDown;
        if (Hand.BestValue < 17) return PlayerAction.Hit;
        return PlayerAction.Stand;
    }

    private static int RandomBet(int minBet, int balance)
    {
        int maxUnits = balance / minBet;
        int betUnits = Random.Shared.Next(1, maxUnits + 1);
        return betUnits * minBet;
    }
}