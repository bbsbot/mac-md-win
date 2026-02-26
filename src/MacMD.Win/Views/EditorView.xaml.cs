using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacMD.Win.ViewModels;

namespace MacMD.Win.Views;

public sealed partial class EditorView : UserControl
{
    public EditorViewModel? ViewModel { get; set; }

    public EditorView()
    {
        this.InitializeComponent();
    }

    private void WrapSelection(string before, string after)
    {
        var start = MarkdownTextBox.SelectionStart;
        var length = MarkdownTextBox.SelectionLength;
        var text = MarkdownTextBox.Text;
        var selected = length > 0 ? text.Substring(start, length) : "";

        var replacement = before + selected + after;
        MarkdownTextBox.Text = text.Remove(start, length).Insert(start, replacement);
        // Place cursor after the inserted text or inside the markers if no selection
        if (length > 0)
            MarkdownTextBox.Select(start, replacement.Length);
        else
            MarkdownTextBox.Select(start + before.Length, 0);
    }

    private void InsertAtLineStart(string prefix)
    {
        var start = MarkdownTextBox.SelectionStart;
        var text = MarkdownTextBox.Text;

        // Find the start of the current line
        var lineStart = text.LastIndexOf('\n', Math.Max(start - 1, 0));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        MarkdownTextBox.Text = text.Insert(lineStart, prefix);
        MarkdownTextBox.Select(start + prefix.Length, 0);
    }

    private void OnBoldClick(object sender, RoutedEventArgs e) => WrapSelection("**", "**");
    private void OnItalicClick(object sender, RoutedEventArgs e) => WrapSelection("*", "*");
    private void OnHeadingClick(object sender, RoutedEventArgs e) => InsertAtLineStart("## ");
    private void OnLinkClick(object sender, RoutedEventArgs e) => WrapSelection("[", "](url)");
    private void OnCodeClick(object sender, RoutedEventArgs e) => WrapSelection("`", "`");
    private void OnBulletListClick(object sender, RoutedEventArgs e) => InsertAtLineStart("- ");
    private void OnNumberedListClick(object sender, RoutedEventArgs e) => InsertAtLineStart("1. ");
    private void OnQuoteClick(object sender, RoutedEventArgs e) => InsertAtLineStart("> ");

    private void OnHrClick(object sender, RoutedEventArgs e)
    {
        var start = MarkdownTextBox.SelectionStart;
        var text = MarkdownTextBox.Text;
        var insert = "\n---\n";
        MarkdownTextBox.Text = text.Insert(start, insert);
        MarkdownTextBox.Select(start + insert.Length, 0);
    }
}
