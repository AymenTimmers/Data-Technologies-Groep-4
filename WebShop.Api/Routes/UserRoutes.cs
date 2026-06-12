using System.Security.Claims;
using WebShop.Api.Models;
using WebShop.Contracts.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public static class UserRoutes
{
    public static WebApplication MapUserRoutes(this WebApplication app)
    {
        app.MapGet("/users/{userId:long}/shipping-addresses", (long userId, ClaimsPrincipal user, DbOptions db) =>
        {
            if (userId <= 0)
            {
                return Results.BadRequest(new { message = "User id must be greater than 0." });
            }

            if (user.GetUserId() != userId && !user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.UserExists(connection, userId))
            {
                return Results.NotFound(new { message = "User not found." });
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, user_id, label, shipping_address, is_default
                FROM user_shipping_addresses
                WHERE user_id = @userId
                ORDER BY is_default DESC, id ASC";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            var addresses = new List<ShippingAddressDto>();
            while (reader.Read())
            {
                addresses.Add(new ShippingAddressDto(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4) == 1
                ));
            }

            return Results.Ok(addresses);
        }).RequireAuthorization();

        app.MapPost("/users/{userId:long}/shipping-addresses", (long userId, CreateShippingAddressRequest request, ClaimsPrincipal user, DbOptions db) =>
        {
            if (userId <= 0)
            {
                return Results.BadRequest(new { message = "User id must be greater than 0." });
            }

            if (user.GetUserId() != userId && !user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var shippingAddress = request.ShippingAddress?.Trim();
            if (string.IsNullOrWhiteSpace(shippingAddress) || shippingAddress.Length > 250)
            {
                return Results.BadRequest(new { message = "Shipping address is required and must be max 250 characters." });
            }

            var label = Input.NormalizeOptional(request.Label, 40);

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.UserExists(connection, userId))
            {
                return Results.NotFound(new { message = "User not found." });
            }

            using var transaction = connection.BeginTransaction();

            if (request.SetAsDefault)
            {
                using var resetDefault = connection.CreateCommand();
                resetDefault.Transaction = transaction;
                resetDefault.CommandText = "UPDATE user_shipping_addresses SET is_default = 0 WHERE user_id = @userId";
                resetDefault.Parameters.AddWithValue("@userId", userId);
                resetDefault.ExecuteNonQuery();
            }

            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = @"
                INSERT INTO user_shipping_addresses (user_id, label, shipping_address, is_default)
                VALUES (@userId, @label, @shippingAddress, @isDefault);
                SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("@userId", userId);
            insert.Parameters.AddWithValue("@label", (object?)label ?? DBNull.Value);
            insert.Parameters.AddWithValue("@shippingAddress", shippingAddress);
            insert.Parameters.AddWithValue("@isDefault", request.SetAsDefault ? 1 : 0);
            var addressId = Convert.ToInt64(insert.ExecuteScalar());

            transaction.Commit();

            return Results.Created($"/users/{userId}/shipping-addresses/{addressId}", new ShippingAddressDto(
                addressId,
                userId,
                label,
                shippingAddress,
                request.SetAsDefault
            ));
        }).RequireAuthorization();

        app.MapDelete("/users/{userId:long}/shipping-addresses/{addressId:long}", (long userId, long addressId, ClaimsPrincipal user, DbOptions db) =>
        {
            if (userId <= 0 || addressId <= 0)
            {
                return Results.BadRequest(new { message = "User id and address id must be greater than 0." });
            }

            if (user.GetUserId() != userId && !user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM user_shipping_addresses
                WHERE id = @addressId AND user_id = @userId";
            command.Parameters.AddWithValue("@addressId", addressId);
            command.Parameters.AddWithValue("@userId", userId);
            var rows = command.ExecuteNonQuery();

            return rows == 0
                ? Results.NotFound(new { message = "Address not found." })
                : Results.Ok(new { message = "Address removed." });
        }).RequireAuthorization();

        app.MapGet("/users/{userId:long}/favorites", (long userId, ClaimsPrincipal user, DbOptions db) =>
        {
            if (userId <= 0)
            {
                return Results.BadRequest(new { message = "User id must be greater than 0." });
            }

            if (user.GetUserId() != userId && !user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.UserExists(connection, userId))
            {
                return Results.NotFound(new { message = "User not found." });
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT f.id, p.id, p.name, p.price, p.stock, p.description
                FROM favorites f
                INNER JOIN products p ON p.id = f.product_id
                WHERE f.user_id = @userId
                ORDER BY f.id DESC";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            var favorites = new List<FavoriteProductDto>();
            while (reader.Read())
            {
                favorites.Add(new FavoriteProductDto(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5)
                ));
            }

            return Results.Ok(favorites);
        }).RequireAuthorization();

        app.MapPost("/users/{userId:long}/favorites/{productId:long}", (long userId, long productId, ClaimsPrincipal user, DbOptions db) =>
        {
            if (userId <= 0 || productId <= 0)
            {
                return Results.BadRequest(new { message = "User id and product id must be greater than 0." });
            }

            if (user.GetUserId() != userId && !user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            if (!Db.UserExists(connection, userId))
            {
                return Results.NotFound(new { message = "User not found." });
            }
            if (!Db.ProductExists(connection, productId))
            {
                return Results.NotFound(new { message = "Product not found." });
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO favorites (user_id, product_id)
                VALUES (@userId, @productId);";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@productId", productId);
            command.ExecuteNonQuery();

            return Results.Ok(new { message = "Product added to favorites." });
        }).RequireAuthorization();

        app.MapDelete("/users/{userId:long}/favorites/{favoriteId:long}", (long userId, long favoriteId, ClaimsPrincipal user, DbOptions db) =>
        {
            if (userId <= 0 || favoriteId <= 0)
            {
                return Results.BadRequest(new { message = "User id and favorite id must be greater than 0." });
            }

            if (user.GetUserId() != userId && !user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM favorites
                WHERE id = @favoriteId AND user_id = @userId";
            command.Parameters.AddWithValue("@favoriteId", favoriteId);
            command.Parameters.AddWithValue("@userId", userId);
            var rows = command.ExecuteNonQuery();

            return rows == 0
                ? Results.NotFound(new { message = "Favorite not found." })
                : Results.Ok(new { message = "Favorite removed." });
        }).RequireAuthorization();

        return app;
    }
}
