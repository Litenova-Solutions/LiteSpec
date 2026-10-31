using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the approval hash reads one exclusion list and hashes numbers by value.</summary>
public sealed class ApprovalHashTests
{
    /// <summary>The fields the guide lists as outside the hash are the fields the code leaves out.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Guide_Excluded_Fields_Match_Code()
    {
        var guide = File.ReadAllText(Path.Combine(Cli.Repository, "docs", "guide", "approval-hash.md"));
        var start = guide.IndexOf("### Know Which Fields Stay Outside", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = guide.IndexOf("### ", start + 1, StringComparison.Ordinal);
        Assert.True(end > start);
        var section = guide[start..end];
        var listed = Regex.Matches(section, "`([^`]+)`")
            .Select(match => match.Groups[1].Value)
            .Where(token => !token.Contains('/', StringComparison.Ordinal) && !token.Contains('#', StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            Checker.ApprovalExcluded.OrderBy(field => field, StringComparer.Ordinal),
            listed.OrderBy(field => field, StringComparer.Ordinal));
    }

    /// <summary>One field written as 1, 1.0 and 1e0 hashes the same.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Numbers_Written_Differently_Hash_The_Same()
    {
        var plain = HashOf("""{"kind": "command", "limit": 1}""");
        var trailed = HashOf("""{"kind": "command", "limit": 1.0}""");
        var exponent = HashOf("""{"kind": "command", "limit": 1e0}""");

        Assert.Equal(plain, trailed);
        Assert.Equal(plain, exponent);
    }

    /// <summary>A decimal with trailing zeros hashes as its shortest form.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Decimal_Trailing_Zeros_Hash_As_Shortest_Form()
    {
        var trailed = HashOf("""{"kind": "command", "limit": 1.50}""");
        var shortest = HashOf("""{"kind": "command", "limit": 1.5}""");
        var other = HashOf("""{"kind": "command", "limit": 2}""");

        Assert.Equal(shortest, trailed);
        Assert.NotEqual(shortest, other);
    }

    /// <summary>The canonical writer writes each number by value.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Canonical_Writer_Writes_Numbers_By_Value()
    {
        Assert.Equal("1", CanonicalOf("1"));
        Assert.Equal("1", CanonicalOf("1.0"));
        Assert.Equal("1", CanonicalOf("1e0"));
        Assert.Equal("1.5", CanonicalOf("1.50"));
    }

    /// <summary>A record already written canonically keeps the hash its approval recorded.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Canonical_Record_Keeps_Its_Recorded_Hash()
    {
        var path = Path.Combine(
            Cli.Repository, "apps", "cli", "tests", "LiteSpec.Cli.Tests", "Fixtures", "Model", "large",
            "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        var body = text["---\n".Length..text.IndexOf("\n---", StringComparison.Ordinal)];

        using var document = JsonDocument.Parse(body);
        var record = new LiteSpec.Model.Record
        {
            Type = "use-case",
            Id = "use-case/lending.borrow-copy",
            Raw = document.RootElement.Clone(),
        };

        Assert.Equal("74687aab", Checker.SemanticHash(record));
    }

    static string HashOf(string json)
    {
        using var document = JsonDocument.Parse(json);
        var record = new LiteSpec.Model.Record
        {
            Type = "use-case",
            Id = "use-case/lending.borrow-copy",
            Raw = document.RootElement.Clone(),
        };

        return Checker.SemanticHash(record);
    }

    static string CanonicalOf(string json)
    {
        using var document = JsonDocument.Parse(json);

        return CanonicalJson.Canonical(document.RootElement.Clone());
    }
}
