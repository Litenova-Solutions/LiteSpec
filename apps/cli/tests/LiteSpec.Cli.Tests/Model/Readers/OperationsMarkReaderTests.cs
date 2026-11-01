using LiteSpec.Model;
using LiteSpec.Model.Readers.Operations;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the operations reader reads follows marks with the comment style.</summary>
public sealed class OperationsMarkReaderTests
{
    /// <summary>Follows Marks Are Read With The Comment Style.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Follows_Marks_Are_Read_With_The_Comment_Style()
    {
        using var temp = Cli.Temp("operations-marks");
        WriteProject(temp.Path);
        Write(temp.Path, "ops/Caddyfile", """
            # <follows>decision/0002-loan-index</follows>
            example.com {
                reverse_proxy localhost:5000
            }
            # <follows>  exception/export-review-bypass  </follows>
            """);

        var marks = OperationsMarkReader.Read(ConsumerConfig.Load(temp.Path));

        Assert.Equal(2, marks.Count);
        Assert.Equal("decision/0002-loan-index", marks[0].Id);
        Assert.Equal(MarkStyle.Comment, marks[0].Style);
        Assert.Equal(1, marks[0].Line);
        Assert.Equal("exception/export-review-bypass", marks[1].Id);
        Assert.Equal(MarkStyle.Comment, marks[1].Style);
        Assert.Equal(5, marks[1].Line);
    }

    /// <summary>Marks Outside Comments And Other Verbs Are Ignored.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Marks_Outside_Comments_And_Other_Verbs_Are_Ignored()
    {
        using var temp = Cli.Temp("operations-marks");
        WriteProject(temp.Path);
        Write(temp.Path, "ops/Caddyfile", """
            <follows>decision/0002-loan-index</follows>
            # <implements>use-case/lending.borrow-copy</implements>
            # <proves>scenario/borrow-copy.borrowing-succeeds</proves>
            # nothing to follow here
            """);

        Assert.Empty(OperationsMarkReader.Read(ConsumerConfig.Load(temp.Path)));
    }

    /// <summary>Markdown Files Below An Operations Folder Are Ignored.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Markdown_Files_Below_An_Operations_Folder_Are_Ignored()
    {
        using var temp = Cli.Temp("operations-marks");
        WriteProject(temp.Path);
        Write(temp.Path, "ops/notes.md", "# <follows>decision/0002-loan-index</follows>\n");

        Assert.Empty(OperationsMarkReader.Read(ConsumerConfig.Load(temp.Path)));
    }

    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "ops"));
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {
              "owners": ["team"],
              "paths": { "operations": ["ops"] }
            }
            """);
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
