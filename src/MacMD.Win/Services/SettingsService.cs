using System.Text.Json;

namespace MacMD.Win.Services;

/// <summary>
/// Simple JSON-based local settings persistence.
/// Stores settings in %LOCALAPPDATA%\MacMD\settings.json.
/// </summary>
public sealed class SettingsService
{
    private readonly string _filePath;
    private Dictionary<string, JsonElement> _data = new();

    public event Action? SettingsChanged;

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MacMD");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    // ── Bool ──────────────────────────────────────────────────────────────

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.True)
            return true;
        if (_data.TryGetValue(key, out el) && el.ValueKind == JsonValueKind.False)
            return false;
        return defaultValue;
    }

    public void SetBool(string key, bool value)
    {
        using var doc = JsonDocument.Parse(value ? "true" : "false");
        _data[key] = doc.RootElement.Clone();
        Save();
        SettingsChanged?.Invoke();
    }

    // ── String ────────────────────────────────────────────────────────────

    public string GetString(string key, string defaultValue = "")
    {
        if (_data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String)
            return el.GetString() ?? defaultValue;
        return defaultValue;
    }

    public void SetString(string key, string value)
    {
        using var doc = JsonDocument.Parse($"\"{JsonEncodedText.Encode(value)}\"");
        _data[key] = doc.RootElement.Clone();
        Save();
        SettingsChanged?.Invoke();
    }

    // ── Double ────────────────────────────────────────────────────────────

    public double GetDouble(string key, double defaultValue = 0.0)
    {
        if (_data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number)
            return el.GetDouble();
        return defaultValue;
    }

    public void SetDouble(string key, double value)
    {
        using var doc = JsonDocument.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _data[key] = doc.RootElement.Clone();
        Save();
        SettingsChanged?.Invoke();
    }

    // ── Typed convenience properties ──────────────────────────────────────

    public string EditorFontFamily
    {
        get => GetString("editorFontFamily", "monospaced");
        set => SetString("editorFontFamily", value);
    }

    public double EditorFontSize
    {
        get => GetDouble("editorFontSize", 14.0);
        set => SetDouble("editorFontSize", value);
    }

    public double PreviewFontSize
    {
        get => GetDouble("previewFontSize", 16.0);
        set => SetDouble("previewFontSize", value);
    }

    public string SelectedTheme
    {
        get => GetString("selectedColorTheme", "Pro");
        set => SetString("selectedColorTheme", value);
    }

    // ── Persistence ───────────────────────────────────────────────────────

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                using var doc = JsonDocument.Parse(json);
                _data = new Dictionary<string, JsonElement>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                    _data[prop.Name] = prop.Value.Clone();
            }
        }
        catch
        {
            _data = new();
        }
    }

    private void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var dict = new Dictionary<string, object>();
        foreach (var (key, el) in _data)
        {
            dict[key] = el.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => el.GetDouble(),
                JsonValueKind.String => el.GetString()!,
                _ => el.ToString(),
            };
        }
        File.WriteAllText(_filePath, JsonSerializer.Serialize(dict, options));
    }
}
