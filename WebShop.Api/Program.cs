using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using WebShop.Api.Models;
using WebShop.Contracts.Models;

var builder = WebApplication.CreateBuilder(args);
var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(configuredUrls) ? "http://0.0.0.0:5088" : configuredUrls);

var databaseFolder = Path.Combine(builder.Environment.ContentRootPath, "Database");
Directory.CreateDirectory(databaseFolder);
var databasePath = Path.Combine(databaseFolder, "webshop.db");

DbBootstrapper.EnsureCreated(databasePath, databaseFolder);

builder.Services.AddSingleton(new DbOptions(databasePath));

var app = builder.Build();

var logFolder = Path.Combine(builder.Environment.ContentRootPath, "Logs");
Directory.CreateDirectory(logFolder);
var requestLogPath = Path.Combine(logFolder, "requests.log");
var documentationFolder = Path.Combine(builder.Environment.ContentRootPath, "Documentation");
Directory.CreateDirectory(documentationFolder);

// Initialize recommendations cache
ProductRecommendationCache.RefreshIfNeeded(databasePath, forceRefresh: true);

app.Use(async (context, next) =>
{
    var start = Stopwatch.StartNew();
    string? errorMessage = null;

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        throw;
    }
    finally
    {
        start.Stop();
        RequestFileLogger.Append(
            requestLogPath,
            context.Request.Method,
            $"{context.Request.Path}{context.Request.QueryString}",
            context.Response.StatusCode,
            start.ElapsedMilliseconds,
            errorMessage
        );
    }
});

app.MapPost("/auth/register", (RegisterRequest request, DbOptions db) =>
{
    if (!Input.TryNormalizeEmail(request.Email, out var normalizedEmail))
    {
        return Results.BadRequest(new { message = "A valid email is required." });
    }

    if (!Input.IsValidPassword(request.Password))
    {
        return Results.BadRequest(new { message = "Password must be between 6 and 128 characters." });
    }

    var firstName = Input.NormalizeOptional(request.FirstName, 100);
    var lastName = Input.NormalizeOptional(request.LastName, 100);

    using var connection = Db.CreateOpenConnection(db.DatabasePath);

    using var existsCommand = connection.CreateCommand();
    existsCommand.CommandText = "SELECT id FROM users WHERE email = @email";
    existsCommand.Parameters.AddWithValue("@email", normalizedEmail);
    if (existsCommand.ExecuteScalar() is not null)
    {
        return Results.Conflict(new { message = "Email already exists." });
    }

    using var transaction = connection.BeginTransaction();

    using var userCommand = connection.CreateCommand();
    userCommand.Transaction = transaction;
    userCommand.CommandText = @"
        INSERT INTO users (email, password_hash, first_name, last_name, role)
        VALUES (@email, @passwordHash, @firstName, @lastName, 0);
        SELECT last_insert_rowid();";
    userCommand.Parameters.AddWithValue("@email", normalizedEmail);
    userCommand.Parameters.AddWithValue("@passwordHash", Security.HashPassword(request.Password));
    userCommand.Parameters.AddWithValue("@firstName", (object?)firstName ?? DBNull.Value);
    userCommand.Parameters.AddWithValue("@lastName", (object?)lastName ?? DBNull.Value);

    var userId = Convert.ToInt64(userCommand.ExecuteScalar());

    using var cartCommand = connection.CreateCommand();
    cartCommand.Transaction = transaction;
    cartCommand.CommandText = "INSERT INTO carts (user_id) VALUES (@userId)";
    cartCommand.Parameters.AddWithValue("@userId", userId);
    cartCommand.ExecuteNonQuery();

    transaction.Commit();

    return Results.Created($"/users/{userId}", new { userId, email = normalizedEmail, role = 0 });
});

app.MapPost("/auth/login", (LoginRequest request, DbOptions db) =>
{
    if (!Input.TryNormalizeEmail(request.Email, out var normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "A valid email and password are required." });
    }

    using var connection = Db.CreateOpenConnection(db.DatabasePath);
    using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT id, email, role
        FROM users
        WHERE email = @email AND password_hash = @passwordHash";
    command.Parameters.AddWithValue("@email", normalizedEmail);
    command.Parameters.AddWithValue("@passwordHash", Security.HashPassword(request.Password));

    using var reader = command.ExecuteReader();
    if (!reader.Read())
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new AuthResponse(
        reader.GetInt64(0),
        reader.GetString(1),
        reader.GetInt32(2)
    ));
});

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
        SELECT id, category_id, name, price, stock, description, brand, publisher, release_year
        FROM products
        {whereClause}
        ORDER BY name";

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

    // Check if cache needs refresh
    ProductRecommendationCache.RefreshIfNeeded(db.DatabasePath);

    // Get recommendations from cache
    var recommendations = ProductRecommendationCache.GetRecommendations(id);

    return Results.Ok(new
    {
        productId = id,
        recommendations,
        cacheLastRefreshed = ProductRecommendationCache.LastCacheTime
    });
});

app.MapPost("/cache/recommendations/refresh", (DbOptions db) =>
{
    ProductRecommendationCache.RefreshIfNeeded(db.DatabasePath, forceRefresh: true);
    
    return Results.Ok(new
    {
        message = "Recommendations cache refreshed.",
        refreshedAt = ProductRecommendationCache.LastCacheTime
    });
});

app.MapGet("/cart/{userId:long}", (long userId, DbOptions db) =>
{
    if (userId <= 0)
    {
        return Results.BadRequest(new { message = "User id must be greater than 0." });
    }

    using var connection = Db.CreateOpenConnection(db.DatabasePath);
    if (!Db.UserExists(connection, userId))
    {
        return Results.NotFound(new { message = "User not found." });
    }

    var cartId = Cart.GetOrCreateCartId(connection, userId);

    using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT ci.id, ci.product_id, p.name, p.price, ci.quantity
        FROM cart_items ci
        INNER JOIN products p ON p.id = ci.product_id
        WHERE ci.cart_id = @cartId
        ORDER BY ci.id";
    command.Parameters.AddWithValue("@cartId", cartId);

    using var reader = command.ExecuteReader();
    var items = new List<CartItemDto>();
    while (reader.Read())
    {
        items.Add(new CartItemDto(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetDouble(3),
            reader.GetInt32(4)
        ));
    }

    return Results.Ok(new CartResponseDto(cartId, userId, items));
});

app.MapPost("/cart/items", (AddCartItemRequest request, DbOptions db) =>
{
    if (request.UserId <= 0 || request.ProductId <= 0)
    {
        return Results.BadRequest(new { message = "User id and product id must be greater than 0." });
    }

    if (request.Quantity <= 0 || request.Quantity > 100)
    {
        return Results.BadRequest(new { message = "Quantity must be between 1 and 100." });
    }

    using var connection = Db.CreateOpenConnection(db.DatabasePath);
    if (!Db.UserExists(connection, request.UserId))
    {
        return Results.NotFound(new { message = "User not found." });
    }

    var cartId = Cart.GetOrCreateCartId(connection, request.UserId);

    using var productCommand = connection.CreateCommand();
    productCommand.CommandText = "SELECT id, stock FROM products WHERE id = @productId";
    productCommand.Parameters.AddWithValue("@productId", request.ProductId);
    using var productReader = productCommand.ExecuteReader();
    if (!productReader.Read())
    {
        return Results.NotFound(new { message = "Product not found." });
    }

    var stock = productReader.GetInt32(1);

    using var existingCommand = connection.CreateCommand();
    existingCommand.CommandText = "SELECT id, quantity FROM cart_items WHERE cart_id = @cartId AND product_id = @productId";
    existingCommand.Parameters.AddWithValue("@cartId", cartId);
    existingCommand.Parameters.AddWithValue("@productId", request.ProductId);

    using var reader = existingCommand.ExecuteReader();
    if (reader.Read())
    {
        var itemId = reader.GetInt64(0);
        var existingQuantity = reader.GetInt32(1);
        var newQuantity = existingQuantity + request.Quantity;

        if (newQuantity > stock)
        {
            return Results.BadRequest(new { message = "Requested quantity exceeds available stock." });
        }

        using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = "UPDATE cart_items SET quantity = @quantity WHERE id = @id";
        updateCommand.Parameters.AddWithValue("@quantity", newQuantity);
        updateCommand.Parameters.AddWithValue("@id", itemId);
        updateCommand.ExecuteNonQuery();
    }
    else
    {
        if (request.Quantity > stock)
        {
            return Results.BadRequest(new { message = "Requested quantity exceeds available stock." });
        }

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO cart_items (cart_id, product_id, quantity) VALUES (@cartId, @productId, @quantity)";
        insertCommand.Parameters.AddWithValue("@cartId", cartId);
        insertCommand.Parameters.AddWithValue("@productId", request.ProductId);
        insertCommand.Parameters.AddWithValue("@quantity", request.Quantity);
        insertCommand.ExecuteNonQuery();
    }

    return Results.Ok(new { message = "Cart updated." });
});

app.MapDelete("/cart/items/{itemId:long}", (long itemId, long userId, DbOptions db) =>
{
    if (itemId <= 0 || userId <= 0)
    {
        return Results.BadRequest(new { message = "Item id and user id must be greater than 0." });
    }

    using var connection = Db.CreateOpenConnection(db.DatabasePath);
    if (!Db.UserExists(connection, userId))
    {
        return Results.NotFound(new { message = "User not found." });
    }

    using var command = connection.CreateCommand();
    command.CommandText = @"
        DELETE FROM cart_items
        WHERE id = @itemId
          AND cart_id = (SELECT id FROM carts WHERE user_id = @userId LIMIT 1)";
    command.Parameters.AddWithValue("@itemId", itemId);
    command.Parameters.AddWithValue("@userId", userId);
    var rows = command.ExecuteNonQuery();

    return rows == 0 ? Results.NotFound() : Results.Ok(new { message = "Item removed." });
});

app.MapPost("/orders/checkout", (CheckoutRequest request, DbOptions db) =>
{
    if (request.UserId <= 0)
    {
        return Results.BadRequest(new { message = "User id must be greater than 0." });
    }

    if (string.IsNullOrWhiteSpace(request.ShippingAddress) || request.ShippingAddress.Trim().Length > 250)
    {
        return Results.BadRequest(new { message = "Shipping address is required and must be max 250 characters." });
    }

    if (!string.IsNullOrWhiteSpace(request.DiscountCode) && request.DiscountCode.Trim().Length > 40)
    {
        return Results.BadRequest(new { message = "Discount code must be max 40 characters." });
    }

    using var connection = Db.CreateOpenConnection(db.DatabasePath);
    if (!Db.UserExists(connection, request.UserId))
    {
        return Results.NotFound(new { message = "User not found." });
    }

    var cartId = Cart.GetOrCreateCartId(connection, request.UserId);

    using var cartItemsCommand = connection.CreateCommand();
    cartItemsCommand.CommandText = @"
        SELECT ci.product_id, ci.quantity, p.price, p.stock
        FROM cart_items ci
        INNER JOIN products p ON p.id = ci.product_id
        WHERE ci.cart_id = @cartId";
    cartItemsCommand.Parameters.AddWithValue("@cartId", cartId);

    var checkoutItems = new List<(long ProductId, int Quantity, double UnitPrice, int Stock)>();
    using (var reader = cartItemsCommand.ExecuteReader())
    {
        while (reader.Read())
        {
            checkoutItems.Add((
                reader.GetInt64(0),
                reader.GetInt32(1),
                reader.GetDouble(2),
                reader.GetInt32(3)
            ));
        }
    }

    if (checkoutItems.Count == 0)
    {
        return Results.BadRequest(new { message = "Cart is empty." });
    }

    foreach (var item in checkoutItems)
    {
        if (item.Quantity > item.Stock)
        {
            return Results.BadRequest(new { message = $"Not enough stock for product {item.ProductId}." });
        }
    }

    double subtotal = checkoutItems.Sum(i => i.UnitPrice * i.Quantity);
    int? discountCodeId = null;
    var discountPercent = 0;

    if (!string.IsNullOrWhiteSpace(request.DiscountCode))
    {
        using var discountCommand = connection.CreateCommand();
        discountCommand.CommandText = @"
            SELECT id, discount_percentage
            FROM discount_codes
            WHERE code = @code
              AND active = 1
              AND date(valid_until) >= date('now')";
        discountCommand.Parameters.AddWithValue("@code", request.DiscountCode.Trim().ToUpperInvariant());

        using var reader = discountCommand.ExecuteReader();
        if (reader.Read())
        {
            discountCodeId = reader.GetInt32(0);
            discountPercent = reader.GetInt32(1);
        }
    }

    var total = Math.Round(subtotal * (1 - (discountPercent / 100.0)), 2);
    var orderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmssfff}";

    using var transaction = connection.BeginTransaction();

    using var orderCommand = connection.CreateCommand();
    orderCommand.Transaction = transaction;
    orderCommand.CommandText = @"
        INSERT INTO orders (user_id, order_number, total_price, shipping_address, discount_code_id)
        VALUES (@userId, @orderNumber, @totalPrice, @shippingAddress, @discountCodeId);
        SELECT last_insert_rowid();";
    orderCommand.Parameters.AddWithValue("@userId", request.UserId);
    orderCommand.Parameters.AddWithValue("@orderNumber", orderNumber);
    orderCommand.Parameters.AddWithValue("@totalPrice", total);
    orderCommand.Parameters.AddWithValue("@shippingAddress", request.ShippingAddress.Trim());
    orderCommand.Parameters.AddWithValue("@discountCodeId", (object?)discountCodeId ?? DBNull.Value);

    var orderId = Convert.ToInt64(orderCommand.ExecuteScalar());

    foreach (var item in checkoutItems)
    {
        using var orderItemCommand = connection.CreateCommand();
        orderItemCommand.Transaction = transaction;
        orderItemCommand.CommandText = @"
            INSERT INTO order_items (order_id, product_id, quantity, price)
            VALUES (@orderId, @productId, @quantity, @price)";
        orderItemCommand.Parameters.AddWithValue("@orderId", orderId);
        orderItemCommand.Parameters.AddWithValue("@productId", item.ProductId);
        orderItemCommand.Parameters.AddWithValue("@quantity", item.Quantity);
        orderItemCommand.Parameters.AddWithValue("@price", item.UnitPrice);
        orderItemCommand.ExecuteNonQuery();

        using var stockCommand = connection.CreateCommand();
        stockCommand.Transaction = transaction;
        stockCommand.CommandText = "UPDATE products SET stock = stock - @quantity WHERE id = @productId";
        stockCommand.Parameters.AddWithValue("@quantity", item.Quantity);
        stockCommand.Parameters.AddWithValue("@productId", item.ProductId);
        stockCommand.ExecuteNonQuery();
    }

    using var paymentCommand = connection.CreateCommand();
    paymentCommand.Transaction = transaction;
    paymentCommand.CommandText = @"
        INSERT INTO payments (order_id, transaction_reference, total_paid)
        VALUES (@orderId, @transactionReference, @totalPaid)";
    paymentCommand.Parameters.AddWithValue("@orderId", orderId);
    paymentCommand.Parameters.AddWithValue("@transactionReference", $"TXR{Guid.NewGuid():N}"[..12].ToUpperInvariant());
    paymentCommand.Parameters.AddWithValue("@totalPaid", total);
    paymentCommand.ExecuteNonQuery();

    using var clearCartCommand = connection.CreateCommand();
    clearCartCommand.Transaction = transaction;
    clearCartCommand.CommandText = "DELETE FROM cart_items WHERE cart_id = @cartId";
    clearCartCommand.Parameters.AddWithValue("@cartId", cartId);
    clearCartCommand.ExecuteNonQuery();

    transaction.Commit();

    return Results.Ok(new { orderId, orderNumber, totalPrice = total });
});

app.MapGet("/orders/{userId:long}", (long userId, DbOptions db) =>
{
    if (userId <= 0)
    {
        return Results.BadRequest(new { message = "User id must be greater than 0." });
    }

    using var connection = Db.CreateOpenConnection(db.DatabasePath);
    if (!Db.UserExists(connection, userId))
    {
        return Results.NotFound(new { message = "User not found." });
    }

    using var ordersCommand = connection.CreateCommand();
    ordersCommand.CommandText = @"
        SELECT id, order_number, total_price, shipping_address
        FROM orders
        WHERE user_id = @userId
        ORDER BY id DESC";
    ordersCommand.Parameters.AddWithValue("@userId", userId);

    using var reader = ordersCommand.ExecuteReader();
    var orders = new List<OrderResponseDto>();
    while (reader.Read())
    {
        var orderId = reader.GetInt64(0);

        using var itemsCommand = connection.CreateCommand();
        itemsCommand.CommandText = @"
            SELECT oi.product_id, p.name, oi.quantity, oi.price
            FROM order_items oi
            INNER JOIN products p ON p.id = oi.product_id
            WHERE oi.order_id = @orderId
            ORDER BY oi.id";
        itemsCommand.Parameters.AddWithValue("@orderId", orderId);

        using var itemsReader = itemsCommand.ExecuteReader();
        var items = new List<OrderItemDto>();
        while (itemsReader.Read())
        {
            items.Add(new OrderItemDto(
                itemsReader.GetInt64(0),
                itemsReader.GetString(1),
                itemsReader.GetInt32(2),
                itemsReader.GetDouble(3)
            ));
        }

        orders.Add(new OrderResponseDto(
            orderId,
            reader.GetString(1),
            reader.GetDouble(2),
            reader.GetString(3),
            items
        ));
    }

    return Results.Ok(orders);
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

app.Run();

static class Db
{
    public static SqliteConnection CreateOpenConnection(string dbPath)
    {
        var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    public static bool UserExists(SqliteConnection connection, long userId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM users WHERE id = @userId LIMIT 1";
        command.Parameters.AddWithValue("@userId", userId);
        return command.ExecuteScalar() is not null;
    }

    public static bool ProductExists(SqliteConnection connection, long productId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM products WHERE id = @productId LIMIT 1";
        command.Parameters.AddWithValue("@productId", productId);
        return command.ExecuteScalar() is not null;
    }
}

public static class Input
{
    public static bool TryNormalizeEmail(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim().ToLowerInvariant();
        try
        {
            _ = new MailAddress(trimmed);
            normalizedEmail = trimmed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string? password)
    {
        return !string.IsNullOrWhiteSpace(password)
            && password.Length >= 6
            && password.Length <= 128;
    }

    public static string? NormalizeOptional(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

static class Cart
{
    public static long GetOrCreateCartId(SqliteConnection connection, long userId)
    {
        using var getCart = connection.CreateCommand();
        getCart.CommandText = "SELECT id FROM carts WHERE user_id = @userId LIMIT 1";
        getCart.Parameters.AddWithValue("@userId", userId);
        var cartId = getCart.ExecuteScalar();
        if (cartId is not null)
        {
            return Convert.ToInt64(cartId);
        }

        using var createCart = connection.CreateCommand();
        createCart.CommandText = "INSERT INTO carts (user_id) VALUES (@userId); SELECT last_insert_rowid();";
        createCart.Parameters.AddWithValue("@userId", userId);
        return Convert.ToInt64(createCart.ExecuteScalar());
    }
}

public static class Security
{
    public static string HashPassword(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

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

        // Get all products
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

        // For each product, find co-purchased products
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

            // Build recommendation list
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

static class DbBootstrapper
{
    public static void EnsureCreated(string dbPath, string databaseFolder)
    {
        var schemaModelsFolder = Path.Combine(databaseFolder, "Models");
        var movedSchemaPath = Path.Combine(schemaModelsFolder, "schema.sql");
        var seedPath = Path.Combine(databaseFolder, "seed.sql");

        if (!Directory.Exists(schemaModelsFolder) || !File.Exists(seedPath))
        {
            throw new InvalidOperationException("Missing Database/Models folder or seed.sql in database folder.");
        }

        var schemaFiles = Directory.GetFiles(schemaModelsFolder, "*.sql", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), "schema.sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (schemaFiles.Count == 0 && File.Exists(movedSchemaPath))
        {
            schemaFiles.Add(movedSchemaPath);
        }

        if (schemaFiles.Count == 0)
        {
            throw new InvalidOperationException("No SQL schema model files found in Database/Models.");
        }

        var expectedFingerprint = ComputeSchemaFingerprint(schemaFiles, seedPath);

        if (!File.Exists(dbPath))
        {
            InitializeDatabase(dbPath, schemaFiles, seedPath, expectedFingerprint);
            return;
        }

        string? currentFingerprint;
        using (var connection = new SqliteConnection($"Data Source={dbPath}"))
        {
            connection.Open();
            EnsureMetaTable(connection);
            currentFingerprint = ReadMetaValue(connection, "schema_fingerprint");

            if (string.Equals(currentFingerprint, expectedFingerprint, StringComparison.Ordinal))
            {
                return;
            }
        }

        var backupPath = BuildBackupPath(dbPath);
        try
        {
            File.Copy(dbPath, backupPath, overwrite: true);
            File.Delete(dbPath);
        }
        catch (IOException)
        {
            // If another process currently has the DB open, skip auto-rebuild for this startup.
            return;
        }

        InitializeDatabase(dbPath, schemaFiles, seedPath, expectedFingerprint);
    }

    private static void InitializeDatabase(
        string dbPath,
        IReadOnlyList<string> schemaFiles,
        string seedPath,
        string schemaFingerprint)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        foreach (var schemaFile in schemaFiles)
        {
            using var schema = connection.CreateCommand();
            schema.CommandText = File.ReadAllText(schemaFile);
            schema.ExecuteNonQuery();
        }

        using (var seed = connection.CreateCommand())
        {
            var seedSql = File.ReadAllText(seedPath)
                .Replace("'hash1'", $"'{Security.HashPassword("password123")}'")
                .Replace("'hash2'", $"'{Security.HashPassword("admin123")}'");
            seed.CommandText = seedSql;
            seed.ExecuteNonQuery();
        }

        EnsureMetaTable(connection);
        WriteMetaValue(connection, "schema_fingerprint", schemaFingerprint);
    }

    private static void EnsureMetaTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS __db_meta (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    private static string? ReadMetaValue(SqliteConnection connection, string key)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM __db_meta WHERE key = @key LIMIT 1";
        command.Parameters.AddWithValue("@key", key);
        return command.ExecuteScalar() as string;
    }

    private static void WriteMetaValue(SqliteConnection connection, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO __db_meta (key, value)
            VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        command.ExecuteNonQuery();
    }

    private static string ComputeSchemaFingerprint(IReadOnlyList<string> schemaFiles, string seedPath)
    {
        var builder = new StringBuilder();
        foreach (var schemaFile in schemaFiles)
        {
            builder.AppendLine(Path.GetFileName(schemaFile));
            builder.AppendLine(File.ReadAllText(schemaFile));
        }

        builder.AppendLine(Path.GetFileName(seedPath));
        builder.AppendLine(File.ReadAllText(seedPath));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hashBytes);
    }

    private static string BuildBackupPath(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(dbPath);
        var extension = Path.GetExtension(dbPath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return Path.Combine(directory, $"{fileName}.bak.{timestamp}{extension}");
    }
}

static class RequestFileLogger
{
    private static readonly object Sync = new();

    public static void Append(
        string filePath,
        string method,
        string path,
        int statusCode,
        long elapsedMs,
        string? errorMessage)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var safeError = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $" | error={errorMessage.Replace(Environment.NewLine, " ")}";
        var line = $"{timestamp} UTC | {method} {path} | status={statusCode} | elapsedMs={elapsedMs}{safeError}";

        lock (Sync)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}

static class ModelDocumentationGenerator
{
    public static DocumentationGenerationResult Generate(string dbPath, string outputPath)
    {
        using var connection = Db.CreateOpenConnection(dbPath);

        var tables = GetTables(connection);
        var allRelations = new List<DbRelation>();
        var generatedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        var markdown = new StringBuilder();
        markdown.AppendLine("# Database Models and Relations");
        markdown.AppendLine();
        markdown.AppendLine($"Generated at: {generatedAtUtc}");
        markdown.AppendLine();

        foreach (var table in tables)
        {
            var columns = GetColumns(connection, table);
            var relations = GetRelations(connection, table);
            allRelations.AddRange(relations);

            markdown.AppendLine($"## {table}");
            markdown.AppendLine();
            markdown.AppendLine("| Column | Type | Not Null | PK | Default |");
            markdown.AppendLine("|---|---|---|---|---|");
            foreach (var column in columns)
            {
                markdown.AppendLine($"| {column.Name} | {column.Type} | {(column.NotNull ? "Yes" : "No")} | {(column.IsPrimaryKey ? "Yes" : "No")} | {column.DefaultValue ?? ""} |");
            }

            if (relations.Count > 0)
            {
                markdown.AppendLine();
                markdown.AppendLine("Relations:");
                foreach (var relation in relations)
                {
                    markdown.AppendLine($"- {relation.FromTable}.{relation.FromColumn} -> {relation.ToTable}.{relation.ToColumn}");
                }
            }

            markdown.AppendLine();
        }

        var uniqueRelations = allRelations
            .DistinctBy(r => (r.FromTable, r.FromColumn, r.ToTable, r.ToColumn))
            .ToList();

        markdown.AppendLine("## ER Diagram (Mermaid)");
        markdown.AppendLine();
        markdown.AppendLine("```mermaid");
        markdown.AppendLine("erDiagram");

        foreach (var table in tables)
        {
            var columns = GetColumns(connection, table);
            markdown.AppendLine($"  {table} {{");
            foreach (var column in columns)
            {
                var markers = new List<string>();
                if (column.IsPrimaryKey)
                {
                    markers.Add("PK");
                }

                if (uniqueRelations.Any(r => r.FromTable == table && r.FromColumn == column.Name))
                {
                    markers.Add("FK");
                }

                var markerText = markers.Count == 0 ? string.Empty : $" {string.Join(" ", markers)}";
                markdown.AppendLine($"    {column.Type} {column.Name}{markerText}");
            }
            markdown.AppendLine("  }");
        }

        foreach (var relation in uniqueRelations)
        {
            markdown.AppendLine($"  {relation.ToTable} ||--o{{ {relation.FromTable} : \"{relation.FromColumn}->{relation.ToColumn}\"");
        }

        markdown.AppendLine("```");

        File.WriteAllText(outputPath, markdown.ToString());

        return new DocumentationGenerationResult(generatedAtUtc, tables.Count, uniqueRelations.Count, outputPath);
    }

    private static List<string> GetTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
              AND name <> '__db_meta'
            ORDER BY name;";

        using var reader = command.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static List<DbColumn> GetColumns(SqliteConnection connection, string table)
    {
        var safeTable = table.Replace("'", "''");
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{safeTable}')";

        using var reader = command.ExecuteReader();
        var columns = new List<DbColumn>();
        while (reader.Read())
        {
            columns.Add(new DbColumn(
                reader.GetString(1),
                string.IsNullOrWhiteSpace(reader.GetString(2)) ? "TEXT" : reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetInt32(5) == 1,
                reader.IsDBNull(4) ? null : reader.GetString(4)
            ));
        }

        return columns;
    }

    private static List<DbRelation> GetRelations(SqliteConnection connection, string table)
    {
        var safeTable = table.Replace("'", "''");
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list('{safeTable}')";

        using var reader = command.ExecuteReader();
        var relations = new List<DbRelation>();
        while (reader.Read())
        {
            relations.Add(new DbRelation(
                table,
                reader.GetString(3),
                reader.GetString(2),
                reader.GetString(4)
            ));
        }

        return relations;
    }

    private sealed record DbColumn(string Name, string Type, bool NotNull, bool IsPrimaryKey, string? DefaultValue);
    private sealed record DbRelation(string FromTable, string FromColumn, string ToTable, string ToColumn);
}

sealed record DocumentationGenerationResult(string GeneratedAtUtc, int TableCount, int RelationCount, string OutputPath);
