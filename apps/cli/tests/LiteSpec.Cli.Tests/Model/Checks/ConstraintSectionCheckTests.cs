using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the constraint explanation sections: Scope, Rules and Effect on the Product are required, and Source is gone.</summary>
public sealed class ConstraintSectionCheckTests
{
    /// <summary>A constraint explanation that omits one required section reports explanation-missing naming it.</summary>
    /// <param name="section">The required section the explanation leaves out.</param>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Theory]
    [InlineData("Scope")]
    [InlineData("Rules")]
    [InlineData("Effect on the Product")]
    public void ExplanationOmittingARequiredSectionReports(string section)
    {
        using var temp = TestCli.Temp("checks");
        var sections = new[] { "Scope", "Rules", "Effect on the Product" }
            .Where(name => name != section)
            .Select(name => $"## {name}\n\nText.\n");
        var explanation = "# Library Privacy Law\n\nReader records stay private.\n\n" + string.Join('\n', sections);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "constraint",
                "constraint/library-privacy-law",
                Path.Combine(temp.Path, "library-privacy-law.yaml"),
                1,
                """{"authority": "City ordinance.", "sources": [{"key": "ordinance", "url": "https://example.com/ordinance", "readOn": "2026-01-10"}], "kind": "legal", "applies": true, "appliesBecause": "We keep reader records."}""",
                Path.Combine(temp.Path, "library-privacy-law.md"),
                explanation));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");

        Assert.Equal($"Constraint 'constraint/library-privacy-law' holds no {section} section in its explanation.", finding.Message);
    }

    /// <summary>A constraint explanation with the three required sections, and no Source section, reports nothing.</summary>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Fact]
    public void ExplanationWithTheRequiredSectionsReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "constraint",
                "constraint/library-privacy-law",
                Path.Combine(temp.Path, "library-privacy-law.yaml"),
                1,
                """{"authority": "City ordinance.", "sources": [{"key": "ordinance", "url": "https://example.com/ordinance", "readOn": "2026-01-10"}], "kind": "legal", "applies": true, "appliesBecause": "We keep reader records."}""",
                Path.Combine(temp.Path, "library-privacy-law.md"),
                "# Library Privacy Law\n\nReader records stay private.\n\n## Scope\n\nReader records.\n\n## Rules\n\nThe ordinance keeps them private [ordinance][ordinance].\n\n## Effect on the Product\n\nNo effect is named.\n\n<!-- generated:source-links -->\n[ordinance]: https://example.com/ordinance \"Read on 2026-01-10\"\n<!-- /generated:source-links -->\n"));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
    }
}
