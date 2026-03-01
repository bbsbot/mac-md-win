# M8 Plan: Mac Parity — Document List, Nav Icons, Preview Layout, Multi-Select, Sort

## Context

M7 (Preferences Parity) is complete. M9 is the old M7 (38-language localization).
This milestone closes the most visible UX gaps between the Mac and Windows versions,
informed by the reference screenshots in `docs/mac-envy/`.

---

## Feature 1 — Document List: Card Layout + Responsive Grid

**Mac behavior (mac-nav-narrow.png, mac-nav-wide.png):**
- Each document is a rounded card with: bold title, 2-3 line preview snippet (gray),
  tag color dots bottom-left, word count bottom-right, gold star if favorited.
- When the list panel is wide enough, cards flow into 2–3 columns automatically.

**Windows implementation:**
- Replace the current plain `ListView` in `DocumentListView` with a `GridView`
  using `ItemsWrapGrid` (already used by ThemePickerView — same pattern).
- `ItemTemplate`: `DataTemplate` → `Border` (CornerRadius=8, BorderThickness=1) containing
  a `Grid` with title `TextBlock` (SemiBold), snippet `TextBlock` (2-line, opacity 0.6),
  and a bottom row with tag dots (`StackPanel` of `Ellipse` elements, up to 3 shown + "+N"
  if more) on the left and word-count + optional star on the right.
- `ItemsWrapGrid.ItemWidth` = 260, `ItemHeight` = 140. When panel is < 320px wide,
  one column fills naturally; wider = 2+ columns.
- `DocumentListViewModel` already has word count and tags; add `FavoriteIcon` computed
  property (gold star `FontIcon` SymbolIcon when `IsFavorite == true`).
- Tag dots: show up to 3 dots (color from `Tag.Color`); if more tags, show a "+N" label.

**Files:**
- `src/MacMD.Win/Views/DocumentListView.xaml` — replace ListView with GridView
- `src/MacMD.Win/ViewModels/DocumentListViewModel.cs` — expose per-item properties

---

## Feature 2 — Left Sidebar: Segoe Fluent Icons

**Mac behavior (mac-nav-narrow.png):**
- Every item in the left sidebar has an SF Symbol icon: document page for All Documents,
  outline star for Favorites, clock for Recent, archive box for Archived.
- Projects show their custom icon (folder variants, lightbulb, star, etc.).
- Tags show a filled color circle (already partially working in Windows).

**Windows implementation (Segoe Fluent Icons via `FontIcon`):**

| Sidebar item   | Segoe Fluent Icon name | Unicode |
|----------------|------------------------|---------|
| All Documents  | `Document`             | \uE8A5  |
| Favorites      | `FavoriteStar`         | \uE734  |
| Recent         | `Recent`               | \uE823  |
| Archived       | `Archive`              | \uE7B8  |
| New Project +  | `Add`                  | \uE710  |
| New Tag +      | `Add`                  | \uE710  |

- Projects: use a `FontIcon` with `Folder` (\uE8B7) for all projects for now.
  (Full icon picker scoped to a future milestone.)
- Tags: replace current colored text with a 10px filled `Ellipse` (tag.Color) + tag name.
  Tags currently show text only; add the dot.
- Add `Documents ⊕` and `Projects ⊕` and `Tags ⊕` section headers with `+` button.
  The `+` buttons trigger existing new-project and new-tag dialogs.

**Files:**
- `src/MacMD.Win/Views/ProjectListView.xaml` — add icons to all nav items

---

## Feature 3 — Preview Layout: Right / Left / Below

**Mac behavior (mac-preview-left-right-top-bottom.png, mac-preview-below.png):**
- Toolbar button (split-view icon) opens a flyout: "Preview on Right",
  "Preview on Left", "Preview Below".
- "Below": editor on top half, preview WebView on bottom half (horizontal split).
- "Left": preview on left column, editor on right.
- "Right": current default (editor left, preview right).

**Windows implementation:**
- Add a `SplitButton` or `Button` with `MenuFlyout` to the main toolbar.
- Three `MenuFlyoutItem` entries; clicking each calls `SetPreviewLayout(layout)`.
- `MainWindow`: the right-side `Grid` currently has 3 columns (editor | splitter | preview).
  Rework it to be a dynamic layout:
  - **Right** (default): 3-column Grid, same as now.
  - **Left**: swap column order (preview col 0, splitter col 1, editor col 2).
  - **Below**: 3-row Grid (editor row 0, splitter row 1, preview row 2).
- Persist chosen layout via `SettingsService.SetString("previewLayout", "right"|"left"|"below")`.
- Restore on startup.
- Segoe MDL2 icon for the button: `DockRight` \uE90C or `FullScreen` \uE740.

**Files:**
- `src/MacMD.Win/MainWindow.xaml` — add toolbar button + dynamic layout Grid
- `src/MacMD.Win/MainWindow.xaml.cs` — `SetPreviewLayout(string)` + restore on load
- `src/MacMD.Win/Services/SettingsService.cs` — new `PreviewLayout` property

---

## Feature 4 — Multi-Select Mode

**Mac behavior (mac-document-batch-multiselect-changes.png):**
- "Select" button in toolbar activates select mode.
- In select mode: document list switches to multi-select; selected items highlight blue.
- Bulk action menu (⋯) appears: Delete N Documents, Move to Project (submenu),
  Apply Tag (submenu).
- "Done" button exits select mode, selection is cleared.

**Windows implementation:**
- Add `SelectMode` bool property to `DocumentListViewModel`.
- In select mode: `GridView.SelectionMode = Multiple`; in normal mode: `Single`.
- Add "Select" `AppBarButton` to `MainWindow` toolbar; toggles `SelectMode`.
  In select mode: button label becomes "Done" and accent-colored.
- Bulk action `AppBarButton` (⋯ icon) appears only when `SelectMode == true`.
  Its `MenuFlyout` contains:
  - "Delete {N} Documents" → calls `DeleteSelectedAsync()`
  - "Move to Project" → submenu lists all projects; calls `MoveSelectedAsync(projectId)`
  - "Apply Tag" → submenu lists all tags with checkmarks; calls `ApplyTagAsync(tagId)`
- `DocumentListViewModel` tracks `SelectedDocumentIds: ObservableCollection<DocumentId>`.
- Wire `GridView.SelectionChanged` → update `SelectedDocumentIds`.

**Files:**
- `src/MacMD.Win/ViewModels/DocumentListViewModel.cs` — SelectMode, SelectedDocumentIds, bulk ops
- `src/MacMD.Win/Views/DocumentListView.xaml/.cs` — GridView SelectionMode binding
- `src/MacMD.Win/MainWindow.xaml/.cs` — Select/Done button, bulk action flyout

---

## Feature 5 — Document Sort

**Mac behavior (mac-document-sort.png):**
- Sort button (↑↓) in toolbar opens flyout: Date Modified (✓ default), Date Created,
  Title, Word Count.

**Windows implementation:**
- Add `SortBy` enum to `DocumentListViewModel`: `DateModified`, `DateCreated`, `Title`, `WordCount`.
- Current `LoadForProjectAsync` etc. return unsorted from DB; add `.OrderBy()` based on `SortBy`.
- `SortBy` default = `DateModified` (matches Mac).
- Add sort `AppBarButton` (↑↓ icon: `Sort` \uE8CB) to `MainWindow` toolbar with `MenuFlyout`.
  Checkmark (`✓`) shows next to current sort.
- Persist via `SettingsService.SetString("documentSort", "dateModified"|...)`.
- Restore on startup.

**Files:**
- `src/MacMD.Win/ViewModels/DocumentListViewModel.cs` — SortBy enum + re-sort on change
- `src/MacMD.Win/MainWindow.xaml/.cs` — sort button + flyout

---

## Suggested Implementation Order

1. **Feature 5 (Sort)** — smallest scope, purely data/VM layer + one toolbar button.
2. **Feature 2 (Nav icons)** — pure XAML, no logic changes, high visual impact.
3. **Feature 3 (Preview layout)** — self-contained Grid rework in MainWindow.
4. **Feature 1 (Document cards)** — replaces ListView; biggest visual payoff.
5. **Feature 4 (Multi-select)** — most complex; depends on card list being in place.

---

## Acceptance Criteria

- Document list renders as cards (title, snippet, tag dots, word count, star).
- Panel wide enough → cards wrap into 2+ columns automatically.
- Sidebar items have Segoe Fluent Icons; tags show colored dot.
- Preview can be repositioned Right / Left / Below via toolbar flyout; choice persists.
- "Select" activates multi-select; bulk Delete / Move / Tag work on selected docs.
- Sort flyout changes document order; choice persists across restarts.
- `dotnet msbuild` clean on ARM64 and x64.
- `git status` clean before merge.
