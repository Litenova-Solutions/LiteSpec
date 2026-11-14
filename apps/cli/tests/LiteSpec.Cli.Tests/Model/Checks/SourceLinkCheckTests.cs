using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the source-links region: a sourced explanation holds the region the record's sources state, a hand
///     edit to it is drift, and the region is not read as a bare link.
/// </summary>
public sealed class SourceLinkCheckTests
{
    /// <summary>The sourced constraint fields the cases share.</summary>
    const string ConstraintFields = """{"authority": "City ordinance.", "sources": [{"key": "ordinance", "url": "https://example.com/ordinance", "readOn": "2026-01-10"}], "kind": "legal", "applies": true, "appliesBecause": "We keep reader records."}""";

    /// <summary>The source-links region the sources state, as the render writes it.</summary>
    const string Region = "<!-- generated:source-links -->\n[ordinance]: https://example.com/ordinance \"Read on 2026-01-10\"\n<!-- /generated:source-links -->";

    /// <summary>A sourced explanation with no region reports source-links-missing.</summary>
    /// <proves>litespec/core/intent#cite-a-source-for-each-constraint-rule</proves>
    [Fact]
    public void ExplanationWithoutRegionReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var finding = Assert.Single(Checker.Check(Model(temp.Path, "# Library Privacy Law\n\nReader records stay private.\n")).Findings, found => found.Code == "source-links-missing");

        Assert.Equal("A", finding.Tier);
        Assert.Equal("constraint/library-privacy-law", finding.Subject);
    }

    /// <summary>A region that differs from the sources reports source-links-drift.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void EditedRegionReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var text = "# Library Privacy Law\n\nReader records stay private.\n\n<!-- generated:source-links -->\n[ordinance]: https://example.com/edited \"Read on 2026-01-10\"\n<!-- /generated:source-links -->\n";

        var finding = Assert.Single(Checker.Check(Model(temp.Path, text)).Findings, found => found.Code == "source-links-drift");

        Assert.Equal("A", finding.Tier);
        Assert.Equal("constraint/library-privacy-law", finding.Subject);
    }

    /// <summary>A region that matches the sources reports nothing and its URL is not a bare link.</summary>
    /// <proves>litespec/core/intent#cite-a-source-for-each-constraint-rule</proves>
    [Fact]
    public void CurrentRegionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var text = $"# Library Privacy Law\n\nReader records stay private under [the ordinance][ordinance].\n\n{Region}\n";

        var findings = Checker.Check(Model(temp.Path, text)).Findings;

        Assert.DoesNotContain(findings, found => found.Code is "source-links-missing" or "source-links-drift" or "source-link-bare" or "source-unused");
    }

    /// <summary>Placing the region appends it when absent, and rewrites it in place when present.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void PlaceAppendsThenRewritesInPlace()
    {
        const string content = "[ordinance]: https://example.com/ordinance \"Read on 2026-01-10\"";

        var appended = SourceLinks.Place("# Title\n\nBody.\n", content);

        Assert.Equal($"# Title\n\nBody.\n\n{Region}\n", appended);

        var rewritten = SourceLinks.Place(appended.Replace("[ordinance]: https://example.com/ordinance", "[ordinance]: https://example.com/old", StringComparison.Ordinal), content);

        Assert.Equal($"# Title\n\nBody.\n\n{Region}\n", rewritten);
    }

    /// <summary>A constraint explanation that links a URL directly outside the region reports source-link-bare.</summary>
    /// <proves>litespec/core/intent#cite-a-source-for-each-constraint-rule</proves>
    [Fact]
    public void BareUrlInConstraintExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var text = $"# Library Privacy Law\n\nSee https://example.com/ordinance for the text.\n\n{Region}\n";

        var finding = Assert.Single(Checker.Check(Model(temp.Path, text)).Findings, found => found.Code == "source-link-bare");

        Assert.Equal("A", finding.Tier);
    }

    /// <summary>A model with one sourced constraint whose explanation holds the given text.</summary>
    /// <param name="root">The temporary project root.</param>
    /// <param name="explanation">The explanation file text.</param>
    /// <returns>The model.</returns>
    static ProjectModel Model(string root, string explanation)
    {
        var file = Path.Combine(root, "library-privacy-law.yaml");
        var explanationFile = Path.Combine(root, "library-privacy-law.md");

        return TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("constraint", "constraint/library-privacy-law", file, 1, ConstraintFields, explanationFile, explanation));
    }
}
