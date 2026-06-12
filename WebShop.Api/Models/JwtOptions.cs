namespace WebShop.Api.Models;

public record JwtOptions(string Secret, int ExpiryHours);
