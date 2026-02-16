using Microsoft.Extensions.DependencyInjection;
using MacMD.Core.Services;
using Microsoft.UI.Xaml;

namespace MacMD.Tests;

public sealed class AppStartupTest
{
    [Fact]
    public void App_Can_Initialize_Service_Provider()
    {
        // Test that the service provider can be created without exceptions
        var services = new ServiceCollection();
        services.AddMacMDCore();
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void App_Can_Resolve_MarkdownService()
    {
        // Test that we can resolve the MarkdownService from DI container
        var services = new ServiceCollection();
        services.AddMacMDCore();
        var serviceProvider = services.BuildServiceProvider();

        var markdownService = serviceProvider.GetService<MarkdownService>();

        Assert.NotNull(markdownService);
    }

    [Fact]
    public void App_Can_Create_App_Instance()
    {
        // Test that we can create the App instance without exceptions
        var app = new App();

        Assert.NotNull(app);
        Assert.NotNull(app.Services);
    }
}