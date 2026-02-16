# DESIGN.md — Mac MD for Windows

Architectural design document for the Windows-native Markdown editor.
References dotnet skills from `.claude/skills/dotnet/skills/` by name.

---

## 1. Solution Structure

Per **project-structure** skill: use `.slnx` format, `Directory.Build.props`,
Central Package Management, `global.json` for SDK pinning.

```
mac-md-win/
├── src/
│   ├── MacMD.Core/              # Domain models + services (no UI refs)
│   │   ├── Models/
│   │   ├── Services/
│   │   └── MacMD.Core.csproj
│   ├── MacMD.Win/               # WinUI 3 app (Views, ViewModels, App.xaml)
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Assets/
│   │   ├── Web/                 # HTML/CSS/JS for WebView2 preview
│   │   ├── Strings/             # Generated .resw per locale
│   │   └── MacMD.Win.csproj
│   └── MacMD.Tests/             # Unit tests (xunit)
│       └── MacMD.Tests.csproj
├── tools/
│   └── localization/            # Python scripts for .resw generation
├── docs/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── NuGet.Config
└── MacMD.slnx
```

**Why two projects (Core + Win)?** Core contains all logic that is UI-agnostic
(models, SQLite access, Markdown conversion, theme definitions). Win contains
WinUI 3 views and view models. This keeps domain logic testable without a
UI host and follows the CLAUDE.md-mandated separation.

---

## 2. Data Model

Per **csharp-coding-standards** skill: use `record` for entities,
`readonly record struct` for value objects. Per **csharp-type-design-performance**
skill: seal all classes, prefer immutable types.

### Domain Records (MacMD.Core/Models/)

```csharp
// Strongly-typed IDs (readonly record struct per coding-standards)
public readonly record struct DocumentId(string Value);
public readonly record struct ProjectId(string Value);
public readonly record struct TagId(string Value);

// Core entities (immutable records)
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

public sealed record Project(
    ProjectId Id,
    string Name,
    DateTimeOffset CreatedAt);

public sealed record Tag(
    TagId Id,
    string Name,
    string Color);
```

### SQLite Schema (MacMD.Core/Services/DatabaseService.cs)

```sql
CREATE TABLE IF NOT EXISTS projects (
    id          TEXT PRIMARY KEY,
    name        TEXT NOT NULL,
    created_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS documents (
    id              TEXT PRIMARY KEY,
    title           TEXT NOT NULL,
    content         TEXT NOT NULL DEFAULT '',
    project_id      TEXT REFERENCES projects(id) ON DELETE SET NULL,
    word_count      INTEGER NOT NULL DEFAULT 0,
    character_count INTEGER NOT NULL DEFAULT 0,
    is_favorite     INTEGER NOT NULL DEFAULT 0,
    created_at      TEXT NOT NULL,
    modified_at     TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS tags (
    id    TEXT PRIMARY KEY,
    name  TEXT NOT NULL,
    color TEXT NOT NULL DEFAULT '#808080'
);

CREATE TABLE IF NOT EXISTS document_tags (
    document_id TEXT NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    tag_id      TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (document_id, tag_id)
);
```

Per **database-performance** skill: no generic repository. Purpose-built
read/write stores with explicit projections and row limits.

---

## 3. Service Layer (MacMD.Core/Services/)

Per **dependency-injection-patterns** skill: group registrations into
`IServiceCollection.AddMacMDCore()` extension. Per **csharp-coding-standards**:
async everywhere with CancellationToken, composition over inheritance.

| Service | Responsibility |
|---------|---------------|
| `DatabaseService` | SQLite connection management, schema migration, CRUD |
| `MarkdownService` | Markdig pipeline: MD -> HTML (stateless, singleton) |
| `ThemeService` | 10 terminal-inspired theme definitions, current theme state |
| `ExportService` | HTML and PDF export via WebView2 print-to-PDF |
| `SettingsService` | User preferences persisted to `ApplicationData.LocalSettings` |

### DatabaseService design (per database-performance skill)

- Separate read DTOs from write commands (no shared entity for both)
- Every list query takes a `limit` parameter
- Raw `Microsoft.Data.Sqlite` — no EF Core (overkill for a local single-user SQLite DB)
- Explicit column projections in every query

```csharp
// Read operations (lightweight projections)
public sealed record DocumentSummary(
    DocumentId Id, string Title, int WordCount, DateTimeOffset ModifiedAt);

// Write commands
public sealed record CreateDocumentCommand(
    string Title, string Content, ProjectId? ProjectId);

public interface IDocumentStore
{
    Task<IReadOnlyList<DocumentSummary>> GetAllAsync(
        int limit, CancellationToken ct = default);
    Task<Document?> GetByIdAsync(
        DocumentId id, CancellationToken ct = default);
    Task<DocumentId> CreateAsync(
        CreateDocumentCommand cmd, CancellationToken ct = default);
    Task UpdateContentAsync(
        DocumentId id, string content, CancellationToken ct = default);
    Task DeleteAsync(
        DocumentId id, CancellationToken ct = default);
}
```

### MarkdownService design (per serialization + type-design-performance skills)

- Stateless, sealed, singleton-lifetime
- Wraps Markdig `MarkdownPipeline` (built once, reused)
- Returns HTML string; no side effects

```csharp
public sealed class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string ToHtml(string markdown)
        => Markdig.Markdown.ToHtml(markdown, _pipeline);
}
```

---

## 4. UI Layer (MacMD.Win/)

### MVVM with CommunityToolkit.Mvvm

Per **csharp-coding-standards** skill: prefer composition, avoid deep
hierarchies. ViewModels use `[ObservableProperty]` and `[RelayCommand]`
source generators from CommunityToolkit.Mvvm.

| ViewModel | View | Purpose |
|-----------|------|---------|
| `MainViewModel` | `MainWindow.xaml` | Shell, column visibility, navigation |
| `ProjectListViewModel` | `ProjectListView.xaml` | Left column: projects/tags |
| `DocumentListViewModel` | `DocumentListView.xaml` | Middle column: doc list |
| `EditorViewModel` | `EditorView.xaml` | Right column: text editing |
| `PreviewViewModel` | `PreviewView.xaml` | Right column: WebView2 preview |
| `SettingsViewModel` | `SettingsView.xaml` | Settings dialog |

### Three-Column Layout

```
┌──────────────┬──────────────────┬──────────────────────────────┐
│  Projects /  │  Document List   │  Editor  │  Preview          │
│  Tags / Nav  │  (title, date,   │  (TextBox,│  (WebView2,      │
│              │   word count)    │  mono font)│  Highlight.js)  │
└──────────────┴──────────────────┴──────────────────────────────┘
```

Implemented as nested `SplitView` controls with adjustable pane widths.

### Preview Pipeline

Per **csharp-concurrency-patterns** skill: use debounced async for preview
updates. The Rx approach (Throttle + DistinctUntilChanged) from the
concurrency skill maps directly to this use case.

```
TextBox.TextChanged
  -> Debounce 300ms (DispatcherTimer or Rx Throttle)
  -> MarkdownService.ToHtml()
  -> Inject into WebView2 via ExecuteScriptAsync / NavigateToString
```

### Theme System

10 themes defined as `sealed record ColorTheme(...)` in Core.
At runtime, `ThemeService` applies the selected theme by updating
WinUI 3 resources and injecting CSS variables into the WebView2 preview.

---

## 5. Tech Stack Decisions

| Dependency | Version | Skill Reference | Rationale |
|-----------|---------|-----------------|-----------|
| .NET 8 | 8.0.x | project-structure (global.json) | LTS, WinUI 3 support |
| Windows App SDK | 1.5+ | — | WinUI 3 + WebView2 hosting |
| Microsoft.Data.Sqlite | 8.x | database-performance | Lightweight; no ORM overhead |
| Markdig | 0.37+ | — | MIT, fast, extensible MD parser |
| CommunityToolkit.Mvvm | 8.x | dependency-injection | Source-gen MVVM, no reflection |
| System.Text.Json | 8.x | serialization (source gen) | Theme/settings JSON; AOT-ready |
| WebView2 | Runtime | — | Ships with Win10/11 |
| xunit + FluentAssertions | latest | package-management (CPM) | Testing framework |

Per **package-management** skill: all versions centralized in
`Directory.Packages.props`. Use `dotnet add package`, never edit XML.

Per **serialization** skill: use `System.Text.Json` with source generators
for any JSON serialization (theme files, settings export). No Newtonsoft.

---

## 6. DI Registration

Per **dependency-injection-patterns** skill: composable `Add*` extensions.

```csharp
// MacMD.Core
public static class CoreServiceExtensions
{
    public static IServiceCollection AddMacMDCore(
        this IServiceCollection services, string dbPath)
    {
        services.AddSingleton(new DatabaseService(dbPath));
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<SettingsService>();
        return services;
    }
}

// MacMD.Win (App.xaml.cs or Program.cs equivalent)
// Register Core + ViewModels
services.AddMacMDCore(dbPath);
services.AddTransient<MainViewModel>();
services.AddTransient<EditorViewModel>();
// etc.
```

---

## 7. API Surface (Internal)

This is a desktop app, not a library. The "API surface" is the set of
services and their public methods exposed to ViewModels.

| Service | Key Methods |
|---------|------------|
| `IDocumentStore` | `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateContentAsync`, `DeleteAsync` |
| `IProjectStore` | `GetAllAsync`, `CreateAsync`, `RenameAsync`, `DeleteAsync` |
| `ITagStore` | `GetAllAsync`, `CreateAsync`, `DeleteAsync` |
| `MarkdownService` | `ToHtml(string markdown)` |
| `ThemeService` | `GetThemes()`, `SetTheme(string name)`, `CurrentTheme` |
| `ExportService` | `ExportHtmlAsync(Document)`, `ExportPdfAsync(Document, WebView2)` |
| `SettingsService` | `Get<T>(key)`, `Set<T>(key, value)` |

---

## 8. Key Design Decisions

1. **No EF Core.** Raw SQLite via `Microsoft.Data.Sqlite`. The app is
   single-user, single-process, local-only. EF Core adds complexity
   without benefit here. Per database-performance skill, purpose-built
   stores with explicit SQL are preferred for read-heavy workloads.

2. **No cloud sync (yet).** CLAUDE.md explicitly defers this. The data
   layer is designed so a `SyncService` can be added later without
   restructuring the schema.

3. **Rx for debounce only if needed.** The concurrency skill recommends
   Rx for UI event composition (debounce, throttle). If a simple
   `DispatcherTimer` suffices for preview debounce, skip the Rx
   dependency. Escalate only when needed.

4. **Sealed classes everywhere.** Per type-design-performance skill,
   all service classes and ViewModels are `sealed` for JIT devirtualization.

5. **Records for models, structs for IDs.** Per coding-standards skill,
   domain entities are `sealed record`, value objects (IDs) are
   `readonly record struct`.

6. **Source-gen JSON.** Per serialization skill, all JSON handling uses
   `System.Text.Json` with `[JsonSerializable]` source generators.
   No Newtonsoft, no reflection.

7. **Central Package Management.** Per package-management skill, all
   NuGet versions live in `Directory.Packages.props`. One source of truth.

---

## 9. Milestones (from CLAUDE.md)

| # | Milestone | Builds On |
|---|-----------|-----------|
| M1 | App shell + 3-column layout + dark/light theming | — |
| M2 | Markdown preview pipeline (editor + WebView2 + debounce) | M1 |
| M3 | Local persistence (SQLite CRUD, list from DB) | M1, M2 |
| M4 | Localization pipeline (.resw generation from JSON) | M3 |
| M5 | Export (HTML + PDF via WebView2) | M2, M3 |
| M6 | Polish + MSIX packaging + accessibility | All |

Each milestone must end with `dotnet build` success and user testing.
