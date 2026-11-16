using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Context;

/// <summary>The smoke test for <c>litespec context</c>: the Claimwick example writes as one text file.</summary>
public sealed class ContextCommandTests
{
    static readonly string Root = Path.Combine(TestCli.Repository, "examples", "claimwick");

    /// <summary>Context writes the specification as one text file with a header and record blocks.</summary>
    [Fact(DisplayName = "context writes the specification as one text file with a header and record blocks")]
    public void WritesTheSpecificationAsOneTextFile()
    {
        using var output = TestCli.Temp("context");
        var file = Path.Combine(output.Path, "litespec-context.txt");

        var result = TestCli.Run("context", "--root", Root, "--out", output.Path);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(file), result.Output);
        var text = File.ReadAllText(file);
        Assert.StartsWith("LiteSpec context for ", text);
        Assert.Contains("=== BEGIN FILE: .litespec/intent/", text);
        Assert.Contains("=== BEGIN FILE: .litespec/behavior/", text);
        var lines = text.Split('\n');
        var begins = lines.Where(line => line.StartsWith("=== BEGIN FILE: ", StringComparison.Ordinal)).ToList();
        Assert.EndsWith("product.yaml ===", begins[0]);

        // Every file closes with its own end marker, and the context closes with the file count.
        var ends = lines.Where(line => line.StartsWith("=== END FILE: ", StringComparison.Ordinal)).ToList();
        Assert.Equal(begins.Select(line => line["=== BEGIN FILE: ".Length..]), ends.Select(line => line["=== END FILE: ".Length..]));
        Assert.Equal($"=== END OF CONTEXT: {begins.Count} files ===", text.TrimEnd('\n').Split('\n')[^1]);
        Assert.Contains($"Files: {begins.Count}", text);

        var recordsOnly = Path.Combine(output.Path, "records-only.txt");
        var filtered = TestCli.Run("context", "--root", Root, "--out", recordsOnly, "--layers", "behavior", "--include", "records");

        Assert.Equal(0, filtered.Status);
        var blocks = File.ReadAllText(recordsOnly)
            .Split('\n')
            .Where(line => line.StartsWith("=== BEGIN FILE: ", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(blocks, block => block.EndsWith(".yaml ===", StringComparison.Ordinal));
        Assert.DoesNotContain(blocks, block => block.EndsWith(".md ===", StringComparison.Ordinal));
    }
}
