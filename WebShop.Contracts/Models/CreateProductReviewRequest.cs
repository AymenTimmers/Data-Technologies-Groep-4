namespace WebShop.Contracts.Models;

public record CreateProductReviewRequest(long UserId, int Stars, string Explanation);
