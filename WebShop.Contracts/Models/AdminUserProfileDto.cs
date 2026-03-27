namespace WebShop.Contracts.Models;

public record AdminUserProfileDto(
    long UserId,
    string Email,
    string? FirstName,
    string? LastName,
    int Role,
    string? BankIban,
    string? BankAccountName,
    List<ShippingAddressDto> ShippingAddresses,
    List<AdminUserOrderDto> Orders
);