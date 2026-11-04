using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the shared file walk: stable order, skipped folders, extension filter and glob match.</summary>
public sealed class SourceTreeTests
{
    /// <summary>Writes a file, creating its folder first.</summary>
    /// <param name="path">The file to write.</param>
    static void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "fixture");
    }

    /// <summary>Every file is returned in ordinal path order, including extensionless files.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Files_Returns_Every_File_In_Ordinal_Path_Order()
    {
        using var temp = Cli.Temp("source-tree");
        Write(Path.Combine(temp.Path, "b.txt"));
        Write(Path.Combine(temp.Path, "a.md"));
        Write(Path.Combine(temp.Path, "LICENSE"));
        Write(Path.Combine(temp.Path, "sub", "c.cs"));
        Write(Path.Combine(temp.Path, "sub", "nested", "d"));

        Assert.Equal(
            [
                Path.Combine(temp.Path, "LICENSE"),
                Path.Combine(temp.Path, "a.md"),
                Path.Combine(temp.Path, "b.txt"),
                Path.Combine(temp.Path, "sub", "c.cs"),
                Path.Combine(temp.Path, "sub", "nested", "d"),
            ],
            SourceTree.Files(temp.Path));
    }

    /// <summary>Build output, installed packages and tool folders are left out at any depth, without regard to case.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Files_Leaves_Out_Build_And_Tool_Folders()
    {
        using var temp = Cli.Temp("source-tree");
        foreach (var skipped in new[] { "bin", "obj", "TestResults", "node_modules", ".git", ".svelte-kit", "build", "dist", "coverage" })
        {
            Write(Path.Combine(temp.Path, skipped, "left-out.txt"));
        }

        Write(Path.Combine(temp.Path, "deep", "bin", "left-out.txt"));
        Write(Path.Combine(temp.Path, "BIN", "left-out.txt"));
        Write(Path.Combine(temp.Path, "keep.cs"));

        Assert.Equal([Path.Combine(temp.Path, "keep.cs")], SourceTree.Files(temp.Path));
    }

    /// <summary>A missing directory walks as no files.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Files_Returns_Empty_When_Directory_Is_Missing()
    {
        using var temp = Cli.Temp("source-tree");

        Assert.Empty(SourceTree.Files(Path.Combine(temp.Path, "no-such-folder")));
    }

    /// <summary>Extensions match by file-name ending, without regard to case.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Files_With_Extensions_Keeps_Matches_Without_Regard_To_Case()
    {
        using var temp = Cli.Temp("source-tree");
        Write(Path.Combine(temp.Path, "a.cs"));
        Write(Path.Combine(temp.Path, "b.CS"));
        Write(Path.Combine(temp.Path, "c.txt"));
        Write(Path.Combine(temp.Path, "d.cobertura.xml"));
        Write(Path.Combine(temp.Path, "e.lcov"));
        Write(Path.Combine(temp.Path, "lcov.info"));
        Write(Path.Combine(temp.Path, "f.COBERTURA.XML"));

        Assert.Equal(
            [
                Path.Combine(temp.Path, "a.cs"),
                Path.Combine(temp.Path, "b.CS"),
            ],
            SourceTree.Files(temp.Path, ".cs"));

        Assert.Equal(
            [
                Path.Combine(temp.Path, "d.cobertura.xml"),
                Path.Combine(temp.Path, "e.lcov"),
                Path.Combine(temp.Path, "f.COBERTURA.XML"),
                Path.Combine(temp.Path, "lcov.info"),
            ],
            SourceTree.Files(temp.Path, ".cobertura.xml", ".lcov", "lcov.info"));
    }

    /// <summary>Includes collect and excludes leave out, both in the glob dialect.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Match_Collects_Includes_And_Honors_Excludes()
    {
        using var temp = Cli.Temp("source-tree");
        Write(Path.Combine(temp.Path, "keep", "a.md"));
        Write(Path.Combine(temp.Path, "keep", "b.yaml"));
        Write(Path.Combine(temp.Path, "keep", "d.txt"));
        Write(Path.Combine(temp.Path, "drafts", "c.md"));

        Assert.Equal(
            [
                Path.Combine(temp.Path, "keep", "a.md"),
                Path.Combine(temp.Path, "keep", "b.yaml"),
            ],
            SourceTree.Match(temp.Path, ["**/*.md", "**/*.yaml"], ["**/drafts/**/*"]));
    }

    /// <summary>A missing directory matches no files.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Match_Returns_Empty_When_Directory_Is_Missing()
    {
        using var temp = Cli.Temp("source-tree");

        Assert.Empty(SourceTree.Match(Path.Combine(temp.Path, "no-such-folder"), ["**/*"]));
    }

    /// <summary>Read returns the file text.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Read_Returns_The_File_Text()
    {
        using var temp = Cli.Temp("source-tree");
        var file = Path.Combine(temp.Path, "note.txt");
        File.WriteAllText(file, "kept text");

        Assert.Equal("kept text", SourceTree.Read(file));
    }
}
