namespace WebShop.Contracts.Models;

public record CheckoutRequest(long UserId, string ShippingAddress, string? DiscountCode);
