using System.Diagnostics;
using WebShop.Api.Helpers;
using WebShop.Api.Models;
using WebShop.Api.Routes;
using StackExchange.Redis;
using Neo4j.Driver;

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

        return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    } catch (Exception ex)
    {
        Console.WriteLine($"Neo4j connection failed: {ex}");
        throw;
    }
});

builder.Services.AddSingleton<ProductRecommendationCache>();
builder.Services.AddSingleton<SystemRoutes>();
builder.Services.AddSingleton<CatalogRoutes>();

builder.Services.AddSingleton<ICartStore, RedisCartStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

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
