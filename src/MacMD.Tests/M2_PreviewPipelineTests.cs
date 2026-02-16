using Microsoft.VisualStudio.TestTools.UnitTesting;
using MacMD.Core.Services;

namespace MacMD.Tests;

[TestClass]
public class M2_PreviewPipelineTests
{
    [TestMethod]
    public void MarkdownService_Can_Convert_Markdown_To_Html()
    {
        // Arrange
        var service = new MarkdownService();

        // Act
        var result = service.ToHtml("# Hello World");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("<h1>Hello World</h1>"));
    }

    [TestMethod]
    public void MarkdownService_Handles_Empty_Input()
    {
        // Arrange
        var service = new MarkdownService();

        // Act
        var result = service.ToHtml(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void MarkdownService_Handles_Null_Input()
    {
        // Arrange
        var service = new MarkdownService();

        // Act
        var result = service.ToHtml(null);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}