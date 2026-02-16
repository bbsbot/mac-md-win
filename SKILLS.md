# SKILLS.md — Required Skills & Competencies for This Project

This file is a checklist of capabilities the agent team must demonstrate.

## Windows App Skills
- WinUI 3 layout composition (Grid, SplitView, NavigationView)
- XAML resource dictionaries for theming
- Windows app lifecycle basics (startup, window activation)
- Input: keyboard shortcuts, focus management, selection patterns

## Markdown + Preview Skills
- Markdown parsing via Markdig (extensions as needed)
- HTML templating for preview
- WebView2 integration:
  - loading local HTML
  - updating content efficiently
  - executing scripts (if needed)
  - printing/Export pipeline later

## Persistence Skills
- SQLite schema design for:
  - Projects
  - Documents (content, title, modified timestamp)
  - Tags + many-to-many mapping (DocumentTags)
- Simple migrations strategy (schema version table)
- Debounced autosave patterns (avoid writing on every keystroke)

## Localization Skills
- Understand canonical translation JSON format
- Generate Windows `.resw` files per locale
- Locale mapping rules (Apple codes vs Windows codes)
- Ensure placeholders/format specifiers survive translations
- Ensure RTL language handling (Arabic/Hebrew) doesn’t break layout

## “Product” Skills (What makes this succeed)
- Milestone planning with stop-and-test gates
- Avoiding overengineering
- Keeping UI stable while internals evolve
- Writing clear docs and status updates

## Non-negotiable Behaviors
- Keep builds green
- Respect the user’s “ZERO CODE” constraint
- Avoid introducing paid services/tools
- Don’t start cloud sync until explicitly scheduled