using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the renamed Intent shapes: rationale, setBy, choice, reopenedWhen and reviewBy.</summary>
public sealed class IntentShapeCheckTests
{
    /// <proves>litespec/core/intent#explain-each-requirement-in-one-line</proves>
    [Fact]
    public void Requirement_Without_Rationale_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.True(Reports(model, "record-shape", "has no rationale."));
    }

    /// <proves>litespec/core/intent#state-requirement-kind-and-statement</proves>
    [Fact]
    public void Requirement_With_Retired_SetBy_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "rationale": "Readers share popular copies.", "setBy": "founder"}"""));

        Assert.True(Reports(model, "invalid-field", "has setBy 'founder'; decider, operator, tenant."));
    }

    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact]
    public void Decision_Without_ReopenedWhen_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.md"), """{"status": "approved", "options": [{"name": "Fourteen days", "pros": "Readers finish in time.", "cons": "Popular copies queue."}], "choice": "Fourteen days"}"""));

        Assert.True(Reports(model, "record-shape", "has no reopenedWhen."));
    }

    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact]
    public void Approved_Decision_Without_Choice_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.md"), """{"status": "approved", "options": [{"name": "Fourteen days", "pros": "Readers finish in time.", "cons": "Popular copies queue."}], "reopenedWhen": "Queues pass one hundred a day."}"""));

        Assert.True(Reports(model, "record-shape", "has no choice."));
    }

    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact]
    public void Topic_Without_ReviewBy_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("topic", "topic/borrowing", Path.Combine(temp.Path, "borrowing.md"), """{"status": "approved"}"""));

        Assert.True(Reports(model, "record-shape", "has no reviewBy."));
    }

    static bool Reports(ProjectModel model, string code, string message) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(message, StringComparison.Ordinal));
}
