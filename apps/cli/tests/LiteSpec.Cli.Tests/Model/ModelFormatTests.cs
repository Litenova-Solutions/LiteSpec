using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves <c>litespec model format</c> rewrites record files into canonical form.</summary>
public sealed class ModelFormatTests
{
    /// <summary>Format on a canonical consumer rewrites nothing and exits 0.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Format_Canonical_Consumer_Rewrites_Nothing()
    {
        using var consumer = WriteConsumer();
        var result = Cli.Run("model", "format", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("0 files rewritten.", result.Output);
    }

    /// <summary>Format rewrites a record file standing outside canonical form.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Format_Rewrites_A_Non_Canonical_Record_File()
    {
        using var consumer = WriteConsumer();
        var path = RecordPath(consumer.Path);
        var canonical = File.ReadAllText(path);
        File.WriteAllText(path, Uncanonically(canonical));

        var result = Cli.Run("model", "format", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("brief.yaml", result.Output);
        Assert.Contains("1 files rewritten.", result.Output);
        Assert.Equal(canonical, File.ReadAllText(path));
    }

    /// <summary>Format --check lists the files it would rewrite and exits 1 without writing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Format_Check_Lists_Would_Change_And_Writes_Nothing()
    {
        using var consumer = WriteConsumer();
        var path = RecordPath(consumer.Path);
        File.WriteAllText(path, Uncanonically(File.ReadAllText(path)));
        var before = File.ReadAllText(path);

        var result = Cli.Run("model", "format", "--root", consumer.Path, "--check");

        Assert.Equal(1, result.Status);
        Assert.Contains("brief.yaml", result.Output);
        Assert.Contains("1 files would change.", result.Output);
        Assert.Equal(before, File.ReadAllText(path));
    }

    /// <summary>Format exits 2 and names the record file that does not parse.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Format_Invalid_File_Exits_2()
    {
        using var consumer = WriteConsumer();
        File.WriteAllText(
            Path.Combine(consumer.Path, ".litespec", "product", "broken.yaml"),
            "type: product\nid: [unclosed\n");

        var result = Cli.Run("model", "format", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("broken.yaml", result.Output);
        Assert.Contains("does not parse.", result.Output);
        Assert.Contains("0 files rewritten.", result.Output);
    }

    /// <summary>The record file of the written consumer.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The record file's full path.</returns>
    static string RecordPath(string root) =>
        Path.Combine(root, ".litespec", "product", "brief.yaml");

    /// <summary>The canonical text with its first two lines swapped: still in the subset, outside canonical form.</summary>
    /// <param name="canonical">The canonical record file text.</param>
    /// <returns>The swapped text.</returns>
    static string Uncanonically(string canonical)
    {
        var lines = canonical.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        return string.Join('\n', [lines[1], lines[0], .. lines[2..]]);
    }

    static TempFolder WriteConsumer()
    {
        var temp = Cli.Temp("model-format");

        try
        {
            File.WriteAllText(
                Path.Combine(temp.Path, ".litespec/project.json"),
                """
                {"project": {"name": "Format Fixture"}, "size": "small", "shape": "server",
                  "stacks": ["dotnet"], "reviewedLiteSpecVersion": "4.0.0",
                  "paths": {"docs": ".litespec", "glossary": ".litespec/glossary.yaml", "testRoots": [], "frontends": []}}
                """);
            TestRecords.Write(temp.Path, ".litespec/product/brief.yaml", """
                {
                  "type": "product",
                  "id": "product/library",
                  "title": "Library",
                  "boundary": "The library lends copies and tracks holds.",
                  "addresses": ["actor/reader"]
                }
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }
}
