namespace WebShop.Contracts.Models;

public record FavoriteProductDto(long FavoriteId, long ProductId, string ProductName, double Price, int Stock, string? Description);