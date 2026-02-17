using MacMD.Core.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MacMD.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DatabaseService _db;

    public PersistenceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"macmd_test_{Guid.NewGuid():N}.db");
        _db = new DatabaseService(_dbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        try { if (File.Exists(_dbPath + "-wal")) File.Delete(_dbPath + "-wal"); } catch { }
        try { if (File.Exists(_dbPath + "-shm")) File.Delete(_dbPath + "-shm"); } catch { }
    }

    [Fact]
    public void Database_Creates_File()
    {
        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public async Task Document_CRUD_Roundtrip()
    {
        var store = new DocumentStore(_db);

        // Create
        var id = await store.CreateAsync("Test Doc");
        var doc = await store.GetByIdAsync(id);
        Assert.NotNull(doc);
        Assert.Equal("Test Doc", doc.Title);
        Assert.Equal(string.Empty, doc.Content);

        // Update content
        await store.UpdateContentAsync(id, "Hello world, this is a test.");
        doc = await store.GetByIdAsync(id);
        Assert.NotNull(doc);
        Assert.Equal("Hello world, this is a test.", doc.Content);
        Assert.Equal(6, doc.WordCount);
        Assert.Equal("Hello world, this is a test.".Length, doc.CharacterCount);

        // List
        var all = await store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Test Doc", all[0].Title);

        // Delete
        await store.DeleteAsync(id);
        Assert.Null(await store.GetByIdAsync(id));
        Assert.Empty(await store.GetAllAsync());
    }

    [Fact]
    public async Task Project_CRUD_Roundtrip()
    {
        var store = new ProjectStore(_db);

        var id = await store.CreateAsync("My Project");
        var all = await store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("My Project", all[0].Name);

        await store.RenameAsync(id, "Renamed");
        all = await store.GetAllAsync();
        Assert.Equal("Renamed", all[0].Name);

        await store.DeleteAsync(id);
        Assert.Empty(await store.GetAllAsync());
    }

    [Fact]
    public async Task Documents_Filter_By_Project()
    {
        var projStore = new ProjectStore(_db);
        var docStore = new DocumentStore(_db);

        var projId = await projStore.CreateAsync("Proj A");
        await docStore.CreateAsync("Doc in project", projId);
        await docStore.CreateAsync("Doc without project");

        var all = await docStore.GetAllAsync();
        Assert.Equal(2, all.Count);

        var filtered = await docStore.GetByProjectAsync(projId);
        Assert.Single(filtered);
        Assert.Equal("Doc in project", filtered[0].Title);
    }

    [Fact]
    public void CheckIntegrity_Returns_Null_For_Healthy_DB()
    {
        var result = _db.CheckIntegrity();
        Assert.Null(result);
    }

    [Theory]
    [InlineData(@"C:\Users\me\Dropbox\notes\macmd.db", true)]
    [InlineData(@"C:\Users\me\OneDrive\Documents\macmd.db", true)]
    [InlineData(@"C:\Users\me\OneDrive - Contoso\macmd.db", true)]
    [InlineData(@"C:\Users\me\iCloud Drive\macmd.db", true)]
    [InlineData(@"C:\Users\me\Google Drive\macmd.db", true)]
    [InlineData(@"C:\Users\me\My Drive\macmd.db", true)]
    [InlineData(@"C:\Users\me\GoogleDriveFS\macmd.db", true)]
    [InlineData(@"C:\Users\me\AppData\Local\MacMD\macmd.db", false)]
    [InlineData(@"/home/user/.local/share/macmd/macmd.db", false)]
    public void DetectCloudSync_Identifies_Synced_Folders(string path, bool expected)
    {
        Assert.Equal(expected, DatabaseService.DetectCloudSync(path));
    }
}
