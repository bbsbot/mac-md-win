using MacMD.Core.Models;

namespace MacMD.Core.Services;

/// <summary>CRUD operations for projects.</summary>
public sealed class ProjectStore
{
    private readonly DatabaseService _db;

    public ProjectStore(DatabaseService db) => _db = db;

    public async Task<IReadOnlyList<Project>> GetAllAsync(int limit = 100)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, created_at
            FROM projects ORDER BY name LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", limit);

        var list = new List<Project>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Project(
                new ProjectId(reader.GetString(0)),
                reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2))));
        }
        return list;
    }

    public async Task<ProjectId> CreateAsync(string name)
    {
        var id = new ProjectId(Guid.NewGuid().ToString("N"));
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO projects (id, name, created_at) VALUES (@id, @name, datetime('now'))";
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    public async Task RenameAsync(ProjectId id, string name)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE projects SET name = @name WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(ProjectId id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM projects WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id.Value);
        await cmd.ExecuteNonQueryAsync();
    }
}
