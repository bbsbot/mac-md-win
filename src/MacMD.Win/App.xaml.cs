using MacMD.Core.Services;
using MacMD.Win.Services;
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
        Services = services.BuildServiceProvider();
    }

    public static new App Current => (App)Application.Current;

    public ServiceProvider Services { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
