using LiteSpec.Cli.Context;

namespace LiteSpec.Cli.Tests.Context;

/// <summary>
///     The YAML compaction contract of <c>litespec context</c>: compacting deletes only
///     comments, blank lines and keys without a value, so every value keeps the key it belongs to.
/// </summary>
public sealed class ContextCompactTests
{
    /// <summary>A list written at its key's indent keeps its key.</summary>
    [Fact(DisplayName = "context keeps the key of a list written at the key's indent")]
    public void KeepsTheKeyOfAnIndentlessList()
    {
        var compacted = ContextCompact.Yaml("type: requirement\ncites:\n- research/double-entry-books\n- decision/0006-double-entry-books\n");

        Assert.Equal("type: requirement\ncites:\n- research/double-entry-books\n- decision/0006-double-entry-books", compacted);
    }

    /// <summary>A list indented below its key keeps its key.</summary>
    [Fact(DisplayName = "context keeps the key of an indented list")]
    public void KeepsTheKeyOfAnIndentedList()
    {
        var compacted = ContextCompact.Yaml("cites:\n  - research/double-entry-books\ntitle: Lines Posted\n");

        Assert.Equal("cites:\n  - research/double-entry-books\ntitle: Lines Posted", compacted);
    }

    /// <summary>A key with no value and no block below it is dropped.</summary>
    [Fact(DisplayName = "context drops a key that holds no value")]
    public void DropsAKeyWithoutAValue()
    {
        var compacted = ContextCompact.Yaml("summary:\ncites: []\ntitle: Lines Posted\n");

        Assert.Equal("title: Lines Posted", compacted);
    }
}
