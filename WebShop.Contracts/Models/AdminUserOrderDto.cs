namespace WebShop.Contracts.Models;

public record AdminUserOrderDto(long OrderId, string OrderNumber, double TotalPrice, string ShippingAddress, string? DiscountCode, List<OrderItemDto> Items);