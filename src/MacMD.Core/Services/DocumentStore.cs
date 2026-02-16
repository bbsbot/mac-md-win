using MacMD.Core.Models;
using Microsoft.Data.Sqlite;

namespace MacMD.Core.Services;

/// <summary>CRUD operations for documents.</summary>
public sealed class DocumentStore
{
    private readonly DatabaseService _db;

    public DocumentStore(DatabaseService db) => _db = db;

    public async Task<IReadOnlyList<DocumentSummary>> GetAllAsync(int limit = 200)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, title, word_count, modified_at
            FROM documents
            ORDER BY modified_at DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", limit);

        var list = new List<DocumentSummary>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new DocumentSummary(
                new DocumentId(reader.GetString(0)),
                reader.GetString(1),
                reader.GetInt32(2),
                DateTimeOffset.Parse(reader.GetString(3))));
        }
        return list;
    }

    public async Task<IReadOnlyList<DocumentSummary>> GetByProjectAsync(ProjectId projectId, int limit = 200)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, title, word_count, modified_at
            FROM documents
            WHERE project_id = @projectId
            ORDER BY modified_at DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@projectId", projectId.Value);
        cmd.Parameters.AddWithValue("@limit", limit);

        var list = new List<DocumentSummary>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new DocumentSummary(
                new DocumentId(reader.GetString(0)),
                reader.GetString(1),
                reader.GetInt32(2),
                DateTimeOffset.Parse(reader.GetString(3))));
        }
        return list;
    }

    public async Task<Document?> GetByIdAsync(DocumentId id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, title, content, project_id, word_count, character_count,
                   is_favorite, created_at, modified_at
            FROM documents WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id.Value);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var docId = new DocumentId(reader.GetString(0));
        var projectId = reader.IsDBNull(3) ? (ProjectId?)null : new ProjectId(reader.GetString(3));

        // Load tags
        var tags = await GetTagIdsAsync(conn, docId);

        return new Document(
            docId,
            reader.GetString(1),
            reader.GetString(2),
            projectId,
            tags,
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6) != 0,
            DateTimeOffset.Parse(reader.GetString(7)),
            DateTimeOffset.Parse(reader.GetString(8)));
    }

    public async Task<DocumentId> CreateAsync(string title, ProjectId? projectId = null)
    {
        var id = new DocumentId(Guid.NewGuid().ToString("N"));
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO documents (id, title, project_id, created_at, modified_at)
            VALUES (@id, @title, @projectId, datetime('now'), datetime('now'))
            """;
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@projectId", projectId?.Value ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    public async Task UpdateContentAsync(DocumentId id, string content)
    {
        var wordCount = string.IsNullOrWhiteSpace(content)
            ? 0
            : content.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Length;
        var charCount = content.Length;

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE documents
            SET content = @content, word_count = @wc, character_count = @cc,
                modified_at = datetime('now')
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@content", content);
        cmd.Parameters.AddWithValue("@wc", wordCount);
        cmd.Parameters.AddWithValue("@cc", charCount);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateTitleAsync(DocumentId id, string title)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE documents SET title = @title, modified_at = datetime('now') WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@title", title);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(DocumentId id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM documents WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<TagId>> GetTagIdsAsync(SqliteConnection conn, DocumentId docId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tag_id FROM document_tags WHERE document_id = @docId";
        cmd.Parameters.AddWithValue("@docId", docId.Value);
        var list = new List<TagId>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new TagId(reader.GetString(0)));
        return list;
    }
}
