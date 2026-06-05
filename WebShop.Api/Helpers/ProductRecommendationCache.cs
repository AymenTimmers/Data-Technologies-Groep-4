using Microsoft.Data.Sqlite;
using Neo4j.Driver;
using WebShop.Contracts.Models;

namespace WebShop.Api.Helpers;

public static class ProductRecommendationCache
{
    private static Dictionary<long, List<ProductRecommendedDto>> _recommendations = new();
    private static DateTime _lastCacheTime = DateTime.MinValue;

    private const int CACHE_HOURS = 24;

    // Neo4j config
    private const string NEO4J_URI = "bolt://145.24.223.151:7687";
    private const string NEO4J_USER = "neo4j";
    private const string NEO4J_PASSWORD = "password123";

    private static readonly IDriver? _driver;

    public static DateTime LastCacheTime => _lastCacheTime;

    static ProductRecommendationCache()
    {
        try
        {
            _driver = GraphDatabase.Driver(
                NEO4J_URI,
                AuthTokens.Basic(NEO4J_USER, NEO4J_PASSWORD)
            );

            using var session = _driver.AsyncSession();
            session.RunAsync("RETURN 1").Wait();

            Console.WriteLine("Neo4j connected successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Neo4j connection failed: {ex}");

            _driver = null;
        }
    }

    public static async Task RefreshIfNeeded(string dbPath, bool forceRefresh = false)
    {
        var timeSinceLastCache = DateTime.UtcNow - _lastCacheTime;

        if (!forceRefresh && timeSinceLastCache.TotalHours < CACHE_HOURS)
            return;

        using var sqliteConnection = Db.CreateOpenConnection(dbPath);

        await SyncSqliteToNeo4j(sqliteConnection);
        await RefreshCacheFromNeo4j();
    }

    private static async Task SyncSqliteToNeo4j(SqliteConnection sqliteConnection)
    {
        await using var session = _driver.AsyncSession();

        // Clear graph (simple school-project approach)
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
                ", new
                {
                    id = reader.GetInt64(0),
                    categoryId = reader.GetInt64(1),
                    name = reader.GetString(2),
                    price = reader.GetDouble(3),
                    stock = reader.GetInt32(4),
                    description = reader.IsDBNull(5) ? "" : reader.GetString(5)
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
                    CREATE (o:Order { id: $id })
                ", new { id = reader.GetInt64(0) });
            }
        }

        // -------------------------
        // Import order-product links
        // -------------------------
        using (var command = sqliteConnection.CreateCommand())
        {
            command.CommandText = @"SELECT order_id, product_id FROM order_items";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                await session.RunAsync(@"
                    MATCH (o:Order {id: $orderId})
                    MATCH (p:Product {id: $productId})
                    CREATE (o)-[:CONTAINS]->(p)
                ", new
                {
                    orderId = reader.GetInt64(0),
                    productId = reader.GetInt64(1)
                });
            }
        }
    }

    private static async Task RefreshCacheFromNeo4j()
    {
        _recommendations.Clear();

        await using var session = _driver.AsyncSession();

        var productResult = await session.RunAsync(@"
            MATCH (p:Product)
            RETURN p.id AS id
        ");

        var productIds = new List<long>();

        await productResult.ForEachAsync(r =>
        {
            productIds.Add(r["id"].As<long>());
        });

        foreach (var productId in productIds)
        {
            var recommendations = new List<ProductRecommendedDto>();

            var result = await session.RunAsync(@"
                MATCH (:Product {id: $productId})<-[:CONTAINS]-(o:Order)-[:CONTAINS]->(recommended:Product)
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
            ", new { productId });

            await result.ForEachAsync(r =>
            {
                recommendations.Add(new ProductRecommendedDto(
                    r["id"].As<long>(),
                    r["name"].As<string>(),
                    r["price"].As<double>(),
                    r["stock"].As<int>(),
                    r["description"].As<string>(),
                    r["score"].As<int>()
                ));
            });

            if (recommendations.Count > 0)
                _recommendations[productId] = recommendations;
        }

        _lastCacheTime = DateTime.UtcNow;
    }

    public static List<ProductRecommendedDto> GetRecommendations(long productId)
    {
        return _recommendations.TryGetValue(productId, out var recs)
            ? recs
            : new List<ProductRecommendedDto>();
    }
}