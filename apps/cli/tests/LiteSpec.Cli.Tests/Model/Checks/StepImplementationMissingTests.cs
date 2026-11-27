using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the step implementation check: a step served by records that no code implements is reported.</summary>
public sealed class StepImplementationMissingTests
{
    /// <summary>A step whose serving use case has code behind it passes.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void StepWithImplementedServerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "command", "serves": ["step/borrow-and-return.borrow-copy"]}""")]);
        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));

        Assert.DoesNotContain("step-implementation-missing", TestModel.Codes(model));
    }

    /// <summary>A step whose serving use case has no code behind it reports.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void StepWithUnimplementedServerReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "command", "serves": ["step/borrow-and-return.borrow-copy"]}""")]);

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "step-implementation-missing" && found.Subject == "step/borrow-and-return.borrow-copy");
    }

    /// <summary>A step with no servers draws the unserved finding only, not the unimplemented one.</summary>
    /// <proves>litespec/core/model#serve-every-flow-step</proves>
    [Fact]
    public void StepWithNoServersDrawsOnlyTheUnservedFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}""")]);

        Assert.Contains("step-use-case-missing", TestModel.Codes(model));
        Assert.DoesNotContain("step-implementation-missing", TestModel.Codes(model));
    }
}
