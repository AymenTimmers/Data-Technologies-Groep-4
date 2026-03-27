namespace WebShop.Contracts.Models;

public record OrderItemDto(long ProductId, string ProductName, int Quantity, double UnitPrice);
