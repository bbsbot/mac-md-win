# Fix: Theme Colors Not Rendering Into UI

## Bug
Theme picker changes dark/light mode only. Actual theme Background/Foreground
colors never apply to UI elements. A hardcoded `Background="#1E1E1E"` on the
root Grid in MainWindow.xaml is the current workaround.

## Fix Plan

### 1. MainWindow.xaml — remove hardcoded color
- Remove `Background="#1E1E1E"` from root Grid
- Keep `RequestedTheme="Dark"` as default (will be overridden at runtime)

### 2. MainWindow.xaml.cs — ApplyTheme to set actual colors
- In `ApplyTheme()`, set background on root Grid using theme.Background
- Set foreground on the editor TextBox via EditorView
- Update the preview CSS/HTML to use theme colors (already partially done via MarkdownService)

### 3. EditorView — expose method to apply theme colors
- Add `ApplyTheme(ColorTheme)` to set TextBox Background and Foreground

### 4. MarkdownService — verify preview uses theme colors
- Already regenerates HTML on theme change, just need to verify CSS picks up colors

## Files
- MainWindow.xaml
- MainWindow.xaml.cs
- EditorView.xaml.cs (add ApplyTheme)
- Possibly MarkdownService.cs (verify)
