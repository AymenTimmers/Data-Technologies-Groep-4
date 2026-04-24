using Microsoft.Data.Sqlite;
using WebShop.Contracts.Models;

namespace WebShop.Api.Helpers;

public static class ProductRecommendationCache
{
    private static Dictionary<long, List<ProductRecommendedDto>> _recommendations = new();
    private static DateTime _lastCacheTime = DateTime.MinValue;
    private const int CACHE_HOURS = 24;

    public static DateTime LastCacheTime => _lastCacheTime;

    public static void RefreshIfNeeded(string dbPath, bool forceRefresh = false)
    {
        var timeSinceLastCache = DateTime.UtcNow - _lastCacheTime;
        if (!forceRefresh && timeSinceLastCache.TotalHours < CACHE_HOURS)
        {
            return;
        }

        using var connection = Db.CreateOpenConnection(dbPath);
        RefreshCache(connection);
    }

    private static void RefreshCache(SqliteConnection connection)
    {
        _recommendations.Clear();

        var products = new Dictionary<long, ProductDto>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT id, category_id, name, price, stock, description, brand, publisher, release_year
                FROM products
                ORDER BY id";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var product = new ProductDto(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                );
                products[product.Id] = product;
            }
        }

        foreach (var productId in products.Keys)
        {
            var coPurchaseCount = new Dictionary<long, int>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT oi2.product_id, COUNT(*) as count
                FROM order_items oi1
                INNER JOIN order_items oi2 ON oi1.order_id = oi2.order_id
                WHERE oi1.product_id = @productId
                  AND oi2.product_id != @productId
                GROUP BY oi2.product_id
                ORDER BY count DESC
                LIMIT 10";
            command.Parameters.AddWithValue("@productId", productId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var coPurchasedProductId = reader.GetInt64(0);
                var count = reader.GetInt32(1);
                coPurchaseCount[coPurchasedProductId] = count;
            }

            var recommendations = new List<ProductRecommendedDto>();
            foreach (var (coProdId, count) in coPurchaseCount.OrderByDescending(x => x.Value))
            {
                if (products.TryGetValue(coProdId, out var coProd))
                {
                    recommendations.Add(new ProductRecommendedDto(
                        coProd.Id,
                        coProd.Name,
                        coProd.Price,
                        coProd.Stock,
                        coProd.Description,
                        count
                    ));
                }
            }

            if (recommendations.Count > 0)
            {
                _recommendations[productId] = recommendations;
            }
        }

        _lastCacheTime = DateTime.UtcNow;
    }

    public static List<ProductRecommendedDto> GetRecommendations(long productId)
    {
        if (_recommendations.TryGetValue(productId, out var recommendations))
        {
            return recommendations;
        }
        return new List<ProductRecommendedDto>();
    }
}
