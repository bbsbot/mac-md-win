using System.Collections.Frozen;
using MacMD.Core.Models;

namespace MacMD.Core.Services;

/// <summary>
/// Manages the 10 terminal-inspired color themes.
/// Stateless lookup + mutable current-theme selection.
/// </summary>
public sealed class ThemeService
{
    private static readonly FrozenDictionary<string, ColorTheme> Themes = BuildThemes();

    private ColorTheme _current;

    public ThemeService()
    {
        _current = Themes["Pro"];
    }

    public ColorTheme CurrentTheme => _current;

    public IReadOnlyList<ColorTheme> GetThemes() => Themes.Values.ToList();

    public ColorTheme? GetTheme(string name) =>
        Themes.TryGetValue(name, out var theme) ? theme : null;

    public void SetTheme(string name)
    {
        if (Themes.TryGetValue(name, out var theme))
        {
            _current = theme;
            ThemeChanged?.Invoke(this, theme);
        }
    }

    public event EventHandler<ColorTheme>? ThemeChanged;

    private static FrozenDictionary<string, ColorTheme> BuildThemes()
    {
        var themes = new Dictionary<string, ColorTheme>
        {
            ["Basic"] = new("Basic",
                Background: "#FFFFFF", Foreground: "#000000",
                Black: "#000000", Red: "#990000", Green: "#00A600",
                Yellow: "#999900", Blue: "#0000B2", Magenta: "#B200B2",
                Cyan: "#00A6B2", White: "#BFBFBF", IsDark: false),

            ["Grass"] = new("Grass",
                Background: "#13773D", Foreground: "#FFFC67",
                Black: "#000000", Red: "#990000", Green: "#00A600",
                Yellow: "#999900", Blue: "#0000B2", Magenta: "#B200B2",
                Cyan: "#00A6B2", White: "#BFBFBF", IsDark: true),

            ["Homebrew"] = new("Homebrew",
                Background: "#000000", Foreground: "#00FF00",
                Black: "#000000", Red: "#990000", Green: "#00A600",
                Yellow: "#999900", Blue: "#0000B2", Magenta: "#B200B2",
                Cyan: "#00A6B2", White: "#BFBFBF", IsDark: true),

            ["Man Page"] = new("Man Page",
                Background: "#FEF49C", Foreground: "#000000",
                Black: "#000000", Red: "#CC0000", Green: "#00A600",
                Yellow: "#999900", Blue: "#0000B2", Magenta: "#B200B2",
                Cyan: "#00A6B2", White: "#BFBFBF", IsDark: false),

            ["Novel"] = new("Novel",
                Background: "#DFDBC3", Foreground: "#3B2322",
                Black: "#000000", Red: "#CC0000", Green: "#00A600",
                Yellow: "#999900", Blue: "#0000B2", Magenta: "#B200B2",
                Cyan: "#00A6B2", White: "#BFBFBF", IsDark: false),

            ["Ocean"] = new("Ocean",
                Background: "#224FBC", Foreground: "#FFFFFF",
                Black: "#000000", Red: "#990000", Green: "#00A600",
                Yellow: "#999900", Blue: "#0000B2", Magenta: "#B200B2",
                Cyan: "#00A6B2", White: "#BFBFBF", IsDark: true),

            ["Pro"] = new("Pro",
                Background: "#1E1E1E", Foreground: "#F2F2F2",
                Black: "#000000", Red: "#FF6B68", Green: "#5BCC5B",
                Yellow: "#F0C569", Blue: "#6FB3F2", Magenta: "#E86BFF",
                Cyan: "#5FCCDB", White: "#CCCCCC", IsDark: true),

            ["Red Sands"] = new("Red Sands",
                Background: "#7A251E", Foreground: "#D7C9A7",
                Black: "#000000", Red: "#FF6640", Green: "#00CC00",
                Yellow: "#FFCC00", Blue: "#0066FF", Magenta: "#CC00FF",
                Cyan: "#00CCCC", White: "#BFBFBF", IsDark: true),

            ["Silver Aerogel"] = new("Silver Aerogel",
                Background: "#929292", Foreground: "#000000",
                Black: "#000000", Red: "#BB0000", Green: "#00BB00",
                Yellow: "#BBBB00", Blue: "#0000BB", Magenta: "#BB00BB",
                Cyan: "#00BBBB", White: "#BBBBBB", IsDark: false),

            ["Solid Colors"] = new("Solid Colors",
                Background: "#000000", Foreground: "#FFFFFF",
                Black: "#000000", Red: "#BB0000", Green: "#00BB00",
                Yellow: "#BBBB00", Blue: "#0000BB", Magenta: "#BB00BB",
                Cyan: "#00BBBB", White: "#FFFFFF", IsDark: true),
        };

        return themes.ToFrozenDictionary();
    }
}
