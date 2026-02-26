namespace MacMD.Core.Models;

/// <summary>Lightweight projection of a document for list display.</summary>
public sealed record DocumentSummary(
    DocumentId Id,
    string Title,
    int WordCount,
    DateTimeOffset ModifiedAt,
    string Preview = "");
