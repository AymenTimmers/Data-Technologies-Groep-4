namespace WebShop.Contracts.Models;

public record ShippingAddressDto(long Id, long UserId, string? Label, string ShippingAddress, bool IsDefault);