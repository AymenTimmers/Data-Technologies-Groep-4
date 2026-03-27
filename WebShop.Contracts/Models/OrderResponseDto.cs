namespace WebShop.Contracts.Models;

public record OrderResponseDto(long OrderId, string OrderNumber, double TotalPrice, string ShippingAddress, List<OrderItemDto> Items);
