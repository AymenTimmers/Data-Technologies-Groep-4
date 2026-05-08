public interface ICartStore
{
    Task<IReadOnlyDictionary<long, int>> GetCartAsync(long userId);
    Task AddItemAsync(long userId, long productId, int quantity);
    Task RemoveItemAsync(long userId, long productId);
    Task ClearCartAsync(long userId);
    Task<int> GetReservedQuantityAsync(long productId, long? excludingUserId = null);
}
