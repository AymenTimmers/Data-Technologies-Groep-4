using Dapper;
using WebShop.Api.Models;
using WebShop.Contracts.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public class CatalogRoutes
{
    private readonly ProductRecommendationCache _recommendationCache;
    private readonly MongoReviewService _mongoReviews;

    public CatalogRoutes(ProductRecommendationCache recommendationCache, MongoReviewService mongoReviews)
    {
        _recommendationCache = recommendationCache;
        _mongoReviews = mongoReviews;
    }

    public WebApplication MapCatalogRoutes(WebApplication app)
    {
        app.MapGet("/products", async (DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            string sql = @"
                SELECT id, category_id, name, price, stock, description, brand, publisher, release_year
                FROM products
                ORDER BY id";

            var products = await connection.QueryAsync<ProductDto>(sql);

            return Results.Ok(products);
        });

        app.MapGet("/categories", async (DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            var categories = await connection.QueryAsync<CategoryDto>("SELECT id, name FROM categories ORDER BY name");
            return Results.Ok(categories);
        });

        app.MapPost("/products/search", async (ProductSearchRequest request, DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            
            var sql = @"
                SELECT p.id, p.category_id AS CategoryId, p.name, p.price, p.stock, p.description, p.brand, p.publisher, p.release_year AS ReleaseYear
                FROM products p
                WHERE (p.name LIKE @Search OR p.description LIKE @Search OR p.brand LIKE @Search OR @Search IS NULL)
                AND (p.category_id = @CategoryId OR @CategoryId <= 0 OR @CategoryId IS NULL)
                AND (p.price >= @MinPrice OR @MinPrice IS NULL)
                AND (p.price <= @MaxPrice OR @MaxPrice IS NULL)
                ORDER BY p.name";

            var searchPattern = string.IsNullOrWhiteSpace(request.SearchTerm) ? null : $"%{request.SearchTerm}%";
            
            var products = await connection.QueryAsync<ProductDto>(sql, new {
                Search = searchPattern,
                CategoryId = request.CategoryId,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice
            });

            return Results.Ok(products);
        });

        app.MapGet("/products/top-sold", async (DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            const string sql = @"
                SELECT p.id, p.name, 
                       COALESCE(SUM(oi.quantity), 0) AS SoldQuantity, 
                       COALESCE(SUM(oi.quantity * oi.price), 0) AS Revenue
                FROM order_items oi
                INNER JOIN products p ON p.id = oi.product_id
                GROUP BY p.id, p.name
                ORDER BY SoldQuantity DESC, Revenue DESC
                LIMIT 5";

            var topSold = await connection.QueryAsync<TopSoldProductDto>(sql);
            return Results.Ok(topSold);
        });

        app.MapGet("/products/{id:long}", async (long id, DbOptions db) =>
        {
            if (id <= 0) return Results.BadRequest(new { message = "Product id must be greater than 0." });

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            const string sql = "SELECT id, category_id AS CategoryId, name, price, stock, description, brand, publisher, release_year AS ReleaseYear FROM products WHERE id = @id";
            
            var product = await connection.QueryFirstOrDefaultAsync<ProductDto>(sql, new { id });
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });

        app.MapGet("/products/{id:long}/reviews", async (long id, DbOptions db) =>
        {
            if (id <= 0) return Results.BadRequest(new { message = "Product id must be greater than 0." });

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            var productExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM products WHERE id = @id", new { id });

            if (!productExists)
            {
                return Results.NotFound(new { message = $"Product with ID {id} does not exist." });
            }

            var reviews = await _mongoReviews.GetReviewsForProductAsync(id);
            return Results.Ok(reviews);
        });

        app.MapPost("/products/{id:long}/reviews", async (long id, CreateProductReviewRequest request, DbOptions db) =>
        {
            if (id <= 0 || request.UserId <= 0) return Results.BadRequest(new { message = "Invalid IDs." });
            if (request.Stars < 1 || request.Stars > 5) return Results.BadRequest(new { message = "Stars 1-5." });

            using var connection = Db.CreateOpenConnection(db.DatabasePath);

            if (!Db.ProductExists(connection, id))
                return Results.NotFound(new { message = "Product not found." });

            using var userCmd = connection.CreateCommand();
            userCmd.CommandText = "SELECT email FROM users WHERE id = @userId LIMIT 1";
            userCmd.Parameters.AddWithValue("@userId", request.UserId);
            var email = userCmd.ExecuteScalar() as string;

            if (email is null)
                return Results.NotFound(new { message = "User not found." });

            await _mongoReviews.UpsertReviewAsync(id, request.UserId, email, request.Stars, request.Explanation);

            return Results.Ok(new { message = "Review saved." });
        });

        app.MapGet("/products/{id:long}/recommendations", async (long id, DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);

            if (!Db.ProductExists(connection, id)) return Results.NotFound();

            await _recommendationCache.RefreshIfNeeded(db.DatabasePath);
            var recs = _recommendationCache.GetRecommendations(id);

            return Results.Ok(new { productId = id, recommendations = recs });
        });

        return app;
    }
}