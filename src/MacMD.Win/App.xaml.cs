using MacMD.Core.Services;
using MacMD.Win.Services;
using MacMD.Win.ViewModels;
using MacMD.Win.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace MacMD.Win;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        this.InitializeComponent();

        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MacMD");
        var dbPath = Path.Combine(dbDir, "macmd.db");

        var services = new ServiceCollection();
        services.AddMacMDCore(dbPath);
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<PdfExportService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<SettingsViewModel>(sp =>
            new SettingsViewModel(
                sp.GetRequiredService<SettingsService>(),
                sp.GetRequiredService<ThemeService>()));
        Services = services.BuildServiceProvider();

        // Apply saved theme before the first window is shown
        var settings     = Services.GetRequiredService<SettingsService>();
        var themeService = Services.GetRequiredService<ThemeService>();
        themeService.SetTheme(settings.SelectedTheme);
    }

    public static new App Current => (App)Application.Current;

    public ServiceProvider Services { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();

        _ = ShowWelcomeIfNeededAsync();
    }

    private async Task ShowWelcomeIfNeededAsync()
    {
        var settings = Services.GetRequiredService<SettingsService>();
        if (settings.GetBool("hideWelcome"))
            return;

        // Small delay to let the window render first
        await Task.Delay(500);

        if (_window?.Content is FrameworkElement root)
        {
            var dialog = new WelcomeDialog { XamlRoot = root.XamlRoot };
            await dialog.ShowAsync();

            if (dialog.DontShowAgain)
                settings.SetBool("hideWelcome", true);
        }
    }
}
