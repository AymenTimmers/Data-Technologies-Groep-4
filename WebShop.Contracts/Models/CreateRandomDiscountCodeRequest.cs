namespace WebShop.Contracts.Models;

public record CreateRandomDiscountCodeRequest(long AdminUserId, int DiscountPercentage, int MaxUses, string ValidUntil);