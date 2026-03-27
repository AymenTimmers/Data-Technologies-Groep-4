namespace WebShop.Contracts.Models;

public record ProductRecommendedDto(
    long ProductId,
    string ProductName,
    double Price,
    int Stock,
    string? Description,
    int BuyCount
);
