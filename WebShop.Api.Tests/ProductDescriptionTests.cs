using MongoDB.Driver;
using Moq;
using WebShop.Api.Helpers;

namespace WebShop.Api.Tests;

public class ProductDescriptionTests
{
    private static MongoProductService CreateService(
        Mock<IMongoCollection<ProductDescriptionDocument>>? collection = null)
    {
        var mockCollection = collection ?? new Mock<IMongoCollection<ProductDescriptionDocument>>();

        var mockIndexManager = new Mock<IMongoIndexManager<ProductDescriptionDocument>>();
        mockIndexManager
            .Setup(i => i.CreateOne(
                It.IsAny<CreateIndexModel<ProductDescriptionDocument>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns("index_name");

        mockCollection.Setup(c => c.Indexes).Returns(mockIndexManager.Object);

        var mockDb = new Mock<IMongoDatabase>();
        mockDb
            .Setup(d => d.GetCollection<ProductDescriptionDocument>("product_descriptions", It.IsAny<MongoCollectionSettings>()))
            .Returns(mockCollection.Object);

        var mockClient = new Mock<IMongoClient>();
        mockClient
            .Setup(c => c.GetDatabase("webshop", It.IsAny<MongoDatabaseSettings>()))
            .Returns(mockDb.Object);

        return new MongoProductService(mockClient.Object);
    }

    private static Mock<IMongoCollection<ProductDescriptionDocument>> CollectionWithDocuments(
        List<ProductDescriptionDocument> documents)
    {
        var mockCursor = new Mock<IAsyncCursor<ProductDescriptionDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(documents);

        var mockCollection = new Mock<IMongoCollection<ProductDescriptionDocument>>();
        mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ProductDescriptionDocument>>(),
                It.IsAny<FindOptions<ProductDescriptionDocument, ProductDescriptionDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        return mockCollection;
    }

    [Fact]
    public async Task GetDescriptionAsync_NoDocument_ReturnsNull()
    {
        var mockCollection = CollectionWithDocuments([]);
        var service = CreateService(mockCollection);

        var result = await service.GetDescriptionAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDescriptionAsync_WithDocument_ReturnsDescription()
    {
        var documents = new List<ProductDescriptionDocument>
        {
            new() { ProductId = 1, Description = "A great product" }
        };

        var mockCollection = CollectionWithDocuments(documents);
        var service = CreateService(mockCollection);

        var result = await service.GetDescriptionAsync(1);

        Assert.Equal("A great product", result);
    }

    [Fact]
    public async Task GetDescriptionsAsync_EmptyIds_ReturnsEmptyDictionary()
    {
        var service = CreateService();

        var result = await service.GetDescriptionsAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDescriptionsAsync_WithDocuments_MapsByProductId()
    {
        var documents = new List<ProductDescriptionDocument>
        {
            new() { ProductId = 1, Description = "First" },
            new() { ProductId = 2, Description = "Second" }
        };

        var mockCollection = CollectionWithDocuments(documents);
        var service = CreateService(mockCollection);

        var result = await service.GetDescriptionsAsync([1, 2]);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[1]);
        Assert.Equal("Second", result[2]);
    }

    [Fact]
    public async Task UpsertDescriptionAsync_CallsUpdateOneWithUpsert()
    {
        var mockCollection = new Mock<IMongoCollection<ProductDescriptionDocument>>();
        mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ProductDescriptionDocument>>(),
                It.IsAny<UpdateDefinition<ProductDescriptionDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var service = CreateService(mockCollection);

        await service.UpsertDescriptionAsync(1, "Updated description");

        mockCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<ProductDescriptionDocument>>(),
            It.IsAny<UpdateDefinition<ProductDescriptionDocument>>(),
            It.Is<UpdateOptions>(o => o.IsUpsert == true),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpsertDescriptionAsync_NullDescription_DoesNotThrow()
    {
        var mockCollection = new Mock<IMongoCollection<ProductDescriptionDocument>>();
        mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ProductDescriptionDocument>>(),
                It.IsAny<UpdateDefinition<ProductDescriptionDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var service = CreateService(mockCollection);

        var ex = await Record.ExceptionAsync(() => service.UpsertDescriptionAsync(1, null));

        Assert.Null(ex);
    }
}
