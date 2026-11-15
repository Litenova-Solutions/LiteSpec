using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the step check counts use cases, reactions and jobs.</summary>
public sealed class StepCheckTests
{
    /// <summary>Use Case Serving A Step Passes.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void UseCaseServingAStepPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Flow(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"serves": ["flow/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("step-use-case-missing", TestModel.Codes(model));
    }

    /// <summary>Reaction Serving A Step Passes.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void ReactionServingAStepPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Flow(temp.Path),
            TestModel.Record("reaction", "reaction/lending.scan-overdue-on-loan-due", Path.Combine(temp.Path, "scan.yaml"), """{"triggeredBy": ["domain-event/loan.loan-due"], "serves": ["flow/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("step-use-case-missing", TestModel.Codes(model));
    }

    /// <summary>Job Serving A Step Passes.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void JobServingAStepPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Flow(temp.Path),
            TestModel.Record("job", "job/lending.scan-overdue", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "manual", "guard": "An operator starts the scan.", "serves": ["flow/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("step-use-case-missing", TestModel.Codes(model));
    }

    /// <summary>Step With No Server Reports.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void StepWithNoServerReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Flow(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "step-use-case-missing");
        Assert.Equal("Flow step 'flow/borrow-and-return#s1' is served by no use case, reaction or job.", finding.Message);
        Assert.Equal("flow/borrow-and-return#s1", finding.Subject);
    }

    /// <summary>A flow walking one step the servers serve.</summary>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The flow record holding the step.</returns>
    static Record Flow(string root) =>
        TestModel.Record("flow", "flow/borrow-and-return", Path.Combine(root, "borrow-and-return.yaml"), """{"actor": ["actor/reader"], "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}""");
}
