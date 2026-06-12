using System.Security.Claims;
using WebShop.Api.Models;
using WebShop.Contracts.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public static class AdminRoutes
{
    public static WebApplication MapAdminRoutes(this WebApplication app)
    {
        app.MapGet("/admin/users/search", (string? query, ClaimsPrincipal user, DbOptions db) =>
        {
            var q = string.IsNullOrWhiteSpace(query) ? null : $"%{query.Trim()}%";

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, email, first_name, last_name, role
                FROM users
                WHERE @query IS NULL
                   OR email LIKE @query
                   OR first_name LIKE @query
                   OR last_name LIKE @query
                ORDER BY id
                LIMIT 100";
            command.Parameters.AddWithValue("@query", (object?)q ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            var users = new List<AdminUserSummaryDto>();
            while (reader.Read())
            {
                users.Add(new AdminUserSummaryDto(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.GetInt32(4)
                ));
            }

            return Results.Ok(users);
        }).RequireAuthorization("Admin");

        app.MapGet("/admin/users/{userId:long}", (long userId, DbOptions db) =>
        {
            if (userId <= 0)
            {
                return Results.BadRequest(new { message = "User id must be greater than 0." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);

            using var userCommand = connection.CreateCommand();
            userCommand.CommandText = @"
                SELECT id, email, first_name, last_name, role, bank_iban, bank_account_name
                FROM users
                WHERE id = @userId";
            userCommand.Parameters.AddWithValue("@userId", userId);

            using var userReader = userCommand.ExecuteReader();
            if (!userReader.Read())
            {
                return Results.NotFound(new { message = "User not found." });
            }

            var profileUserId = userReader.GetInt64(0);
            var email = userReader.GetString(1);
            var firstName = userReader.IsDBNull(2) ? null : userReader.GetString(2);
            var lastName = userReader.IsDBNull(3) ? null : userReader.GetString(3);
            var role = userReader.GetInt32(4);
            var bankIban = userReader.IsDBNull(5) ? null : userReader.GetString(5);
            var bankAccountName = userReader.IsDBNull(6) ? null : userReader.GetString(6);

            var shippingAddresses = new List<ShippingAddressDto>();
            using (var addressCommand = connection.CreateCommand())
            {
                addressCommand.CommandText = @"
                    SELECT id, user_id, label, shipping_address, is_default
                    FROM user_shipping_addresses
                    WHERE user_id = @userId
                    ORDER BY is_default DESC, id ASC";
                addressCommand.Parameters.AddWithValue("@userId", userId);

                using var addressReader = addressCommand.ExecuteReader();
                while (addressReader.Read())
                {
                    shippingAddresses.Add(new ShippingAddressDto(
                        addressReader.GetInt64(0),
                        addressReader.GetInt64(1),
                        addressReader.IsDBNull(2) ? null : addressReader.GetString(2),
                        addressReader.GetString(3),
                        addressReader.GetInt32(4) == 1
                    ));
                }
            }

            var orders = new List<AdminUserOrderDto>();
            using (var ordersCommand = connection.CreateCommand())
            {
                ordersCommand.CommandText = @"
                    SELECT o.id, o.order_number, o.total_price, o.shipping_address, dc.code
                    FROM orders o
                    LEFT JOIN discount_codes dc ON dc.id = o.discount_code_id
                    WHERE o.user_id = @userId
                    ORDER BY o.id DESC";
                ordersCommand.Parameters.AddWithValue("@userId", userId);

                using var ordersReader = ordersCommand.ExecuteReader();
                while (ordersReader.Read())
                {
                    var orderId = ordersReader.GetInt64(0);

                    using var itemsCommand = connection.CreateCommand();
                    itemsCommand.CommandText = @"
                        SELECT oi.product_id, p.name, oi.quantity, oi.price
                        FROM order_items oi
                        INNER JOIN products p ON p.id = oi.product_id
                        WHERE oi.order_id = @orderId
                        ORDER BY oi.id";
                    itemsCommand.Parameters.AddWithValue("@orderId", orderId);

                    var items = new List<OrderItemDto>();
                    using var itemsReader = itemsCommand.ExecuteReader();
                    while (itemsReader.Read())
                    {
                        items.Add(new OrderItemDto(
                            itemsReader.GetInt64(0),
                            itemsReader.GetString(1),
                            itemsReader.GetInt32(2),
                            itemsReader.GetDouble(3)
                        ));
                    }

                    orders.Add(new AdminUserOrderDto(
                        orderId,
                        ordersReader.GetString(1),
                        ordersReader.GetDouble(2),
                        ordersReader.GetString(3),
                        ordersReader.IsDBNull(4) ? null : ordersReader.GetString(4),
                        items
                    ));
                }
            }

            return Results.Ok(new AdminUserProfileDto(
                profileUserId,
                email,
                firstName,
                lastName,
                role,
                bankIban,
                bankAccountName,
                shippingAddresses,
                orders
            ));
        }).RequireAuthorization("Admin");

        app.MapPost("/admin/discount-codes/random", (CreateRandomDiscountCodeRequest request, DbOptions db) =>
        {
            if (request.DiscountPercentage < 1 || request.DiscountPercentage > 90)
            {
                return Results.BadRequest(new { message = "Discount percentage must be between 1 and 90." });
            }

            if (request.MaxUses < 1)
            {
                return Results.BadRequest(new { message = "Max uses must be at least 1." });
            }

            if (!DateTime.TryParse(request.ValidUntil, out var validUntilDate))
            {
                return Results.BadRequest(new { message = "Valid until must be a valid date." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);

            var validUntil = validUntilDate.Date.ToString("yyyy-MM-dd");
            string code;
            while (true)
            {
                code = DiscountCodeGenerator.Create(10);

                using var exists = connection.CreateCommand();
                exists.CommandText = "SELECT 1 FROM discount_codes WHERE code = @code LIMIT 1";
                exists.Parameters.AddWithValue("@code", code);
                if (exists.ExecuteScalar() is null)
                {
                    break;
                }
            }

            using var insert = connection.CreateCommand();
            insert.CommandText = @"
                INSERT INTO discount_codes (code, discount_percentage, active, valid_until, max_uses, uses_count)
                VALUES (@code, @discountPercentage, 1, @validUntil, @maxUses, 0);
                SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("@code", code);
            insert.Parameters.AddWithValue("@discountPercentage", request.DiscountPercentage);
            insert.Parameters.AddWithValue("@validUntil", validUntil);
            insert.Parameters.AddWithValue("@maxUses", request.MaxUses);
            var id = Convert.ToInt64(insert.ExecuteScalar());

            return Results.Created($"/discount-codes/{id}", new DiscountCodeDto(
                id,
                code,
                request.DiscountPercentage,
                true,
                validUntil,
                request.MaxUses,
                0
            ));
        }).RequireAuthorization("Admin");

        return app;
    }
}
