using Microsoft.Data.Sqlite;
using System.Text;
namespace WebShop.Api;
static class ModelDocumentationGenerator
{
    public static DocumentationGenerationResult Generate(string dbPath, string outputPath)
    {
        using var connection = Db.CreateOpenConnection(dbPath);

        var tables = GetTables(connection);
        var allRelations = new List<DbRelation>();
        var generatedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        var markdown = new StringBuilder();
        markdown.AppendLine("# Database Models and Relations");
        markdown.AppendLine();
        markdown.AppendLine($"Generated at: {generatedAtUtc}");
        markdown.AppendLine();

        foreach (var table in tables)
        {
            var columns = GetColumns(connection, table);
            var relations = GetRelations(connection, table);
            allRelations.AddRange(relations);

            markdown.AppendLine($"## {table}");
            markdown.AppendLine();
            markdown.AppendLine("| Column | Type | Not Null | PK | Default |");
            markdown.AppendLine("|---|---|---|---|---|");
            foreach (var column in columns)
            {
                markdown.AppendLine($"| {column.Name} | {column.Type} | {(column.NotNull ? "Yes" : "No")} | {(column.IsPrimaryKey ? "Yes" : "No")} | {column.DefaultValue ?? ""} |");
            }

            if (relations.Count > 0)
            {
                markdown.AppendLine();
                markdown.AppendLine("Relations:");
                foreach (var relation in relations)
                {
                    markdown.AppendLine($"- {relation.FromTable}.{relation.FromColumn} -> {relation.ToTable}.{relation.ToColumn}");
                }
            }

            markdown.AppendLine();
        }

        var uniqueRelations = allRelations
            .DistinctBy(r => (r.FromTable, r.FromColumn, r.ToTable, r.ToColumn))
            .ToList();

        markdown.AppendLine("## ER Diagram (Mermaid)");
        markdown.AppendLine();
        markdown.AppendLine("```mermaid");
        markdown.AppendLine("erDiagram");

        foreach (var table in tables)
        {
            var columns = GetColumns(connection, table);
            markdown.AppendLine($"  {table} {{");
            foreach (var column in columns)
            {
                var markers = new List<string>();
                if (column.IsPrimaryKey)
                {
                    markers.Add("PK");
                }

                if (uniqueRelations.Any(r => r.FromTable == table && r.FromColumn == column.Name))
                {
                    markers.Add("FK");
                }

                var markerText = markers.Count == 0 ? string.Empty : $" {string.Join(" ", markers)}";
                markdown.AppendLine($"    {column.Type} {column.Name}{markerText}");
            }
            markdown.AppendLine("  }");
        }

        foreach (var relation in uniqueRelations)
        {
            markdown.AppendLine($"  {relation.ToTable} ||--o{{ {relation.FromTable} : \"{relation.FromColumn}->{relation.ToColumn}\"");
        }

        markdown.AppendLine("```");

        File.WriteAllText(outputPath, markdown.ToString());

        return new DocumentationGenerationResult(generatedAtUtc, tables.Count, uniqueRelations.Count, outputPath);
    }

    private static List<string> GetTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
              AND name <> '__db_meta'
            ORDER BY name;";

        using var reader = command.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static List<DbColumn> GetColumns(SqliteConnection connection, string table)
    {
        var safeTable = table.Replace("'", "''");
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{safeTable}')";

        using var reader = command.ExecuteReader();
        var columns = new List<DbColumn>();
        while (reader.Read())
        {
            columns.Add(new DbColumn(
                reader.GetString(1),
                string.IsNullOrWhiteSpace(reader.GetString(2)) ? "TEXT" : reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetInt32(5) == 1,
                reader.IsDBNull(4) ? null : reader.GetString(4)
            ));
        }

        return columns;
    }

    private static List<DbRelation> GetRelations(SqliteConnection connection, string table)
    {
        var safeTable = table.Replace("'", "''");
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list('{safeTable}')";

        using var reader = command.ExecuteReader();
        var relations = new List<DbRelation>();
        while (reader.Read())
        {
            relations.Add(new DbRelation(
                table,
                reader.GetString(3),
                reader.GetString(2),
                reader.GetString(4)
            ));
        }

        return relations;
    }

    private sealed record DbColumn(string Name, string Type, bool NotNull, bool IsPrimaryKey, string? DefaultValue);
    private sealed record DbRelation(string FromTable, string FromColumn, string ToTable, string ToColumn);
}

sealed record DocumentationGenerationResult(string GeneratedAtUtc, int TableCount, int RelationCount, string OutputPath);
