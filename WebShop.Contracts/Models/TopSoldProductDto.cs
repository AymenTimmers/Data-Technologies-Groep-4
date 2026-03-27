namespace WebShop.Contracts.Models;

public record TopSoldProductDto(
    long ProductId,
    string ProductName,
    long SoldQuantity,
    double Revenue
);
