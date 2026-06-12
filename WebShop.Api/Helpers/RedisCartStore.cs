using StackExchange.Redis;

public class RedisCartStore : ICartStore
{
    private const string CartKeyPrefix = "cart:user:";

    private readonly IDatabase _database;
    private readonly IServer _server;

    public RedisCartStore(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
    }

    public async Task<IReadOnlyDictionary<long, int>> GetCartAsync(long userId)
    {
        var entries = await _database.HashGetAllAsync(GetCartKey(userId));
        var cart = new Dictionary<long, int>();

        foreach (var entry in entries)
        {
            if (long.TryParse(entry.Name.ToString(), out var productId) &&
                int.TryParse(entry.Value.ToString(), out var quantity))
            {
                cart[productId] = quantity;
            }
        }

        return cart;
    }

    public async Task AddItemAsync(long userId, long productId, int quantity)
    {
        var key = GetCartKey(userId);

        await _database.HashIncrementAsync(key, productId.ToString(), quantity);
        await _database.KeyExpireAsync(key, GetCartTimeToLive());
    }

    public async Task DecrementItemAsync(long userId, long productId)
    {
        var key = GetCartKey(userId);
        var field = productId.ToString();
        var currentValue = await _database.HashGetAsync(key, field);

        if (!currentValue.HasValue || !int.TryParse(currentValue.ToString(), out var currentQuantity))
        {
            return;
        }

        if (currentQuantity <= 1)
        {
            await _database.HashDeleteAsync(key, field);
        }
        else
        {
            await _database.HashIncrementAsync(key, field, -1);
        }

        if (await _database.KeyExistsAsync(key))
        {
            await _database.KeyExpireAsync(key, GetCartTimeToLive());
        }
    }

    public async Task RemoveItemAsync(long userId, long productId)
    {
        var key = GetCartKey(userId);

        await _database.HashDeleteAsync(key, productId.ToString());

        if (await _database.KeyExistsAsync(key))
        {
            await _database.KeyExpireAsync(key, GetCartTimeToLive());
        }
    }

    public Task ClearCartAsync(long userId)
    {
        return _database.KeyDeleteAsync(GetCartKey(userId));
    }

    public Task<TimeSpan?> GetCartTimeToLiveAsync(long userId)
    {
        return _database.KeyTimeToLiveAsync(GetCartKey(userId));
    }

    public async Task<int> GetReservedQuantityAsync(long productId, long? excludingUserId = null)
    {
        var totalReserved = 0;
        var productField = productId.ToString();

        foreach (var key in _server.Keys(pattern: $"{CartKeyPrefix}*"))
        {
            var userId = TryGetUserIdFromCartKey(key);

            if (excludingUserId.HasValue && userId == excludingUserId.Value)
            {
                continue;
            }

            var quantityValue = await _database.HashGetAsync(key, productField);
            if (quantityValue.HasValue && int.TryParse(quantityValue.ToString(), out var quantity))
            {
                totalReserved += quantity;
            }
        }

        return totalReserved;
    }

    private static string GetCartKey(long userId) => $"{CartKeyPrefix}{userId}";

    private static long? TryGetUserIdFromCartKey(RedisKey key)
    {
        var keyText = key.ToString();

        if (!keyText.StartsWith(CartKeyPrefix))
        {
            return null;
        }

        return long.TryParse(keyText[CartKeyPrefix.Length..], out var userId) ? userId : null;
    }

    private static TimeSpan GetCartTimeToLive() => TimeSpan.FromHours(24);
}
