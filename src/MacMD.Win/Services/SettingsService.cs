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

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MacMD");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

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
    }

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
