using MongoDB.Driver;

namespace WebShop.Api.Helpers;

public class MongoRequestLogger
{
    private readonly IMongoCollection<MongoRequestLog> _collection;

    public MongoRequestLogger(IMongoClient mongoClient, string databaseName)
    {
        var db = mongoClient.GetDatabase(databaseName);
        _collection = db.GetCollection<MongoRequestLog>("request_logs");

        var indexModel = new CreateIndexModel<MongoRequestLog>(
            Builders<MongoRequestLog>.IndexKeys.Descending(x => x.TimestampUtc));
        _collection.Indexes.CreateOne(indexModel);
    }

    public void Append(string method, string path, int statusCode, long elapsedMs, string? errorMessage)
    {
        var doc = new MongoRequestLog
        {
            TimestampUtc = DateTime.UtcNow,
            Method = method,
            Path = path,
            StatusCode = statusCode,
            ElapsedMs = elapsedMs,
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage
        };

        _ = _collection.InsertOneAsync(doc).ContinueWith(
            t => Console.Error.WriteLine($"[MongoRequestLogger] Failed to write log: {t.Exception?.GetBaseException().Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public async Task<List<MongoRequestLog>> GetRecentAsync(int limit = 100)
    {
        return await _collection
            .Find(Builders<MongoRequestLog>.Filter.Empty)
            .SortByDescending(x => x.TimestampUtc)
            .Limit(limit)
            .ToListAsync();
    }
}
