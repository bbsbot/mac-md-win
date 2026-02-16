# Mac MD for Windows — Development Progress

A milestone-by-milestone chronicle of building a native Windows Markdown editor with Claude Code agents. Every line of code, every build fix, every architecture decision — made by AI, tested by a human.

---

## M0 — Project Scaffolding

**Goal:** Buildable solution with three projects and proper .NET conventions.

### What We Built

Created the full solution structure from scratch — no Visual Studio IDE, no templates. WinUI 3 doesn't have a `dotnet new` template outside Visual Studio, so every `.csproj` and XAML file was hand-authored.

**Central Package Management** keeps all NuGet versions in one place:

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.WindowsAppSDK" Version="1.6.240829007" />
    <PackageVersion Include="Markdig" Version="0.38.0" />
    <PackageVersion Include="Microsoft.Data.Sqlite" Version="8.0.11" />
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

### The Hard Part: WinUI 3 Without Visual Studio

The biggest surprise of M0: **`dotnet build` doesn't work for WinUI 3 projects.** The Windows App SDK build tasks only resolve correctly through MSBuild from Visual Studio Build Tools:

```bash
# This fails with MSB4062 (missing Pri.Tasks.dll):
dotnet build MacMD.sln

# This works:
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe" \
    MacMD.sln -restore -p:Platform=x64
```

We also discovered that the **WinUI 3 runtime must be bundled** with the app. Without these two lines, the app crashes on launch with `REGDB_E_CLASSNOTREG (0x80040154)`:

```xml
<!-- MacMD.Win.csproj — these two lines are non-negotiable -->
<WindowsPackageType>None</WindowsPackageType>
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
```

**Files created:** 8 | **Tests:** 1 passing

---

## M1 — App Shell + Layout + Theming

**Goal:** Three-column UI, theme system, dependency injection, app launches.

### Domain Models

Clean, immutable records with strongly-typed IDs — no primitive obsession:

```csharp
// Strongly-typed IDs prevent mixing up document/project/tag references
public readonly record struct DocumentId(string Value);
public readonly record struct ProjectId(string Value);

// Immutable domain objects
public sealed record Document(
    DocumentId Id,
    string Title,
    string Content,
    ProjectId? ProjectId,
    IReadOnlyList<TagId> TagIds,
    int WordCount,
    int CharacterCount,
    bool IsFavorite,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
```

### 10 Terminal-Inspired Themes

The theme system mirrors the Mac app's terminal palette. Each theme is a `ColorTheme` record stored in a `FrozenDictionary` for zero-allocation lookups:

```csharp
public sealed record ColorTheme(
    string Name,
    string Background, string Foreground,
    string AnsiBlack,   string AnsiRed,
    string AnsiGreen,   string AnsiYellow,
    string AnsiBlue,    string AnsiMagenta,
    string AnsiCyan,    string AnsiWhite,
    bool IsDark);

// ThemeService.cs — the full palette
private static readonly FrozenDictionary<string, ColorTheme> s_themes =
    new Dictionary<string, ColorTheme>
    {
        ["Pro"]           = new("Pro",           "#1A1A1A", "#F2F2F2", ...),
        ["Homebrew"]      = new("Homebrew",      "#0A0A0A", "#00FF00", ...),
        ["Ocean"]         = new("Ocean",         "#1A2634", "#FFFFFF", ...),
        ["Red Sands"]     = new("Red Sands",     "#7A2F1A", "#D7C9A7", ...),
        // ... 10 themes total
    }.ToFrozenDictionary();
```

### Three-Column Layout

The XAML layout uses a simple `Grid` with fixed + star columns:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220" MinWidth="150" />   <!-- Projects -->
        <ColumnDefinition Width="Auto" />                  <!-- Splitter -->
        <ColumnDefinition Width="280" MinWidth="180" />   <!-- Documents -->
        <ColumnDefinition Width="Auto" />                  <!-- Splitter -->
        <ColumnDefinition Width="*" />                     <!-- Editor+Preview -->
    </Grid.ColumnDefinitions>
    <!-- ... -->
</Grid>
```

**Files created:** 15 | **App launches:** Yes

---

## M2 — Markdown Preview Pipeline

**Goal:** Type Markdown, see live HTML preview with syntax highlighting.

### Markdig Integration

One singleton, one method, zero configuration headaches:

```csharp
public sealed class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()  // tables, task lists, pipe tables, etc.
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;
        return Markdown.ToHtml(markdown, _pipeline);
    }
}
```

### Live Preview with Debounce

The editor fires `PropertyChanged` on every keystroke. A `DispatcherTimer` debounces updates so the WebView2 preview only re-renders after 300ms of silence:

```csharp
// MainWindow.xaml.cs — the wiring
_editorViewModel.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(EditorViewModel.MarkdownText))
    {
        _previewDebounce.Stop();
        _previewDebounce.Start();  // restart the 300ms countdown
    }
};

private void OnPreviewDebounceTick(object? sender, object e)
{
    _previewDebounce.Stop();
    PreviewView.UpdateHtml(_markdownService.ToHtml(_editorViewModel.MarkdownText));
}
```

### Dark-Themed Preview HTML

The preview renders inside a WebView2 control with an embedded HTML template. No external CSS files — everything is self-contained:

```csharp
private const string HtmlTemplate = """
    <!DOCTYPE html>
    <html><head>
    <style>
      body { font-family: -apple-system, 'Segoe UI', sans-serif;
             padding: 16px; line-height: 1.6;
             color: #e0e0e0; background: #1e1e1e; }
      code { background: #2d2d2d; padding: 2px 6px; border-radius: 3px; }
      pre  { background: #2d2d2d; padding: 12px; border-radius: 6px; }
      blockquote { border-left: 4px solid #444; padding-left: 16px; color: #aaa; }
      a { color: #58a6ff; }
    </style>
    </head><body><!-- CONTENT --></body></html>
    """;

public void UpdateHtml(string html)
{
    PreviewWebView.NavigateToString(HtmlTemplate.Replace("<!-- CONTENT -->", html));
}
```

### MVVM Toolkit Compatibility Lesson

CommunityToolkit.Mvvm 8.4 emits `MVVMTK0045` errors for field-based `[ObservableProperty]` in WinUI 3 apps — it wants partial properties, which require C# 13 (.NET 9). Since we're on .NET 8, we suppress the warning:

```xml
<!-- MacMD.Win.csproj -->
<NoWarn>$(NoWarn);MVVMTK0045</NoWarn>
```

**Files created:** 7 | **Tests:** 4 passing

---

## M3 — Local Persistence

**Goal:** SQLite database, CRUD for projects/documents/tags, auto-save, navigation.

### Schema Design

Four tables with proper foreign keys, cascade deletes, and indexes:

```csharp
// DatabaseService.cs — schema migration runs on first launch
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

    CREATE TABLE IF NOT EXISTS document_tags (
        document_id TEXT NOT NULL,
        tag_id      TEXT NOT NULL,
        PRIMARY KEY (document_id, tag_id),
        FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE,
        FOREIGN KEY (tag_id)      REFERENCES tags(id)      ON DELETE CASCADE
    );
    """;
```

WAL mode is enabled per-connection for better concurrent read performance:

```csharp
public SqliteConnection CreateConnection()
{
    var conn = new SqliteConnection(_connectionString);
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
    cmd.ExecuteNonQuery();
    return conn;
}
```

### Auto-Save with Dual Debounce

Two independent timers run in parallel — one for preview (fast, 300ms) and one for persistence (slow, 2 seconds):

```csharp
// Preview updates quickly so you see changes immediately
_previewDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };

// Auto-save waits longer to avoid hammering SQLite
_saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };

// Both restart on every keystroke
_editorViewModel.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(EditorViewModel.MarkdownText))
    {
        _previewDebounce.Stop();  _previewDebounce.Start();
        _saveDebounce.Stop();     _saveDebounce.Start();
    }
};
```

### Auto Word Count on Save

Word and character counts are computed automatically when content is saved — no separate tracking needed:

```csharp
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
    // ...
}
```

### Full Navigation Flow

Project selection cascades to document filtering, which cascades to editor loading:

```
User clicks project → ProjectSelected event
    → DocumentListViewModel.LoadForProjectAsync(projectId)
        → DocumentStore.GetByProjectAsync(projectId)
            → ListView updates

User clicks document → DocumentSelected event
    → Save current document first
    → DocumentStore.GetByIdAsync(docId)
        → EditorViewModel.MarkdownText = doc.Content
        → PreviewView.UpdateHtml(...)
```

**Files created:** 12 | **Tests:** 8 passing | **Database:** `%LOCALAPPDATA%\MacMD\macmd.db`

---

## Test Suite

8 tests covering core functionality:

| Test | What It Verifies |
|------|-----------------|
| `DocumentId_RoundTrips` | Strongly-typed ID value preservation |
| `MarkdownService_Converts_Heading` | Markdig pipeline produces correct HTML |
| `MarkdownService_Returns_Empty_For_Null` | Null safety |
| `DI_Resolves_Core_Services` | Service registration works |
| `Database_Creates_File` | SQLite file created on disk |
| `Document_CRUD_Roundtrip` | Create → read → update → delete lifecycle |
| `Project_CRUD_Roundtrip` | Project create → rename → delete |
| `Documents_Filter_By_Project` | Project-scoped document queries |

---

## What's Next

| Milestone | Key Features |
|-----------|-------------|
| **M4 — Localization** | Python script generates `.resw` files for 38 languages from shared JSON |
| **M5 — Export** | HTML export with inline CSS, PDF via WebView2 `PrintToPdfAsync` |
| **M6 — Polish** | Settings UI, keyboard shortcuts (Ctrl+N/S/F), MSIX installer, accessibility |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    MacMD.Win (WinUI 3)                  │
│                                                         │
│  ┌──────────┐  ┌──────────────┐  ┌───────┐  ┌────────┐│
│  │ Project  │  │  Document    │  │Editor │  │Preview ││
│  │ ListView │→ │  ListView    │→ │ View  │→ │ View   ││
│  │          │  │              │  │(XAML) │  │(Web    ││
│  │          │  │              │  │       │  │ View2) ││
│  └────┬─────┘  └──────┬───────┘  └───┬───┘  └────────┘│
│       │               │              │                  │
│  ┌────┴─────┐  ┌──────┴───────┐  ┌───┴──────────┐     │
│  │ProjectLst│  │DocumentList  │  │  Editor       │     │
│  │ViewModel │  │  ViewModel   │  │  ViewModel    │     │
│  └────┬─────┘  └──────┬───────┘  └──────────────┘     │
│───────┼────────────────┼────────────────────────────────│
│       │    MacMD.Core  │  (no UI dependency)            │
│  ┌────┴─────┐  ┌──────┴───────┐  ┌──────────────┐     │
│  │ Project  │  │  Document    │  │  Markdown    │     │
│  │  Store   │  │   Store      │  │  Service     │     │
│  └────┬─────┘  └──────┬───────┘  └──────────────┘     │
│       │               │                                 │
│       └───────┬───────┘                                 │
│         ┌─────┴──────┐                                  │
│         │ Database   │                                  │
│         │ Service    │ → SQLite (WAL mode)              │
│         └────────────┘                                  │
└─────────────────────────────────────────────────────────┘
```

---

*Built with Claude Code. Every line of code authored by AI, every milestone tested by a human.*
