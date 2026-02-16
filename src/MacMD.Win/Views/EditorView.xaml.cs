using Microsoft.UI.Xaml.Controls;
using MacMD.Win.ViewModels;

namespace MacMD.Win.Views;

public sealed partial class EditorView : UserControl
{
    public EditorViewModel? ViewModel { get; set; }

    public EditorView()
    {
        this.InitializeComponent();
    }
}