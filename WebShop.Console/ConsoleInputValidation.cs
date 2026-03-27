using System.Net.Mail;

public static class ConsoleInputValidation
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

    public static bool IsValidPassword(string? password, int minLength, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(password)
            && password.Length >= minLength
            && password.Length <= maxLength;
    }

    public static bool TryParsePositiveLong(string? input, out long value)
    {
        value = 0;
        return long.TryParse(input, out var parsed) && parsed > 0 && (value = parsed) > 0;
    }

    public static bool TryParseIntInRange(string? input, int min, int max, out int value)
    {
        value = 0;
        return int.TryParse(input, out var parsed) && parsed >= min && parsed <= max && (value = parsed) >= min;
    }

    public static bool IsValidRequiredText(string? input, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(input) && input.Trim().Length <= maxLength;
    }

    public static bool IsValidOptionalText(string? input, int maxLength)
    {
        return string.IsNullOrWhiteSpace(input) || input.Trim().Length <= maxLength;
    }
}
