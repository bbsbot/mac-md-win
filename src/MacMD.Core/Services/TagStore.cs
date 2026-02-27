using MacMD.Core.Models;

namespace MacMD.Core.Services;

/// <summary>CRUD operations for tags and the document_tags junction table.</summary>
public sealed class TagStore
{
    private readonly DatabaseService _db;

    public TagStore(DatabaseService db) => _db = db;

    public async Task<IReadOnlyList<Tag>> GetAllAsync(int limit = 100)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, color FROM tags ORDER BY name LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        var list = new List<Tag>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Tag(new TagId(reader.GetString(0)), reader.GetString(1), reader.GetString(2)));
        return list;
    }

    public async Task<TagId> CreateAsync(string name, string color = "#808080")
    {
        var id = new TagId(Guid.NewGuid().ToString("N"));
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO tags (id, name, color) VALUES (@id, @name, @color)";
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@color", color);
        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    public async Task RenameAsync(TagId id, string name)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tags SET name = @name WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateColorAsync(TagId id, string color)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tags SET color = @color WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@color", color);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(TagId id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM tags WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddTagToDocumentAsync(DocumentId docId, TagId tagId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO document_tags (document_id, tag_id) VALUES (@docId, @tagId)";
        cmd.Parameters.AddWithValue("@docId", docId.Value);
        cmd.Parameters.AddWithValue("@tagId", tagId.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveTagFromDocumentAsync(DocumentId docId, TagId tagId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM document_tags WHERE document_id = @docId AND tag_id = @tagId";
        cmd.Parameters.AddWithValue("@docId", docId.Value);
        cmd.Parameters.AddWithValue("@tagId", tagId.Value);
        await cmd.ExecuteNonQueryAsync();
    }
}
