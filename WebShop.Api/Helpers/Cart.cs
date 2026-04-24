using Microsoft.Data.Sqlite;

namespace WebShop.Api.Helpers;

public static class Cart
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
