namespace WebShop.Api.Tests;

public class ProductReviewTests
{
    [Fact]
    public void Review_CanBeCreated_ForProduct()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateReviewDatabase(dbPath);
            CreateReviewTestData(dbPath);
            
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO product_ratings (user_id, product_id, rating, explanation, created_at)
                    VALUES (1, 1, 5, 'Great product!', datetime('now'));";
                cmd.ExecuteNonQuery();
            }

            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM product_ratings WHERE product_id = 1";
                var count = (long?)cmd.ExecuteScalar();
                Assert.Equal(1, count);
            }
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    [Fact]
    public void Review_EnforcesRatingConstraint_1To5()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateReviewDatabase(dbPath);
            CreateReviewTestData(dbPath);
            
            // Try to insert invalid rating
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO product_ratings (user_id, product_id, rating, explanation, created_at)
                VALUES (1, 1, 6, 'Invalid rating', datetime('now'));";
            
            var exception = Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => cmd.ExecuteNonQuery());
            Assert.Contains("CHECK", exception.Message);
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    [Fact]
    public void Review_UniqueUserProductConstraint_EnforcedCorrectly()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        try
        {
            CreateReviewDatabase(dbPath);
            CreateReviewTestData(dbPath);
            
            // Insert first review
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO product_ratings (user_id, product_id, rating, explanation, created_at)
                    VALUES (1, 1, 5, 'Great!', datetime('now'));";
                cmd.ExecuteNonQuery();
            }

            // Insert duplicate (should use upsert in real scenario, but test the constraint)
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO product_ratings (user_id, product_id, rating, explanation, created_at)
                    VALUES (1, 1, 4, 'Actually good', datetime('now'));";
                cmd.ExecuteNonQuery();
            }

            // Verify only one review exists
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM product_ratings WHERE user_id = 1 AND product_id = 1";
                var count = (long?)cmd.ExecuteScalar();
                Assert.Equal(1, count);
            }
        }
        finally
        {
            TestDataHelper.SafeDeleteTestDatabase(dbPath);
        }
    }

    private static void CreateReviewDatabase(string dbPath)
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
                role INTEGER DEFAULT 0
            );
            CREATE TABLE products (
                id INTEGER PRIMARY KEY,
                category_id INTEGER,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL
            );
            CREATE TABLE product_ratings (
                id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                rating INTEGER NOT NULL CHECK(rating BETWEEN 1 AND 5),
                explanation TEXT NOT NULL,
                created_at TEXT NOT NULL,
                UNIQUE(user_id, product_id),
                FOREIGN KEY(user_id) REFERENCES users(id),
                FOREIGN KEY(product_id) REFERENCES products(id)
            );";
        cmd.ExecuteNonQuery();
    }

    private static void CreateReviewTestData(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (email, password_hash, role) VALUES
            ('reviewer@example.com', 'hash123', 0);
            INSERT INTO products (category_id, name, price, stock) VALUES
            (1, 'Test Product', 29.99, 50);";
        cmd.ExecuteNonQuery();
    }
}