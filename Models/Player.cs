public class Player
{
    public string Name { get; }
    public Hand Hand { get; }
    public bool IsDealer { get; }
    public int Balance { get; set; } = 500;

    // New: the wager for the current round
    public int CurrentBet { get; private set; }

    public Player(string name, bool isDealer = false)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? (isDealer ? "Dealer" : "Player")
            : name;
        IsDealer = isDealer;
        Hand = new Hand();
    }

    public static Player Dealer(string name = "Dealer") => new Player(name, isDealer: true);

    public Card Receive(Card card) => Hand.Add(card);
    public void ClearForNewRound()
    {
        Hand.Clear();
        CurrentBet = 0;
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

    public PlayerAction Act()
    {
        // If hand is bust or blackjack, no action needed
        if (Hand.IsBust || Hand.IsBlackjack) return PlayerAction.Stand;

        if (IsDealer) return PlayerAction.Stand;

        // Random for now; AI strategy will replace this later
        if (CanDoubleDown && Hand.BestValue >= 9 && Hand.BestValue <= 11)
            return PlayerAction.DoubleDown;
        if (Hand.BestValue < 17) return PlayerAction.Hit;
        return PlayerAction.Stand;
    }
}