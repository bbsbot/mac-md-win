using CommunityToolkit.Mvvm.ComponentModel;
using MacMD.Core.Services;
using MacMD.Win.Services;

namespace MacMD.Win.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private string _editorFontFamily;

    [ObservableProperty]
    private double _editorFontSize;

    [ObservableProperty]
    private double _previewFontSize;

    [ObservableProperty]
    private string _selectedTheme;

    public SettingsViewModel(SettingsService settings, ThemeService themeService)
    {
        _settings = settings;
        _themeService = themeService;

        _editorFontFamily = settings.EditorFontFamily;
        _editorFontSize   = settings.EditorFontSize;
        _previewFontSize  = settings.PreviewFontSize;
        _selectedTheme    = settings.SelectedTheme;
    }

    partial void OnEditorFontFamilyChanged(string value)
        => _settings.EditorFontFamily = value;

    partial void OnEditorFontSizeChanged(double value)
        => _settings.EditorFontSize = value;

    partial void OnPreviewFontSizeChanged(double value)
        => _settings.PreviewFontSize = value;

    partial void OnSelectedThemeChanged(string value)
    {
        _settings.SelectedTheme = value;
        _themeService.SetTheme(value);
    }

    // ── Editor font size helpers (step +/- 1, clamped 8–36) ──────────────

    public void IncrementEditorFontSize()
    {
        if (EditorFontSize < 36) EditorFontSize += 1;
    }

    public void DecrementEditorFontSize()
    {
        if (EditorFontSize > 8) EditorFontSize -= 1;
    }

    // ── Preview font size helpers (step +/- 1, clamped 10–32) ────────────

    public void IncrementPreviewFontSize()
    {
        if (PreviewFontSize < 32) PreviewFontSize += 1;
    }

    public void DecrementPreviewFontSize()
    {
        if (PreviewFontSize > 10) PreviewFontSize -= 1;
    }
}
