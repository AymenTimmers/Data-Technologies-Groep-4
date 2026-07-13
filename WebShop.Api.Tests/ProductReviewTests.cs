using MongoDB.Driver;
using Moq;
using WebShop.Api.Helpers;

namespace WebShop.Api.Tests;

public class ProductReviewTests
{
    private static MongoReviewService CreateService(
        Mock<IMongoCollection<ProductReviewDocument>>? collection = null)
    {
        var mockCollection = collection ?? new Mock<IMongoCollection<ProductReviewDocument>>();

        var mockIndexManager = new Mock<IMongoIndexManager<ProductReviewDocument>>();
        mockIndexManager
            .Setup(i => i.CreateOne(
                It.IsAny<CreateIndexModel<ProductReviewDocument>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns("index_name");

        mockCollection.Setup(c => c.Indexes).Returns(mockIndexManager.Object);

        var mockDb = new Mock<IMongoDatabase>();
        mockDb
            .Setup(d => d.GetCollection<ProductReviewDocument>("product_reviews", It.IsAny<MongoCollectionSettings>()))
            .Returns(mockCollection.Object);

        var mockClient = new Mock<IMongoClient>();
        mockClient
            .Setup(c => c.GetDatabase("webshop", It.IsAny<MongoDatabaseSettings>()))
            .Returns(mockDb.Object);

        return new MongoReviewService(mockClient.Object);
    }

    private static Mock<IMongoCollection<ProductReviewDocument>> CollectionWithDocuments(
        List<ProductReviewDocument> documents)
    {
        var mockCursor = new Mock<IAsyncCursor<ProductReviewDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(documents);

        var mockCollection = new Mock<IMongoCollection<ProductReviewDocument>>();
        mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ProductReviewDocument>>(),
                It.IsAny<FindOptions<ProductReviewDocument, ProductReviewDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        return mockCollection;
    }

    [Fact]
    public async Task GetReviewsForProductAsync_NoReviews_ReturnsEmptyList()
    {
        var mockCollection = CollectionWithDocuments([]);
        var service = CreateService(mockCollection);

        var result = await service.GetReviewsForProductAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReviewsForProductAsync_WithReviews_MapsFieldsCorrectly()
    {
        var createdAt = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var documents = new List<ProductReviewDocument>
        {
            new()
            {
                ProductId = 1,
                UserId = 42,
                Email = "user@example.com",
                Rating = 5,
                Explanation = "Great product!",
                CreatedAt = createdAt
            }
        };

        var mockCollection = CollectionWithDocuments(documents);
        var service = CreateService(mockCollection);

        var result = await service.GetReviewsForProductAsync(1);

        Assert.Single(result);
        var review = result[0];
        Assert.Equal(1, review.ProductId);
        Assert.Equal(42, review.UserId);
        Assert.Equal("user@example.com", review.UserEmail);
        Assert.Equal(5, review.Stars);
        Assert.Equal("Great product!", review.Explanation);
        Assert.Equal(createdAt.ToString("o"), review.CreatedAtUtc);
    }

    [Fact]
    public async Task GetReviewsForProductAsync_NullExplanation_MapsToEmptyString()
    {
        var documents = new List<ProductReviewDocument>
        {
            new()
            {
                ProductId = 2,
                UserId = 7,
                Email = "a@b.com",
                Rating = 3,
                Explanation = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        var mockCollection = CollectionWithDocuments(documents);
        var service = CreateService(mockCollection);

        var result = await service.GetReviewsForProductAsync(2);

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Explanation);
    }

    [Fact]
    public async Task GetReviewsForProductAsync_MultipleReviews_ReturnsAll()
    {
        var documents = new List<ProductReviewDocument>
        {
            new() { ProductId = 3, UserId = 1, Email = "a@a.com", Rating = 5, Explanation = "A", CreatedAt = DateTime.UtcNow },
            new() { ProductId = 3, UserId = 2, Email = "b@b.com", Rating = 4, Explanation = "B", CreatedAt = DateTime.UtcNow },
            new() { ProductId = 3, UserId = 3, Email = "c@c.com", Rating = 3, Explanation = "C", CreatedAt = DateTime.UtcNow },
        };

        var mockCollection = CollectionWithDocuments(documents);
        var service = CreateService(mockCollection);

        var result = await service.GetReviewsForProductAsync(3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task UpsertReviewAsync_CallsUpdateOneWithUpsert()
    {
        var mockCollection = new Mock<IMongoCollection<ProductReviewDocument>>();
        mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ProductReviewDocument>>(),
                It.IsAny<UpdateDefinition<ProductReviewDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var service = CreateService(mockCollection);

        await service.UpsertReviewAsync(1, 42, "user@example.com", 5, "Excellent!");

        mockCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<ProductReviewDocument>>(),
            It.IsAny<UpdateDefinition<ProductReviewDocument>>(),
            It.Is<UpdateOptions>(o => o.IsUpsert == true),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpsertReviewAsync_NullExplanation_DoesNotThrow()
    {
        var mockCollection = new Mock<IMongoCollection<ProductReviewDocument>>();
        mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ProductReviewDocument>>(),
                It.IsAny<UpdateDefinition<ProductReviewDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var service = CreateService(mockCollection);

        var ex = await Record.ExceptionAsync(() =>
            service.UpsertReviewAsync(10, 5, "test@test.com", 4, null));

        Assert.Null(ex);
    }
}
