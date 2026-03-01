using System.Diagnostics;
using MacMD.Core.Models;
using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.Views;

public sealed partial class PreviewView : UserControl
{
    private string _bgColor = "#1e1e1e";
    private string _fgColor = "#e0e0e0";
    private string _headingColor = "#ffffff";
    private string _codeBgColor = "#2d2d2d";
    private string _borderColor = "#444";
    private string _linkColor = "#58a6ff";
    private string _quoteColor = "#aaa";
    private double _previewFontSize = 16.0;

    public Microsoft.UI.Xaml.Controls.WebView2 WebView => PreviewWebView;

    public PreviewView()
    {
        this.InitializeComponent();
        PreviewWebView.NavigationStarting += OnNavigationStarting;
    }

    private void OnNavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        // Allow internal content loads (about:blank and data: URIs)
        if (args.Uri is null || args.Uri.StartsWith("about:") || args.Uri.StartsWith("data:"))
            return;

        // External link — cancel navigation and open in default browser
        args.Cancel = true;
        Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
    }

    public void ApplyFontSize(double size)
    {
        _previewFontSize = size;
    }

    public void ApplyTheme(ColorTheme theme)
    {
        _bgColor = theme.Background;
        _fgColor = theme.Foreground;
        _headingColor = theme.IsDark ? "#ffffff" : "#000000";
        _linkColor = theme.Blue;

        // Derive code background: slightly lighter/darker than main background
        _codeBgColor = theme.IsDark
            ? LightenColor(theme.Background, 20)
            : DarkenColor(theme.Background, 15);
        _borderColor = theme.IsDark ? "#444444" : "#cccccc";
        _quoteColor = theme.IsDark ? "#aaaaaa" : "#666666";
    }

    public void UpdateHtml(string html)
    {
        var content = string.IsNullOrEmpty(html) ? "" : html;
        var template = $$"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8"/>
            <style>
              body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                     font-size: {{_previewFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}}px;
                     padding: 16px; line-height: 1.6; color: {{_fgColor}}; background: {{_bgColor}}; }
              h1,h2,h3,h4 { color: {{_headingColor}}; }
              code { background: {{_codeBgColor}}; padding: 2px 6px; border-radius: 3px; }
              pre { background: {{_codeBgColor}}; padding: 12px; border-radius: 6px; overflow-x: auto; }
              pre code { padding: 0; }
              blockquote { border-left: 4px solid {{_borderColor}}; margin-left: 0; padding-left: 16px; color: {{_quoteColor}}; }
              a { color: {{_linkColor}}; }
              table { border-collapse: collapse; }
              th, td { border: 1px solid {{_borderColor}}; padding: 6px 12px; }
            </style>
            </head>
            <body>{{content}}</body>
            </html>
            """;
        PreviewWebView.NavigateToString(template);
    }

    private static string LightenColor(string hex, int amount)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#2d2d2d";
        int r = Math.Min(255, int.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber) + amount);
        int g = Math.Min(255, int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber) + amount);
        int b = Math.Min(255, int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber) + amount);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string DarkenColor(string hex, int amount)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#cccccc";
        int r = Math.Max(0, int.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber) - amount);
        int g = Math.Max(0, int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber) - amount);
        int b = Math.Max(0, int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber) - amount);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
