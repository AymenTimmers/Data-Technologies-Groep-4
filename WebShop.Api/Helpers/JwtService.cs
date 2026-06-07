using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebShop.Api.Helpers;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public class JwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtService(string secretKey, string issuer = "webshop", string audience = "webshop-api", int expirationMinutes = 1440)
    {
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
        }

        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _expirationMinutes = expirationMinutes;
    }

    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    public string GenerateToken(int userId, string email, string role = "user")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and returns the principal if valid.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts user ID from a JWT token.
    /// </summary>
    public static int? GetUserIdFromToken(ClaimsPrincipal principal)
    {
        var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim?.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Extracts role from a JWT token.
    /// </summary>
    public static string? GetRoleFromToken(ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Extracts email from a JWT token.
    /// </summary>
    public static string? GetEmailFromToken(ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Generates a JWT secret key for configuration.
    /// Should be at least 32 characters for security.
    /// </summary>
    public static string GenerateSecretKey()
    {
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            byte[] key = new byte[32];
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }
    }
}
