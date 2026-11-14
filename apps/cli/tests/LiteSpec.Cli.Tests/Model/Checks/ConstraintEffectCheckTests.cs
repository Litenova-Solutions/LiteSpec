using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the two-way check between a constraint's Effect on the Product section and the requirements that cite it:
///     a citing requirement the section leaves out is unlisted, and a requirement the section names without citing is uncited.
/// </summary>
public sealed class ConstraintEffectCheckTests
{
    /// <summary>The constraint explanation with the Effect on the Product section the cases name requirements in.</summary>
    /// <param name="effect">The text of the Effect on the Product section.</param>
    /// <returns>The explanation file text.</returns>
    static string Explanation(string effect) =>
        $"# Tax Receipt Retention\n\nThe national tax law keeps receipts.\n\n## Scope\n\nBusiness receipts.\n\n## Rules\n\nThe law requires keeping them.\n\n## Effect on the Product\n\n{effect}\n";

    /// <summary>A citing requirement that the Effect section does not name reports constraint-effect-unlisted.</summary>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Fact]
    public void CitingRequirementMissingFromEffectReportsUnlisted()
    {
        using var temp = TestCli.Temp("checks");
        var finding = Assert.Single(
            Checker.Check(Model(temp.Path, Explanation("A receipt is kept for seven years."), cites: ["constraint/tax-receipt-retention"])).Findings,
            found => found.Code == "constraint-effect-unlisted");

        Assert.Equal("Requirement 'requirement/approval.above-threshold-receipt' cites constraint 'constraint/tax-receipt-retention', but its Effect on the Product section does not name it.", finding.Message);
        Assert.Equal("constraint/tax-receipt-retention", finding.Subject);
    }

    /// <summary>An Effect section that names a requirement which does not cite the constraint reports constraint-effect-uncited.</summary>
    /// <proves>litespec/core/intent#state-each-constraint-rule-with-its-condition</proves>
    [Fact]
    public void NamedRequirementThatDoesNotCiteReportsUncited()
    {
        using var temp = TestCli.Temp("checks");
        var finding = Assert.Single(
            Checker.Check(Model(temp.Path, Explanation("The `requirement/approval.above-threshold-receipt` keeps the receipt."), cites: [])).Findings,
            found => found.Code == "constraint-effect-uncited");

        Assert.Equal("Effect on the Product of 'constraint/tax-receipt-retention' names requirement 'requirement/approval.above-threshold-receipt', which does not cite the constraint.", finding.Message);
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

        Assert.DoesNotContain(findings, found => found.Code is "constraint-effect-unlisted" or "constraint-effect-uncited");
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
