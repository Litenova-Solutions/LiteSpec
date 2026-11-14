using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the constraint basedOn check: a constraint rests on research records only.</summary>
public sealed class ConstraintBasedOnCheckTests
{
    /// <summary>A basedOn entry that names a decision, not a research record, reports field-invalid.</summary>
    /// <proves>litespec/core/intent#answer-every-constraint</proves>
    [Fact]
    public void BasedOnNamingNonResearchReportsFieldInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "constraint",
                "constraint/library-privacy-law",
                Path.Combine(temp.Path, "library-privacy-law.yaml"),
                1,
                """{"authority": "City ordinance.", "sources": [{"key": "ordinance", "url": "https://example.com/ordinance", "readOn": "2026-01-10"}], "kind": "legal", "applies": true, "appliesBecause": "We keep reader records.", "basedOn": ["decision/0001-loan-store"]}"""),
            TestModel.Record(
                "decision",
                "decision/0001-loan-store",
                Path.Combine(temp.Path, "0001-loan-store.yaml"),
                1,
                """{"summary": "Loan storage.", "options": [{"name": "Documents", "pros": "Simple.", "cons": "Slow."}], "choice": "Documents.", "consequences": "Loans are documents.", "reopenedWhen": "Reads exceed one second."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "field-invalid" && found.Message.Contains("basedOn", StringComparison.Ordinal));

        Assert.Equal("constraint/library-privacy-law", finding.Subject);
        Assert.Equal("Constraint 'constraint/library-privacy-law' names 'decision/0001-loan-store' in basedOn, which is no research record.", finding.Message);
    }

    /// <summary>A basedOn entry that names a research record reports nothing.</summary>
    /// <proves>litespec/core/intent#answer-every-constraint</proves>
    [Fact]
    public void BasedOnNamingResearchReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "constraint",
                "constraint/library-privacy-law",
                Path.Combine(temp.Path, "library-privacy-law.yaml"),
                1,
                """{"authority": "City ordinance.", "sources": [{"key": "ordinance", "url": "https://example.com/ordinance", "readOn": "2026-01-10"}], "kind": "legal", "applies": true, "appliesBecause": "We keep reader records.", "basedOn": ["research/loan-terms"]}"""),
            TestModel.Record(
                "research",
                "research/loan-terms",
                Path.Combine(temp.Path, "loan-terms.yaml"),
                1,
                """{"sources": [{"key": "lib-manual", "url": "https://example.com/manual", "readOn": "2026-01-02"}], "question": "How long is a loan?", "conclusion": "Loans run for two weeks."}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Message.Contains("basedOn", StringComparison.Ordinal));
    }
}
