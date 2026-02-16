namespace MacMD.Core.Models;

/// <summary>
/// A terminal-inspired color theme with background, foreground,
/// and 8 ANSI color slots.
/// </summary>
public sealed record ColorTheme(
    string Name,
    string Background,
    string Foreground,
    string Black,
    string Red,
    string Green,
    string Yellow,
    string Blue,
    string Magenta,
    string Cyan,
    string White,
    bool IsDark);
