namespace WebShop.Contracts.Models;

public record CartResponseDto(long CartId, long UserId, List<CartItemDto> Items);
