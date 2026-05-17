using StackExchange.Redis;

// Het idee is per gebruiker een Redis HASH voor de cart

// Redis key: cart:user:{userId}
// Redis value bestaat uit: 
    // field = productId (string)
    // value = quantity (int)

// cart:user:1
//   product 5 -> quantity 3
//   product 8 -> quantity 1

public class RedisCartStore : ICartStore
{
    private readonly IDatabase _database;
    private readonly IServer _server;

    public RedisCartStore(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
    }

    // Haalt de hele cart van user uit Redis
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

    // verhoogt quantity en zet TTL tot morgen
    public async Task AddItemAsync(long userId, long productId, int quantity)
    {
        var key = GetCartKey(userId);

        await _database.HashIncrementAsync(key, productId.ToString(), quantity);
        await _database.KeyExpireAsync(key, GetCartTimeToLive());
    }

    // haalt 1 stuk van een product uit de cart
    public async Task DecrementItemAsync(long userId, long productId)
    {
        var key = GetCartKey(userId);
        var field = productId.ToString();
        var currentValue = await _database.HashGetAsync(key, field);

        // check of product in cart zit, dan quantity omzetten naar int
        if (!currentValue.HasValue || !int.TryParse(currentValue.ToString(), out var currentQuantity))
        {
            return;
        }

        // laatste product? --> verwijderen, anders quantity verlagen met 1
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

    // verwijdert 1 product uit cart
    public async Task RemoveItemAsync(long userId, long productId)
    {
        var key = GetCartKey(userId);

        await _database.HashDeleteAsync(key, productId.ToString());

        if (await _database.KeyExistsAsync(key))
        {
            await _database.KeyExpireAsync(key, GetCartTimeToLive());
        }
    }

    // wist hele cart na succesvolle checkout
    public Task ClearCartAsync(long userId)
    {
        return _database.KeyDeleteAsync(GetCartKey(userId));
    }

    // geeft de redis TTL van de cart key
    public Task<TimeSpan?> GetCartTimeToLiveAsync(long userId)
    {
        return _database.KeyTimeToLiveAsync(GetCartKey(userId));
    }

    // telt product quantity in alle actieve Redis carts
    public async Task<int> GetReservedQuantityAsync(long productId, long? excludingUserId = null)
    {
        var totalReserved = 0;
        var productField = productId.ToString();

        foreach (var key in _server.Keys(pattern: $"{"cart:user:"}*"))
        {
            var userId = TryGetUserIdFromCartKey(key);

            // huidige user niet meetellen, skip naar volgende loop
            if (excludingUserId.HasValue && userId == excludingUserId.Value)
            {
                continue;
            }

            // product quantity van de cart optellen bij totaal gereserveerd
            var quantityValue = await _database.HashGetAsync(key, productField);
            if (quantityValue.HasValue && int.TryParse(quantityValue.ToString(), out var quantity))
            {
                totalReserved += quantity;
            }
        }

        return totalReserved;
    }


    // Helper methods
    private static string GetCartKey(long userId)
    {
        return $"{"cart:user:"}{userId}";
    }

    private static long? TryGetUserIdFromCartKey(RedisKey key)
    {
        // redis key omzetten naar string
        var keyText = key.ToString();

        // check of het echt een cart key is
        if (!keyText.StartsWith("cart:user:"))
        {
            return null;
        }

        // probeer userId om te zetten naar long en terug geven
        var prefixLength = "cart:user:".Length;
        return long.TryParse(keyText[prefixLength..], out var userId) ? userId : null;
    }

    private static TimeSpan GetCartTimeToLive()
    {
        return TimeSpan.FromHours(24);
    }
}
