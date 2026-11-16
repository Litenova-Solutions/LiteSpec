using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the Effect on the Product section stays free prose: the book lists the citing requirements from their cites links.</summary>
public sealed class ConstraintEffectCheckTests
{
    /// <summary>The constraint explanation with the Effect on the Product section the cases name requirements in.</summary>
    /// <param name="effect">The text of the Effect on the Product section.</param>
    /// <returns>The explanation file text.</returns>
    static string Explanation(string effect) =>
        $"# Tax Receipt Retention\n\nThe national tax law keeps receipts.\n\n## Scope\n\nBusiness receipts.\n\n## Rules\n\nThe law requires keeping them.\n\n## Effect on the Product\n\n{effect}\n";

    /// <summary>A citing requirement that the Effect section does not name reports nothing.</summary>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Fact]
    public void CitingRequirementMissingFromEffectReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var findings = Checker.Check(Model(temp.Path, Explanation("A receipt is kept for seven years."), cites: ["constraint/tax-receipt-retention"])).Findings;

        Assert.DoesNotContain(findings, found => found.Message.Contains("Effect on the Product", StringComparison.Ordinal));
    }

    /// <summary>An Effect section that names a requirement which does not cite the constraint reports nothing.</summary>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Fact]
    public void NamedRequirementThatDoesNotCiteReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var findings = Checker.Check(Model(temp.Path, Explanation("The `requirement/approval.above-threshold-receipt` keeps the receipt."), cites: [])).Findings;

        Assert.DoesNotContain(findings, found => found.Message.Contains("Effect on the Product", StringComparison.Ordinal));
    }

    /// <summary>An Effect section that names each citing requirement, and no other, reports nothing.</summary>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Fact]
    public void MatchingEffectReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var findings = Checker.Check(Model(
            temp.Path,
            Explanation("The `requirement/approval.above-threshold-receipt` keeps the receipt."),
            cites: ["constraint/tax-receipt-retention"])).Findings;

        Assert.DoesNotContain(findings, found => found.Message.Contains("Effect on the Product", StringComparison.Ordinal));
    }

    /// <summary>A model with one applying constraint, one citing requirement and the explanation given.</summary>
    /// <param name="root">The temporary project root.</param>
    /// <param name="explanation">The constraint explanation text.</param>
    /// <param name="cites">The constraint identifiers the requirement cites.</param>
    /// <returns>The model.</returns>
    static ProjectModel Model(string root, string explanation, string[] cites)
    {
        var citesJson = string.Join(", ", cites.Select(cite => $"\"{cite}\""));

        return TestModel.Model(
            TestModel.Config(root),
            TestModel.Record(
                "constraint",
                "constraint/tax-receipt-retention",
                Path.Combine(root, "tax-receipt-retention.yaml"),
                1,
                """{"authority": "National tax law.", "sources": [{"key": "national-tax-law", "url": "https://example.com/tax-law", "readOn": "2026-02-10"}], "kind": "legal", "applies": true, "appliesBecause": "We store receipts."}""",
                Path.Combine(root, "tax-receipt-retention.md"),
                explanation),
            TestModel.Record(
                "requirement",
                "requirement/approval.above-threshold-receipt",
                Path.Combine(root, "above-threshold-receipt.yaml"),
                1,
                $$"""{"kind": "functional", "statement": "An expense above the receipt threshold carries a receipt.", "rationale": "Proof keeps the tax file defensible.", "cites": [{{citesJson}}]}"""));
    }
}
