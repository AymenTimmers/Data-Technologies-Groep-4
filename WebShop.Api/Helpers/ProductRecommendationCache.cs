using Microsoft.Data.Sqlite;
using Neo4j.Driver;
using WebShop.Contracts.Models;

namespace WebShop.Api.Helpers;

public class ProductRecommendationCache
{
    private readonly IDriver _driver;

    private readonly Dictionary<long, List<ProductRecommendedDto>> _recommendations = new();
    private DateTime _lastCacheTime = DateTime.MinValue;

    private const int CACHE_HOURS = 24;

    public DateTime LastCacheTime => _lastCacheTime;

    public ProductRecommendationCache(IDriver driver)
    {
        _driver = driver;
    }

    public async Task RefreshIfNeeded(string dbPath, bool forceRefresh = false)
    {
        var timeSinceLastCache = DateTime.UtcNow - _lastCacheTime;

        if (!forceRefresh && timeSinceLastCache.TotalHours < CACHE_HOURS)
            return;

        using var sqliteConnection = Db.CreateOpenConnection(dbPath);

        await SyncSqliteToNeo4j(sqliteConnection);
        await RefreshCacheFromNeo4j();
    }

    private async Task SyncSqliteToNeo4j(SqliteConnection sqliteConnection)
    {
        await using var session = _driver.AsyncSession();

        // Clear graph
        await session.RunAsync("MATCH (n) DETACH DELETE n");

        // -------------------------
        // Import products
        // -------------------------
        using (var command = sqliteConnection.CreateCommand())
        {
            command.CommandText = @"
                SELECT id, category_id, name, price, stock, description
                FROM products
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                await session.RunAsync(@"
                    CREATE (p:Product {
                        id: $id,
                        categoryId: $categoryId,
                        name: $name,
                        price: $price,
                        stock: $stock,
                        description: $description
                    })
                ",
                new
                {
                    id = reader.GetInt64(0),
                    categoryId = reader.GetInt64(1),
                    name = reader.GetString(2),
                    price = reader.GetDouble(3),
                    stock = reader.GetInt32(4),
                    description = reader.IsDBNull(5)
                        ? string.Empty
                        : reader.GetString(5)
                });
            }
        }

        // -------------------------
        // Import orders
        // -------------------------
        using (var command = sqliteConnection.CreateCommand())
        {
            command.CommandText = @"SELECT DISTINCT order_id FROM order_items";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                await session.RunAsync(@"
                    CREATE (o:Order {
                        id: $id
                    })
                ",
                new
                {
                    id = reader.GetInt64(0)
                });
            }
        }

        // -------------------------
        // Import order-product links
        // -------------------------
        using (var command = sqliteConnection.CreateCommand())
        {
            command.CommandText = @"
                SELECT order_id, product_id
                FROM order_items
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                await session.RunAsync(@"
                    MATCH (o:Order {id: $orderId})
                    MATCH (p:Product {id: $productId})
                    CREATE (o)-[:CONTAINS]->(p)
                ",
                new
                {
                    orderId = reader.GetInt64(0),
                    productId = reader.GetInt64(1)
                });
            }
        }
    }

    private async Task RefreshCacheFromNeo4j()
    {
        _recommendations.Clear();

        await using var session = _driver.AsyncSession();

        var productResult = await session.RunAsync(@"
            MATCH (p:Product)
            RETURN p.id AS id
        ");

        var productIds = new List<long>();

        await productResult.ForEachAsync(record =>
        {
            productIds.Add(record["id"].As<long>());
        });

        foreach (var productId in productIds)
        {
            var recommendations = new List<ProductRecommendedDto>();

            var result = await session.RunAsync(@"
                MATCH (:Product {id: $productId})
                      <-[:CONTAINS]-(o:Order)
                      -[:CONTAINS]->(recommended:Product)
                WHERE recommended.id <> $productId

                RETURN
                    recommended.id AS id,
                    recommended.name AS name,
                    recommended.price AS price,
                    recommended.stock AS stock,
                    recommended.description AS description,
                    COUNT(*) AS score

                ORDER BY score DESC
                LIMIT 10
            ",
            new
            {
                productId
            });

            await result.ForEachAsync(record =>
            {
                recommendations.Add(
                    new ProductRecommendedDto(
                        record["id"].As<long>(),
                        record["name"].As<string>(),
                        record["price"].As<double>(),
                        record["stock"].As<int>(),
                        record["description"].As<string>(),
                        record["score"].As<int>()
                    )
                );
            });

            if (recommendations.Count > 0)
            {
                _recommendations[productId] = recommendations;
            }
        }

        _lastCacheTime = DateTime.UtcNow;
    }

    public List<ProductRecommendedDto> GetRecommendations(long productId)
    {
        return _recommendations.TryGetValue(productId, out var recommendations)
            ? recommendations
            : new List<ProductRecommendedDto>();
    }
}