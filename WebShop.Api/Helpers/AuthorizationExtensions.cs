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
    /// Checks if the current user is authenticated.
    /// </summary>
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User?.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Checks if the current user has admin role.
    /// </summary>
    public static bool IsAdmin(this HttpContext context)
    {
        var role = context.User?.FindFirst(ClaimTypes.Role)?.Value;
        return role == "admin";
    }

    /// <summary>
    /// Checks if the current user ID matches the requested user ID (or is admin).
    /// Used for protecting endpoints where users can only access their own data.
    /// </summary>
    public static bool CanAccessUserData(this HttpContext context, int targetUserId)
    {
        var currentUserId = context.GetCurrentUserId();
        if (currentUserId == null)
        {
            return false;
        }

        // Allow if user is accessing their own data or if they're admin
        return currentUserId == targetUserId || context.IsAdmin();
    }

    /// <summary>
    /// Returns 403 Forbidden if user is not authorized.
    /// </summary>
    public static IResult ForbidIfUnauthorized(this HttpContext context, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return Results.Forbid();
        }

        return Results.Empty; // Placeholder, should not be used directly
    }

    /// <summary>
    /// Returns 401 Unauthorized if user is not authenticated.
    /// </summary>
    public static IResult UnauthorizedIfNotAuthenticated(this HttpContext context)
    {
        if (!context.IsAuthenticated())
        {
            return Results.Unauthorized();
        }

        return Results.Empty; // Placeholder, should not be used directly
    }
}
