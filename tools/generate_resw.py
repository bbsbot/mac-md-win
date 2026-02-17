#!/usr/bin/env python3
"""Generate Windows .resw resource files from canonical translations JSON.

Reads shared/translations/strings.json and generates one .resw file per locale
under src/MacMD.Win/Strings/<locale>/Resources.resw.

Usage:
    python generate_resw.py
"""

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
STRINGS_JSON = REPO / "shared" / "translations" / "strings.json"
OUTPUT_DIR = REPO / "src" / "MacMD.Win" / "Strings"

# Map BCP-47 locale codes to Windows resource folder names
LOCALE_MAP = {
    "zh-Hans": "zh-Hans",
    "zh-Hant": "zh-Hant",
    "pt-BR": "pt-BR",
    "es-419": "es-419",
    "es-US": "es-US",
    "fr-CA": "fr-CA",
}

# x:Uid mappings: maps "UidName.Property" to a canonical string key.
# This allows XAML elements with x:Uid="UidName" to be auto-localized.
XUID_MAP = {
    "ProjectsHeader.Text": "projects",
    "NewProjectButton.Content": "new_project",
    "AllDocumentsLink.Content": "all_documents",
    "DocumentsHeader.Text": "documents",
    "NewDocumentButton.Content": "new_document",
    "DeleteButton.Content": "delete_project",
    "EditorHeader.Text": "editor",
    "PreviewHeader.Text": "preview",
    "SettingsHeader.Text": "settings",
    "SearchBox.PlaceholderText": "search_documents",
    "NoDocumentSelected.Text": "no_document_selected",
    "WordCountLabel.Text": "words",
    "CharCountLabel.Text": "characters",
    "FavoritesHeader.Text": "favorites",
    "TagsHeader.Text": "tags",
    "SortByLabel.Text": "sort_by",
    "ExportHtmlItem.Text": "export_as_html",
    "ExportPdfItem.Text": "export_as_pdf",
    "ExportMdItem.Text": "export_as_markdown",
    "AboutItem.Text": "about",
    "CancelButton.Content": "cancel",
    "SaveButton.Content": "save",
    "DoneButton.Content": "done",
}

RESW_HEADER = """\
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
"""

RESW_FOOTER = "</root>\n"


def escape_xml(text: str) -> str:
    """Escape XML special characters."""
    return (text
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
            .replace('"', "&quot;")
            .replace("'", "&apos;"))


def generate_resw(locale: str, entries: dict[str, str]) -> str:
    """Generate .resw XML content for a locale."""
    lines = [RESW_HEADER]
    for key, value in sorted(entries.items()):
        lines.append(f'  <data name="{escape_xml(key)}" xml:space="preserve">')
        lines.append(f"    <value>{escape_xml(value)}</value>")
        lines.append("  </data>")
    lines.append(RESW_FOOTER)
    return "\n".join(lines)


def main():
    if not STRINGS_JSON.exists():
        print(f"ERROR: {STRINGS_JSON} not found. Run extract_translations.py first.", file=sys.stderr)
        sys.exit(1)

    with open(STRINGS_JSON, "r", encoding="utf-8") as f:
        data = json.load(f)

    strings = data.get("strings", {})
    locales = data.get("_meta", {}).get("locales", [])

    if not strings:
        print("ERROR: No strings found in JSON.", file=sys.stderr)
        sys.exit(1)

    print(f"Generating .resw files for {len(locales)} locales from {len(strings)} strings...")

    generated = 0
    for locale in locales:
        # Collect entries for this locale
        entries = {}
        for key, translations in strings.items():
            value = translations.get(locale)
            if value:
                entries[key] = value

        # Add x:Uid entries (UidName.Property -> translated value)
        for uid_key, canonical_key in XUID_MAP.items():
            if canonical_key in entries:
                entries[uid_key] = entries[canonical_key]

        if not entries:
            print(f"  SKIP {locale}: no translations")
            continue

        # Determine output folder name
        folder_name = LOCALE_MAP.get(locale, locale)
        out_dir = OUTPUT_DIR / folder_name
        out_dir.mkdir(parents=True, exist_ok=True)

        resw_content = generate_resw(locale, entries)
        out_file = out_dir / "Resources.resw"
        with open(out_file, "w", encoding="utf-8") as f:
            f.write(resw_content)

        print(f"  {folder_name}: {len(entries)} strings")
        generated += 1

    print(f"\nDone! Generated {generated} .resw files in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
