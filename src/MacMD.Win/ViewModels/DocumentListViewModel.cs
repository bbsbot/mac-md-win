using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacMD.Core.Models;
using MacMD.Core.Services;

namespace MacMD.Win.ViewModels;

public partial class DocumentListViewModel : ObservableObject
{
    private readonly DocumentStore _documentStore;
    private ProjectId? _currentProjectId;

    public DocumentListViewModel(DocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public ObservableCollection<DocumentSummary> Documents { get; } = new();

    [ObservableProperty]
    private DocumentSummary? _selectedDocument;

    /// <summary>Raised when the user selects a document to load in the editor.</summary>
    public event Action<DocumentId>? DocumentSelected;

    partial void OnSelectedDocumentChanged(DocumentSummary? value)
    {
        if (value is not null)
            DocumentSelected?.Invoke(value.Id);
    }

    public async Task LoadForProjectAsync(ProjectId? projectId)
    {
        _currentProjectId = projectId;
        var docs = projectId is { } pid
            ? await _documentStore.GetByProjectAsync(pid)
            : await _documentStore.GetAllAsync();
        Documents.Clear();
        foreach (var d in docs)
            Documents.Add(d);
    }

    [RelayCommand]
    private async Task CreateDocumentAsync()
    {
        var id = await _documentStore.CreateAsync("Untitled", _currentProjectId);
        await LoadForProjectAsync(_currentProjectId);
        SelectedDocument = Documents.FirstOrDefault(d => d.Id.Value == id.Value);
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync()
    {
        if (SelectedDocument is null) return;
        await _documentStore.DeleteAsync(SelectedDocument.Id);
        SelectedDocument = null;
        await LoadForProjectAsync(_currentProjectId);
    }
}
