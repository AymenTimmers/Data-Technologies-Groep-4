using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace WebShop.Api;

public static class DbBootstrapper
{
    public static void EnsureCreated(string dbPath, string databaseFolder)
    {
        var schemaModelsFolder = Path.Combine(databaseFolder, "Models");
        var movedSchemaPath = Path.Combine(schemaModelsFolder, "schema.sql");
        var seedPath = Path.Combine(databaseFolder, "seed.sql");

        if (!Directory.Exists(schemaModelsFolder) || !File.Exists(seedPath))
        {
            throw new InvalidOperationException("Missing Database/Models folder or seed.sql in database folder.");
        }

        var schemaFiles = Directory.GetFiles(schemaModelsFolder, "*.sql", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), "schema.sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (schemaFiles.Count == 0 && File.Exists(movedSchemaPath))
        {
            schemaFiles.Add(movedSchemaPath);
        }

        if (schemaFiles.Count == 0)
        {
            throw new InvalidOperationException("No SQL schema model files found in Database/Models.");
        }

        var expectedFingerprint = ComputeSchemaFingerprint(schemaFiles, seedPath);

        if (!File.Exists(dbPath))
        {
            InitializeDatabase(dbPath, schemaFiles, seedPath, expectedFingerprint);
            return;
        }

        string? currentFingerprint;
        using (var connection = new SqliteConnection($"Data Source={dbPath}"))
        {
            connection.Open();
            EnsureMetaTable(connection);
            currentFingerprint = ReadMetaValue(connection, "schema_fingerprint");

            if (string.Equals(currentFingerprint, expectedFingerprint, StringComparison.Ordinal))
            {
                return;
            }
        }

        var backupPath = BuildBackupPath(dbPath);
        try
        {
            File.Copy(dbPath, backupPath, overwrite: true);
            File.Delete(dbPath);
        }
        catch (IOException)
        {
            return;
        }

        InitializeDatabase(dbPath, schemaFiles, seedPath, expectedFingerprint);
    }

    private static void InitializeDatabase(
        string dbPath,
        IReadOnlyList<string> schemaFiles,
        string seedPath,
        string schemaFingerprint)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        foreach (var schemaFile in schemaFiles)
        {
            using var schema = connection.CreateCommand();
            schema.CommandText = File.ReadAllText(schemaFile);
            schema.ExecuteNonQuery();
        }

        using (var seed = connection.CreateCommand())
        {
            var seedSql = File.ReadAllText(seedPath)
                .Replace("'hash1'", $"'{Security.HashPassword("password123")}'")
                .Replace("'hash2'", $"'{Security.HashPassword("admin123")}'");
            seed.CommandText = seedSql;
            seed.ExecuteNonQuery();
        }

        EnsureMetaTable(connection);
        WriteMetaValue(connection, "schema_fingerprint", schemaFingerprint);
    }

    private static void EnsureMetaTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS __db_meta (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    private static string? ReadMetaValue(SqliteConnection connection, string key)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM __db_meta WHERE key = @key LIMIT 1";
        command.Parameters.AddWithValue("@key", key);
        return command.ExecuteScalar() as string;
    }

    private static void WriteMetaValue(SqliteConnection connection, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO __db_meta (key, value)
            VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        command.ExecuteNonQuery();
    }

    private static string ComputeSchemaFingerprint(IReadOnlyList<string> schemaFiles, string seedPath)
    {
        var builder = new StringBuilder();
        foreach (var schemaFile in schemaFiles)
        {
            builder.AppendLine(Path.GetFileName(schemaFile));
            builder.AppendLine(File.ReadAllText(schemaFile));
        }

        builder.AppendLine(Path.GetFileName(seedPath));
        builder.AppendLine(File.ReadAllText(seedPath));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hashBytes);
    }

    private static string BuildBackupPath(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(dbPath);
        var extension = Path.GetExtension(dbPath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return Path.Combine(directory, $"{fileName}.bak.{timestamp}{extension}");
    }
}
