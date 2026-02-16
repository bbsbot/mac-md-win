# Mac MD for Windows

A native Windows Markdown editor built with **WinUI 3** and **.NET 8**, porting the [Mac MD](https://github.com/bbsbot/mac-md) SwiftUI app to Windows 10/11.

**[Download Alpha 1](https://github.com/bbsbot/mac-md-win/releases/tag/v0.1.0-alpha1)** — extract, run `MacMD.Win.exe`, no installer needed.

![Mac MD Screenshot](src/MacMD.Tests/M3_acceptance.png)

---

## What It Does Today

- **Three-column layout** — Projects | Documents | Editor + Live Preview
- **Live Markdown preview** — type in the editor, see rendered HTML update in real-time (300ms debounce) via WebView2
- **Local persistence** — projects, documents, and tags stored in SQLite at `%LOCALAPPDATA%\MacMD\macmd.db`
- **Auto-save** — your work is saved 2 seconds after you stop typing
- **Dark/light theme toggle** — one click to switch
- **10 terminal-inspired color themes** — Pro, Homebrew, Ocean, Red Sands, and more (theme selector UI coming in M6)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | WinUI 3 (Windows App SDK 1.6) |
| Runtime | .NET 8 (C# 12) |
| Markdown | [Markdig](https://github.com/xoofx/markdig) with advanced extensions |
| Preview | WebView2 with dark-themed CSS |
| Persistence | SQLite via `Microsoft.Data.Sqlite` |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Packaging | Self-contained (no runtime install needed) |

## Building From Source

**Requirements:** Visual Studio 2022 Build Tools with UWP workload, .NET 8 SDK

```bash
# Clone
git clone https://github.com/bbsbot/mac-md-win.git
cd mac-md-win

# Build (must use MSBuild, not dotnet build, for WinUI 3)
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe" MacMD.sln -restore -p:Platform=x64

# Run tests (dotnet works for non-WinUI projects)
dotnet test src/MacMD.Tests

# Launch
src\MacMD.Win\bin\x64\Debug\net8.0-windows10.0.19041.0\MacMD.Win.exe
```

## Project Structure

```
mac-md-win/
├── src/
│   ├── MacMD.Core/          # Domain models + services (no UI dependency)
│   │   ├── Models/          # Document, Project, Tag, ColorTheme
│   │   └── Services/        # MarkdownService, DatabaseService, stores
│   ├── MacMD.Win/           # WinUI 3 app
│   │   ├── ViewModels/      # EditorViewModel, ProjectList, DocumentList
│   │   └── Views/           # EditorView, PreviewView, list views
│   └── MacMD.Tests/         # xUnit tests (8 passing)
├── tools/                   # Screenshot tool, build helpers
├── docs/                    # Milestone progress, architecture decisions
└── DESIGN.md                # Full architecture document
```

## Development Progress

See **[PROGRESS.md](PROGRESS.md)** for a detailed milestone-by-milestone breakdown with code highlights.

| Milestone | Status | Description |
|-----------|--------|-------------|
| M0 — Scaffolding | Done | Solution structure, CPM, build tooling |
| M1 — App Shell | Done | Three-column layout, theme toggle, DI |
| M2 — Preview Pipeline | Done | Markdig + WebView2 live preview |
| M3 — Persistence | Done | SQLite CRUD, auto-save, navigation |
| M4 — Localization | Next | 38-language .resw generation |
| M5 — Export | Planned | HTML + PDF export |
| M6 — Polish | Planned | Settings, shortcuts, MSIX packaging |

## How This Project Is Built

This app is built entirely by **Claude Code agents** — AI pair programming from architecture through implementation. The human tests at milestone gates. See [PROGRESS.md](PROGRESS.md) for the full story.

## License

TBD
