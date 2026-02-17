using Microsoft.Windows.ApplicationModel.Resources;

namespace MacMD.Win.Services;

/// <summary>
/// Provides access to localized strings from .resw resource files.
/// Uses the Windows App SDK ResourceLoader which automatically picks
/// the correct locale based on the user's system settings.
/// </summary>
public sealed class LocalizationService
{
    private readonly ResourceLoader _loader;

    public LocalizationService()
    {
        _loader = new ResourceLoader();
    }

    /// <summary>
    /// Get a localized string by key name.
    /// Returns the key itself if the resource is not found.
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            var value = _loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// Get a localized string with format arguments.
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
