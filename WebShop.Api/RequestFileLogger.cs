namespace WebShop.Api;

public static class RequestFileLogger
{
    private static readonly object Sync = new();

    public static void Append(
        string filePath,
        string method,
        string path,
        int statusCode,
        long elapsedMs,
        string? errorMessage)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var safeError = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $" | error={errorMessage.Replace(Environment.NewLine, " ")}";
        var line = $"{timestamp} UTC | {method} {path} | status={statusCode} | elapsedMs={elapsedMs}{safeError}";

        lock (Sync)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}
