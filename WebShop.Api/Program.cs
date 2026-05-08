using System.Diagnostics;
using WebShop.Api.Helpers;
using WebShop.Api.Models;
using WebShop.Api.Routes;

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

var app = builder.Build();
app.UseCors();

var logFolder = Path.Combine(builder.Environment.ContentRootPath, "Logs");
Directory.CreateDirectory(logFolder);
var requestLogPath = Path.Combine(logFolder, "requests.log");
var documentationFolder = Path.Combine(builder.Environment.ContentRootPath, "Documentation");
Directory.CreateDirectory(documentationFolder);

// Initialize recommendations cache
ProductRecommendationCache.RefreshIfNeeded(databasePath, forceRefresh: true);

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
app.MapCatalogRoutes();
app.MapCartAndOrderRoutes();
app.MapSystemRoutes(documentationFolder);
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
