# M7: Parity — Localization

## Goal
Bring the Windows app to full 38-language parity with the Mac app's localization, upgrading from the current 23-locale set.

## Context
- The Mac app supports ~38 languages via JSON translation sources
- The Windows app currently has 23 locales generated as `.resw` files
- Python tooling in `tools/` generates `.resw` from shared JSON
- The localization pipeline is already working — M7 is about expanding coverage and ensuring all UI strings are localized

## Tasks

### Phase 1: Audit & Expand Language Coverage
1. Inventory the Mac app's 38 supported languages (check `reference/` or shared JSON)
2. Identify the 15+ missing locales in the Windows app
3. Add missing translation JSON sources (or generate stubs)
4. Run the `.resw` generation tool for all 38 locales
5. Verify generated files are valid and placed correctly

### Phase 2: Ensure All UI Strings Are Localizable
1. Audit all hardcoded strings in XAML and code-behind files
2. Replace any remaining hardcoded strings with `x:Uid` references
3. Ensure context menu text (Rename, Delete, Archive, etc.) uses localized strings
4. Ensure dialog text (ContentDialog titles, buttons) uses localized strings
5. Ensure toolbar tooltips use localized strings

### Phase 3: Verify & Test
1. Build and verify no missing resource warnings
2. Test with at least 2-3 non-English locales to confirm rendering
3. Verify RTL layout basics for Arabic/Hebrew if included

## Acceptance Criteria
- All 38 locales from the Mac app have corresponding `.resw` files
- All user-visible strings in the app are localized (no hardcoded English in XAML/code)
- `dotnet build` / MSBuild succeeds with all locale resources
- App launches and displays correctly in at least one non-English locale

## Known Issues to Carry Forward
- Toolbar insert-at-cursor bug (lists/quotes/headings) — tracked separately, not M7 scope
