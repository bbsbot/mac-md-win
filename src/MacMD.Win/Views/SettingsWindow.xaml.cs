using MacMD.Core.Models;
using MacMD.Core.Services;
using MacMD.Win.Services;
using MacMD.Win.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MacMD.Win.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    private readonly ThemeService _themeService;
    private bool _initializing;

    public event Action<ColorTheme>? ThemeChanged;

    public SettingsWindow(SettingsViewModel vm, ThemeService themeService)
    {
        this.InitializeComponent();
        _vm = vm;
        _themeService = themeService;

        AppWindow.Resize(new Windows.Graphics.SizeInt32(560, 480));
        AppWindow.Title = "Settings";

        this.Activated += OnFirstActivated;

        // About
        VersionValue.Text   = AppVersion.Version;
        BuildValue.Text     = AppVersion.Build;
        DeveloperValue.Text = AppVersion.Developer;

        // Themes — always in the visual tree, safe to call now
        ThemePicker.Initialize(_themeService);
        ThemePicker.ThemeSelected += OnThemePickerThemeSelected;

        // Editor
        _initializing = true;
        InitFontFamilyCombo();
        UpdateEditorFontSizeLabel();
        UpdatePreviewFontSizeLabel();
        _initializing = false;

        UpdateSampleText();

        _vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsViewModel.EditorFontSize):
                    UpdateEditorFontSizeLabel();
                    UpdateSampleText();
                    break;
                case nameof(SettingsViewModel.EditorFontFamily):
                    UpdateSampleText();
                    break;
                case nameof(SettingsViewModel.PreviewFontSize):
                    UpdatePreviewFontSizeLabel();
                    break;
            }
        };

        // Select first sidebar item
        SidebarList.SelectedIndex = 0;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        this.Activated -= OnFirstActivated;
        var theme = _themeService.CurrentTheme;
        if (Content is FrameworkElement root)
            root.RequestedTheme = theme.IsDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    // ── Sidebar selection ─────────────────────────────────────────────────

    private void OnSidebarSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SidebarList.SelectedItem is ListBoxItem item && item.Tag is string tag)
            ShowPanel(tag);
    }

    private void ShowPanel(string tag)
    {
        EditorPanel.Visibility  = tag == "Editor"  ? Visibility.Visible : Visibility.Collapsed;
        PreviewPanel.Visibility = tag == "Preview" ? Visibility.Visible : Visibility.Collapsed;
        ThemesPanel.Visibility  = tag == "Themes"  ? Visibility.Visible : Visibility.Collapsed;
        SyncPanel.Visibility    = tag == "Sync"    ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility   = tag == "About"   ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Font Family ───────────────────────────────────────────────────────

    private void InitFontFamilyCombo()
    {
        var key = _vm.EditorFontFamily;
        for (int i = 0; i < FontFamilyCombo.Items.Count; i++)
        {
            if (FontFamilyCombo.Items[i] is ComboBoxItem item && item.Tag is string tag && tag == key)
            {
                FontFamilyCombo.SelectedIndex = i;
                return;
            }
        }
        FontFamilyCombo.SelectedIndex = 0;
    }

    private void OnFontFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;
        if (FontFamilyCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            _vm.EditorFontFamily = tag;
    }

    // ── Editor font size ──────────────────────────────────────────────────

    private void OnEditorFontDec(object sender, RoutedEventArgs e) => _vm.DecrementEditorFontSize();
    private void OnEditorFontInc(object sender, RoutedEventArgs e) => _vm.IncrementEditorFontSize();
    private void UpdateEditorFontSizeLabel() => EditorFontSizeLabel.Text = $"{_vm.EditorFontSize:0} pt";

    // ── Preview font size ─────────────────────────────────────────────────

    private void OnPreviewFontDec(object sender, RoutedEventArgs e) => _vm.DecrementPreviewFontSize();
    private void OnPreviewFontInc(object sender, RoutedEventArgs e) => _vm.IncrementPreviewFontSize();
    private void UpdatePreviewFontSizeLabel() => PreviewFontSizeLabel.Text = $"{_vm.PreviewFontSize:0} pt";

    // ── Sample text ───────────────────────────────────────────────────────

    private void UpdateSampleText()
    {
        SampleText.FontFamily = FontFamilyKeyToObject(_vm.EditorFontFamily);
        SampleText.FontSize   = _vm.EditorFontSize;
    }

    // ── Theme picker ──────────────────────────────────────────────────────

    private void OnThemePickerThemeSelected(ColorTheme theme)
    {
        _vm.SelectedTheme = theme.Name;
        ThemeChanged?.Invoke(theme);
    }

    // ── Font family key → FontFamily object (also used by EditorView) ─────

    internal static FontFamily FontFamilyKeyToObject(string key)
    {
        return key switch
        {
            "sans-serif" => new FontFamily("Segoe UI, Arial"),
            "serif"      => new FontFamily("Georgia, Times New Roman"),
            "rounded"    => new FontFamily("Bahnschrift"),
            _            => new FontFamily("Cascadia Code, Consolas, Courier New"),
        };
    }
}
