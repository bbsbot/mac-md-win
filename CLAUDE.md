# Mac MD for Windows (Win32/WinUI) — Project Memory for Claude Code

This repository contains the Windows-native implementation of **Mac MD**, a Markdown editor originally built in SwiftUI/SwiftData for Apple platforms. The goal is to build a **native Windows 10/11 desktop app** that looks and feels extremely close to the SwiftUI app while still respecting Windows conventions.

This file is the primary operating manual for Claude Code agents. Follow it strictly.

## Product Goals (Non-Negotiable)
1. **Cross-platform look**: The Windows UI should resemble the SwiftUI UI strongly (layout, spacing, typography, theme system, preview rendering).
2. **Native Windows**: Must run on Windows 10 + Windows 11 and support x64 and ARM64 (and optionally x86 if practical).
3. **Fast + offline-first**: Local persistence first, cloud sync later.
4. **Localization parity**: Support the same ~38 languages as the Apple app by sharing JSON translation sources and generating Windows resources.
5. **Zero manual coding by user**: Claude Code agents do all coding. The user only tests at defined checkpoints.

## Key Architectural Decision (How to think about this repo)
This is **NOT a code fork** of the SwiftUI app. It is a **sibling implementation**.

We may keep a copy of the Apple project in `/reference/` strictly as read-only reference material for UI/behavior/spec. The Windows app lives in `/src/`.

Shared assets (localization JSON, markdown preview HTML/CSS/JS if applicable) live in `/shared/` or `/tools/`.

## UI Specification (High-level)
The Windows app should replicate the Mac MD core layout:

- **Three-column structure**:
  - Left: Projects / groups / tags / navigation
  - Middle: Document list (title, modified date, metadata like word count)
  - Right: Editor + Preview (side-by-side or toggled)
- **Editor**: Plain text Markdown editing with monospaced font and theming.
- **Preview**: WebView-based rendering with code highlighting (Highlight.js) and GitHub-like styles.
- **Themes**: Terminal-inspired themes (dark + light support), with user-selectable theme.
- **Menus/Commands**: Standard app commands (New, Open, Save/Export PDF/HTML, Find, Toggle Preview, Settings).

## Recommended Windows Tech Stack (Default)
- Language/runtime: **C# on .NET 8**
- UI: **WinUI 3 (Windows App SDK)**
- Markdown parsing: **Markdig** (or equivalent, MIT)
- Preview: **WebView2**
- Local persistence: **SQLite** via `Microsoft.Data.Sqlite`
- MVVM helpers: **CommunityToolkit.Mvvm**
- Packaging: **MSIX** (Store-ready later)

Do not introduce heavy frameworks or nonstandard UI frameworks unless explicitly approved.

## Development Workflow (How agents must work)
Agents must work in **milestones**. Each milestone must result in a buildable app.

### Golden rule
**Never break the build.** If something fails, revert/repair immediately.

### Stop points (User testing gates)
At the end of each milestone, STOP and ask the user to:
- build the solution
- run the app
- confirm basic acceptance criteria

Do not proceed to the next milestone until the user confirms.

### Suggested Milestones (initial)
M1 — App shell + layout
- WinUI 3 app launches
- 3-column UI layout exists with placeholder data
- basic theming (dark/light) works

M2 — Markdown preview pipeline
- editor text box hooked up
- WebView2 renders HTML generated from Markdown
- preview updates with debounce (e.g., 300ms)

M3 — Local persistence
- SQLite DB schema
- CRUD for Projects/Documents/Tags
- list loads from DB at startup

M4 — Localization pipeline (Windows .resw generation)
- share the same JSON translation source
- Python generates `Strings/<locale>/Resources.resw`

M5 — Export (PDF/HTML) (local-only)
- HTML export
- PDF export via WebView2 printing or print-to-PDF

M6 — Polish + Store packaging prep
- icons/assets
- MSIX packaging project
- accessibility basics
- settings, about, privacy policy stub

Cloud sync is explicitly postponed until after local-first is solid.

## Code Organization (Required)
Use a clean, readable structure:

- `/src/MacMD.Win/` — main WinUI 3 app
- `/src/MacMD.Core/` — domain models + services (no UI references)
- `/src/MacMD.Tests/` — optional tests
- `/tools/` — Python scripts for localization and shared tooling
- `/docs/` — specs, milestones, decisions, prompts, screenshots
- `/reference/` — read-only Apple app reference (optional)

## Naming & Style
- Use clear names: `Document`, `Project`, `Tag`, `Snippet`
- Avoid clever abstractions.
- Prefer straightforward services: `DatabaseService`, `MarkdownService`, `ThemeService`, `LocalizationService`.
- Prefer async where needed, but do not overcomplicate concurrency.

## Constraints / Don’ts
- Do not implement cloud sync until explicitly scheduled.
- Do not add paid tooling requirements.
- Do not assume access to Apple frameworks (SwiftData, CloudKit, iCloud).
- Do not add malware-like behavior or anything that trips Store certification.
- Do not create giant “rewrite everything” commits.

## Definition of Done (for any milestone)
A milestone is done only when:
- `dotnet build` succeeds for x64 (and ideally arm64)
- app launches
- key acceptance criteria for that milestone are met
- user has tested and approved

## Reference Context (Apple App)
The original Mac MD app (SwiftUI/SwiftData/CloudKit) includes:
- three-column layout
- live preview with WebView
- export PDF (WebView-based)
- tags/projects/documents
- 38-language localization with Python automation

Use it as inspiration and behavioral reference only.