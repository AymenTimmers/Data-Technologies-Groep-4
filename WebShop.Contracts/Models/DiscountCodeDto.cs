namespace WebShop.Contracts.Models;

public record DiscountCodeDto(long Id, string Code, int DiscountPercentage, bool Active, string ValidUntil, int MaxUses, int UsesCount);