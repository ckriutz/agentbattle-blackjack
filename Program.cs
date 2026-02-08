using System.Diagnostics;

using var logger = new GameLogger();
var table = new Table(deckCount: 2, shuffle: true, logger: logger);

var player1 = table.AddPlayer("Xiaomi Mimo V2 Flash");
player1.Strategy = new AIPlayerStrategy("xiaomi/mimo-v2-flash", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"), "https://openrouter.ai/api/v1");
player1.ModelName = "xiaomi/mimo-v2-flash";

var player2 = table.AddPlayer("Baidu Ernie 4.5");
player2.Strategy = new AIPlayerStrategy("baidu/ernie-4.5-21b-a3b-thinking", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"), "https://openrouter.ai/api/v1");
player2.ModelName = "baidu/ernie-4.5-21b-a3b-thinking";

var player3 = table.AddPlayer("ByteDance Seed 1.6");
player3.Strategy = new AIPlayerStrategy("bytedance-seed/seed-1.6", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"), "https://openrouter.ai/api/v1");
player3.ModelName = "bytedance-seed/seed-1.6";

var player4 = table.AddPlayer("Minimax M2.1");
player4.Strategy = new AIPlayerStrategy("minimax/minimax-m2.1", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"), "https://openrouter.ai/api/v1");
player4.ModelName = "minimax/minimax-m2.1";

var gameTimer = Stopwatch.StartNew();
await table.PlayUntilOneRemainingAsync(minBet: 5, hitSoft17: true, maxRounds: 100);
gameTimer.Stop();

logger.LogEmpty();
logger.Log(UsageTracker.GetSummary());
logger.Log($"Game duration: {gameTimer.Elapsed:hh\\:mm\\:ss}");