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

        // The section also names hashed fields as examples of what feeds the hash,
        // so only the sentences stating which fields stay outside list exclusions.
        var listed = Regex.Split(section, @"(?<=[.!?])\s+")
            .Where(sentence => sentence.Contains("stay outside", StringComparison.Ordinal))
            .SelectMany(sentence => Regex.Matches(sentence, "`([^`]+)`").Select(match => match.Groups[1].Value))
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
            ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml");
        Assert.True(RecordFile.TryParse(File.ReadAllText(path), out var root, out _, out var error), error);
        var record = new LiteSpec.Model.Record
        {
            Type = "use-case",
            Id = "use-case/lending.borrow-copy",
            Raw = root,
        };

        Assert.Equal("74687aab", Checker.SemanticHash(record));
    }

    /// <summary>An article page hashes its explanation with its fields.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Article_Page_Hashes_Its_Explanation()
    {
        var fieldsOnly = HashOf(ResearchFields, "research", "research/hold-limits", null);
        var answered = HashOf(ResearchFields, "research", "research/hold-limits", "# Hold limits\n\n## Answer\n\nSeven days.\n");
        var reworded = HashOf(ResearchFields, "research", "research/hold-limits", "# Hold limits\n\n## Answer\n\nAbout a week.\n");

        Assert.Equal(Checker.SemanticHash(RawOf(ResearchFields)), fieldsOnly);
        Assert.NotEqual(fieldsOnly, answered);
        Assert.NotEqual(answered, reworded);
    }

    /// <summary>A page of any other type hashes its fields alone, ignoring its explanation.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Non_Article_Page_Ignores_Its_Explanation()
    {
        var fieldsOnly = HashOf(UseCaseFields, "use-case", "use-case/lending.borrow-copy", null);
        var explained = HashOf(UseCaseFields, "use-case", "use-case/lending.borrow-copy", "# Borrow copy\n\nA reader borrows an available copy.\n");

        Assert.Equal(fieldsOnly, explained);
    }

    /// <summary>An entry of an article type hashes its fields alone, ignoring any explanation.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Article_Entry_Ignores_Its_Explanation()
    {
        var fieldsOnly = HashOf(ResearchFields, "research", "research/hold-limits", null, isEntry: true);
        var explained = HashOf(ResearchFields, "research", "research/hold-limits", "# Hold limits\n\n## Answer\n\nSeven days.\n", isEntry: true);

        Assert.Equal(fieldsOnly, explained);
    }

    /// <summary>The article hash reads the explanation normalized: line ends, trailing space and surrounding blank lines change nothing.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Article_Hash_Ignores_Explanation_Line_Ends_And_Surrounding_Blanks()
    {
        var clean = HashOf(ResearchFields, "research", "research/hold-limits", "# Hold limits\n\n## Answer\n\nSeven days.\n");
        var noisy = HashOf(ResearchFields, "research", "research/hold-limits", "\r\n# Hold limits  \r\n\r\n## Answer\r\n\r\nSeven days.  \r\n\r\n");

        Assert.Equal(clean, noisy);
    }

    const string ResearchFields = """
        {
          "type": "research",
          "id": "research/hold-limits",
          "title": "Hold limits",
          "owner": "lending-team",
          "status": "approved",
          "question": "How long does a hold last?",
          "sources": [{"key": "desk-manual", "url": "https://example.com/manual", "readOn": "2026-01-01"}],
          "conclusion": "Seven days."
        }
        """;

    const string UseCaseFields = """
        {
          "type": "use-case",
          "id": "use-case/lending.borrow-copy",
          "title": "Borrow copy",
          "owner": "lending-team",
          "status": "reviewed",
          "kind": "command"
        }
        """;

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

    static string HashOf(string json, string type, string id, string? explanation, bool isEntry = false)
    {
        using var document = JsonDocument.Parse(json);
        var record = new LiteSpec.Model.Record
        {
            Type = type,
            Id = id,
            Raw = document.RootElement.Clone(),
            Explanation = explanation,
            IsEntry = isEntry,
        };

        return Checker.SemanticHash(record);
    }

    static JsonElement RawOf(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    static string CanonicalOf(string json)
    {
        using var document = JsonDocument.Parse(json);

        return CanonicalJson.Canonical(document.RootElement.Clone());
    }
}
