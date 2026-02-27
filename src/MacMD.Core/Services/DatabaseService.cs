using Microsoft.Data.Sqlite;

namespace MacMD.Core.Services;

/// <summary>
/// Manages the SQLite database lifecycle: creates the file, runs schema
/// migrations, and hands out connections.
///
/// Cloud-sync awareness: when the DB lives inside a folder synced by
/// Dropbox / OneDrive / iCloud / Google Drive, WAL mode is unsafe because
/// the -wal and -shm sidecar files may sync independently of the main DB,
/// causing silent corruption. In that case we fall back to DELETE
/// (rollback journal) mode, which uses a single temporary file that is
/// removed after each transaction.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private readonly string _journalMode;

    /// <summary>Path to the SQLite database file.</summary>
    public string DbPath { get; }

    /// <summary>True when the DB path appears to be inside a cloud-synced folder.</summary>
    public bool IsCloudSynced { get; }

    public DatabaseService(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        DbPath = Path.GetFullPath(dbPath);

        var dir = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        IsCloudSynced = DetectCloudSync(DbPath);
        _journalMode = IsCloudSynced ? "DELETE" : "WAL";

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();

        InitializeSchema();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA journal_mode={_journalMode}; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();

        return conn;
    }

    /// <summary>
    /// Run an integrity check on the database.  Returns null if healthy,
    /// or a description of the problem.
    /// </summary>
    public string? CheckIntegrity()
    {
        try
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = cmd.ExecuteScalar()?.ToString();
            return result == "ok" ? null : result;
        }
        catch (SqliteException ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Detects whether <paramref name="path"/> is inside a well-known
    /// cloud sync folder (Dropbox, OneDrive, iCloud Drive, Google Drive).
    /// </summary>
    internal static bool DetectCloudSync(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();

        // Common sync folder markers
        ReadOnlySpan<string> markers =
        [
            "/dropbox/",
            "/onedrive/",
            "/onedrive -",          // "OneDrive - CompanyName"
            "/icloud drive/",
            "/icloudrive/",
            "/google drive/",
            "/my drive/",
            "/googledrivefs/",
        ];

        foreach (var m in markers)
        {
            if (normalized.Contains(m))
                return true;
        }

        return false;
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

        RunMigrations(conn);
    }

    private static void RunMigrations(SqliteConnection conn)
    {
        // Add is_archived column (idempotent — ignores if already present)
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE documents ADD COLUMN is_archived INTEGER NOT NULL DEFAULT 0";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException) { /* column already exists */ }
    }

    public void Dispose()
    {
        // Nothing to dispose — connections are created on-demand.
    }
}
