namespace WebShop.Api.Helpers;

/// <summary>
/// Security utility class for password hashing.
/// Uses bcrypt for secure password hashing.
/// </summary>
public static class Security
{
    /// <summary>
    /// Hashes a password using bcrypt with work factor 12.
    /// </summary>
    public static string HashPassword(string input)
    {
        return PasswordService.HashPassword(input);
    }

    /// <summary>
    /// Verifies a plaintext password against a bcrypt hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        return PasswordService.VerifyPassword(password, hash);
    }
}
