using Microsoft.Data.Sqlite;

namespace MacMD.Core.Services;

/// <summary>
/// Manages the SQLite database lifecycle: creates the file, runs schema
/// migrations, and hands out connections.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();

        InitializeSchema();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Enable WAL mode and foreign keys per-connection
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();

        return conn;
    }

    private void InitializeSchema()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS projects (
                id          TEXT PRIMARY KEY,
                name        TEXT NOT NULL,
                created_at  TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS documents (
                id              TEXT PRIMARY KEY,
                title           TEXT NOT NULL DEFAULT 'Untitled',
                content         TEXT NOT NULL DEFAULT '',
                project_id      TEXT,
                word_count      INTEGER NOT NULL DEFAULT 0,
                character_count INTEGER NOT NULL DEFAULT 0,
                is_favorite     INTEGER NOT NULL DEFAULT 0,
                created_at      TEXT NOT NULL DEFAULT (datetime('now')),
                modified_at     TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS tags (
                id    TEXT PRIMARY KEY,
                name  TEXT NOT NULL,
                color TEXT NOT NULL DEFAULT '#808080'
            );

            CREATE TABLE IF NOT EXISTS document_tags (
                document_id TEXT NOT NULL,
                tag_id      TEXT NOT NULL,
                PRIMARY KEY (document_id, tag_id),
                FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE,
                FOREIGN KEY (tag_id)      REFERENCES tags(id)      ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_documents_project_id ON documents(project_id);
            CREATE INDEX IF NOT EXISTS idx_documents_modified_at ON documents(modified_at DESC);
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        // Nothing to dispose — connections are created on-demand.
    }
}
