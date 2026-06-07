using Microsoft.Data.Sqlite;
using System.Text;

namespace WebShop.Api.Helpers;

/// <summary>
/// Service for managing automated database backups and recovery.
/// </summary>
public class BackupService
{
    private readonly string _dbPath;
    private readonly string _backupDirectory;
    private const int MaxBackupsToKeep = 10;

    public BackupService(string dbPath, string? backupDirectory = null)
    {
        _dbPath = dbPath;
        _backupDirectory = backupDirectory ?? Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "backups");
    }

    /// <summary>
    /// Creates a backup of the database.
    /// </summary>
    public async Task<string> CreateBackupAsync()
    {
        try
        {
            Directory.CreateDirectory(_backupDirectory);

            if (!File.Exists(_dbPath))
            {
                throw new FileNotFoundException($"Database file not found at {_dbPath}");
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(_dbPath)}.backup.{timestamp}.db";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            await Task.Run(() =>
            {
                File.Copy(_dbPath, backupPath, overwrite: false);
            });

            // Clean up old backups
            CleanupOldBackups();

            return backupPath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create backup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Exports database data as SQL dump (for manual backups/archiving).
    /// </summary>
    public async Task<string> ExportAsSqlDumpAsync()
    {
        try
        {
            Directory.CreateDirectory(_backupDirectory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var dumpFileName = $"{Path.GetFileNameWithoutExtension(_dbPath)}.dump.{timestamp}.sql";
            var dumpPath = Path.Combine(_backupDirectory, dumpFileName);

            using var sourceConnection = new SqliteConnection($"Data Source={_dbPath}");
            await sourceConnection.OpenAsync();

            var sqlDump = new StringBuilder();
            sqlDump.AppendLine("-- WebShop Database SQL Dump");
            sqlDump.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            sqlDump.AppendLine("PRAGMA foreign_keys = ON;");
            sqlDump.AppendLine();

            // Get all table names
            using var command = sourceConnection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__%' ORDER BY name;";

            using var reader = await command.ExecuteReaderAsync();
            var tables = new List<string>();

            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            // Dump schema and data
            foreach (var table in tables)
            {
                await DumpTableAsync(sourceConnection, table, sqlDump);
            }

            await File.WriteAllTextAsync(dumpPath, sqlDump.ToString());
            return dumpPath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export SQL dump: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Restores the database from a backup file.
    /// </summary>
    public async Task RestoreFromBackupAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException($"Backup file not found at {backupPath}");
            }

            // Create a safety backup first
            var safetyBackupPath = await CreateBackupAsync();

            try
            {
                await Task.Run(() =>
                {
                    File.Copy(backupPath, _dbPath, overwrite: true);
                });
            }
            catch
            {
                // Restore from safety backup if restoration failed
                File.Copy(safetyBackupPath, _dbPath, overwrite: true);
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to restore from backup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets list of available backups.
    /// </summary>
    public List<BackupInfo> GetAvailableBackups()
    {
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(_backupDirectory))
        {
            return backups;
        }

        var backupFiles = Directory.GetFiles(_backupDirectory, "*.db", SearchOption.TopDirectoryOnly)
            .Where(f => f.Contains(".backup."))
            .OrderByDescending(f => f)
            .ToList();

        foreach (var file in backupFiles)
        {
            var fileInfo = new FileInfo(file);
            backups.Add(new BackupInfo
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                Created = fileInfo.CreationTimeUtc,
                SizeBytes = fileInfo.Length
            });
        }

        return backups;
    }

    private void CleanupOldBackups()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
            {
                return;
            }

            var backupFiles = Directory.GetFiles(_backupDirectory, "*.db", SearchOption.TopDirectoryOnly)
                .Where(f => f.Contains(".backup."))
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .ToList();

            if (backupFiles.Count > MaxBackupsToKeep)
            {
                for (int i = MaxBackupsToKeep; i < backupFiles.Count; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
        }
        catch
        {
            // Log but don't throw - cleanup failures shouldn't break backup process
        }
    }

    private async Task DumpTableAsync(SqliteConnection connection, string tableName, StringBuilder dump)
    {
        dump.AppendLine($"\n-- Table: {tableName}");
        dump.AppendLine($"DROP TABLE IF EXISTS {tableName};");

        // Get CREATE TABLE statement
        using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = $"SELECT sql FROM sqlite_master WHERE type='table' AND name=@name;";
        schemaCommand.Parameters.AddWithValue("@name", tableName);

        var createTableSql = await schemaCommand.ExecuteScalarAsync() as string;
        if (!string.IsNullOrEmpty(createTableSql))
        {
            dump.AppendLine(createTableSql + ";");
        }

        // Get data
        using var dataCommand = connection.CreateCommand();
        dataCommand.CommandText = $"SELECT * FROM {tableName};";

        using var reader = await dataCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                values.Add(reader.IsDBNull(i) ? "NULL" : $"'{reader.GetValue(i)}'");
            }

            var columnNames = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToList();

            dump.AppendLine($"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", values)});");
        }
    }
}

/// <summary>
/// Information about a backup file.
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public long SizeBytes { get; set; }
}
