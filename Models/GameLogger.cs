public class GameLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly string _filePath;
    private readonly object _lock = new object();

    public GameLogger(string? customFileName = null)
    {
        var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "Results");
        Directory.CreateDirectory(resultsDir);

        var fileName = customFileName ?? $"game_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        _filePath = Path.Combine(resultsDir, fileName);

        _writer = new StreamWriter(_filePath, append: true) 
        { 
            AutoFlush = true 
        };

        Console.WriteLine($"Logging to: {_filePath}");
    }

    public void Log(string message)
    {
        lock (_lock)
        {
            Console.WriteLine(message);
            _writer.WriteLine(message);
        }
    }

    public void LogEmpty()
    {
        lock (_lock)
        {
            Console.WriteLine();
            _writer.WriteLine();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        Console.WriteLine($"Game log saved to: {_filePath}");
    }

    public string FilePath => _filePath;
}
