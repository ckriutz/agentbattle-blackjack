using System.Collections.Concurrent;

public static class UsageTracker
{
    private sealed record UsageTotals(long InputTokens, long OutputTokens, long TotalTokens)
    {
        public UsageTotals Add(long input, long output, long total) =>
            new UsageTotals(InputTokens + input, OutputTokens + output, TotalTokens + total);
    }

    private static readonly ConcurrentDictionary<string, UsageTotals> _byModel = new();

    public static void AddUsage(string model, long input, long output, long total)
    {
        _byModel.AddOrUpdate(
            model,
            _ => new UsageTotals(input, output, total),
            (_, existing) => existing.Add(input, output, total)
        );
    }

    public static IReadOnlyDictionary<string, (long Input, long Output, long Total)> Snapshot()
    {
        return _byModel.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.InputTokens, kvp.Value.OutputTokens, kvp.Value.TotalTokens)
        );
    }

    public static string GetSummary()
    {
        var lines = _byModel
            .OrderBy(k => k.Key)
            .Select(k => $"{k.Key}: input={k.Value.InputTokens}, output={k.Value.OutputTokens}, total={k.Value.TotalTokens}");

        return "Token usage by model:\n" + string.Join("\n", lines);
    }
}