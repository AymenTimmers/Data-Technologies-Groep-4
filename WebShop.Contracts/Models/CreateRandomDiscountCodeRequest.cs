namespace WebShop.Contracts.Models;

public record CreateRandomDiscountCodeRequest(int DiscountPercentage, int MaxUses, string ValidUntil);