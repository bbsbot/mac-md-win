using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MacMD.Core.Services;

namespace MacMD.Win.Views;

public sealed partial class PreviewView : UserControl
{
    private readonly MarkdownService _markdownService;

    public PreviewView(MarkdownService markdownService)
    {
        _markdownService = markdownService ?? throw new ArgumentNullException(nameof(markdownService));
        this.InitializeComponent();
    }

    public void UpdatePreview(string markdown)
    {
        var html = _markdownService.ToHtml(markdown);
        // In a real implementation, we would inject the HTML into the WebView2
        // For now, we'll just set a placeholder
        PreviewWebView.NavigateToString($"<html><body>{html}</body></html>");
    }
}