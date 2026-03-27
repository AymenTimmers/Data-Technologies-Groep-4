using System.Linq;
using WebShop.Api.Models;
using WebShop.Contracts.Models;

namespace WebShop.Api;

public static class CartAndOrderRoutes
{
    public static WebApplication MapCartAndOrderRoutes(this WebApplication app)
    {
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

            var shippingAddress = request.ShippingAddress?.Trim();
            if (request.ShippingAddressId is null && (string.IsNullOrWhiteSpace(shippingAddress) || shippingAddress.Length > 250))
            {
                return Results.BadRequest(new { message = "Shipping address is required and must be max 250 characters when no saved address is selected." });
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

            if (request.ShippingAddressId.HasValue)
            {
                using var addressCommand = connection.CreateCommand();
                addressCommand.CommandText = @"
                    SELECT shipping_address
                    FROM user_shipping_addresses
                    WHERE id = @addressId AND user_id = @userId";
                addressCommand.Parameters.AddWithValue("@addressId", request.ShippingAddressId.Value);
                addressCommand.Parameters.AddWithValue("@userId", request.UserId);
                var selectedAddress = addressCommand.ExecuteScalar() as string;
                if (string.IsNullOrWhiteSpace(selectedAddress))
                {
                    return Results.NotFound(new { message = "Selected shipping address was not found." });
                }

                shippingAddress = selectedAddress;
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
            var shouldIncrementDiscountUses = false;

            if (!string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                using var discountCommand = connection.CreateCommand();
                discountCommand.CommandText = @"
                                SELECT id, discount_percentage, max_uses, uses_count
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
                    var maxUses = reader.GetInt32(2);
                    var usesCount = reader.GetInt32(3);
                    if (usesCount >= maxUses)
                    {
                        return Results.BadRequest(new { message = "Discount code has reached its usage limit." });
                    }

                    shouldIncrementDiscountUses = true;
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
            orderCommand.Parameters.AddWithValue("@shippingAddress", shippingAddress!);
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

            if (shouldIncrementDiscountUses && discountCodeId.HasValue)
            {
                using var incrementDiscount = connection.CreateCommand();
                incrementDiscount.Transaction = transaction;
                incrementDiscount.CommandText = @"
                    UPDATE discount_codes
                    SET uses_count = uses_count + 1,
                        active = CASE WHEN uses_count + 1 >= max_uses THEN 0 ELSE active END
                    WHERE id = @id";
                incrementDiscount.Parameters.AddWithValue("@id", discountCodeId.Value);
                incrementDiscount.ExecuteNonQuery();
            }

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

        return app;
    }
}
