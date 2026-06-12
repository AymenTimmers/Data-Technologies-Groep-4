namespace WebShop.Contracts.Models;

public record CheckoutRequest(string? ShippingAddress, string? DiscountCode, long? ShippingAddressId = null);
