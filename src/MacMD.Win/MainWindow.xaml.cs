using Microsoft.UI.Xaml;

namespace MacMD.Win;

public sealed partial class MainWindow : Window
{
    private bool _isDark = true;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Mac MD";

        // Set initial size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;

        if (this.Content is FrameworkElement root)
        {
            root.RequestedTheme = _isDark
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }
    }
}
