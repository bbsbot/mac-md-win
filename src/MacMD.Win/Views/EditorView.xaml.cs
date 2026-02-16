using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MacMD.Win.ViewModels;

namespace MacMD.Win.Views;

public sealed partial class EditorView : UserControl
{
    public EditorViewModel? ViewModel { get; set; }

    public EditorView()
    {
        this.InitializeComponent();
    }

    private void OnUpdatePreviewClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.UpdatePreviewCommand.Execute(null);
    }
}