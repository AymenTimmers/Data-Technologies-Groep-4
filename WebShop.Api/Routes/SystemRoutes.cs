using WebShop.Api.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public class SystemRoutes
{
    private ProductRecommendationCache _recommendationCache;
    public SystemRoutes(ProductRecommendationCache recommendationCache)
    {
        _recommendationCache = recommendationCache;
    }

    public void MapSystemRoutes(WebApplication app, string documentationFolder)
    {
        app.MapPost("/cache/recommendations/refresh", async (DbOptions db) =>
        {
            await _recommendationCache.RefreshIfNeeded(db.DatabasePath, forceRefresh: true);

            return Results.Ok(new
            {
                message = "Recommendations cache refreshed.",
                refreshedAt = _recommendationCache.LastCacheTime
            });
        });

        app.MapPost("/docs/models/generate", (DbOptions db) =>
        {
            var outputPath = Path.Combine(documentationFolder, "models-and-relations.md");
            var result = ModelDocumentationGenerator.Generate(db.DatabasePath, outputPath);

            return Results.Ok(new
            {
                message = "Model documentation generated.",
                path = outputPath,
                generatedAtUtc = result.GeneratedAtUtc,
                tableCount = result.TableCount,
                relationCount = result.RelationCount
            });
        });
    }
}
