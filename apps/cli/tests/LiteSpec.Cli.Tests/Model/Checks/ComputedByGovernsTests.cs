using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that a calculation rule cited only by computedBy reaches the use case it computes.</summary>
public sealed class ComputedByGovernsTests
{
    /// <summary>A calculation rule governed by one use case that cites it in computedBy passes.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void ComputedByOnlyCalculationRulePasses()
    {
        var model = ModelWith("""{"kind": "query", "computedBy": ["rule/spending.approval-days"], "summary": "Shows the approval days."}""");

        Assert.DoesNotContain("rule-governs-drift", TestModel.Codes(model));
        Assert.DoesNotContain("calculation-table-missing", TestModel.Codes(model));
    }

    /// <summary>The same calculation rule cited by no use case reports drift with empty reach.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void CalculationRuleWithoutComputedByReportsDrift()
    {
        var model = ModelWith("""{"kind": "query", "summary": "Shows the approval days."}""");

        var finding = Assert.Single(GovernsFindings(model));
        Assert.Contains("links reach ''", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A calculation rule governed by a use case whose computedBy names another rule still reports drift.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void ComputedByAnotherRuleReportsDrift()
    {
        var model = ModelWith("""{"kind": "query", "computedBy": ["rule/spending.other-calculation"], "summary": "Shows the approval days."}""");

        Assert.Single(GovernsFindings(model));
    }

    /// <summary>A model holding the calculation rule, the use case and a scenario with an examples table.</summary>
    /// <param name="useCase">The use-case record fields as JSON.</param>
    /// <returns>The hand-built project model.</returns>
    static ProjectModel ModelWith(string useCase)
    {
        var temp = TestCli.Temp("checks");

        return TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/spending.approval-days",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "rules", "approval-days.yaml"),
                """{"kind": "calculation", "statement": "Approval days count business days from submission.", "governs": ["use-case/spending.show-approval-days"]}"""),
            TestModel.Record(
                "use-case",
                "use-case/spending.show-approval-days",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "use-cases", "show-approval-days", "show-approval-days.yaml"),
                useCase),
            TestModel.Record(
                "scenario",
                "scenario/show-approval-days.approval-days-example",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "use-cases", "show-approval-days", "show-approval-days.md"),
                """{"given": "An expense was submitted.", "when": "The days are shown.", "then": "The table holds.", "examples": [{"submitted": "2026-01-01", "days": 3}]}"""));
    }

    /// <summary>The rule-governs findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is rule-governs-drift.</returns>
    static List<Finding> GovernsFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "rule-governs-drift").ToList();
}
