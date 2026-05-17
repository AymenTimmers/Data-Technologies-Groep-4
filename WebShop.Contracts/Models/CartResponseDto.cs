namespace WebShop.Contracts.Models;

public record CartResponseDto(long UserId, List<CartItemDto> Items, int? ExpiresInSeconds);
