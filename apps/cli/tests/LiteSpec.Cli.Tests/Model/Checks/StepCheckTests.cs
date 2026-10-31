using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the step check counts use cases, reactions and jobs.</summary>
public sealed class StepCheckTests
{
    [Fact]
    public void Use_Case_Serving_A_Step_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Journey(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"serves": ["journey/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("step-use-case-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Reaction_Serving_A_Step_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Journey(temp.Path),
            TestModel.Record("reaction", "reaction/lending.scan-overdue-on-loan-due", Path.Combine(temp.Path, "scan.md"), """{"triggeredBy": ["event/loan.loan-due"], "serves": ["journey/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("step-use-case-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Job_Serving_A_Step_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Journey(temp.Path),
            TestModel.Record("job", "job/lending.scan-overdue", Path.Combine(temp.Path, "scan.md"), """{"kind": "manual", "guard": "An operator starts the scan.", "serves": ["journey/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("step-use-case-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Step_With_No_Server_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Journey(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "step-use-case-missing");
        Assert.Equal("Journey step 'journey/borrow-and-return#s1' is served by no use case, reaction or job.", finding.Message);
        Assert.Equal("journey/borrow-and-return#s1", finding.Subject);
    }

    [Fact]
    public void Retired_Server_Does_Not_Cover_A_Step()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Journey(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "retired", "serves": ["journey/borrow-and-return#s1"]}"""));

        Assert.Contains("step-use-case-missing", TestModel.Codes(model));
    }

    /// <summary>A journey walking one step the servers serve.</summary>
    static Record Journey(string root) =>
        TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(root, "borrow-and-return.md"), """{"actor": ["actor/reader"], "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}""");
}
