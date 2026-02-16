namespace MacMD.Core.Models;

/// <summary>A Markdown document.</summary>
public sealed record Document(
    DocumentId Id,
    string Title,
    string Content,
    ProjectId? ProjectId,
    IReadOnlyList<TagId> TagIds,
    int WordCount,
    int CharacterCount,
    bool IsFavorite,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
