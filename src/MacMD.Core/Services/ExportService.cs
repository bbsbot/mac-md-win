namespace MacMD.Core.Services;

/// <summary>
/// Exports markdown documents as standalone HTML files.
/// PDF export is handled by PdfExportService in MacMD.Win (requires WebView2).
/// </summary>
public sealed class ExportService
{
    private readonly MarkdownService _markdownService;

    public ExportService(MarkdownService markdownService)
    {
        _markdownService = markdownService;
    }

    /// <summary>
    /// Converts markdown to a full standalone HTML document and writes it to disk.
    /// </summary>
    public async Task ExportHtmlAsync(string title, string markdownContent, string outputPath)
    {
        var htmlBody = _markdownService.ToHtml(markdownContent);
        var document = BuildHtmlDocument(title, htmlBody);
        await File.WriteAllTextAsync(outputPath, document);
    }

    /// <summary>
    /// Builds a complete HTML document string with inline CSS.
    /// Used by both HTML export and PDF export (light theme, print-friendly).
    /// </summary>
    public string BuildHtmlDocument(string title, string htmlBody)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8"/>
            <title>{{EscapeHtml(title)}}</title>
            <style>
              * { box-sizing: border-box; }
              body {
                font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
                max-width: 800px;
                margin: 0 auto;
                padding: 40px 24px;
                line-height: 1.7;
                color: #1a1a1a;
                background: #ffffff;
              }
              h1, h2, h3, h4, h5, h6 { color: #111; margin-top: 1.4em; margin-bottom: 0.6em; }
              h1 { font-size: 2em; border-bottom: 1px solid #ddd; padding-bottom: 0.3em; }
              h2 { font-size: 1.5em; border-bottom: 1px solid #eee; padding-bottom: 0.2em; }
              code {
                background: #f4f4f4;
                padding: 2px 6px;
                border-radius: 3px;
                font-size: 0.9em;
              }
              pre {
                background: #f6f8fa;
                padding: 16px;
                border-radius: 6px;
                overflow-x: auto;
                border: 1px solid #e1e4e8;
              }
              pre code { background: none; padding: 0; }
              blockquote {
                border-left: 4px solid #ddd;
                margin-left: 0;
                padding-left: 16px;
                color: #555;
              }
              a { color: #0366d6; }
              table { border-collapse: collapse; width: 100%; margin: 1em 0; }
              th, td { border: 1px solid #ddd; padding: 8px 12px; text-align: left; }
              th { background: #f6f8fa; font-weight: 600; }
              img { max-width: 100%; }
              hr { border: none; border-top: 1px solid #ddd; margin: 2em 0; }
              @media print {
                body { max-width: none; padding: 0; }
              }
            </style>
            </head>
            <body>
            {{htmlBody}}
            </body>
            </html>
            """;
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
