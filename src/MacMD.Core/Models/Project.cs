namespace MacMD.Core.Models;

/// <summary>A project that groups documents.</summary>
public sealed record Project(
    ProjectId Id,
    string Name,
    DateTimeOffset CreatedAt);
