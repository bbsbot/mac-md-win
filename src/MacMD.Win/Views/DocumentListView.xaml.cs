using MacMD.Core.Models;
using MacMD.Win.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.Views;

public sealed partial class DocumentListView : UserControl
{
    private readonly DispatcherTimer _searchDebounce;

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
        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounce.Tick += OnSearchDebounceTick;
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

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private async void OnSearchDebounceTick(object? sender, object e)
    {
        _searchDebounce.Stop();
        if (ViewModel is null) return;
        await ViewModel.SearchAsync(SearchBox.Text);
    }
}
