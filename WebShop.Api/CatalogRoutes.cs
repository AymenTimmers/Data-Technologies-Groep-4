using Microsoft.Data.Sqlite;
using WebShop.Api.Models;
using WebShop.Contracts.Models;

namespace WebShop.Api;

public static class CatalogRoutes
{
    public static WebApplication MapCatalogRoutes(this WebApplication app)
    {
        app.MapGet("/products", (DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, category_id, name, price, stock, description, brand, publisher, release_year
                FROM products
                ORDER BY id";

            using var reader = command.ExecuteReader();
            var products = new List<ProductDto>();
            while (reader.Read())
            {
                products.Add(new ProductDto(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                ));
            }

            return Results.Ok(products);
        });

        app.MapGet("/categories", (DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, name
                FROM categories
                ORDER BY name";

            using var reader = command.ExecuteReader();
            var categories = new List<CategoryDto>();
            while (reader.Read())
            {
                categories.Add(new CategoryDto(
                    reader.GetInt64(0),
                    reader.GetString(1)
                ));
            }

            return Results.Ok(categories);
        });

        app.MapPost("/products/search", (ProductSearchRequest request, DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);

            var whereConditions = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = $"%{request.SearchTerm}%";
                whereConditions.Add("(p.name LIKE @searchTerm OR p.description LIKE @searchTerm OR p.brand LIKE @searchTerm)");
                parameters.Add(new SqliteParameter("@searchTerm", searchTerm));
            }

            if (request.CategoryId.HasValue && request.CategoryId > 0)
            {
                whereConditions.Add("p.category_id = @categoryId");
                parameters.Add(new SqliteParameter("@categoryId", request.CategoryId.Value));
            }

            if (request.MinPrice.HasValue && request.MinPrice >= 0)
            {
                whereConditions.Add("p.price >= @minPrice");
                parameters.Add(new SqliteParameter("@minPrice", request.MinPrice.Value));
            }

            if (request.MaxPrice.HasValue && request.MaxPrice >= 0)
            {
                whereConditions.Add("p.price <= @maxPrice");
                parameters.Add(new SqliteParameter("@maxPrice", request.MaxPrice.Value));
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT p.id, p.category_id, p.name, p.price, p.stock, p.description, p.brand, p.publisher, p.release_year
                FROM products p
                {whereClause}
                ORDER BY p.name";

            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = command.ExecuteReader();
            var products = new List<ProductDto>();
            while (reader.Read())
            {
                products.Add(new ProductDto(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                ));
            }

            return Results.Ok(products);
        });

        app.MapGet("/products/top-sold", (DbOptions db) =>
        {
            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    p.id,
                    p.name,
                    COALESCE(SUM(oi.quantity), 0) AS sold_quantity,
                    COALESCE(SUM(oi.quantity * oi.price), 0) AS revenue
                FROM order_items oi
                INNER JOIN products p ON p.id = oi.product_id
                GROUP BY p.id, p.name
                ORDER BY sold_quantity DESC, revenue DESC, p.id ASC
                LIMIT 5";

            using var reader = command.ExecuteReader();
            var topSold = new List<TopSoldProductDto>();
            while (reader.Read())
            {
                topSold.Add(new TopSoldProductDto(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetInt64(2),
                    reader.GetDouble(3)
                ));
            }

            return Results.Ok(topSold);
        });

        app.MapGet("/products/{id:long}", (long id, DbOptions db) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new { message = "Product id must be greater than 0." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, category_id, name, price, stock, description, brand, publisher, release_year
                FROM products
                WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return Results.NotFound();
            }

            return Results.Ok(new ProductDto(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
            ));
        });

        app.MapGet("/products/{id:long}/reviews", (long id, DbOptions db) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new { message = "Product id must be greater than 0." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.ProductExists(connection, id))
            {
                return Results.NotFound(new { message = "Product not found." });
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT pr.id, pr.product_id, pr.user_id, u.email, pr.rating, pr.explanation, pr.created_at
                FROM product_ratings pr
                INNER JOIN users u ON u.id = pr.user_id
                WHERE pr.product_id = @productId
                ORDER BY datetime(pr.created_at) DESC, pr.id DESC";
            command.Parameters.AddWithValue("@productId", id);

            using var reader = command.ExecuteReader();
            var reviews = new List<ProductReviewDto>();
            while (reader.Read())
            {
                reviews.Add(new ProductReviewDto(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetInt64(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetString(5),
                    reader.GetString(6)
                ));
            }

            return Results.Ok(reviews);
        });

        app.MapPost("/products/{id:long}/reviews", (long id, CreateProductReviewRequest request, DbOptions db) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new { message = "Product id must be greater than 0." });
            }

            if (request.UserId <= 0)
            {
                return Results.BadRequest(new { message = "User id must be greater than 0." });
            }

            if (request.Stars < 1 || request.Stars > 5)
            {
                return Results.BadRequest(new { message = "Stars must be between 1 and 5." });
            }

            var explanation = request.Explanation?.Trim();
            if (string.IsNullOrWhiteSpace(explanation) || explanation.Length > 1000)
            {
                return Results.BadRequest(new { message = "Explanation is required and must be max 1000 characters." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.UserExists(connection, request.UserId))
            {
                return Results.NotFound(new { message = "User not found." });
            }

            if (!Db.ProductExists(connection, id))
            {
                return Results.NotFound(new { message = "Product not found." });
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO product_ratings (user_id, product_id, rating, explanation, created_at)
                VALUES (@userId, @productId, @rating, @explanation, datetime('now'))
                ON CONFLICT(user_id, product_id)
                DO UPDATE SET
                    rating = excluded.rating,
                    explanation = excluded.explanation,
                    created_at = datetime('now');";
            command.Parameters.AddWithValue("@userId", request.UserId);
            command.Parameters.AddWithValue("@productId", id);
            command.Parameters.AddWithValue("@rating", request.Stars);
            command.Parameters.AddWithValue("@explanation", explanation);
            command.ExecuteNonQuery();

            return Results.Ok(new { message = "Review saved." });
        });

        app.MapGet("/products/{id:long}/recommendations", (long id, DbOptions db) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new { message = "Product id must be greater than 0." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.ProductExists(connection, id))
            {
                return Results.NotFound(new { message = "Product not found." });
            }

            ProductRecommendationCache.RefreshIfNeeded(db.DatabasePath);
            var recommendations = ProductRecommendationCache.GetRecommendations(id);

            return Results.Ok(new
            {
                productId = id,
                recommendations,
                cacheLastRefreshed = ProductRecommendationCache.LastCacheTime
            });
        });

        return app;
    }
}
