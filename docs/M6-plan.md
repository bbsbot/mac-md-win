# M6 — Feature Parity, Polish & Localization

## Goal
Close the functional gaps between the Mac and Windows versions of Mac MD,
then localize everything in a single pass. The result should be an app that
feels like a real product — not a prototype.

## Gap Analysis (Mac vs Windows as of v0.2.0-alpha)

### Left Column
| Mac | Windows (current) | Status |
|-----|-------------------|--------|
| All Documents / Favorites / Recent / Archived nav | All Documents only | **Missing** |
| Projects with colored circle indicators | Project names, no color | **Missing** |
| Tags section with colored tag chips | No tags UI | **Missing** |

### Middle Column
| Mac | Windows (current) | Status |
|-----|-------------------|--------|
| Search bar ("Search documents") | None | **Missing** |
| Document title + date + word count | Title + word count | **Partial** |
| Content preview snippet (first lines) | None | **Missing** |

### Right Column / Toolbar
| Mac | Windows (current) | Status |
|-----|-------------------|--------|
| Formatting toolbar (bold, italic, list…) | None | **Missing** |
| Breadcrumb showing current filter context | None | **Missing** |

### App-Level
| Mac | Windows (current) | Status |
|-----|-------------------|--------|
| Custom app icon | Default WinUI icon | **Generated .ico, not wired** |
| Splash screen with "don't show again" | None | **Missing** |
| Theme picker (10 themes, swatch grid) | Toggle dark/light only | **Missing** |

---

## Task Breakdown

### Phase 1: Core Feature Gaps (structural work that adds new strings)

#### Task 1 — Wire app icon
- Set `app.ico` as application icon in `MacMD.Win.csproj`
- Verify it shows in taskbar, title bar, and Alt-Tab

#### Task 2 — Tags UI in sidebar
- Add "Tags" section below Projects in `ProjectListView`
- Show tag name + colored circle for each tag
- Clicking a tag filters the document list to documents with that tag
- "New Tag" button with name + color picker dialog
- Delete tag (context menu or button)

#### Task 3 — Navigation filters (Favorites / Recent / Archived)
- Add nav items above Projects: All Documents, Favorites, Recent
- "Favorites" filters to documents where `IsFavorite == true`
- "Recent" filters to documents modified in last 7 days
- Add `IsFavorite` and `IsArchived` fields to Document model if missing
- Toggle favorite from document list (star icon or context menu)

#### Task 4 — Search bar in document list
- Add search TextBox at top of `DocumentListView`
- Filter documents by title and content as user types (debounced)
- Clear button to reset search

#### Task 5 — Document list enhancements
- Show modified date below title (relative: "2 hours ago", "Feb 16")
- Show first ~80 chars of content as preview snippet
- Adjust item template layout to match Mac's vertical stacking

#### Task 6 — Formatting toolbar
- Add toolbar above editor with common Markdown actions:
  Bold, Italic, Heading, Link, List (bulleted), List (numbered),
  Code, Quote, Horizontal Rule
- Each button inserts/wraps Markdown syntax at cursor position
- Use standard icons (SymbolIcon or FontIcon)

### Phase 2: Polish (visual/UX features)

#### Task 7 — Theme picker
- New `ThemePickerView` (UserControl or ContentDialog)
- Grid of color swatches: background color + "Aa" in foreground + theme name
- Selected theme gets accent border
- Wired to `ThemeService.SetTheme()`
- Apply theme to editor background/foreground and preview CSS
- Replace "Toggle Theme" button with "Themes" button that opens picker

#### Task 8 — Splash screen
- New `WelcomeDialog` (ContentDialog)
- App icon, "Mac MD" title, "Modern Markdown Editing" tagline
- 4 feature highlight rows with icons:
  - Three-Column Layout
  - Live Preview
  - Projects & Tags
  - Export HTML/PDF
- CheckBox: "Don't show this again" (checked = don't show)
- "Get Started" button to dismiss
- Persisted via local settings file (`%LOCALAPPDATA%\MacMD\settings.json`)
- Shows on every launch by default until user unchecks

#### Task 9 — Project color indicators
- Add color property to Project model if missing
- Show colored circle next to project name in sidebar
- Color picker when creating/editing a project

### Phase 3: Localization Pass

#### Task 10 — Audit string keys
- List every user-visible string in all XAML and C# files
- Map each to an existing key in `shared/translations/strings.json`
- Identify any new keys needed (from Tasks 2–9 additions)
- Mark Mac-only keys (iCloud, premium, etc.) as unused

#### Task 11 — Add x:Uid bindings everywhere
- Add `x:Uid` to all hardcoded strings in XAML (all views, MainWindow, dialogs)
- Use `LocalizationService.GetString()` for strings set in C# code-behind
- Update `generate_resw.py` XUID_MAP with new entries

#### Task 12 — Add missing keys and regenerate
- Add any new Windows-specific keys to `strings.json`
- For new keys: add English, mark others as needing translation
- Run `generate_resw.py` to regenerate all 23 locale .resw files
- Verify app works in at least one non-English locale

### Phase 4: Verify

#### Task 13 — Build, test, screenshot
- MSBuild x64 Release succeeds
- App launches with icon, splash screen appears
- All features functional: tags, search, favorites, theme picker, export
- Screenshot for comparison with Mac version

---

## Key Files (expected modifications)

### New Files
- `src/MacMD.Win/Views/ThemePickerView.xaml(.cs)` — theme swatch grid
- `src/MacMD.Win/Views/WelcomeDialog.xaml(.cs)` — splash screen
- `src/MacMD.Win/Views/TagListView.xaml(.cs)` — tags sidebar section
- `src/MacMD.Win/Services/SettingsService.cs` — local prefs persistence
- `tools/generate-ico.ps1` — icon generation script (already created)

### Modified Files
- `src/MacMD.Win/MacMD.Win.csproj` — icon reference
- `src/MacMD.Win/MainWindow.xaml(.cs)` — toolbar, layout changes
- `src/MacMD.Win/Views/ProjectListView.xaml(.cs)` — tags section, nav filters, color dots
- `src/MacMD.Win/Views/DocumentListView.xaml(.cs)` — search, date, preview snippet
- `src/MacMD.Win/Views/EditorView.xaml(.cs)` — formatting toolbar
- `src/MacMD.Core/Models/Document.cs` — IsFavorite, IsArchived fields
- `src/MacMD.Core/Models/Project.cs` — Color field
- `src/MacMD.Core/Services/DatabaseService.cs` — schema migration for new fields
- `src/MacMD.Core/Services/DocumentStore.cs` — filter queries (favorites, recent, search)
- `src/MacMD.Core/Services/TagStore.cs` — query by tag
- `src/MacMD.Win/App.xaml.cs` — register new services, show splash
- `tools/generate_resw.py` — updated XUID_MAP
- `shared/translations/strings.json` — new keys

## Order of Operations
1. Wire icon (quick win, builds confidence)
2. Feature gaps (Tasks 2–6) — these add strings
3. Polish (Tasks 7–9) — these also add strings
4. Localization (Tasks 10–12) — one pass after all strings exist
5. Verify (Task 13)

## Notes
- Each task should result in a buildable app (golden rule)
- User tests at end of each phase, not each task
- If a feature introduces something novel for Windows that Mac doesn't have,
  document it — we may want to back-port to the Mac version
