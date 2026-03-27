namespace WebShop.Contracts.Models;

public record CreateShippingAddressRequest(string? Label, string ShippingAddress, bool SetAsDefault);