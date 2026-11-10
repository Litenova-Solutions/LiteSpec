using LiteSpec.Consumer;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Proves the page scan's file walk: record and Markdown files in, generated output and hidden entries out.</summary>
public sealed class GlobWalkerTests
{
    /// <summary>Writes a file, creating its folder first.</summary>
    /// <param name="path">The file to write.</param>
    static void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "fixture");
    }

    /// <summary>Record files and Markdown files are collected in ordinal path order, and the glossary file is left out.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact]
    public void PageFilesCollectsRecordAndMarkdownFilesInOrder()
    {
        using var temp = TestCli.Temp("glob-walker");
        Write(Path.Combine(temp.Path, "b.md"));
        Write(Path.Combine(temp.Path, "a.yaml"));
        Write(Path.Combine(temp.Path, "sub", "c.md"));
        Write(Path.Combine(temp.Path, "sub", "d.yaml"));
        Write(Path.Combine(temp.Path, "notes.txt"));
        Write(Path.Combine(temp.Path, "data.json"));
        var glossary = Path.Combine(temp.Path, "glossary.yaml");
        Write(glossary);

        Assert.Equal(
            [
                Path.Combine(temp.Path, "a.yaml"),
                Path.Combine(temp.Path, "b.md"),
                Path.Combine(temp.Path, "sub", "c.md"),
                Path.Combine(temp.Path, "sub", "d.yaml"),
            ],
            GlobWalker.PageFiles(temp.Path, Excluded(glossary)));
    }

    /// <summary>The glossary file is left out without regard to case.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact]
    public void PageFilesLeavesOutTheGlossaryFileWithoutRegardToCase()
    {
        using var temp = TestCli.Temp("glob-walker");
        Write(Path.Combine(temp.Path, "Glossary.yaml"));
        Write(Path.Combine(temp.Path, "page.md"));

        Assert.Equal(
            [Path.Combine(temp.Path, "page.md")],
            GlobWalker.PageFiles(temp.Path, Excluded(Path.Combine(temp.Path, "glossary.yaml"))));
    }

    /// <summary>Only the top-level model and book folders are skipped; nested ones are walked.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact]
    public void PageFilesSkipsTopLevelModelAndBookFolders()
    {
        using var temp = TestCli.Temp("glob-walker");
        Write(Path.Combine(temp.Path, "model", "a.md"));
        Write(Path.Combine(temp.Path, "book", "b.yaml"));
        Write(Path.Combine(temp.Path, "sub", "model", "c.md"));
        Write(Path.Combine(temp.Path, "sub", "book", "d.yaml"));
        Write(Path.Combine(temp.Path, "e.md"));

        Assert.Equal(
            [
                Path.Combine(temp.Path, "e.md"),
                Path.Combine(temp.Path, "sub", "book", "d.yaml"),
                Path.Combine(temp.Path, "sub", "model", "c.md"),
            ],
            GlobWalker.PageFiles(temp.Path, Excluded(Path.Combine(temp.Path, "glossary.yaml"))));
    }

    /// <summary>Hidden files and folders and installed packages are skipped, and matching stays case-sensitive.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact]
    public void PageFilesSkipsHiddenEntriesAndNodeModules()
    {
        using var temp = TestCli.Temp("glob-walker");
        Write(Path.Combine(temp.Path, ".hidden.md"));
        Write(Path.Combine(temp.Path, ".git", "x.md"));
        Write(Path.Combine(temp.Path, "sub", ".cache", "y.md"));
        Write(Path.Combine(temp.Path, "node_modules", "z.md"));
        Write(Path.Combine(temp.Path, "deep", "node_modules", "w.yaml"));
        Write(Path.Combine(temp.Path, "UPPER.MD"));
        Write(Path.Combine(temp.Path, "Model", "cap.md"));
        Write(Path.Combine(temp.Path, "ok.md"));
        Write(Path.Combine(temp.Path, "sub", "ok.yaml"));

        Assert.Equal(
            [
                Path.Combine(temp.Path, "Model", "cap.md"),
                Path.Combine(temp.Path, "ok.md"),
                Path.Combine(temp.Path, "sub", "ok.yaml"),
            ],
            GlobWalker.PageFiles(temp.Path, Excluded(Path.Combine(temp.Path, "glossary.yaml"))));
    }

    /// <summary>A missing root walks as no files.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact]
    public void PageFilesReturnsEmptyWhenRootIsMissing()
    {
        using var temp = TestCli.Temp("glob-walker");

        Assert.Empty(GlobWalker.PageFiles(Path.Combine(temp.Path, "no-such-folder"), Excluded(Path.Combine(temp.Path, "glossary.yaml"))));
    }

    /// <summary>The excluded set holding one file, compared without regard to case as the glossary is.</summary>
    /// <param name="file">The excluded file.</param>
    /// <returns>The set holding the file.</returns>
    static IReadOnlySet<string> Excluded(string file) => new HashSet<string>([file], StringComparer.OrdinalIgnoreCase);
}
