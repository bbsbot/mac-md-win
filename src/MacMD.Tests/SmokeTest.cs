using MacMD.Core.Models;
using MacMD.Core.Services;
using Microsoft.Extensions.DependencyInjection;
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
    public void MarkdownService_Converts_Heading()
    {
        var svc = new MarkdownService();
        var html = svc.ToHtml("# Hello");
        Assert.Contains("Hello</h1>", html);
    }

    [Fact]
    public void MarkdownService_Returns_Empty_For_Null()
    {
        var svc = new MarkdownService();
        Assert.Equal(string.Empty, svc.ToHtml(null!));
    }

    [Fact]
    public void DI_Resolves_Core_Services()
    {
        var services = new ServiceCollection();
        services.AddMacMDCore();
        using var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<ThemeService>());
        Assert.NotNull(sp.GetService<MarkdownService>());
    }
}
