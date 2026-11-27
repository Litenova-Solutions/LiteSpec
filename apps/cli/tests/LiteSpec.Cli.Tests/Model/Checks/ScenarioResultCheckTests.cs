using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a scenario ends in a result its use case declares, written or derived.</summary>
public sealed class ScenarioResultCheckTests
{
    /// <summary>A scenario ending in a result no record declares reports.</summary>
    [Fact]
    public void ScenarioResultNoRecordDeclaresReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Scenario(temp.Path, "result/borrow-copy.undeclared"));

        Assert.Contains("scenario-result-unknown", TestModel.Codes(model));
    }

    /// <summary>A scenario ending in a written result passes.</summary>
    [Fact]
    public void ScenarioEndingInWrittenResultPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Scenario(temp.Path, "result/borrow-copy.borrowed"),
            TestModel.Record("result", "result/borrow-copy.borrowed", Path.Combine(temp.Path, "result.yaml"),
                """{"outcome": "success", "meaning": "The copy was borrowed."}"""));

        Assert.DoesNotContain("scenario-result-unknown", TestModel.Codes(model));
    }

    /// <summary>A scenario ending in a derived generic refusal passes.</summary>
    [Fact]
    public void ScenarioEndingInDerivedRefusalPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Scenario(temp.Path, "result/borrow-copy.not-found"));

        Assert.DoesNotContain("scenario-result-unknown", TestModel.Codes(model));
    }

    private static LiteSpec.Cli.Model.Records.Record Scenario(string root, string result) => TestModel.Record(
        "scenario",
        "scenario/lending.borrow-copy-succeeds",
        Path.Combine(root, "scenario.yaml"),
        $$"""{"given": "A copy is on the shelf.", "when": "A reader borrows it.", "then": "The copy is on loan.", "result": "{{result}}"}""");
}
