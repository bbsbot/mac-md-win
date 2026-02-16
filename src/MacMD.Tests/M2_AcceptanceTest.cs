using Microsoft.VisualStudio.TestTools.UnitTesting;
using MacMD.Core.Services;
using MacMD.Win.ViewModels;
using System.Threading.Tasks;

namespace MacMD.Tests
{
    /// <summary>
    /// Acceptance test for M2 - Markdown preview pipeline
    /// Verifies the complete end-to-end functionality works as implemented
    /// </summary>
    [TestClass]
    public class M2_AcceptanceTest
    {
        [TestMethod]
        public void Markdown_Preview_Pipeline_EndToEnd_Workflow()
        {
            // Arrange
            var markdownService = new MarkdownService();
            var viewModel = new EditorViewModel(markdownService);

            // Test markdown input
            string testMarkdown = @"
# Hello World

This is a **test** document with:
- Item 1
- Item 2
- Item 3

> This is a blockquote

```
code block
```

[Link](https://example.com)
";

            // Act
            viewModel.MarkdownText = testMarkdown;
            viewModel.UpdatePreviewCommand.Execute(null);

            // Assert
            Assert.IsNotNull(viewModel.HtmlPreview);
            Assert.IsFalse(string.IsNullOrEmpty(viewModel.HtmlPreview));

            // Verify key HTML elements were generated
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<h1>Hello World</h1>"));
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<strong>test</strong>"));
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<li>Item 1</li>"));
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<blockquote>"));
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<code>"));
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<a href=\"https://example.com\">Link</a>"));
        }

        [TestMethod]
        public void EditorViewModel_Updates_Preview_When_Markdown_Changes()
        {
            // Arrange
            var markdownService = new MarkdownService();
            var viewModel = new EditorViewModel(markdownService);

            // Act
            viewModel.MarkdownText = "# Header 1";
            viewModel.UpdatePreviewCommand.Execute(null);

            // Assert - First preview
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<h1>Header 1</h1>"));

            // Act - Change markdown
            viewModel.MarkdownText = "## Header 2";
            viewModel.UpdatePreviewCommand.Execute(null);

            // Assert - Updated preview
            Assert.IsTrue(viewModel.HtmlPreview.Contains("<h2>Header 2</h2>"));
            Assert.IsFalse(viewModel.HtmlPreview.Contains("<h1>Header 1</h1>"));
        }

        [TestMethod]
        public void Empty_Markdown_Generates_Empty_Html()
        {
            // Arrange
            var markdownService = new MarkdownService();
            var viewModel = new EditorViewModel(markdownService);

            // Act
            viewModel.MarkdownText = string.Empty;
            viewModel.UpdatePreviewCommand.Execute(null);

            // Assert
            Assert.AreEqual(string.Empty, viewModel.HtmlPreview);
        }
    }
}