using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace WebShop.Api.Helpers;

public class ProductDescriptionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("productId")]
    public long ProductId { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }
}

public class MongoProductService
{
    private readonly IMongoCollection<ProductDescriptionDocument> _descriptions;

    public MongoProductService(IMongoClient mongoClient)
    {
        var db = mongoClient.GetDatabase("webshop");
        _descriptions = db.GetCollection<ProductDescriptionDocument>("product_descriptions");

        var indexKeys = Builders<ProductDescriptionDocument>.IndexKeys.Ascending(d => d.ProductId);

        _descriptions.Indexes.CreateOne(
            new CreateIndexModel<ProductDescriptionDocument>(
                indexKeys,
                new CreateIndexOptions { Unique = true }
            )
        );
    }

    public async Task<string?> GetDescriptionAsync(long productId)
    {
        var filter = Builders<ProductDescriptionDocument>.Filter.Eq(d => d.ProductId, productId);
        var doc = await _descriptions.Find(filter).FirstOrDefaultAsync();
        return doc?.Description;
    }

    public async Task<Dictionary<long, string?>> GetDescriptionsAsync(IEnumerable<long> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, string?>();

        var filter = Builders<ProductDescriptionDocument>.Filter.In(d => d.ProductId, ids);
        var docs = await _descriptions.Find(filter).ToListAsync();

        return docs.ToDictionary(d => d.ProductId, d => d.Description);
    }

    public async Task<List<long>> SearchProductIdsByDescriptionAsync(string searchTerm)
    {
        var pattern = System.Text.RegularExpressions.Regex.Escape(searchTerm);
        var filter = Builders<ProductDescriptionDocument>.Filter.Regex(
            d => d.Description,
            new BsonRegularExpression(pattern, "i")
        );

        var docs = await _descriptions.Find(filter).ToListAsync();
        return docs.Select(d => d.ProductId).ToList();
    }

    public async Task UpsertDescriptionAsync(long productId, string? description)
    {
        var filter = Builders<ProductDescriptionDocument>.Filter.Eq(d => d.ProductId, productId);
        var update = Builders<ProductDescriptionDocument>.Update
            .Set(d => d.Description, description)
            .SetOnInsert(d => d.ProductId, productId);

        await _descriptions.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }
}
