using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace WebShop.Api.Helpers;

/// <summary>
/// Anonymous request file logger that sanitizes sensitive data.
/// All user IDs and personal identifiers are hashed before logging.
/// </summary>
public static class RequestFileLogger
{
    private static readonly object Sync = new();
    private static readonly SHA256 SHA256Instance = SHA256.Create();

    /// <summary>
    /// Logs a request to file with anonymized user identifiers.
    /// User IDs are hashed, paths are sanitized, and error messages are filtered.
    /// </summary>
    public static void Append(
        string filePath,
        string method,
        string path,
        int statusCode,
        long elapsedMs,
        string? errorMessage)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        
        // Sanitize path to remove user IDs and sensitive parameters
        var sanitizedPath = SanitizePath(path);
        
        // Sanitize error message
        var safeError = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $" | error={SanitizeErrorMessage(errorMessage)}";
        
        var line = $"{timestamp} UTC | {method} {sanitizedPath} | status={statusCode} | elapsedMs={elapsedMs}{safeError}";

        lock (Sync)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }

    /// <summary>
    /// Sanitizes a path by replacing user IDs and other identifiers with hashed values.
    /// </summary>
    private static string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "/";
        }

        // Hash user IDs in paths like /api/users/123 -> /api/users/[user:hash]
        var sanitized = Regex.Replace(path, @"/users/(\d+)", match =>
        {
            var userId = match.Groups[1].Value;
            var hash = HashIdentifier(userId);
            return $"/users/[user:{hash}]";
        });

        // Hash product IDs in paths like /api/products/456 -> /api/products/[product:hash]
        sanitized = Regex.Replace(sanitized, @"/products/(\d+)", match =>
        {
            var productId = match.Groups[1].Value;
            var hash = HashIdentifier(productId);
            return $"/products/[product:{hash}]";
        });

        // Remove query parameters that might contain sensitive data
        sanitized = Regex.Replace(sanitized, @"\?.*$", "?[sanitized]");

        return sanitized;
    }

    /// <summary>
    /// Sanitizes error messages by removing sensitive data.
    /// </summary>
    private static string SanitizeErrorMessage(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            return string.Empty;
        }

        var sanitized = errorMessage
            .Replace(Environment.NewLine, " ")
            .Substring(0, Math.Min(200, errorMessage.Length)); // Limit error message length

        // Remove email addresses
        sanitized = Regex.Replace(sanitized, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "[email]");

        // Remove potential IBAN/card numbers
        sanitized = Regex.Replace(sanitized, @"[A-Z]{2}\d{2}[A-Z0-9]{1,30}", "[iban]");
        sanitized = Regex.Replace(sanitized, @"\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}", "[card]");

        // Remove SQL fragments
        sanitized = Regex.Replace(sanitized, @"(?i)(SELECT|INSERT|UPDATE|DELETE|DROP|ALTER)", "[sql]");

        return sanitized;
    }

    /// <summary>
    /// Creates a deterministic hash of an identifier for consistent anonymization.
    /// </summary>
    private static string HashIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return "unknown";
        }

        var bytes = Encoding.UTF8.GetBytes(identifier);
        var hash = SHA256Instance.ComputeHash(bytes);
        return Convert.ToHexString(hash).Substring(0, 8); // Use first 8 chars of hash
    }
}

