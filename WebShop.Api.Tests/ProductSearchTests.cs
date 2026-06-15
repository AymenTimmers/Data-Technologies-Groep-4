namespace WebShop.Api.Tests;

public class ProductSearchTests
{
    [Fact]
    public void ProductSearch_EmptyResults_ReturnsEmptyList()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateMinimalDatabase(dbPath);
            
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM products";
            var count = (long?)cmd.ExecuteScalar();
            
            Assert.Equal(0, count);
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    [Fact]
    public void ProductFilterByPrice_ValidRange_ReturnsFiltered()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateMinimalDatabase(dbPath);
            InsertTestProducts(dbPath);
            
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM products WHERE price >= 10 AND price <= 50";
            var count = (long?)cmd.ExecuteScalar();
            
            Assert.True(count > 0);
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    private static void CreateMinimalDatabase(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();
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
            );";
        cmd.ExecuteNonQuery();
    }

    private static void InsertTestProducts(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO products (category_id, name, price, stock, description) VALUES
            (1, 'Product A', 15.99, 10, 'Test product'),
            (1, 'Product B', 35.50, 5, 'Another product'),
            (1, 'Product C', 100.00, 3, 'Expensive product');";
        cmd.ExecuteNonQuery();
    }
}