using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacMD.Core.Models;
using MacMD.Core.Services;

namespace MacMD.Win.ViewModels;

public enum DocumentFilter { All, Project, Tag, Favorites, Recent }

public partial class DocumentListViewModel : ObservableObject
{
    private readonly DocumentStore _documentStore;
    private ProjectId? _currentProjectId;
    private TagId? _currentTagId;
    private DocumentFilter _currentFilter = DocumentFilter.All;
    private string _searchQuery = "";

    public DocumentListViewModel(DocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public ObservableCollection<DocumentSummary> Documents { get; } = new();

    [ObservableProperty]
    private DocumentSummary? _selectedDocument;

    public event Action<DocumentId>? DocumentSelected;

    partial void OnSelectedDocumentChanged(DocumentSummary? value)
    {
        if (value is not null)
            DocumentSelected?.Invoke(value.Id);
    }

    public async Task LoadForProjectAsync(ProjectId? projectId)
    {
        _currentFilter = projectId is null ? DocumentFilter.All : DocumentFilter.Project;
        _currentProjectId = projectId;
        _currentTagId = null;
        await ReloadAsync();
    }

    public async Task LoadForTagAsync(TagId tagId)
    {
        _currentFilter = DocumentFilter.Tag;
        _currentTagId = tagId;
        _currentProjectId = null;
        await ReloadAsync();
    }

    public async Task LoadFavoritesAsync()
    {
        _currentFilter = DocumentFilter.Favorites;
        _currentProjectId = null;
        _currentTagId = null;
        await ReloadAsync();
    }

    public async Task LoadRecentAsync()
    {
        _currentFilter = DocumentFilter.Recent;
        _currentProjectId = null;
        _currentTagId = null;
        await ReloadAsync();
    }

    public async Task SearchAsync(string query)
    {
        _searchQuery = query;
        await ReloadAsync();
    }

    public async Task ToggleFavoriteAsync(DocumentId id)
    {
        await _documentStore.ToggleFavoriteAsync(id);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        IReadOnlyList<DocumentSummary> docs;

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            docs = await _documentStore.SearchAsync(_searchQuery);
        }
        else
        {
            docs = _currentFilter switch
            {
                DocumentFilter.Project when _currentProjectId is { } pid
                    => await _documentStore.GetByProjectAsync(pid),
                DocumentFilter.Tag when _currentTagId is { } tid
                    => await _documentStore.GetByTagAsync(tid),
                DocumentFilter.Favorites => await _documentStore.GetFavoritesAsync(),
                DocumentFilter.Recent => await _documentStore.GetRecentAsync(),
                _ => await _documentStore.GetAllAsync(),
            };
        }

        Documents.Clear();
        foreach (var d in docs)
            Documents.Add(d);
    }

    [RelayCommand]
    private async Task CreateDocumentAsync()
    {
        var id = await _documentStore.CreateAsync("Untitled", _currentProjectId);
        await ReloadAsync();
        SelectedDocument = Documents.FirstOrDefault(d => d.Id.Value == id.Value);
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync()
    {
        if (SelectedDocument is null) return;
        await _documentStore.DeleteAsync(SelectedDocument.Id);
        SelectedDocument = null;
        await ReloadAsync();
    }
}
