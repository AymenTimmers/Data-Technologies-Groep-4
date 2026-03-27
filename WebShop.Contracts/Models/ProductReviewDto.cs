namespace WebShop.Contracts.Models;

public record ProductReviewDto(
    long ReviewId,
    long ProductId,
    long UserId,
    string UserEmail,
    int Stars,
    string Explanation,
    string CreatedAtUtc
);
