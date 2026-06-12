using System.Diagnostics;
using WebShop.Api.Helpers;
using WebShop.Api.Models;
using WebShop.Api.Routes;

using MongoDB.Driver;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(configuredUrls) ? "http://0.0.0.0:5088" : configuredUrls);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var databaseFolder = Path.Combine(builder.Environment.ContentRootPath, "Database");
Directory.CreateDirectory(databaseFolder);
var databasePath = Path.Combine(databaseFolder, "webshop.db");

DbBootstrapper.EnsureCreated(databasePath, databaseFolder);

builder.Services.AddSingleton(new DbOptions(databasePath));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(redisConnection);
});

builder.Services.AddSingleton<ICartStore, RedisCartStore>();

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "webshop";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp =>
    new MongoRequestLogger(sp.GetRequiredService<IMongoClient>(), mongoDatabaseName));

var app = builder.Build();
app.UseCors();

var logFolder = Path.Combine(builder.Environment.ContentRootPath, "Logs");
Directory.CreateDirectory(logFolder);
var requestLogPath = Path.Combine(logFolder, "requests.log");
var documentationFolder = Path.Combine(builder.Environment.ContentRootPath, "Documentation");
Directory.CreateDirectory(documentationFolder);

// Initialize recommendations cache
ProductRecommendationCache.RefreshIfNeeded(databasePath, forceRefresh: true);

var mongoLogger = app.Services.GetRequiredService<MongoRequestLogger>();

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
        var fullPath = $"{context.Request.Path}{context.Request.QueryString}";
        RequestFileLogger.Append(requestLogPath, context.Request.Method, fullPath, context.Response.StatusCode, start.ElapsedMilliseconds, errorMessage);
        mongoLogger.Append(context.Request.Method, context.Request.Path.ToString(), context.Response.StatusCode, start.ElapsedMilliseconds, errorMessage);
    }
});

app.MapAuthRoutes();
app.MapUserRoutes();
app.MapAdminRoutes();
app.MapCatalogRoutes();
app.MapCartAndOrderRoutes();
app.MapSystemRoutes(documentationFolder);
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
