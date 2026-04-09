namespace WebShop.Contracts.Models;

public record ProductReviewDto(
    long ReviewId,
    long ProductId,
    long UserId,
    string UserEmail,
    int Stars,
    string Explanation,
    string CreatedAtUtc
)
{
    public ProductReviewDto() : this(0, 0, 0, string.Empty, 0, string.Empty, string.Empty) {}
}
