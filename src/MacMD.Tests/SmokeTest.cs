using MacMD.Core.Models;
using MacMD.Core.Services;
using Xunit;

namespace MacMD.Tests;

public sealed class SmokeTest
{
    [Fact]
    public void DocumentId_RoundTrips()
    {
        var id = new DocumentId("test-123");
        Assert.Equal("test-123", id.Value);
    }

    [Fact]
    public void MarkdownService_Can_Be_Created()
    {
        var service = new MarkdownService();
        Assert.NotNull(service);
    }

    [Fact]
    public void M2_PreviewPipeline_Working()
    {
        // Test that our M2 preview pipeline works
        var service = new MarkdownService();

        // Test basic markdown conversion
        var markdown = "# Hello\n\nThis is a **test**.";
        var html = service.ToHtml(markdown);

        Assert.False(string.IsNullOrEmpty(html));
        Assert.Contains("<h1>Hello</h1>", html);
    }
}
