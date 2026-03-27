namespace WebShop.Contracts.Models;

public record CartItemDto(long ItemId, long ProductId, string ProductName, double UnitPrice, int Quantity);
