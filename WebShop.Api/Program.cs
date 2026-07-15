using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebShop.Api.Helpers;
using WebShop.Api.Models;
using WebShop.Api.Routes;
using StackExchange.Redis;
using Neo4j.Driver;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(configuredUrls) ? "http://0.0.0.0:5088" : configuredUrls);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"];
        if (string.IsNullOrWhiteSpace(allowedOrigins))
        {
            allowedOrigins = "http://localhost;https://localhost;http://127.0.0.1;https://127.0.0.1";
        }

        var origins = allowedOrigins
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var databaseFolder = Path.Combine(builder.Environment.ContentRootPath, "Database");
Directory.CreateDirectory(databaseFolder);
var databasePath = Path.Combine(databaseFolder, "webshop.db");

DbBootstrapper.EnsureCreated(databasePath, databaseFolder);

builder.Services.AddSingleton(new DbOptions(databasePath));

// Initialize encryption service
var encryptionKeyBase64 = builder.Configuration["ENCRYPTION_KEY"];

if (string.IsNullOrEmpty(encryptionKeyBase64))
{
    throw new InvalidOperationException(
        "ENCRYPTION_KEY environment variable must be set. " +
        "Generate one with: EncryptionService.GenerateEncryptionKey()");
}
builder.Services.AddSingleton(new EncryptionService(encryptionKeyBase64));

// Initialize backup service
var backupDirectory = Path.Combine(databaseFolder, "backups");
builder.Services.AddSingleton(new BackupService(databasePath, backupDirectory));

// Initialize JWT service
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"];
if (string.IsNullOrEmpty(jwtSecretKey) || jwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET_KEY environment variable must be set with at least 32 characters. " +
        "Generate one with: JwtService.GenerateSecretKey()");
}
builder.Services.AddSingleton(new JwtService(jwtSecretKey));

// Configure JWT authentication
var key = Encoding.UTF8.GetBytes(jwtSecretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = "webshop",
        ValidateAudience = true,
        ValidAudience = "webshop-api",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnection =
        builder.Configuration.GetConnectionString("Redis")
        ?? "redis:6379";

    try
    {
        var mux = ConnectionMultiplexer.Connect(redisConnection);
        Console.WriteLine($"Redis connected: {redisConnection}");
        return mux;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis connection failed: {ex}");
        throw;
    }
});

builder.Services.AddSingleton<IDriver>(_ =>
{
    try
    {
        var uri = builder.Configuration.GetConnectionString("Neo4j" ?? "bolt://neo4j:7687");
        var user = Environment.GetEnvironmentVariable("NEO4J_USERNAME");
        var password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD");
        Console.WriteLine($"Neo4j URI: {uri}");
        Console.WriteLine($"Neo4j User: {user}");
        Console.WriteLine($"Neo4j Password set: {!string.IsNullOrEmpty(password)}");
        
        return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    } catch (Exception ex)
    {
        Console.WriteLine($"Neo4j connection failed: {ex}");
        throw;
    }
});

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var mongoConnection =
        builder.Configuration.GetConnectionString("MongoDb")
        ?? "mongodb://localhost:27017";

    try
    {
        var client = new MongoClient(mongoConnection);
        Console.WriteLine($"MongoDB connected: {mongoConnection}");
        return client;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MongoDB connection failed: {ex}");
        throw;
    }
});

builder.Services.AddSingleton<MongoReviewService>();
builder.Services.AddSingleton<MongoProductService>();
builder.Services.AddSingleton<ProductRecommendationCache>();
builder.Services.AddSingleton<SystemRoutes>();
builder.Services.AddSingleton<CatalogRoutes>();

builder.Services.AddSingleton<ICartStore, RedisCartStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await MongoMigrations.BackfillProductDescriptionsAsync(
    databasePath,
    app.Services.GetRequiredService<MongoProductService>());

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

var logFolder = Path.Combine(builder.Environment.ContentRootPath, "Logs");
Directory.CreateDirectory(logFolder);
var requestLogPath = Path.Combine(logFolder, "requests.log");
var documentationFolder = Path.Combine(builder.Environment.ContentRootPath, "Documentation");
Directory.CreateDirectory(documentationFolder);

app.Use(async (context, next) =>
{
    var start = Stopwatch.StartNew();
    string? errorMessage = null;

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        throw;
    }
    finally
    {
        start.Stop();
        RequestFileLogger.Append(
            requestLogPath,
            context.Request.Method,
            $"{context.Request.Path}{context.Request.QueryString}",
            context.Response.StatusCode,
            start.ElapsedMilliseconds,
            errorMessage
        );
    }
});

app.MapAuthRoutes();
app.MapUserRoutes();
app.MapAdminRoutes();
app.Services.GetRequiredService<CatalogRoutes>()
    .MapCatalogRoutes(app);
app.MapCartAndOrderRoutes();
app.Services.GetRequiredService<SystemRoutes>()
    .MapSystemRoutes(app, documentationFolder);
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
