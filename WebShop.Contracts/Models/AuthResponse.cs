namespace WebShop.Contracts.Models;

public record AuthResponse(long UserId, string Email, int Role, string? Token = null);
