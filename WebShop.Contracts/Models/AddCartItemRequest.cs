namespace WebShop.Contracts.Models;

public record AddCartItemRequest(long UserId, long ProductId, int Quantity);
