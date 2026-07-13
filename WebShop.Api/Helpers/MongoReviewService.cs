using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WebShop.Contracts.Models;

namespace WebShop.Api.Helpers;

public class ProductReviewDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("productId")]
    public long ProductId { get; set; }

    [BsonElement("userId")]
    public long UserId { get; set; }

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; }

    [BsonElement("explanation")]
    public string? Explanation { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class MongoReviewService
{
    private readonly IMongoCollection<ProductReviewDocument> _reviews;

    public MongoReviewService(IMongoClient mongoClient)
    {
        var db = mongoClient.GetDatabase("webshop");
        _reviews = db.GetCollection<ProductReviewDocument>("product_reviews");

        var indexKeys = Builders<ProductReviewDocument>.IndexKeys
            .Ascending(r => r.ProductId)
            .Ascending(r => r.UserId);

        _reviews.Indexes.CreateOne(
            new CreateIndexModel<ProductReviewDocument>(
                indexKeys,
                new CreateIndexOptions { Unique = true }
            )
        );
    }

    public async Task<List<ProductReviewDto>> GetReviewsForProductAsync(long productId)
    {
        var filter = Builders<ProductReviewDocument>.Filter.Eq(r => r.ProductId, productId);
        var sort = Builders<ProductReviewDocument>.Sort.Descending(r => r.CreatedAt);
        var options = new FindOptions<ProductReviewDocument> { Sort = sort };

        using var cursor = await _reviews.FindAsync(filter, options);
        var docs = await cursor.ToListAsync();

        return docs.Select(d => new ProductReviewDto(
            0,
            d.ProductId,
            d.UserId,
            d.Email,
            d.Rating,
            d.Explanation ?? string.Empty,
            d.CreatedAt.ToString("o")
        )).ToList();
    }

    public async Task UpsertReviewAsync(long productId, long userId, string email, int rating, string? explanation)
    {
        var filter = Builders<ProductReviewDocument>.Filter.And(
            Builders<ProductReviewDocument>.Filter.Eq(r => r.ProductId, productId),
            Builders<ProductReviewDocument>.Filter.Eq(r => r.UserId, userId)
        );

        var update = Builders<ProductReviewDocument>.Update
            .Set(r => r.Email, email)
            .Set(r => r.Rating, rating)
            .Set(r => r.Explanation, explanation)
            .Set(r => r.CreatedAt, DateTime.UtcNow)
            .SetOnInsert(r => r.ProductId, productId)
            .SetOnInsert(r => r.UserId, userId);

        await _reviews.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }
}
