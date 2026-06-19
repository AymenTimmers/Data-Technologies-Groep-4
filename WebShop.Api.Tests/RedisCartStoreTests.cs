using System.Net;
using Moq;
using StackExchange.Redis;

namespace WebShop.Api.Tests;

public class RedisCartStoreTests
{
    private static RedisCartStore CreateStore(Mock<IDatabase>? database = null, Mock<IServer>? server = null)
    {
        var db = (database ?? new Mock<IDatabase>()).Object;
        var srv = (server ?? new Mock<IServer>()).Object;
        var connectionMultiplexer = new TestConnectionMultiplexer(db, srv);
        return new RedisCartStore(connectionMultiplexer);
    }

    private sealed class TestConnectionMultiplexer : IConnectionMultiplexer
    {
        private readonly IDatabase _database;
        private readonly IServer _server;
        private readonly EndPoint[] _endpoints;

        public TestConnectionMultiplexer(IDatabase database, IServer server)
        {
            _database = database;
            _server = server;
            _endpoints = new EndPoint[] { new DnsEndPoint("localhost", 6379) };
        }

        public string ClientName => throw new NotImplementedException();
        public string Configuration => throw new NotImplementedException();
        public int TimeoutMilliseconds => throw new NotImplementedException();
        public long OperationCount => throw new NotImplementedException();
        public bool PreserveAsyncOrder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsConnected => throw new NotImplementedException();
        public bool IsConnecting => throw new NotImplementedException();
        public bool IncludeDetailInExceptions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int StormLogThreshold { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<RedisErrorEventArgs>? ErrorMessage;
        public event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;
        public event EventHandler<InternalErrorEventArgs>? InternalError;
        public event EventHandler<ConnectionFailedEventArgs>? ConnectionRestored;
        public event EventHandler<EndPointEventArgs>? ConfigurationChanged;
        public event EventHandler<EndPointEventArgs>? ConfigurationChangedBroadcast;
        public event EventHandler<StackExchange.Redis.Maintenance.ServerMaintenanceEvent>? ServerMaintenanceEvent;
        public event EventHandler<HashSlotMovedEventArgs>? HashSlotMoved;

        public void RegisterProfiler(Func<StackExchange.Redis.Profiling.ProfilingSession?> profiler) => throw new NotImplementedException();
        public ServerCounters GetCounters() => throw new NotImplementedException();
        public EndPoint[] GetEndPoints(bool configuredOnly = false) => _endpoints;
        public void Wait(Task task) => throw new NotImplementedException();
        public T Wait<T>(Task<T> task) => throw new NotImplementedException();
        public void WaitAll(Task[] tasks) => throw new NotImplementedException();
        public int HashSlot(RedisKey key) => throw new NotImplementedException();
        public ISubscriber GetSubscriber(object? asyncState = null) => throw new NotImplementedException();
        public IDatabase GetDatabase(int db = -1, object? asyncState = null) => _database;
        public IServer GetServer(string host, int port, object? asyncState = null) => _server;
        public IServer GetServer(string host, object? asyncState = null) => _server;
        public IServer GetServer(System.Net.IPAddress host, int port) => _server!;
        public IServer GetServer(EndPoint endpoint, object? asyncState = null) => _server;
        public IServer GetServer(RedisKey key, object? asyncState = null, CommandFlags flags = CommandFlags.None) => _server;
        public IServer[] GetServers() => new[] { _server };
        public Task<bool> ConfigureAsync(System.IO.TextWriter? writer) => throw new NotImplementedException();
        public bool Configure(System.IO.TextWriter? writer) => throw new NotImplementedException();
        public string GetStatus() => throw new NotImplementedException();
        public void GetStatus(System.IO.TextWriter? writer) => throw new NotImplementedException();
        public void Close(bool allowCommandsToComplete = true) => throw new NotImplementedException();
        public Task CloseAsync(bool allowCommandsToComplete = true) => throw new NotImplementedException();
        public string GetStormLog() => throw new NotImplementedException();
        public void ResetStormLog() => throw new NotImplementedException();
        public long PublishReconfigure(CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public int GetHashSlot(RedisKey key) => throw new NotImplementedException();
        public void ExportConfiguration(System.IO.Stream destination, ExportOptions options = ExportOptions.All) => throw new NotImplementedException();
        public void AddLibraryNameSuffix(string suffix) => throw new NotImplementedException();
        public ValueTask DisposeAsync() => new ValueTask();
        public void Dispose() { }
        string IConnectionMultiplexer.ToString() => base.ToString();
    }

    [Fact]
    public void RedisCartStore_ImplementsICartStore()
    {
        var store = CreateStore();

        Assert.IsAssignableFrom<ICartStore>(store);
    }

    [Fact]
    public async Task GetCartAsync_ReturnsParsedEntriesOnly()
    {
        var entries = new HashEntry[]
        {
            new HashEntry("10", "2"),
            new HashEntry("bad", "3"),
            new HashEntry("20", "bad")
        };

        var database = new Mock<IDatabase>();
        database.Setup(d => d.HashGetAllAsync("cart:user:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(entries);

        var store = CreateStore(database: database);
        var cart = await store.GetCartAsync(1);

        Assert.Single(cart);
        Assert.Equal(2, cart[10]);
    }

    [Fact]
    public async Task AddItemAsync_IncrementsHashAndSetsExpiry()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.HashIncrementAsync("cart:user:1", "5", 2, It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);
        database.Setup(d => d.KeyExpireAsync("cart:user:1", TimeSpan.FromHours(24), ExpireWhen.Always, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var store = CreateStore(database: database);
        await store.AddItemAsync(1, 5, 2);

        database.Verify(d => d.HashIncrementAsync("cart:user:1", "5", 2, It.IsAny<CommandFlags>()), Times.Once);
        database.Verify(d => d.KeyExpireAsync("cart:user:1", TimeSpan.FromHours(24), ExpireWhen.Always, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task DecrementItemAsync_RemovesFieldWhenQuantityIsOne()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.HashGetAsync("cart:user:1", "5", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)1);
        database.Setup(d => d.HashDeleteAsync("cart:user:1", "5", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        database.Setup(d => d.KeyExistsAsync("cart:user:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var store = CreateStore(database: database);
        await store.DecrementItemAsync(1, 5);

        database.Verify(d => d.HashDeleteAsync("cart:user:1", "5", It.IsAny<CommandFlags>()), Times.Once);
        database.Verify(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task DecrementItemAsync_DecrementsQuantityAndUpdatesExpiry()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.HashGetAsync("cart:user:1", "5", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)3);
        database.Setup(d => d.HashIncrementAsync("cart:user:1", "5", -1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);
        database.Setup(d => d.KeyExistsAsync("cart:user:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        database.Setup(d => d.KeyExpireAsync("cart:user:1", TimeSpan.FromHours(24), ExpireWhen.Always, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var store = CreateStore(database: database);
        await store.DecrementItemAsync(1, 5);

        database.Verify(d => d.HashIncrementAsync("cart:user:1", "5", -1, It.IsAny<CommandFlags>()), Times.Once);
        database.Verify(d => d.KeyExpireAsync("cart:user:1", TimeSpan.FromHours(24), ExpireWhen.Always, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_DeletesFieldAndUpdatesExpiryIfKeyStillExists()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.HashDeleteAsync("cart:user:1", "5", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        database.Setup(d => d.KeyExistsAsync("cart:user:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        database.Setup(d => d.KeyExpireAsync("cart:user:1", TimeSpan.FromHours(24), ExpireWhen.Always, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var store = CreateStore(database: database);
        await store.RemoveItemAsync(1, 5);

        database.Verify(d => d.HashDeleteAsync("cart:user:1", "5", It.IsAny<CommandFlags>()), Times.Once);
        database.Verify(d => d.KeyExpireAsync("cart:user:1", TimeSpan.FromHours(24), ExpireWhen.Always, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ClearCartAsync_DeletesCartKey()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.KeyDeleteAsync("cart:user:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var store = CreateStore(database: database);
        await store.ClearCartAsync(1);

        database.Verify(d => d.KeyDeleteAsync("cart:user:1", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetCartTimeToLiveAsync_ForwardsTimeToLiveCall()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.KeyTimeToLiveAsync("cart:user:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromHours(24));

        var store = CreateStore(database: database);
        var ttl = await store.GetCartTimeToLiveAsync(1);

        Assert.Equal(TimeSpan.FromHours(24), ttl);
    }

    [Fact]
    public async Task GetReservedQuantityAsync_SumsQuantitiesAcrossAllCartsExcludingUser()
    {
        var database = new Mock<IDatabase>();
        database.Setup(d => d.HashGetAsync("cart:user:1", "5", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)2);
        database.Setup(d => d.HashGetAsync("cart:user:2", "5", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)3);

        var server = new Mock<IServer>();
        server.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(new[] { (RedisKey)"cart:user:1", (RedisKey)"cart:user:2" });

        var store = CreateStore(database: database, server: server);
        var reserved = await store.GetReservedQuantityAsync(5, excludingUserId: 1);

        Assert.Equal(3, reserved);
    }
}
