namespace WebShop.Api.Tests;

public class OrderProcessingTests
{
    [Fact]
    public void Order_CanBeCreated_WithItems()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateOrderDatabase(dbPath);
            CreateOrderTestData(dbPath);
            
            // Create order
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO orders (user_id, order_number, total_price, shipping_address)
                    VALUES (1, 'ORD001', 99.99, '123 Main St');
                    INSERT INTO order_items (order_id, product_id, quantity, price)
                    SELECT last_insert_rowid(), 1, 2, 49.99;";
                cmd.ExecuteNonQuery();
            }

            // Verify order was created
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM orders";
                var orderCount = (long?)cmd.ExecuteScalar();
                Assert.Equal(1, orderCount);
            }
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    [Fact]
    public void OrderItems_AreLinkedCorrectly_ToOrder()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateOrderDatabase(dbPath);
            CreateOrderTestData(dbPath);
            
            // Create order with 2 items
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO orders (user_id, order_number, total_price, shipping_address)
                    VALUES (1, 'ORD001', 199.99, '123 Main St');
                    INSERT INTO order_items (order_id, product_id, quantity, price)
                    SELECT last_insert_rowid(), 1, 2, 49.99;
                    INSERT INTO order_items (order_id, product_id, quantity, price)
                    SELECT last_insert_rowid(), 2, 1, 99.99;";
                cmd.ExecuteNonQuery();
            }

            // Verify items count
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM order_items WHERE order_id = 1";
                var itemCount = (long?)cmd.ExecuteScalar();
                Assert.Equal(2, itemCount);
            }
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    private static void CreateOrderDatabase(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            PRAGMA foreign_keys = ON;
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                email TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                first_name TEXT,
                last_name TEXT,
                role INTEGER DEFAULT 0
            );
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
                user_id INTEGER NOT NULL,
                order_number TEXT NOT NULL,
                total_price REAL NOT NULL,
                shipping_address TEXT NOT NULL,
                discount_code_id INTEGER,
                FOREIGN KEY(user_id) REFERENCES users(id)
            );
            CREATE TABLE order_items (
                id INTEGER PRIMARY KEY,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                price REAL NOT NULL,
                FOREIGN KEY(order_id) REFERENCES orders(id),
                FOREIGN KEY(product_id) REFERENCES products(id)
            );";
        cmd.ExecuteNonQuery();
    }

    private static void CreateOrderTestData(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (email, password_hash, role) VALUES
            ('customer@example.com', 'hash123', 0);
            INSERT INTO products (category_id, name, price, stock) VALUES
            (1, 'Product 1', 49.99, 100),
            (1, 'Product 2', 99.99, 50);";
        cmd.ExecuteNonQuery();
    }
}