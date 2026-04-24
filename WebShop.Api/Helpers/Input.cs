using System.Net.Mail;

namespace WebShop.Api.Helpers;

public static class Input
{
    public static bool TryNormalizeEmail(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim().ToLowerInvariant();
        try
        {
            _ = new MailAddress(trimmed);
            normalizedEmail = trimmed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string? password)
    {
        return !string.IsNullOrWhiteSpace(password)
            && password.Length >= 6
            && password.Length <= 128;
    }

    public static string? NormalizeOptional(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
