using MacMD.Core.Services;
using Microsoft.Web.WebView2.Core;

namespace MacMD.Win.Services;

/// <summary>
/// Exports a document as PDF by rendering HTML in a WebView2 and using PrintToPdfAsync.
/// Lives in MacMD.Win because it depends on WebView2/WinUI types.
/// </summary>
public sealed class PdfExportService
{
    private readonly ExportService _exportService;

    public PdfExportService(ExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// Renders the given markdown as HTML, loads it into the WebView2, and prints to PDF.
    /// </summary>
    public async Task ExportPdfAsync(
        string title,
        string markdownContent,
        string outputPath,
        Microsoft.UI.Xaml.Controls.WebView2 webView)
    {
        var htmlBody = new MarkdownService().ToHtml(markdownContent);
        var htmlDocument = _exportService.BuildHtmlDocument(title, htmlBody);

        var tcs = new TaskCompletionSource<bool>();

        void OnNavigationCompleted(Microsoft.UI.Xaml.Controls.WebView2 sender,
            CoreWebView2NavigationCompletedEventArgs args)
        {
            webView.NavigationCompleted -= OnNavigationCompleted;
            tcs.TrySetResult(args.IsSuccess);
        }

        webView.NavigationCompleted += OnNavigationCompleted;
        webView.NavigateToString(htmlDocument);

        var success = await tcs.Task;
        if (!success)
            throw new InvalidOperationException("WebView2 navigation failed during PDF export.");

        var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
        printSettings.ShouldPrintBackgrounds = false;

        await webView.CoreWebView2.PrintToPdfAsync(outputPath, printSettings);
    }
}
