using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the renamed Intent shapes: rationale, setBy, choice and reopenedWhen.</summary>
public sealed class IntentShapeCheckTests
{
    /// <summary>Requirement Without Rationale Reports.</summary>
    /// <proves>litespec/core/intent#explain-each-requirement-in-one-line</proves>
    [Fact]
    public void RequirementWithoutRationaleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.True(Reports(model, "record-invalid", "has no rationale."));
    }

    /// <summary>Requirement With Retired SetBy Reports.</summary>
    /// <proves>litespec/core/intent#state-requirement-kind-and-statement</proves>
    [Fact]
    public void RequirementWithRetiredSetByReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "rationale": "Readers share popular copies.", "setBy": "founder"}"""));

        Assert.True(Reports(model, "field-invalid", "has setBy 'founder' outside product, operator, tenant."));
    }

    /// <summary>Decision Without ReopenedWhen Reports.</summary>
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact]
    public void DecisionWithoutReopenedWhenReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.yaml"), """{"options": [{"name": "Fourteen days", "pros": "Readers finish in time.", "cons": "Popular copies queue."}], "choice": "Fourteen days"}"""));

        Assert.True(Reports(model, "record-invalid", "has no reopenedWhen."));
    }

    /// <summary>Decision Without Choice Reports.</summary>
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact]
    public void DecisionWithoutChoiceReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.yaml"), """{"options": [{"name": "Fourteen days", "pros": "Readers finish in time.", "cons": "Popular copies queue."}], "reopenedWhen": "Queues pass one hundred a day."}"""));

        Assert.True(Reports(model, "record-invalid", "has no choice."));
    }

    static bool Reports(ProjectModel model, string code, string message) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(message, StringComparison.Ordinal));
}
