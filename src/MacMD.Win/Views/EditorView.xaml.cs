using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MacMD.Core.Models;
using MacMD.Win.ViewModels;

namespace MacMD.Win.Views;

public sealed partial class EditorView : UserControl
{
    public EditorViewModel? ViewModel { get; set; }

    // Cache selection state so toolbar clicks (which steal focus) still work.
    // We track both a "live" value (updated on every SelectionChanged) and
    // a "saved" value (snapshot taken on LostFocus). The toolbar handlers
    // read from _savedSelection* which won't be clobbered by the focus-loss
    // SelectionChanged(0,0) that fires before the click handler.
    private int _liveSelectionStart;
    private int _liveSelectionLength;
    private int _savedSelectionStart;
    private int _savedSelectionLength;

    public EditorView()
    {
        this.InitializeComponent();
        MarkdownTextBox.SelectionChanged += (_, _) =>
        {
            _liveSelectionStart = MarkdownTextBox.SelectionStart;
            _liveSelectionLength = MarkdownTextBox.SelectionLength;
        };
        MarkdownTextBox.LostFocus += (_, _) =>
        {
            _savedSelectionStart = _liveSelectionStart;
            _savedSelectionLength = _liveSelectionLength;
        };
    }

    private void WrapSelection(string before, string after)
    {
        var start = _savedSelectionStart;
        var length = _savedSelectionLength;
        var text = MarkdownTextBox.Text;

        // Clamp to valid range
        if (start > text.Length) start = text.Length;
        if (start + length > text.Length) length = text.Length - start;

        var selected = length > 0 ? text.Substring(start, length) : "";
        var replacement = before + selected + after;
        MarkdownTextBox.Text = text.Remove(start, length).Insert(start, replacement);

        if (length > 0)
            MarkdownTextBox.Select(start, replacement.Length);
        else
            MarkdownTextBox.Select(start + before.Length, 0);

        _savedSelectionStart = MarkdownTextBox.SelectionStart;
        _savedSelectionLength = MarkdownTextBox.SelectionLength;
        MarkdownTextBox.Focus(FocusState.Programmatic);
    }

    private void PrefixLines(string prefix, bool numbered = false)
    {
        var start = _savedSelectionStart;
        var length = _savedSelectionLength;
        var text = MarkdownTextBox.Text;

        // Clamp
        if (start > text.Length) start = text.Length;
        if (start + length > text.Length) length = text.Length - start;

        if (length > 0)
        {
            // Multi-line: prefix each line in the selection
            var selected = text.Substring(start, length);
            var lines = selected.Split('\r');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimStart('\n');

            var result = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) result.Append("\r\n");
                var linePrefix = numbered ? $"{i + 1}. " : prefix;
                result.Append(linePrefix);
                result.Append(lines[i]);
            }

            var replacement = result.ToString();
            MarkdownTextBox.Text = text.Remove(start, length).Insert(start, replacement);
            MarkdownTextBox.Select(start, replacement.Length);
        }
        else
        {
            // Single cursor: insert prefix at the start of the current line
            var lineStart = start > 0 ? text.LastIndexOf('\n', start - 1) : -1;
            lineStart = lineStart < 0 ? 0 : lineStart + 1;

            MarkdownTextBox.Text = text.Insert(lineStart, prefix);
            MarkdownTextBox.Select(start + prefix.Length, 0);
        }

        _savedSelectionStart = MarkdownTextBox.SelectionStart;
        _savedSelectionLength = MarkdownTextBox.SelectionLength;
        MarkdownTextBox.Focus(FocusState.Programmatic);
    }

    public void ApplyTheme(ColorTheme theme)
    {
        var bg = ParseHexColor(theme.Background);
        var fg = ParseHexColor(theme.Foreground);
        MarkdownTextBox.Background = new SolidColorBrush(bg);
        MarkdownTextBox.Foreground = new SolidColorBrush(fg);
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
        }
        return Windows.UI.Color.FromArgb(255, 30, 30, 30);
    }

    private void OnBoldClick(object sender, RoutedEventArgs e) => WrapSelection("**", "**");
    private void OnItalicClick(object sender, RoutedEventArgs e) => WrapSelection("*", "*");
    private void OnHeadingClick(object sender, RoutedEventArgs e) => PrefixLines("## ");
    private void OnLinkClick(object sender, RoutedEventArgs e) => WrapSelection("[", "](url)");
    private void OnCodeClick(object sender, RoutedEventArgs e) => WrapSelection("`", "`");
    private void OnBulletListClick(object sender, RoutedEventArgs e) => PrefixLines("- ");
    private void OnNumberedListClick(object sender, RoutedEventArgs e) => PrefixLines("1. ", numbered: true);
    private void OnQuoteClick(object sender, RoutedEventArgs e) => PrefixLines("> ");

    private void OnHrClick(object sender, RoutedEventArgs e)
    {
        var start = _savedSelectionStart;
        var text = MarkdownTextBox.Text;
        if (start > text.Length) start = text.Length;
        var insert = "\n---\n";
        MarkdownTextBox.Text = text.Insert(start, insert);
        MarkdownTextBox.Select(start + insert.Length, 0);
        _savedSelectionStart = MarkdownTextBox.SelectionStart;
        _savedSelectionLength = 0;
        MarkdownTextBox.Focus(FocusState.Programmatic);
    }
}
