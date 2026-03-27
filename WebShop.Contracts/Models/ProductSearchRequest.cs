namespace WebShop.Contracts.Models;

public record ProductSearchRequest(
    string? SearchTerm,
    long? CategoryId,
    double? MinPrice,
    double? MaxPrice
);
