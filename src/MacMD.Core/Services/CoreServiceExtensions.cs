using Microsoft.Extensions.DependencyInjection;

namespace MacMD.Core.Services;

public static class CoreServiceExtensions
{
    /// <summary>
    /// Registers all MacMD.Core services. Call <paramref name="dbPath"/> to
    /// set the SQLite database location (pass null to skip persistence registration).
    /// </summary>
    public static IServiceCollection AddMacMDCore(
        this IServiceCollection services, string? dbPath = null)
    {
        services.AddSingleton<ThemeService>();
        services.AddSingleton<MarkdownService>();

        if (dbPath is not null)
        {
            services.AddSingleton(new DatabaseService(dbPath));
            services.AddSingleton<DocumentStore>();
            services.AddSingleton<ProjectStore>();
            services.AddSingleton<TagStore>();
        }

        return services;
    }
}
