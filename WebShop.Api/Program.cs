using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5088");

var databaseFolder = Path.Combine(builder.Environment.ContentRootPath, "Database");
Directory.CreateDirectory(databaseFolder);
var databasePath = Path.Combine(databaseFolder, "webshop.db");

DbBootstrapper.EnsureCreated(databasePath, databaseFolder);

builder.Services.AddSingleton(new DbOptions(databasePath));

var app = builder.Build();

var logFolder = Path.Combine(builder.Environment.ContentRootPath, "Logs");
Directory.CreateDirectory(logFolder);
var requestLogPath = Path.Combine(logFolder, "requests.log");

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

    return Results.Ok(new
    {
        userId = reader.GetInt64(0),
        email = reader.GetString(1),
        role = reader.GetInt32(2)
    });
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
    var products = new List<object>();
    while (reader.Read())
    {
        products.Add(new
        {
            id = reader.GetInt64(0),
            categoryId = reader.GetInt64(1),
            name = reader.GetString(2),
            price = reader.GetDouble(3),
            stock = reader.GetInt32(4),
            description = reader.IsDBNull(5) ? null : reader.GetString(5),
            brand = reader.IsDBNull(6) ? null : reader.GetString(6),
            publisher = reader.IsDBNull(7) ? null : reader.GetString(7),
            releaseYear = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
        });
    }

    return Results.Ok(products);
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

    return Results.Ok(new
    {
        id = reader.GetInt64(0),
        categoryId = reader.GetInt64(1),
        name = reader.GetString(2),
        price = reader.GetDouble(3),
        stock = reader.GetInt32(4),
        description = reader.IsDBNull(5) ? null : reader.GetString(5),
        brand = reader.IsDBNull(6) ? null : reader.GetString(6),
        publisher = reader.IsDBNull(7) ? null : reader.GetString(7),
        releaseYear = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
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
    var items = new List<object>();
    while (reader.Read())
    {
        items.Add(new
        {
            itemId = reader.GetInt64(0),
            productId = reader.GetInt64(1),
            productName = reader.GetString(2),
            unitPrice = reader.GetDouble(3),
            quantity = reader.GetInt32(4)
        });
    }

    return Results.Ok(new { cartId, userId, items });
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
    var orders = new List<object>();
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
        var items = new List<object>();
        while (itemsReader.Read())
        {
            items.Add(new
            {
                productId = itemsReader.GetInt64(0),
                productName = itemsReader.GetString(1),
                quantity = itemsReader.GetInt32(2),
                unitPrice = itemsReader.GetDouble(3)
            });
        }

        orders.Add(new
        {
            orderId,
            orderNumber = reader.GetString(1),
            totalPrice = reader.GetDouble(2),
            shippingAddress = reader.GetString(3),
            items
        });
    }

    return Results.Ok(orders);
});

app.Run();

record DbOptions(string DatabasePath);
record RegisterRequest(string Email, string Password, string? FirstName, string? LastName);
record LoginRequest(string Email, string Password);
record AddCartItemRequest(long UserId, long ProductId, int Quantity);
record CheckoutRequest(long UserId, string ShippingAddress, string? DiscountCode);

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

static class DbBootstrapper
{
    public static void EnsureCreated(string dbPath, string databaseFolder)
    {
        if (File.Exists(dbPath))
        {
            return;
        }

        var schemaPath = Path.Combine(databaseFolder, "schema.sql");
        var seedPath = Path.Combine(databaseFolder, "seed.sql");

        if (!File.Exists(schemaPath) || !File.Exists(seedPath))
        {
            throw new InvalidOperationException("Missing schema.sql or seed.sql in database folder.");
        }

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using (var schema = connection.CreateCommand())
        {
            schema.CommandText = File.ReadAllText(schemaPath);
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
