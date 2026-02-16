# TODO.md — Mac MD for Windows

Implementation tasks derived from DESIGN.md. Each task is scoped for
a single Implementer agent invocation. Tasks are grouped by milestone.

---

## M0 — Project Scaffolding

- [ ] **1. Create solution and project files**
  Create `MacMD.slnx` with three projects: `src/MacMD.Core/MacMD.Core.csproj`
  (class library, net8.0), `src/MacMD.Win/MacMD.Win.csproj` (WinUI 3 app,
  net8.0-windows10.0.19041.0), `src/MacMD.Tests/MacMD.Tests.csproj` (xunit,
  net8.0). Use `dotnet new` commands. Add `global.json` pinning SDK 8.0.x
  with `latestFeature` rollForward.

- [ ] **2. Add Directory.Build.props and Directory.Packages.props**
  Create `Directory.Build.props` with: `LangVersion=latest`, `Nullable=enable`,
  `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`. Create
  `Directory.Packages.props` with CPM enabled and initial package versions:
  `Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools`,
  `CommunityToolkit.Mvvm`, `Markdig`, `Microsoft.Data.Sqlite`,
  `Microsoft.Web.WebView2`, `xunit`, `FluentAssertions`,
  `Microsoft.NET.Test.Sdk`. Create `NuGet.Config` with cleared sources
  and nuget.org only.

- [ ] **3. Add .gitignore for .NET + WinUI 3**
  Update `.gitignore` to cover `bin/`, `obj/`, `*.user`, `.vs/`,
  `AppPackages/`, `BundleArtifacts/`, `*.appx`, `*.msix`.

- [ ] **4. Verify scaffold builds**
  Run `dotnet build` for the solution. Fix any errors. This is the
  build-gate for M0.

---

## M1 — App Shell + Layout + Theming

- [ ] **5. Define domain models in MacMD.Core**
  Create `Models/` directory with: `DocumentId`, `ProjectId`, `TagId`
  (readonly record structs), `Document`, `Project`, `Tag` (sealed records),
  `DocumentSummary` (sealed record for list display), `ColorTheme`
  (sealed record with Background, Foreground, and 8 ANSI color properties).

- [ ] **6. Implement ThemeService in MacMD.Core**
  Create `Services/ThemeService.cs`: sealed class, defines the 10
  terminal-inspired themes as a `FrozenDictionary<string, ColorTheme>`.
  Exposes `GetThemes()`, `GetTheme(string name)`, observable
  `CurrentTheme` property. No UI dependency.

- [ ] **7. Create MainWindow with three-column layout**
  In MacMD.Win, implement `MainWindow.xaml` using nested `SplitView`
  controls: left pane (projects/tags, 220px default), middle pane
  (document list, 280px default), right pane (editor + preview).
  Add placeholder `TextBlock` content in each column. Wire up
  `App.xaml.cs` to show `MainWindow` on launch.

- [ ] **8. Implement dark/light theme switching**
  In MacMD.Win, create `ThemeHelper.cs` that reads `ThemeService.CurrentTheme`
  and applies colors to WinUI resource dictionaries. Support system-default,
  light, and dark base themes. Wire a temporary toggle button in MainWindow
  to switch themes.

- [ ] **9. Add DI registration and App startup**
  Create `CoreServiceExtensions.AddMacMDCore()` in MacMD.Core. In
  `App.xaml.cs`, build a `ServiceProvider`, register Core services and
  ViewModels. Resolve `MainWindow` from DI. Verify app launches.

- [ ] **10. Verify M1 builds and launches**
  Run `dotnet build`. Launch app. Confirm: three columns visible,
  theme toggle works (dark/light). This is the user test gate for M1.

---

## M2 — Markdown Preview Pipeline

- [ ] **11. Implement MarkdownService in MacMD.Core**
  Create `Services/MarkdownService.cs`: sealed singleton, wraps a
  `MarkdownPipeline` built with `UseAdvancedExtensions()`. Single
  method `string ToHtml(string markdown)`.

- [ ] **12. Create EditorView and EditorViewModel**
  In MacMD.Win, create `Views/EditorView.xaml` with a `TextBox`
  (AcceptsReturn, monospaced font, themed colors). Create
  `ViewModels/EditorViewModel.cs` with `[ObservableProperty] string Content`.
  Place EditorView in the right column of MainWindow.

- [ ] **13. Create PreviewView with WebView2**
  In MacMD.Win, create `Views/PreviewView.xaml` with a `WebView2` control.
  Create `ViewModels/PreviewViewModel.cs`. Add `Web/preview.html` with
  a base HTML template that includes Highlight.js CDN link and
  GitHub-style CSS. The ViewModel receives HTML and calls
  `WebView2.NavigateToString()`.

- [ ] **14. Wire editor-to-preview with debounce**
  Connect `EditorViewModel.Content` changes to `PreviewViewModel` via
  a 300ms debounce (use `DispatcherTimer`). On debounce tick: call
  `MarkdownService.ToHtml()`, pass result to PreviewViewModel.
  Add a toggle button to show/hide preview pane.

- [ ] **15. Verify M2 preview pipeline**
  Run app. Type Markdown in editor. Confirm: preview updates after
  ~300ms pause, code blocks are syntax-highlighted, toggle works.
  User test gate for M2.

---

## M3 — Local Persistence

- [ ] **16. Implement DatabaseService (schema + connection)**
  In MacMD.Core, create `Services/DatabaseService.cs`: sealed class,
  takes `dbPath` in constructor, creates SQLite DB file if missing,
  runs schema migration (CREATE TABLE IF NOT EXISTS for projects,
  documents, tags, document_tags). Expose `SqliteConnection` factory
  method.

- [ ] **17. Implement DocumentStore (CRUD)**
  Create `Services/DocumentStore.cs` implementing `IDocumentStore`
  from DESIGN.md. Methods: `GetAllAsync(limit)`, `GetByIdAsync(id)`,
  `CreateAsync(cmd)`, `UpdateContentAsync(id, content)`, `DeleteAsync(id)`.
  All queries use explicit column lists, parameterized SQL.
  Auto-calculate `WordCount` and `CharacterCount` on write.

- [ ] **18. Implement ProjectStore and TagStore**
  Create `Services/ProjectStore.cs` with `GetAllAsync(limit)`,
  `CreateAsync(name)`, `RenameAsync(id, name)`, `DeleteAsync(id)`.
  Create `Services/TagStore.cs` with `GetAllAsync(limit)`,
  `CreateAsync(name, color)`, `DeleteAsync(id)`. Create
  `Services/DocumentTagStore.cs` for managing the junction table.

- [ ] **19. Create ProjectListView and DocumentListView**
  In MacMD.Win, create `Views/ProjectListView.xaml` with a `ListView`
  bound to projects from `ProjectStore`. Create
  `Views/DocumentListView.xaml` with a `ListView` showing
  `DocumentSummary` items (title, modified date, word count).
  Create corresponding ViewModels.

- [ ] **20. Wire navigation: project -> document list -> editor**
  When a project is selected, `DocumentListViewModel` loads documents
  for that project. When a document is selected, `EditorViewModel`
  loads its content. Auto-save on a 2-second debounce after edit
  (calls `UpdateContentAsync`). Add New Document and New Project
  commands.

- [ ] **21. Verify M3 persistence**
  Run app. Create a project, create documents, edit content, close
  and reopen — data persists. Delete a document — it's gone.
  User test gate for M3.

---

## M4 — Localization Pipeline

- [ ] **22. Create canonical translations JSON**
  In `tools/localization/`, create `translations.json` with initial
  UI string keys (menu items, buttons, labels) and English values.
  Create `languages.json` listing all 38 target locale codes with
  Windows locale mappings (e.g., `zh-Hans` -> `zh-CN`).

- [ ] **23. Write Python .resw generator script**
  Create `tools/localization/generate_resw.py` that reads
  `translations.json` and outputs `Strings/{locale}/Resources.resw`
  XML files for each language. Handle locale code mapping. Initially
  generate English only; other languages get English fallback values
  until translated.

- [ ] **24. Integrate .resw files into MacMD.Win**
  Add generated `Strings/` directory to MacMD.Win project. Use
  `x:Uid` in XAML for all user-facing strings. Verify the app
  displays localized strings from .resw (English initially).

- [ ] **25. Verify M4 localization**
  Run `generate_resw.py`. Build. Confirm strings load from .resw.
  Change Windows language (or test with a second locale) to verify
  resource fallback works. User test gate for M4.

---

## M5 — Export (HTML + PDF)

- [ ] **26. Implement ExportService**
  In MacMD.Core, create `Services/ExportService.cs` with:
  `ExportHtmlAsync(Document, string outputPath)` — wraps MD-to-HTML
  in a full HTML document with inline CSS. In MacMD.Win, add
  `ExportPdfAsync(Document, WebView2, string outputPath)` — loads
  HTML into a hidden WebView2 and calls `PrintToPdfAsync()`.

- [ ] **27. Add export menu commands**
  In MainWindow, add `MenuBar` with File menu: New, Save, Export HTML,
  Export PDF, Exit. Wire commands to `ExportService`. Use file picker
  dialogs for output path selection.

- [ ] **28. Verify M5 export**
  Export a document as HTML — open in browser, verify rendering.
  Export as PDF — open in PDF viewer, verify content and formatting.
  User test gate for M5.

---

## M6 — Polish + Packaging

- [ ] **29. Add app icons and assets**
  Create multi-scale icon assets (scale-100, 150, 200, 400) for app
  icon, taskbar icon, splash screen. Place in `Assets/`. Update
  `Package.appxmanifest` with icon references.

- [ ] **30. Create SettingsView**
  Add `Views/SettingsView.xaml` with: theme selector (dropdown of 10
  themes), font size slider, editor font selector, preview toggle
  default. Persist to `SettingsService`. Wire as a navigation page
  or dialog from MainWindow.

- [ ] **31. Add keyboard shortcuts and command bar**
  Implement keyboard accelerators: Ctrl+N (new doc), Ctrl+S (save),
  Ctrl+Shift+P (toggle preview), Ctrl+F (find), Ctrl+E (export HTML),
  Ctrl+Shift+E (export PDF). Add to MenuBar items.

- [ ] **32. Accessibility pass**
  Add `AutomationProperties.Name` to all interactive controls.
  Verify keyboard navigation works for all views (Tab, Arrow keys).
  Test with Windows Narrator enabled.

- [ ] **33. Add .md file association**
  In `Package.appxmanifest`, declare file type association for `.md`
  and `.markdown`. Handle file activation in `App.xaml.cs` to open
  the file in the editor on launch.

- [ ] **34. MSIX packaging project**
  Add or configure the MSIX packaging. Set `Package.appxmanifest`
  metadata (display name, publisher, description, capabilities).
  Build MSIX for x64 and arm64. Verify `dotnet publish` produces
  installable package.

- [ ] **35. Final build verification**
  Run `dotnet build -c Release` for x64 and arm64. Run all tests.
  Launch from MSIX install. Full walkthrough: create project, write
  markdown, preview, export PDF, switch themes, restart — all data
  persists. User test gate for M6.

---

## Task Count

| Milestone | Tasks | Range |
|-----------|-------|-------|
| M0 Scaffolding | 4 | 1–4 |
| M1 Shell + Theme | 6 | 5–10 |
| M2 Preview | 5 | 11–15 |
| M3 Persistence | 6 | 16–21 |
| M4 Localization | 4 | 22–25 |
| M5 Export | 3 | 26–28 |
| M6 Polish | 7 | 29–35 |
| **Total** | **35** | |
