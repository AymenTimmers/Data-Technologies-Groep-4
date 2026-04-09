namespace WebShop.Contracts.Models;

public record TopSoldProductDto(
    long ProductId,
    string ProductName,
    long SoldQuantity,
    double Revenue
)
{
    public TopSoldProductDto() : this(0, string.Empty, 0, 0.0) 
    { 
    }
}
