using MacMD.Core.Models;
using MacMD.Win.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.Views;

public sealed partial class DocumentListView : UserControl
{
    public DocumentListViewModel? ViewModel
    {
        get => _vm;
        set
        {
            _vm = value;
            if (_vm is not null)
                DocumentsList.ItemsSource = _vm.Documents;
        }
    }
    private DocumentListViewModel? _vm;

    public DocumentListView()
    {
        this.InitializeComponent();
    }

    private void OnDocumentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null && DocumentsList.SelectedItem is DocumentSummary d)
            ViewModel.SelectedDocument = d;
    }

    private void OnNewDocumentClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.CreateDocumentCommand.Execute(null);
    }

    private void OnDeleteDocumentClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.DeleteDocumentCommand.Execute(null);
    }
}
