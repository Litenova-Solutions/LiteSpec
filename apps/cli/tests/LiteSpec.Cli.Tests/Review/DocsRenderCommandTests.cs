using LiteSpec.Cli.Review;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Review;

/// <summary>Proves <c>litespec docs render --changed</c> renders only the records whose content hash changed.</summary>
public sealed class DocsRenderCommandTests
{
    /// <summary>The first render writes the page and the hashes, and the second render writes nothing.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void FirstRenderWritesPageAndSecondRenderWritesNothing()
    {
        using var folder = TestCli.Temp("docs-render");
        File.WriteAllText(Path.Combine(folder.Path, "record.yaml"), "title: Expense\nsummary: A claim an approver clears.\n");

        using var firstOut = new StringWriter();
        using var firstError = new StringWriter();
        Assert.Equal(0, DocsRenderCommand.Execute(folder.Path, firstOut, firstError));
        Assert.Contains("Rendered 1 pages", firstOut.ToString());
        Assert.True(File.Exists(Path.Combine(folder.Path, ".litespec", "render", "record.md")));
        Assert.True(File.Exists(Path.Combine(folder.Path, ".litespec", ".cache", "render-hashes.json")));

        using var secondOut = new StringWriter();
        using var secondError = new StringWriter();
        Assert.Equal(0, DocsRenderCommand.Execute(folder.Path, secondOut, secondError));
        Assert.Contains("Rendered 0 pages", secondOut.ToString());
    }

    /// <summary>A changed record renders again: only its hash mismatch decides.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ChangedRecordRendersAgain()
    {
        using var folder = TestCli.Temp("docs-render-changed");
        File.WriteAllText(Path.Combine(folder.Path, "record.yaml"), "title: Expense\n");

        using var firstOut = new StringWriter();
        using var firstError = new StringWriter();
        Assert.Equal(0, DocsRenderCommand.Execute(folder.Path, firstOut, firstError));

        File.AppendAllText(Path.Combine(folder.Path, "record.yaml"), "summary: A claim an approver clears.\n");

        using var secondOut = new StringWriter();
        using var secondError = new StringWriter();
        Assert.Equal(0, DocsRenderCommand.Execute(folder.Path, secondOut, secondError));
        Assert.Contains("Rendered 1 pages", secondOut.ToString());
        Assert.Contains("Content hash:", File.ReadAllText(Path.Combine(folder.Path, ".litespec", "render", "record.md")));
    }
}
