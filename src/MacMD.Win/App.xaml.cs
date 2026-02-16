using MacMD.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace MacMD.Win;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        this.InitializeComponent();

        var services = new ServiceCollection();
        services.AddMacMDCore();
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
