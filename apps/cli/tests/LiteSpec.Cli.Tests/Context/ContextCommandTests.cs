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
        Assert.Contains("== .litespec/intent/", text);
        Assert.Contains("== .litespec/behavior/", text);
        var firstBlock = text.Split('\n').First(line => line.StartsWith("== ", StringComparison.Ordinal));
        Assert.EndsWith("product.yaml", firstBlock);

        var recordsOnly = Path.Combine(output.Path, "records-only.txt");
        var filtered = TestCli.Run("context", "--root", Root, "--out", recordsOnly, "--layers", "behavior", "--include", "records");

        Assert.Equal(0, filtered.Status);
        var blocks = File.ReadAllText(recordsOnly)
            .Split('\n')
            .Where(line => line.StartsWith("== ", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(blocks, block => block.EndsWith(".yaml", StringComparison.Ordinal));
        Assert.DoesNotContain(blocks, block => block.EndsWith(".md", StringComparison.Ordinal));
    }
}
