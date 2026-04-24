using Microsoft.Data.Sqlite;
using WebShop.Api.Helpers;

namespace WebShop.Api.Tests;

public class NewFeatureTests
{
    [Fact]
    public void DiscountCodeUsage_UpdateDeactivatesWhenLimitReached()
    {
        var dbPath = CreateTempDbPath();
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using (var create = connection.CreateCommand())
            {
                create.CommandText = @"
                    CREATE TABLE discount_codes (
                        id INTEGER PRIMARY KEY,
                        code TEXT NOT NULL UNIQUE,
                        discount_percentage INTEGER NOT NULL,
                        active INTEGER NOT NULL,
                        valid_until TEXT NOT NULL,
                        max_uses INTEGER NOT NULL DEFAULT 1,
                        uses_count INTEGER NOT NULL DEFAULT 0
                    );
                    INSERT INTO discount_codes (code, discount_percentage, active, valid_until, max_uses, uses_count)
                    VALUES ('LIMIT2', 10, 1, '2028-12-31', 2, 1);";
                create.ExecuteNonQuery();
            }

            using (var update = connection.CreateCommand())
            {
                update.CommandText = @"
                    UPDATE discount_codes
                    SET uses_count = uses_count + 1,
                        active = CASE WHEN uses_count + 1 >= max_uses THEN 0 ELSE active END
                    WHERE code = 'LIMIT2';";
                update.ExecuteNonQuery();
            }

            using var query = connection.CreateCommand();
            query.CommandText = "SELECT uses_count, active FROM discount_codes WHERE code = 'LIMIT2'";
            using var reader = query.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(0));
            Assert.Equal(0, reader.GetInt32(1));
        }
        finally
        {
            SafeDelete(dbPath);
        }
    }

    [Fact]
    public void FavoritesTable_UniqueConstraintPreventsDuplicateFavorites()
    {
        var dbPath = CreateTempDbPath();
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using (var create = connection.CreateCommand())
            {
                create.CommandText = @"
                    CREATE TABLE favorites (
                        id INTEGER PRIMARY KEY,
                        user_id INTEGER NOT NULL,
                        product_id INTEGER NOT NULL,
                        UNIQUE (user_id, product_id)
                    );
                    INSERT INTO favorites (user_id, product_id) VALUES (1, 7);
                    INSERT OR IGNORE INTO favorites (user_id, product_id) VALUES (1, 7);";
                create.ExecuteNonQuery();
            }

            using var count = connection.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM favorites WHERE user_id = 1 AND product_id = 7";
            Assert.Equal(1L, (long)count.ExecuteScalar()!);
        }
        finally
        {
            SafeDelete(dbPath);
        }
    }

    [Fact]
    public void ShippingAddress_DefaultSwitchLeavesOnlyOneDefault()
    {
        var dbPath = CreateTempDbPath();
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using (var create = connection.CreateCommand())
            {
                create.CommandText = @"
                    CREATE TABLE user_shipping_addresses (
                        id INTEGER PRIMARY KEY,
                        user_id INTEGER NOT NULL,
                        label TEXT,
                        shipping_address TEXT NOT NULL,
                        is_default INTEGER NOT NULL DEFAULT 0
                    );
                    INSERT INTO user_shipping_addresses (user_id, label, shipping_address, is_default)
                    VALUES (1, 'Home', 'Address A', 1),
                           (1, 'Office', 'Address B', 0);";
                create.ExecuteNonQuery();
            }

            using (var switchDefault = connection.CreateCommand())
            {
                switchDefault.CommandText = @"
                    UPDATE user_shipping_addresses SET is_default = 0 WHERE user_id = 1;
                    UPDATE user_shipping_addresses SET is_default = 1 WHERE user_id = 1 AND label = 'Office';";
                switchDefault.ExecuteNonQuery();
            }

            using var countDefaults = connection.CreateCommand();
            countDefaults.CommandText = "SELECT COUNT(*) FROM user_shipping_addresses WHERE user_id = 1 AND is_default = 1";
            Assert.Equal(1L, (long)countDefaults.ExecuteScalar()!);
        }
        finally
        {
            SafeDelete(dbPath);
        }
    }

    [Fact]
    public void IsAdmin_ReturnsTrueOnlyForRoleOne()
    {
        var dbPath = CreateTempDbPath();
        try
        {
            using var connection = Db.CreateOpenConnection(dbPath);
            using (var create = connection.CreateCommand())
            {
                create.CommandText = @"
                    CREATE TABLE users (
                        id INTEGER PRIMARY KEY,
                        email TEXT NOT NULL UNIQUE,
                        password_hash TEXT NOT NULL,
                        role INTEGER NOT NULL
                    );
                    INSERT INTO users (id, email, password_hash, role)
                    VALUES (1, 'admin@test.com', 'hash', 1),
                           (2, 'user@test.com', 'hash', 0);";
                create.ExecuteNonQuery();
            }

            Assert.True(Db.IsAdmin(connection, 1));
            Assert.False(Db.IsAdmin(connection, 2));
        }
        finally
        {
            SafeDelete(dbPath);
        }
    }

    [Fact]
    public void DiscountCodeGenerator_CreatesRequestedLengthWithAllowedCharset()
    {
        var code = DiscountCodeGenerator.Create(12);

        Assert.Equal(12, code.Length);
        Assert.Matches("^[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]+$", code);
    }

    private static string CreateTempDbPath()
    {
        return Path.Combine(Path.GetTempPath(), $"new_feature_tests_{Guid.NewGuid()}.db");
    }

    private static void SafeDelete(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            return;
        }

        try
        {
            File.Delete(dbPath);
        }
        catch (IOException)
        {
            // Best effort cleanup for temporary test DB file.
        }
    }
}
