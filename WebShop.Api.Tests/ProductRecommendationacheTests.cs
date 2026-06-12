using Microsoft.Data.Sqlite;
using WebShop.Api.Helpers;
using WebShop.Contracts.Models;

namespace WebShop.Api.Tests;

// We skip testing the Neo4j sync entirely — that's an integration concern.
// Instead we test:
//   1. Cache state before any refresh (no DB needed)
//   2. The staleness/skip logic (no DB needed)
//   3. GetRecommendations once we seed the internal cache via reflection

public class ProductRecommendationCacheTests
{
    // Seed the private _recommendations dict directly so we can test
    // GetRecommendations without needing Neo4j at all
    private static ProductRecommendationCache CacheWithData(
        Dictionary<long, List<ProductRecommendedDto>> data)
    {
        // Pass null driver — it's never called unless RefreshIfNeeded runs
        var cache = new ProductRecommendationCache(null!);

        var field = typeof(ProductRecommendationCache)
            .GetField("_recommendations",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!;
        field.SetValue(cache, data);

        var timeField = typeof(ProductRecommendationCache)
            .GetField("_lastCacheTime",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!;
        timeField.SetValue(cache, DateTime.UtcNow);

        return cache;
    }

    [Fact]
    public void GetRecommendations_EmptyCache_ReturnsEmptyList()
    {
        var cache = new ProductRecommendationCache(null!);

        Assert.Empty(cache.GetRecommendations(999L));
    }

    [Fact]
    public void LastCacheTime_BeforeAnyRefresh_IsMinValue()
    {
        var cache = new ProductRecommendationCache(null!);

        Assert.Equal(DateTime.MinValue, cache.LastCacheTime);
    }

    [Fact]
    public void GetRecommendations_KnownProduct_ReturnsRecommendations()
    {
        var expected = new List<ProductRecommendedDto>
        {
            new(2L, "Gadget", 20.0, 5, null, 3),
            new(3L, "Doohickey", 5.0, 20, null, 1),
        };

        var cache = CacheWithData(new Dictionary<long, List<ProductRecommendedDto>>
        {
            [1L] = expected
        });

        var result = cache.GetRecommendations(1L);

        Assert.Equal(2, result.Count);
        Assert.Equal(2L, result[0].ProductId);
        Assert.Equal("Gadget", result[0].ProductName);
        Assert.Equal(3L, result[1].ProductId);
    }

    [Fact]
    public void GetRecommendations_UnknownProduct_ReturnsEmptyList()
    {
        var cache = CacheWithData(new Dictionary<long, List<ProductRecommendedDto>>
        {
            [1L] = [new(2L, "Gadget", 20.0, 5, null, 1)]
        });

        Assert.Empty(cache.GetRecommendations(999L));
    }

    [Fact]
    public void LastCacheTime_AfterSeeding_IsRecent()
    {
        var cache = CacheWithData([]);

        Assert.True(cache.LastCacheTime > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RefreshIfNeeded_FreshCache_SkipsRefresh()
    {
        // Seed the cache as recently refreshed — RefreshIfNeeded should skip
        // and never touch the null driver
        var cache = CacheWithData([]);
        var firstRefreshTime = cache.LastCacheTime;

        await Task.Delay(50);
        await cache.RefreshIfNeeded("unused", forceRefresh: false);

        Assert.Equal(firstRefreshTime, cache.LastCacheTime);
    }
}