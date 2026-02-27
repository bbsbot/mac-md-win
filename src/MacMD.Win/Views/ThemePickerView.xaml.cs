using MacMD.Core.Models;
using MacMD.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace MacMD.Win.Views;

public sealed partial class ThemePickerView : UserControl
{
    private ThemeService? _themeService;

    public event Action<ColorTheme>? ThemeSelected;

    public ThemePickerView()
    {
        this.InitializeComponent();
    }

    public void Initialize(ThemeService themeService)
    {
        _themeService = themeService;
        BuildSwatches();
    }

    private void BuildSwatches()
    {
        if (_themeService is null) return;

        ThemeGrid.Items.Clear();
        foreach (var theme in _themeService.GetThemes())
        {
            var swatch = CreateSwatch(theme);
            ThemeGrid.Items.Add(swatch);
        }

        // Select current theme
        var currentName = _themeService.CurrentTheme.Name;
        for (int i = 0; i < ThemeGrid.Items.Count; i++)
        {
            if (ThemeGrid.Items[i] is FrameworkElement fe && fe.Tag is string name && name == currentName)
            {
                ThemeGrid.SelectedIndex = i;
                break;
            }
        }
    }

    private static FrameworkElement CreateSwatch(ColorTheme theme)
    {
        var bgColor = ParseColor(theme.Background);
        var fgColor = ParseColor(theme.Foreground);

        var panel = new StackPanel
        {
            Tag = theme.Name,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        // Color preview rectangle with "Aa" text
        var previewGrid = new Grid
        {
            Width = 80,
            Height = 40,
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(bgColor),
            BorderBrush = new SolidColorBrush(Colors.Gray),
            BorderThickness = new Thickness(1),
        };

        var aaText = new TextBlock
        {
            Text = "Aa",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontFamily = new FontFamily("Cascadia Code,Consolas"),
            Foreground = new SolidColorBrush(fgColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // ANSI color dots
        var dotPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 6, 4),
        };
        dotPanel.Children.Add(CreateDot(theme.Red));
        dotPanel.Children.Add(CreateDot(theme.Green));
        dotPanel.Children.Add(CreateDot(theme.Blue));

        previewGrid.Children.Add(aaText);
        previewGrid.Children.Add(dotPanel);
        panel.Children.Add(previewGrid);

        // Theme name
        var nameText = new TextBlock
        {
            Text = theme.Name,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 80,
        };
        panel.Children.Add(nameText);

        return panel;
    }

    private static Ellipse CreateDot(string hexColor)
    {
        return new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = new SolidColorBrush(ParseColor(hexColor)),
        };
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }
        return Colors.Gray;
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeGrid.SelectedItem is FrameworkElement fe
            && fe.Tag is string name
            && _themeService is not null)
        {
            _themeService.SetTheme(name);
            ThemeSelected?.Invoke(_themeService.CurrentTheme);
        }
    }
}
