using WebShop.Api.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public static class SystemRoutes
{
    public static WebApplication MapSystemRoutes(this WebApplication app, string documentationFolder)
    {
        app.MapPost("/cache/recommendations/refresh", (DbOptions db) =>
        {
            ProductRecommendationCache.RefreshIfNeeded(db.DatabasePath, forceRefresh: true);

            return Results.Ok(new
            {
                message = "Recommendations cache refreshed.",
                refreshedAt = ProductRecommendationCache.LastCacheTime
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

        return app;
    }
}
