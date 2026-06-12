namespace WebShop.Contracts.Models;

public record AddCartItemRequest(long ProductId, int Quantity);
