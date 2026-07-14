namespace WebShop.Api.Helpers;

public static class MongoMigrations
{
    public static async Task BackfillProductDescriptionsAsync(string dbPath, MongoProductService mongoProducts)
    {
        using var connection = Db.CreateOpenConnection(dbPath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, description FROM products WHERE description IS NOT NULL";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var productId = reader.GetInt64(0);
            var description = reader.IsDBNull(1) ? null : reader.GetString(1);
            await mongoProducts.UpsertDescriptionAsync(productId, description);
        }
    }
}
