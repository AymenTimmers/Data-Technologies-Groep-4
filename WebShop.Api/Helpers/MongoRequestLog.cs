using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebShop.Api.Helpers;

public class MongoRequestLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public DateTime TimestampUtc { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long ElapsedMs { get; set; }
    public string? ErrorMessage { get; set; }
}
