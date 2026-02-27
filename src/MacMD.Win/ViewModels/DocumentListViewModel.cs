using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacMD.Core.Models;
using MacMD.Core.Services;

namespace MacMD.Win.ViewModels;

public enum DocumentFilter { All, Project, Tag, Favorites, Recent, Archived }

public partial class DocumentListViewModel : ObservableObject
{
    private readonly DocumentStore _documentStore;
    private readonly ProjectStore _projectStore;
    private readonly TagStore _tagStore;
    private ProjectId? _currentProjectId;
    private TagId? _currentTagId;
    private DocumentFilter _currentFilter = DocumentFilter.All;
    public DocumentFilter CurrentFilter => _currentFilter;
    private string _searchQuery = "";

    public DocumentListViewModel(DocumentStore documentStore, ProjectStore projectStore, TagStore tagStore)
    {
        _documentStore = documentStore;
        _projectStore = projectStore;
        _tagStore = tagStore;
    }

    public ObservableCollection<DocumentSummary> Documents { get; } = new();
    public ObservableCollection<Project> AvailableProjects { get; } = new();
    public ObservableCollection<Tag> AvailableTags { get; } = new();

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

    public async Task DuplicateDocumentAsync(DocumentId id)
    {
        var newId = await _documentStore.DuplicateAsync(id);
        await ReloadAsync();
        SelectedDocument = Documents.FirstOrDefault(d => d.Id.Value == newId.Value);
    }

    public async Task RenameDocumentAsync(DocumentId id, string newTitle)
    {
        await _documentStore.UpdateTitleAsync(id, newTitle);
        await ReloadAsync();
    }

    public async Task ArchiveDocumentAsync(DocumentId id)
    {
        await _documentStore.ArchiveAsync(id);
        SelectedDocument = null;
        await ReloadAsync();
    }

    public async Task UnarchiveDocumentAsync(DocumentId id)
    {
        await _documentStore.UnarchiveAsync(id);
        await ReloadAsync();
    }

    public async Task LoadArchivedAsync()
    {
        _currentFilter = DocumentFilter.Archived;
        _currentProjectId = null;
        _currentTagId = null;
        await ReloadAsync();
    }

    public async Task MoveToProjectAsync(DocumentId id, ProjectId? projectId)
    {
        await _documentStore.MoveToProjectAsync(id, projectId);
        await ReloadAsync();
    }

    public async Task ToggleTagAsync(DocumentId docId, TagId tagId)
    {
        var currentTags = await _documentStore.GetDocumentTagIdsAsync(docId);
        if (currentTags.Any(t => t.Value == tagId.Value))
            await _tagStore.RemoveTagFromDocumentAsync(docId, tagId);
        else
            await _tagStore.AddTagToDocumentAsync(docId, tagId);
    }

    public async Task RefreshContextMenuDataAsync()
    {
        var projects = await _projectStore.GetAllAsync();
        AvailableProjects.Clear();
        foreach (var p in projects)
            AvailableProjects.Add(p);

        var tags = await _tagStore.GetAllAsync();
        AvailableTags.Clear();
        foreach (var t in tags)
            AvailableTags.Add(t);
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
                DocumentFilter.Archived => await _documentStore.GetArchivedAsync(),
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
