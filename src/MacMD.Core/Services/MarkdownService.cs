using Markdig;

namespace MacMD.Core.Services;

/// <summary>
/// Converts Markdown text to HTML using Markdig.
/// Stateless singleton — the pipeline is built once and reused.
/// </summary>
public sealed class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, _pipeline);
    }
}
