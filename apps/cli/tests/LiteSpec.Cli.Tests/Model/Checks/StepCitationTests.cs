using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the step citation checks: a step citation names a step of a flow, and the old flow-step form is reported.</summary>
public sealed class StepCitationTests
{
    /// <summary>A use case serving a step the flow holds passes.</summary>
    /// <proves>litespec/core/language#resolve-every-identifier-citation</proves>
    [Fact]
    public void UseCaseServingAHeldStepPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "command", "serves": ["step/borrow-and-return.borrow-copy"]}"""),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goal.yaml"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days."}""")]);

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "reference-unknown" && found.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>A use case serving a step no flow holds reports.</summary>
    /// <proves>litespec/core/language#resolve-every-identifier-citation</proves>
    [Fact]
    public void UseCaseServingNoStepReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "command", "serves": ["step/borrow-and-return.shelve-copy"]}""")]);

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "reference-unknown" && found.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>A use case serving a step in the old flow-step form reports.</summary>
    /// <proves>litespec/core/language#resolve-every-identifier-citation</proves>
    [Fact]
    public void UseCaseServingAnOldFormStepReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "command", "serves": ["flow/borrow-and-return#s1"]}""")]);

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "reference-unknown" && found.Subject == "use-case/lending.borrow-copy");
    }
}
