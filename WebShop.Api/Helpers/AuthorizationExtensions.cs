using System.Security.Claims;

namespace WebShop.Api.Helpers;

/// <summary>
/// Extension methods for authorization checks in endpoints.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Extracts the current user ID from the HTTP context.
    /// Returns null if user is not authenticated.
    /// </summary>
    public static int? GetCurrentUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim?.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user is authenticated (whitelist approach).
    /// </summary>
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User?.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Checks if the current user has admin role (whitelist approach).
    /// </summary>
    public static bool IsAdmin(this HttpContext context)
    {
        var role = context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "admin")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current user is allowed to access the target user data.
    /// Whitelist: only explicitly allowed cases return true.
    /// </summary>
    public static bool CanAccessUserData(this HttpContext context, int targetUserId)
    {
        var currentUserId = context.GetCurrentUserId();

        if (currentUserId == null)
        {
            return false;
        }

        if (currentUserId == targetUserId)
        {
            return true;
        }

        if (context.IsAdmin())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns 403 Forbidden if user is not authorized (whitelist approach).
    /// </summary>
    public static IResult ForbidIfUnauthorized(this HttpContext context, bool isAuthorized)
    {
        if (isAuthorized == true)
        {
            // Gevolgd door de code die je in deze functie wil uitvoeren
            return Results.Empty;
        }

        return Results.Forbid();
    }

    /// <summary>
    /// Returns 401 Unauthorized if user is not authenticated (whitelist approach).
    /// </summary>
    public static IResult UnauthorizedIfNotAuthenticated(this HttpContext context)
    {
        if (context.IsAuthenticated() == true)
        {
            // Gevolgd door de code die je in deze functie wil uitvoeren
            return Results.Empty;
        }

        return Results.Forbid();
    }
}