namespace AdoTimeTracker.Core.Services;

public class LogService
{
    private readonly string _logFile;

    public LogService()
    {
        _logFile = "logs.json";

        var directory =
            Path.GetDirectoryName(_logFile);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    private void Write(
        string level,
        string message)
    {
        var line =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        File.AppendAllText(
            _logFile,
            line + Environment.NewLine);
    }
}