using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.Views;

public sealed partial class WelcomeDialog : ContentDialog
{
    public bool DontShowAgain => DontShowAgainCheckBox.IsChecked == true;

    public WelcomeDialog()
    {
        this.InitializeComponent();
    }
}
