using BCrypt.Net;

namespace WebShop.Api.Helpers;

/// <summary>
/// Service for securely hashing and verifying passwords using bcrypt.
/// </summary>
public class PasswordService
{
    private const int WorkFactor = 12; // Controls computational cost

    /// <summary>
    /// Hashes a plaintext password using bcrypt with a work factor of 12.
    /// </summary>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    /// <summary>
    /// Verifies that a plaintext password matches a bcrypt hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
