

# Porting Mac MD to Win32 — A Comprehensive Strategy Guide

I've thoroughly analyzed your project documents — CLAUDE.md, IDEA.md, Journal.md, and the localization README.md — along with your [GitHub repository](https://github.com/bbsbot/markdown-editor-win32). This is an ambitious but absolutely doable project. Here's everything you need to know, organized around your 8 requirements, with honest assessments of risks and unknowns.

---

## 1. Matching the SwiftUI UI on Win32

Your Mac MD app has a very specific visual architecture: a **three-column NavigationSplitView** (Projects → Documents → Editor+Preview), 10 Terminal-inspired color themes, Highlight.js syntax highlighting in a WebView-based preview, and responsive layouts that collapse on smaller screens.

**Recommended Framework: C# + WinUI 3 (Windows App SDK)**

This is your strongest option for replicating the SwiftUI experience on Windows, and here's why. WinUI 3 uses **XAML**, which is a declarative UI language conceptually very similar to SwiftUI. Where SwiftUI has `NavigationSplitView` with three columns, WinUI 3 has `NavigationView` with a pane structure and you can compose a three-column layout using nested `SplitView` controls. The mapping is surprisingly close:

| SwiftUI Concept | WinUI 3 Equivalent |
|---|---|
| `NavigationSplitView` | `NavigationView` + `SplitView` |
| `List` with selection | `ListView` with `SelectionMode` |
| `TextEditor` | `RichEditBox` or custom `TextBox` |
| `WKWebView` (preview) | `WebView2` (Chromium-based) |
| `@State` / `@Binding` | `x:Bind` / `DependencyProperty` |
| `.searchable()` | `AutoSuggestBox` |
| `Commands` (menu bar) | `MenuBar` + `MenuBarItem` |
| `.toolbar` | `CommandBar` / `AppBarButton` |
| `@AppStorage` | `ApplicationData.Current.LocalSettings` |
| `ContentUnavailableView` | Custom empty-state `UserControl` |

For the **Markdown live preview**, your current architecture using WKWebView with Highlight.js translates almost perfectly. Windows 10 and 11 ship with **WebView2** (Microsoft Edge/Chromium-based), which is functionally equivalent to WKWebView. You can inject the same HTML/CSS/JS rendering pipeline, including your Highlight.js integration with the GitHub light/dark themes and CDN delivery from cdnjs.cloudflare.com. The preview pane will look *identical* because it's rendering the same HTML.

For the **editor pane**, you'll want to build a custom text editing surface. WinUI 3's `RichEditBox` gives you a solid foundation, and you can layer syntax-aware coloring on top. It won't be pixel-identical to SwiftUI's `TextEditor`, but with careful styling of fonts, line spacing, and the color theme system, it can feel like the same app.

For the **color themes**, your 10 Terminal-inspired themes (Basic, Grass, Homebrew, Man Page, Novel, Ocean, Pro, Red Sands, Silver Aerogel, Solid Colors) are stored as `ColorTheme` structs with background, foreground, and 8 ANSI colors. These translate directly to WinUI 3 `ResourceDictionary` theme definitions. You can define each theme as a XAML resource dictionary and swap them at runtime.

---

## 2. Native Windows 10/11, x86 and ARM Support

WinUI 3 with the Windows App SDK natively supports **Windows 10 version 1809 (build 17763) and later**, including Windows 11, across **x86, x64, and ARM64** architectures. This is exactly what you need.

Your build will produce separate binaries for each architecture, but this is handled automatically by the build system. When you package for the Microsoft Store (more on that later), you create an MSIX bundle that contains all architecture variants, and Windows automatically installs the correct one.

**Key considerations for ARM support:** Windows on ARM runs x86/x64 apps through emulation, but native ARM64 binaries are significantly faster and more battery-efficient. By targeting ARM64 natively in your build configuration, you get first-class performance on Surface Pro X, Surface Pro 11, and other ARM Windows devices. The .NET 8 runtime and WinUI 3 both have full ARM64 support.

---

## 3. Backend Sync — Replacing SwiftData + CloudKit

This is the hardest part of your entire port, and it requires modifying the Apple-side app too. Let me be very direct about the challenge: **CloudKit is Apple-only, and SwiftData is Apple-only.** There is no way for a Windows app to natively speak to either. You need a cross-platform sync layer.

**Recommended Approach: Firebase Firestore as the Universal Sync Backend**

Here's the architecture I'd recommend, broken into three layers:

**Layer 1 — Local Database (per platform):**
On Windows, use **SQLite** via `Microsoft.Data.Sqlite` (MIT licensed, included with .NET). On Apple, you keep **SwiftData** as-is for the local persistence layer. Both platforms store documents locally for offline-first operation.

**Layer 2 — Cloud Sync (shared):**
**Firebase Firestore** becomes your single source of truth in the cloud. It provides real-time sync, offline caching, conflict resolution, and cross-platform SDKs. The free Spark plan gives you 1 GiB storage, 50K reads/day, 20K writes/day — more than enough for a document editor.

Your Firestore data model would map directly from your SwiftData schema:

```
Collection: users/{userId}/documents
  - id: String
  - title: String
  - content: String (Markdown text)
  - projectId: String (reference)
  - tagIds: [String] (array of references)
  - wordCount: Int
  - characterCount: Int
  - isFavorite: Bool
  - createdAt: Timestamp
  - modifiedAt: Timestamp

Collection: users/{userId}/projects
  - id: String
  - name: String
  - createdAt: Timestamp

Collection: users/{userId}/tags
  - id: String
  - name: String
  - color: String
```

**Layer 3 — Sync Engine (per platform):**
Each platform runs a sync engine that reconciles local changes with Firestore. On Apple, you'd add the **Firebase iOS SDK** alongside your existing SwiftData layer — SwiftData remains the local persistence, but changes are pushed to/pulled from Firestore. On Windows, the .NET app uses the **Firebase Admin SDK for .NET** or the **Firebase REST API** to do the same.

**The migration path for your Apple app:**

This is the part that requires care. You have two strategies:

*Strategy A — Dual-write:* Keep CloudKit for Apple-to-Apple sync (it works great, as your Journal.md confirms), and add Firestore as an additional sync target. This means Apple devices sync via both CloudKit and Firestore, while Windows syncs via Firestore only. Downside: two sync systems to maintain, potential for divergence.

*Strategy B — Full migration to Firestore:* Remove CloudKit integration, replace it entirely with Firestore on all platforms. This is cleaner architecturally but means ripping out working code. The Firebase iOS SDK handles offline caching and sync, so you don't lose functionality.

**My recommendation is Strategy B** for long-term sanity, but Strategy A is fine for an initial release if you want to minimize changes to the shipping Apple app.

**Alternative worth considering: Supabase**
If you prefer open-source infrastructure over Google's, **Supabase** (PostgreSQL-based, MIT licensed) offers real-time subscriptions, a REST API, and client libraries for both Swift and .NET. You can self-host it or use their free cloud tier. The trade-off is less mature mobile SDKs compared to Firebase.

---

## 4. Sharing Localizations Across 38 Languages

Your localization strategy is actually one of the cleanest parts of this port. Based on your README.md, you already have a Python pipeline that manages translations with `languages.json`, `keys.json`, and the Localizable.xcstrings format. You support 38 languages (ar, cs, da, de, el, en, es, fi, fr, he, hi, hr, hu, id, it, ja, ko, ms, nb, nl, pl, pt-BR, pt-PT, ro, ru, sk, sv, th, tr, uk, vi, zh-Hans, zh-Hant, zh-HK, and more).

**The architecture for shared localization:**

Create a **canonical JSON format** as your single source of truth. This is essentially what your `keys.json` and the translation data already represent. The structure would be:

```json
{
  "key_name": {
    "en": "English text",
    "de": "German text",
    "ja": "Japanese text",
    ...
  }
}
```

Then extend your Python pipeline with **output adapters:**

**For Apple (existing):** Your current pipeline already generates `Localizable.xcstrings`. Keep this as-is.

**For Win32 (.NET/WinUI 3):** Generate `.resw` files (one per language). The format is simple XML:
```xml
<data name="key_name" xml:space="preserve">
  <value>Translated text</value>
</data>
```
Each language gets its own file at `Strings/{locale}/Resources.resw`. WinUI 3 loads these automatically based on the user's Windows language setting.

**For the website (existing):** You mentioned you already have a pipeline for multi-lingual website content. Keep this too.

Your Python script essentially becomes a **polyglot localization compiler**: one JSON input, multiple platform-specific outputs. The translation engine (macOS Translation framework) stays on the Mac side, but the *distribution* of translations is platform-agnostic.

**Important detail for Win32 localization:** Windows uses different locale codes than Apple in some cases. Your Python script will need a mapping table: `zh-Hans` → `zh-CN`, `zh-Hant` → `zh-TW`, `pt-BR` → `pt-BR` (same), `nb` → `nb-NO`, etc. This is a small but critical detail.

---

## 5. Making the Win32 App Visually Appealing and "Native"

WinUI 3 gives you **Fluent Design System** out of the box: Mica material (the translucent background that shows your desktop wallpaper through the title bar), Acrylic blur effects, smooth animations, rounded corners on Windows 11, and adaptive color schemes that respect the user's light/dark mode and accent color settings.

Here's how to make it feel both "cross-platform with Mac MD" and "native to Windows":

**Match these from the SwiftUI app:** The three-column layout structure, the same Markdown preview rendering (identical HTML/CSS/JS pipeline via WebView2), the same 10 color themes for the editor, the same typography choices (use system fonts: Segoe UI for UI text, Cascadia Code for the editor — these are the Windows equivalents of SF Pro and SF Mono), and the same iconography concepts (using Segoe Fluent Icons, which map well to SF Symbols).

**Embrace these Windows-native features:** Mica/Acrylic material in the title bar and sidebar, Windows 11 rounded corners and snap layouts, system notification toasts for sync status, jump lists on the taskbar (recent documents), dark/light mode that follows Windows settings, and the Windows share contract for exporting documents.

**The result** should feel like "the same app, at home on Windows" — not a foreign macOS app awkwardly running on Windows, and not a generic cross-platform app that looks native nowhere.

---

## 6. Free and OSS Toolchain on Windows 10

Here's your complete development environment, all free and open source or free-to-use:

**Core toolchain:**
The **.NET 8 SDK** (MIT license, fully OSS) includes the C# compiler (Roslyn, also MIT), MSBuild, and the `dotnet` CLI. Download from dotnet.microsoft.com. This is your compiler, build system, and runtime — all in one install.

**IDE/Editor:**
**Visual Studio Code** (free, with the C# Dev Kit extension) or **Visual Studio 2022 Community Edition** (free for individuals and small teams, though the binary itself isn't OSS, the license is free). For a Claude Code-driven workflow where you're not writing code yourself, VS Code with the CLI is more than sufficient.

**Additional tools:**
**Git for Windows** (GPLv2) for source control. **Python 3** (PSF license) for your localization pipeline. **Windows App SDK** (MIT license) for WinUI 3. **WebView2 Runtime** (already installed on Windows 10/11, free).

**Build command:** Once everything is set up, building is as simple as:
```
dotnet build -c Release -r win-x64
dotnet build -c Release -r win-arm64
```

**MSIX packaging** (for Store distribution) is handled by the `Windows Application Packaging Project` template included with the Windows App SDK. No paid tools needed.

---

## 7. Claude Code Automation — Zero Manual Coding

This is actually very feasible given your setup (128 GB RAM, isolated Windows 10 machine, dedicated accounts). Here's how to structure the Claude Code workflow:

**Phase 1 — Environment Bootstrap:**
Have Claude Code install the .NET 8 SDK, Windows App SDK, Git, Python 3, and VS Code. Create the solution structure with `dotnet new` commands. Set up the GitHub repo with proper `.gitignore` for .NET projects.

**Phase 2 — Data Layer:**
Build the SQLite-backed data models mirroring your SwiftData schema (Document, Project, Tag, Snippet). Implement the Firebase/Supabase sync engine. This is self-contained and testable.

**Phase 3 — UI Shell:**
Create the three-column layout with WinUI 3 XAML. Implement navigation, document list, and editor pane. Add WebView2 for the preview pane. Port the HTML/CSS/JS rendering pipeline from your SwiftUI app's WKWebView implementation.

**Phase 4 — Features:**
Keyboard shortcuts, search, multi-select with bulk operations, settings page with theme selection, PDF export (using Windows' built-in PDF printing), splash screen.

**Phase 5 — Localization:**
Extend the Python pipeline to generate `.resw` files. Integrate all 38 languages.

**Phase 6 — Store Packaging:**
Create MSIX package, generate screenshots, prepare Store listing.

**Practical tips for Claude Code swarming:**
Each phase can be a separate agent session with clear inputs and outputs. Phase 2 and Phase 3 can run in parallel since they're independent (data layer vs. UI shell). Your 128 GB RAM machine can easily handle multiple agent sessions. Keep each session focused — one agent per feature area prevents conflicts. Use Git branches (matching your existing convention: `feature/win32-data-layer`, `feature/win32-ui-shell`, etc.) and merge sequentially.

**The honest caveat:** While Claude Code can generate all the code, you'll still need to *run* the builds, test the results visually, and provide feedback. "Zero code" is realistic. "Zero involvement" is not — you're the product owner, tester, and quality gate. Your Journal.md already documents this workflow perfectly: "Make changes → Build → STOP and ask user to test → Only after user confirms → Then proceed."

---

## 8. What You Don't Know You Don't Know

This is where I want to be most helpful. Here are the significant unknowns and risks:

**Microsoft Store Distribution:**
You need a **Microsoft Partner Center developer account**. It costs approximately 19 USD (one-time fee for individuals). Registration requires a Microsoft account (your separate Gmail won't work directly — you'll need a Microsoft/Outlook account, which is free to create). The Store submission process involves uploading your MSIX package, providing screenshots (at least 1 per supported resolution), writing a description, setting age ratings (you'll go through the IARC questionnaire — a Markdown editor will get an "Everyone" rating), and providing a privacy policy URL (required even if you collect no data — GitHub Pages works fine for hosting this).

**Code Signing:**
For Store distribution, Microsoft handles code signing through the Store upload process — your app gets signed with Microsoft's certificate automatically. For sideloading (distributing outside the Store), you'd need your own code signing certificate, which costs money. Stick with Store-only distribution initially.

**Windows App Certification Kit (WACK):**
Before submitting to the Store, you must pass WACK testing. This automated tool checks for API compatibility, performance baselines, security requirements, and proper manifest declarations. Common failures include: using restricted APIs, missing capability declarations, and not handling suspend/resume lifecycle properly. Claude Code should build with WACK compliance from the start.

**WebView2 Dependency:**
WebView2 is your Markdown preview engine (equivalent to WKWebView). It's pre-installed on Windows 10 (April 2021 update and later) and all Windows 11 machines. For older Windows 10 installations, you'll need to bundle the WebView2 Evergreen Bootstrapper with your MSIX package — this is a small (~2 MB) download that ensures the runtime is present. This is a detail Claude Code should handle in the packaging phase.

**PDF Export is Different on Windows:**
Your Mac app uses `WKWebView.createPDF()` with the JavaScript-measured height trick you documented in your Journal.md. On Windows, the equivalent is `WebView2.PrintToPdfAsync()` or using the system print dialog with "Microsoft Print to PDF." The JavaScript height measurement approach will still work — you just call it through WebView2's `ExecuteScriptAsync` instead of `evaluateJavaScript`. The PDF output will look slightly different due to font rendering differences between macOS and Windows (ClearType vs. Core Text), but the content will be identical.

**Font Rendering:**
This is subtle but important. macOS and Windows render fonts differently at the subpixel level. Your app will look slightly different in text rendering no matter what you do. The strategy is to use platform-native fonts (Segoe UI / Cascadia Code on Windows, SF Pro / SF Mono on Mac) rather than trying to force macOS fonts onto Windows. The Markdown preview (rendered in WebView2) will also render text slightly differently because it uses the platform's text rendering engine. Users expect this — a "native-feeling" app uses native fonts.

**High DPI / Display Scaling:**
Windows has a complex DPI scaling system, especially on multi-monitor setups where monitors may have different scale factors. WinUI 3 handles most of this automatically, but you need to be aware of it for any custom rendering or image assets. Test at 100%, 125%, 150%, and 200% scaling. Your app icon and any raster assets need to include multiple resolution variants (@1x, @1.5x, @2x, @4x — Windows uses `.scale-100`, `.scale-150`, `.scale-200`, `.scale-400` suffixes).

**File Associations:**
Your Mac app presumably associates with `.md` and `.markdown` files. On Windows, file associations are declared in the MSIX manifest. Users should be able to right-click a `.md` file and "Open with" your app. This is a manifest declaration, not code, but Claude Code needs to include it.

**Auto-Save and File System:**
SwiftData auto-persists to SQLite. On Windows, you'll need to implement auto-save explicitly — debounced writes to SQLite (similar to your 300ms preview debounce documented in Journal.md, but for persistence). Also, Windows has a different file system model: `AppData\Local` for local data, `AppData\Roaming` for roaming data, and the app's **sandboxed local folder** for Store apps. Store apps run in a sandbox similar to iOS — they can't freely access the entire file system without user consent via file pickers.

**Accessibility:**
The Microsoft Store requires basic accessibility compliance. WinUI 3 has good built-in accessibility (narrator support, keyboard navigation, high contrast themes), but you need to ensure all UI elements have proper `AutomationProperties.Name` labels. This is the Windows equivalent of SwiftUI's `.accessibilityLabel()`.

**Testing on ARM:**
You mentioned wanting ARM support. If you don't have a Windows ARM device, you can use **Microsoft's ARM64 virtual machines** available through the Windows Insider program, or test on an ARM-based Azure VM. Alternatively, just ensure your code doesn't use any x86-specific native libraries, and the ARM64 build will work correctly — .NET is fully architecture-independent for managed code.

**Privacy Policy:**
The Microsoft Store requires a privacy policy URL for all apps. Even if your app collects zero data locally, if you integrate Firebase for sync, you're technically transmitting user content to Google's servers. You'll need a privacy policy that covers this. Host it on your GitHub Pages site alongside the Mac MD marketing page you already have.

**Update Cadence:**
Store apps update automatically through the Microsoft Store. You push a new MSIX, it goes through certification (usually 1-3 business days), and users get the update. This is simpler than managing your own update mechanism.

---

## Recommended Project Structure

```
MacMD-Win32/
├── src/
│   ├── MacMD/                          # Main WinUI 3 app project
│   │   ├── Models/
│   │   │   ├── Document.cs
│   │   │   ├── Project.cs
│   │   │   ├── Tag.cs
│   │   │   └── Snippet.cs
│   │   ├── Services/
│   │   │   ├── MarkdownService.cs      # Equivalent to MarkdownManager
│   │   │   ├── DatabaseService.cs      # SQLite local persistence
│   │   │   ├── SyncService.cs          # Firebase/Supabase sync engine
│   │   │   ├── ExportService.cs        # PDF/HTML export
│   │   │   └── ThemeService.cs         # 10 color themes
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml         # Three-column layout
│   │   │   ├── SidebarView.xaml        # Projects list
│   │   │   ├── DocumentListView.xaml   # Documents list
│   │   │   ├── EditorView.xaml         # Text editor
│   │   │   ├── PreviewView.xaml        # WebView2 Markdown preview
│   │   │   ├── SettingsView.xaml       # Preferences
│   │   │   └── WelcomeView.xaml        # First-launch splash
│   │   ├── Assets/                     # Icons, images (multi-scale)
│   │   ├── Strings/                    # Generated .resw files (38 languages)
│   │   │   ├── en/Resources.resw
│   │   │   ├── de/Resources.resw
│   │   │   ├── ja/Resources.resw
│   │   │   └── .../
│   │   ├── Web/                        # HTML/CSS/JS for preview rendering
│   │   │   ├── preview.html
│   │   │   ├── preview.css
│   │   │   └── highlight.min.js
│   │   └── App.xaml                    # App entry point
│   └── MacMD.Package/                  # MSIX packaging project
│       └── Package.appxmanifest
├── tools/
│   ├── localization.py                 # Extended: outputs .resw + .xcstrings
│   └── data/
│       ├── translations.json           # Canonical source of truth
│       ├── languages.json
│       └── keys.json
├── MacMD.sln                           # Solution file
└── README.md
```

---

## Summary of Recommendations

**UI Framework:** C# + WinUI 3 (Windows App SDK) — closest match to SwiftUI's declarative model, native Fluent Design, supports all your target platforms and architectures.

**Sync Backend:** Firebase Firestore (or Supabase if you prefer OSS) — modify both Apple and Windows apps to use a shared cloud backend, replacing CloudKit with a cross-platform solution.

**Localization:** Extend your existing Python pipeline to output `.resw` files for Windows alongside `.xcstrings` for Apple, from a shared canonical JSON source.

**Toolchain:** .NET 8 SDK (free, MIT licensed) + VS Code + Git + Python 3 — entirely free, runs on your Windows 10 x64 machine.

**Distribution:** Microsoft Store via MSIX packaging, approximately 19 USD developer account, automated code signing through Store upload.

**Biggest Risks:** The sync backend migration is the most complex and riskiest part. It touches both the existing shipping Apple app and the new Windows app. Start here, validate it works, then build the UI.

---

This is a substantial project, but the pieces all fit together logically. Your existing architecture documents (especially the Journal.md engineering wisdom) show exactly the kind of disciplined thinking that makes a port like this succeed. The SwiftUI → WinUI 3 mapping is surprisingly clean, and the hardest part — the sync layer — is a well-understood problem with mature solutions.

Would you like me to dive deeper into any specific area — for example, creating a detailed Claude Code task plan with exact prompts for each phase, or working through the Firebase data model and sync conflict resolution strategy in more detail?