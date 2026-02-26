using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.Views;

public sealed partial class PreviewView : UserControl
{
    private const string HtmlTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8"/>
        <style>
          body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                 padding: 16px; line-height: 1.6; color: #e0e0e0; background: #1e1e1e; }
          h1,h2,h3,h4 { color: #ffffff; }
          code { background: #2d2d2d; padding: 2px 6px; border-radius: 3px; }
          pre { background: #2d2d2d; padding: 12px; border-radius: 6px; overflow-x: auto; }
          pre code { padding: 0; }
          blockquote { border-left: 4px solid #444; margin-left: 0; padding-left: 16px; color: #aaa; }
          a { color: #58a6ff; }
          table { border-collapse: collapse; }
          th, td { border: 1px solid #444; padding: 6px 12px; }
        </style>
        </head>
        <body><!-- CONTENT --></body>
        </html>
        """;

    public Microsoft.UI.Xaml.Controls.WebView2 WebView => PreviewWebView;

    public PreviewView()
    {
        this.InitializeComponent();
    }

    public void UpdateHtml(string html)
    {
        var content = string.IsNullOrEmpty(html) ? "" : html;
        PreviewWebView.NavigateToString(HtmlTemplate.Replace("<!-- CONTENT -->", content));
    }
}