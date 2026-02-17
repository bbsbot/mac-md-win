#!/usr/bin/env python3
"""
Extract translations from Apple Localizable.xcstrings and generate canonical JSON.
"""
import json
import re
from pathlib import Path
from datetime import date

def to_snake_case(text):
    """Convert text to snake_case identifier."""
    # Remove format specifiers
    text = re.sub(r'%[@lld\d$]+', '', text)
    # Remove special characters, keep letters/numbers/spaces
    text = re.sub(r'[^\w\s-]', '', text)
    # Replace spaces and hyphens with underscores
    text = re.sub(r'[\s-]+', '_', text)
    # Convert to lowercase
    text = text.lower()
    # Remove leading/trailing underscores
    text = text.strip('_')
    return text if text else None

def convert_format_string(text):
    """Convert Apple format specifiers to Windows .NET format."""
    # %lld -> {0}
    text = re.sub(r'%lld', '{0}', text)
    # %@ -> {0}, {1}, etc. (numbered sequentially)
    count = [0]
    def replace_at(match):
        result = f'{{{count[0]}}}'
        count[0] += 1
        return result
    text = re.sub(r'%[@d]', replace_at, text)
    # %1$@, %2$@ -> {0}, {1}
    text = re.sub(r'%(\d+)\$[@d]', lambda m: f'{{{int(m.group(1)) - 1}}}', text)
    return text

def is_relevant_key(key):
    """Check if a key is relevant for a Markdown editor app."""
    # Skip empty, single char, or punctuation-only keys
    if not key or len(key) <= 1 or key in ['•', '...', '-', '+']:
        return False
    # Skip pure format specifiers
    if re.match(r'^[%@lld\d$\s]+$', key):
        return False
    # Skip abbreviations that are just format codes
    if re.match(r'^%lld[a-z]+$', key):
        return False
    return True

def extract_translations(xcstrings_path):
    """Extract all translations from xcstrings file."""
    print(f"Loading {xcstrings_path}...")
    with open(xcstrings_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    source_language = data.get('sourceLanguage', 'en')
    strings = data.get('strings', {})

    # Collect all locales
    all_locales = set([source_language])
    for key, value in strings.items():
        if isinstance(value, dict) and 'localizations' in value:
            all_locales.update(value['localizations'].keys())

    print(f"Found {len(all_locales)} locales: {sorted(all_locales)}")
    print(f"Found {len(strings)} string keys")

    # Extract relevant strings
    result = {}
    skipped = 0

    for apple_key, value in strings.items():
        if not is_relevant_key(apple_key):
            skipped += 1
            continue

        if not isinstance(value, dict) or 'localizations' not in value:
            # No translations, skip
            skipped += 1
            continue

        # Generate canonical key name
        canonical_key = to_snake_case(apple_key)
        if not canonical_key:
            skipped += 1
            continue

        # Extract translations for all locales
        translations = {}
        localizations = value['localizations']

        # Get English value (source or from localizations)
        if source_language in localizations:
            en_value = localizations[source_language].get('stringUnit', {}).get('value', apple_key)
        else:
            en_value = apple_key

        # Convert format specifiers for English
        translations['en'] = convert_format_string(en_value)

        # Extract all other locales
        for locale, loc_data in localizations.items():
            if locale == source_language:
                continue
            if isinstance(loc_data, dict) and 'stringUnit' in loc_data:
                value_text = loc_data['stringUnit'].get('value', '')
                if value_text:
                    translations[locale] = convert_format_string(value_text)

        # Only include if we have at least English
        if translations:
            result[canonical_key] = translations

    print(f"Extracted {len(result)} relevant strings (skipped {skipped})")
    return result, sorted(all_locales)

def main():
    project_root = Path(__file__).parent.parent
    xcstrings_path = project_root / 'reference' / 'apple' / 'Markdown Editor' / 'Localizable.xcstrings'
    output_path = project_root / 'shared' / 'translations' / 'strings.json'

    # Extract translations
    strings, locales = extract_translations(xcstrings_path)

    # Create output structure
    output = {
        "_meta": {
            "source": "reference/apple/Markdown Editor/Localizable.xcstrings",
            "generated": date.today().isoformat(),
            "description": "Canonical translation strings shared between Apple and Windows builds",
            "locales": locales,
            "string_count": len(strings)
        },
        "strings": strings
    }

    # Ensure output directory exists
    output_path.parent.mkdir(parents=True, exist_ok=True)

    # Write output
    print(f"\nWriting to {output_path}...")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    print(f"Done! Generated {len(strings)} string keys for {len(locales)} locales")

    # Print sample (ASCII-safe)
    print(f"\nSample keys: {list(strings.keys())[:10]}")

if __name__ == '__main__':
    main()
