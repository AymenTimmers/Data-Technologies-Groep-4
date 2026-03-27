namespace WebShop.Contracts.Models;

public record ProductDto(
    long Id,
    long CategoryId,
    string Name,
    double Price,
    int Stock,
    string? Description,
    string? Brand,
    string? Publisher,
    int? ReleaseYear
);
