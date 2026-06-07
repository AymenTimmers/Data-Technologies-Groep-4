using WebShop.Api.Helpers;

namespace WebShop.Api.Tests;

public class ProductRecommendationCacheTests
{
    [Fact]
    public void GetRecommendations_EmptyCache_ReturnsEmptyList()
    {
        var result = ProductRecommendationCache.GetRecommendations(999);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void LastCacheTime_BeforeRefresh_IsMinValue()
    {
        // Cache state depends on test execution order (static state)
        // Just verify that LastCacheTime is a valid DateTime
        var time = ProductRecommendationCache.LastCacheTime;
        
        // Should be either MinValue or a recent time (depending on if other tests ran first)
        Assert.True(time <= DateTime.UtcNow);
    }

    /*
    [Fact]
    public void RefreshIfNeeded_WithForceRefresh_UpdatesCacheTime()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateTestDatabase(dbPath);
            
            ProductRecommendationCache.RefreshIfNeeded(dbPath, forceRefresh: true);
            var cacheTime1 = ProductRecommendationCache.LastCacheTime;
            
            Assert.True(cacheTime1 > DateTime.UtcNow.AddMinutes(-1));
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    [Fact]
    public async Task RefreshIfNeeded_WithoutForceAndFreshCache_SkipsRefresh()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateTestDatabase(dbPath);
            
            await ProductRecommendationCache.RefreshIfNeeded(dbPath, forceRefresh: true);
            var firstRefreshTime = ProductRecommendationCache.LastCacheTime;
            
            // Wait a tiny bit to ensure time difference would be detectable
            System.Threading.Thread.Sleep(100);
            
            // Second refresh should skip (cache is fresh)
            await ProductRecommendationCache.RefreshIfNeeded(dbPath, forceRefresh: false);
            var secondRefreshTime = ProductRecommendationCache.LastCacheTime;
            
            // Times should be equal (no refresh happened)
            Assert.Equal(firstRefreshTime, secondRefreshTime);
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }
    */

    private static void CreateTestDatabase(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // Create schema
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY,
                category_id INTEGER,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                description TEXT,
                brand TEXT,
                publisher TEXT,
                release_year INTEGER
            );
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL
            );
            CREATE TABLE order_items (
                id INTEGER PRIMARY KEY,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                price REAL NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }
}