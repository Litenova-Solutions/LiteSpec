using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the step identifier checks: a step names its own flow, and its act is a verb phrase rather than a position.</summary>
public sealed class StepIdTests
{
    /// <summary>A step naming its own flow with a verb act passes.</summary>
    /// <proves>litespec/core/language#use-the-declared-record-identifier-grammar</proves>
    [Fact]
    public void StepNamingItsOwnFlowPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}""")]);

        Assert.DoesNotContain("identifier-invalid", TestModel.Codes(model));
    }

    /// <summary>A step naming another flow reports.</summary>
    /// <proves>litespec/core/language#use-the-declared-record-identifier-grammar</proves>
    [Fact]
    public void StepNamingAnotherFlowReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/renew-loan.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}""")]);

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "identifier-invalid" && found.Subject == "step/renew-loan.borrow-copy");
    }

    /// <summary>A step with a positional act reports.</summary>
    /// <param name="act">The positional act written into the step id.</param>
    /// <proves>litespec/core/language#use-the-declared-record-identifier-grammar</proves>
    [Theory]
    [InlineData("s3")]
    [InlineData("step-2")]
    [InlineData("first")]
    public void StepWithAPositionalActReports(string act)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "step/borrow-and-return.ACT", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}""".Replace("ACT", act))]);

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "identifier-invalid" && found.Subject == $"step/borrow-and-return.{act}");
    }
}
