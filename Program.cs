Table table = new Table(deckCount: 2, shuffle: true);
table.AddPlayer("Alice");
table.AddPlayer("Bob");
table.PlayUntilOneRemaining(minBet: 5, hitSoft17: true);