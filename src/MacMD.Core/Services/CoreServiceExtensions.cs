using Microsoft.Extensions.DependencyInjection;

namespace MacMD.Core.Services;

/// <summary>
/// Registers all MacMD.Core services into the DI container.
/// </summary>
public static class CoreServiceExtensions
{
    public static IServiceCollection AddMacMDCore(
        this IServiceCollection services)
    {
        services.AddSingleton<ThemeService>();
        return services;
    }
}
