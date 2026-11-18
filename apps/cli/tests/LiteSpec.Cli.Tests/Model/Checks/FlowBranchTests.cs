using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the flow shape checks that need the record graph: a branch continues at a step
///     of its own flow, holds exactly one of then and ends, states one condition, and a step
///     no path reaches is reported; a flow states where it starts, holds no top-level
///     branches, and no longer carries betweenSteps.
/// </summary>
public sealed class FlowBranchTests
{
    /// <summary>The flow fields with two steps, a valid start and one branch that the case then changes.</summary>
    const string Flow = """{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "startedWhen": "The copy is on the desk.", "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "step/loan-desk.scan-copy", "title": "Scan Copy", "statement": "Scan the copy.", "actor": "actor/librarian", "branches": [{"when": "The copy is damaged.", "ends": "The copy is retired and nothing is lent."}]}, {"id": "step/loan-desk.shelve-copy", "title": "Shelve Copy", "statement": "Shelve the copy.", "actor": "actor/librarian"}]}""";

    /// <summary>A branch that continues at a step of another flow is reported as an invalid field.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void BranchContinuingAtAnotherFlowsStepIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/loan-desk", Path.Combine(temp.Path, ".litespec", "record.yaml"), WithBranches("""[{"when": "The copy is damaged.", "then": "step/hold-desk.shelve-copy"}]""")),
            .. TestModel.Records("flow", "flow/hold-desk", Path.Combine(temp.Path, ".litespec", "holds.yaml"), """{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "startedWhen": "The copy is on the desk.", "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "step/hold-desk.shelve-copy", "title": "Shelve Copy", "statement": "Shelve the copy.", "actor": "actor/librarian"}]}""")]);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "step/loan-desk.scan-copy");
        Assert.Equal("step/loan-desk.scan-copy", finding.Subject);
    }

    /// <summary>A branch that continues at no step is reported as an unknown reference.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void BranchContinuingAtNoStepIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = Model(temp.Path, WithBranches("""[{"when": "The copy is damaged.", "then": "step/loan-desk.shelve-again"}]"""));

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "reference-unknown" && found.Subject == "step/loan-desk.scan-copy");
    }

    /// <summary>A branch with then and ends together, or with neither, is reported as an invalid field.</summary>
    /// <param name="branches">The branches list written into the first step.</param>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Theory]
    [InlineData("""[{"when": "The copy is damaged.", "then": "step/loan-desk.shelve-copy", "ends": "The copy is retired."}]""")]
    [InlineData("""[{"when": "The copy is damaged."}]""")]
    public void BranchWithThenAndEndsTogetherOrNeitherIsReported(string branches)
    {
        using var temp = TestCli.Temp("checks");
        var model = Model(temp.Path, WithBranches(branches));

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "step/loan-desk.scan-copy");
    }

    /// <summary>A branch joining two conditions with or is reported; one condition passes.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void BranchJoiningTwoConditionsWithOrIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var joined = Model(temp.Path, WithBranches("""[{"when": "The copy is damaged or it is on hold.", "ends": "The copy is set aside."}]"""));

        Assert.Contains(Checker.Check(joined).Findings, found => found.Code == "record-invalid" && found.Subject == "step/loan-desk.scan-copy");

        var single = Model(temp.Path, WithBranches("""[{"when": "The copy is damaged.", "ends": "The copy is set aside."}]"""));

        Assert.DoesNotContain(Checker.Check(single).Findings, found => found.Code == "record-invalid" && found.Subject == "step/loan-desk.scan-copy");
    }

    /// <summary>A branch to a step of the flow, or one that ends, passes the branch check.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void BranchToAStepOfTheFlowOrToEndPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = Model(temp.Path, WithBranches("""[{"when": "The copy is damaged.", "ends": "The copy is retired and nothing is lent."}, {"when": "The copy is on hold.", "then": "step/loan-desk.shelve-copy"}]"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "step/loan-desk.scan-copy");
    }

    /// <summary>A flow still holding its branches at the top level is reported as an invalid record.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void FlowWithTopLevelBranchesIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/loan-desk", Path.Combine(temp.Path, ".litespec", "record.yaml"), Flow.Replace(@"""finishedWhen"": ""The copy is on its shelf.""", @"""finishedWhen"": ""The copy is on its shelf."", ""branches"": [{""when"": ""The copy is damaged."", ""ends"": ""The copy is retired.""}]"))]);

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "flow/loan-desk");
    }

    /// <summary>A step that no path reaches is reported; a routed step passes.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void StepNoPathReachesIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var stranded = Model(temp.Path, WithBranches("""[{"when": "The copy is lent out.", "then": "step/loan-desk.scan-copy"}]"""));

        Assert.Contains(Checker.Check(stranded).Findings, found => found.Code == "step-unreached" && found.Subject == "step/loan-desk.shelve-copy");

        var routed = Model(temp.Path, WithBranches("""[{"when": "The copy is lent out.", "then": "step/loan-desk.shelve-copy"}]"""));

        Assert.DoesNotContain(Checker.Check(routed).Findings, found => found.Code == "step-unreached");
    }

    /// <summary>A flow without where it starts, or still carrying betweenSteps, is reported as an invalid record.</summary>
    /// <param name="fields">The flow fields as JSON, with the case's change applied.</param>
    /// <param name="message">The finding message the check writes.</param>
    /// <proves>litespec/core/intent#state-when-each-flow-starts</proves>
    [Theory]
    [InlineData("""{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "step/loan-desk.scan-copy", "title": "Scan Copy", "statement": "Scan the copy.", "actor": "actor/librarian"}]}""", "Record 'flow/loan-desk' has no startedWhen.")]
    [InlineData("""{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "startedWhen": "The copy is on the desk.", "betweenSteps": "The copy may be damaged.", "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "step/loan-desk.scan-copy", "title": "Scan Copy", "statement": "Scan the copy.", "actor": "actor/librarian"}]}""", "Flow 'flow/loan-desk' holds betweenSteps; state each path in branches.")]
    public void FlowWithoutItsStartOrWithBetweenStepsIsReported(string fields, string message)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/loan-desk", Path.Combine(temp.Path, ".litespec", "record.yaml"), fields)]);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message == message);
        Assert.Equal("flow/loan-desk", finding.Subject);
    }

    /// <summary>The flow with the branches of its first step replaced by the given list.</summary>
    /// <param name="branches">The branches list written into the first step.</param>
    /// <returns>The flow fields as JSON.</returns>
    static string WithBranches(string branches) =>
        Flow.Replace("""[{"when": "The copy is damaged.", "ends": "The copy is retired and nothing is lent."}]""", branches);

    /// <summary>A project model holding the one flow, written from the given fields.</summary>
    /// <param name="root">The temporary project root.</param>
    /// <param name="fields">The flow fields as JSON.</param>
    /// <returns>The project model.</returns>
    static ProjectModel Model(string root, string fields) =>
        TestModel.Model(
            TestModel.Config(root),
            [.. TestModel.Records("flow", "flow/loan-desk", Path.Combine(root, ".litespec", "record.yaml"), fields)]);
}
