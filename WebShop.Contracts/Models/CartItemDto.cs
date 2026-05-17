namespace WebShop.Contracts.Models;

public record CartItemDto(long ProductId, string ProductName, double UnitPrice, int Quantity);
