using WebShop.Api.Models;
using WebShop.Contracts.Models;
using WebShop.Api.Helpers;

namespace WebShop.Api.Routes;

public static class AuthRoutes
{
    public static WebApplication MapAuthRoutes(this WebApplication app)
    {
        app.MapPost("/auth/register", (RegisterRequest request, DbOptions db, JwtService jwtService) =>
        {
            // Validate email
            var (isValidEmail, emailError) = InputValidator.ValidateEmail(request.Email);
            if (!isValidEmail)
            {
                return Results.BadRequest(new { message = emailError });
            }

            // Validate password with strong requirements
            var (isValidPassword, passwordError) = InputValidator.ValidatePassword(request.Password);
            if (!isValidPassword)
            {
                return Results.BadRequest(new { message = passwordError });
            }

            // Validate names
            var (isValidFirstName, firstNameError) = InputValidator.ValidateName(request.FirstName, "First name");
            if (!isValidFirstName)
            {
                return Results.BadRequest(new { message = firstNameError });
            }

            var (isValidLastName, lastNameError) = InputValidator.ValidateName(request.LastName, "Last name");
            if (!isValidLastName)
            {
                return Results.BadRequest(new { message = lastNameError });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
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

            var userId = (int)Convert.ToInt64(userCommand.ExecuteScalar());

            transaction.Commit();

            // Generate JWT token for new user
            var token = jwtService.GenerateToken(userId, normalizedEmail, "user");

            return Results.Created($"/users/{userId}", new AuthResponse(userId, normalizedEmail, 0, token));
        });

        app.MapPost("/auth/login", (LoginRequest request, DbOptions db, JwtService jwtService) =>
        {
            if (!Input.TryNormalizeEmail(request.Email, out var normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "A valid email and password are required." });
            }

            using var connection = Db.CreateOpenConnection(db.DatabasePath);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, email, password_hash, role
                FROM users
                WHERE email = @email";
            command.Parameters.AddWithValue("@email", normalizedEmail);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return Results.Unauthorized();
            }

            var userId = (int)reader.GetInt64(0);
            var email = reader.GetString(1);
            var storedHash = reader.GetString(2);
            var role = reader.GetInt32(3);

            if (!PasswordService.VerifyPassword(request.Password, storedHash))
            {
                return Results.Unauthorized();
            }

            // Generate JWT token
            var token = jwtService.GenerateToken(userId, email, role == 1 ? "admin" : "user");

            return Results.Ok(new AuthResponse(userId, email, role, token));
        });

        return app;
    }
}
