using Microsoft.Data.Sqlite;

namespace WebShop.Api.Helpers;

public static class Db
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

    public static bool IsAdmin(SqliteConnection connection, long userId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM users WHERE id = @userId AND role = 1 LIMIT 1";
        command.Parameters.AddWithValue("@userId", userId);
        return command.ExecuteScalar() is not null;
    }
}
