namespace MacMD.Core.Models;

/// <summary>A tag that can be applied to documents.</summary>
public sealed record Tag(
    TagId Id,
    string Name,
    string Color);
