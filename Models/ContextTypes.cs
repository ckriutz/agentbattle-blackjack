public record BetContext(string PlayerName, int Balance, int MinBet, IReadOnlyList<OpponentInfo> Opponents);

public record ActionContext(
    string PlayerName,
    string HandDescription,
    int HandValue,
    bool IsSoft,
    Card DealerUpCard,
    int CurrentBet,
    int Balance,
    bool CanDoubleDown,
    IReadOnlyList<OpponentHandInfo> OtherPlayers,
    IReadOnlyList<ActionHistoryEntry> ActionHistory
);