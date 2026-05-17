using WebShop.Api.Models;
using WebShop.Contracts.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public static class AuthRoutes
{
    public static WebApplication MapAuthRoutes(this WebApplication app)
    {
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

            return Results.Ok(new AuthResponse(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2)
            ));
        });

        return app;
    }
}
