namespace WebShop.Contracts.Models;

public record AdminUserSummaryDto(long UserId, string Email, string? FirstName, string? LastName, int Role);