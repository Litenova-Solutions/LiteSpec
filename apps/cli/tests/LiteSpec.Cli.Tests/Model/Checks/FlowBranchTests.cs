using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the flow shape checks that need the record graph: a branch names steps the flow holds,
///     a flow states where it starts, and a flow no longer carries betweenSteps.
/// </summary>
public sealed class FlowBranchTests
{
    /// <summary>The flow fields with two steps, a valid start and one branch that the case then changes.</summary>
    const string Flow = """{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "startedWhen": "The copy is on the desk.", "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "s1", "statement": "Scan the copy.", "actor": "actor/librarian"}, {"id": "s2", "statement": "Shelve the copy.", "actor": "actor/librarian"}], "branches": [{"step": "s1", "condition": "The copy is damaged.", "nextStep": "end"}]}""";

    /// <summary>A branch that leaves a step the flow does not hold, or continues at one, is reported as an invalid field.</summary>
    /// <param name="branches">The branches list written into the flow.</param>
    /// <param name="message">The finding message the check writes.</param>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Theory]
    [InlineData("""[{"step": "s9", "condition": "The copy is damaged.", "nextStep": "end"}]""", "Flow 'flow/loan-desk' branches from step 's9', which it does not hold.")]
    [InlineData("""[{"step": "s1", "condition": "The copy is damaged.", "nextStep": "s9"}]""", "Flow 'flow/loan-desk' branches to step 's9', which it does not hold.")]
    public void BranchNamingNoStepOfTheFlowIsReported(string branches, string message)
    {
        using var temp = TestCli.Temp("checks");
        var model = Model(temp.Path, WithBranches(branches));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message == message);
        Assert.Equal("flow/loan-desk", finding.Subject);
    }

    /// <summary>A branch to end, or to a step the flow holds, passes the branch check.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void BranchToAStepOfTheFlowOrToEndPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = Model(temp.Path, WithBranches("""[{"step": "s1", "condition": "The copy is damaged.", "nextStep": "end"}, {"step": "s1", "condition": "The copy is on hold.", "nextStep": "s2"}]"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("branches", StringComparison.Ordinal));
    }

    /// <summary>A flow without where it starts, or still carrying betweenSteps, is reported as an invalid record.</summary>
    /// <param name="fields">The flow fields as JSON, with the case's change applied.</param>
    /// <param name="message">The finding message the check writes.</param>
    /// <proves>litespec/core/intent#state-when-each-flow-starts</proves>
    [Theory]
    [InlineData("""{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "s1", "statement": "Scan the copy.", "actor": "actor/librarian"}]}""", "Record 'flow/loan-desk' has no startedWhen.")]
    [InlineData("""{"actor": "actor/librarian", "serves": ["goal/faster-pickup"], "startedWhen": "The copy is on the desk.", "betweenSteps": "The copy may be damaged.", "finishedWhen": "The copy is on its shelf.", "steps": [{"id": "s1", "statement": "Scan the copy.", "actor": "actor/librarian"}]}""", "Flow 'flow/loan-desk' holds betweenSteps; state each path in branches.")]
    public void FlowWithoutItsStartOrWithBetweenStepsIsReported(string fields, string message)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("flow", "flow/loan-desk", Path.Combine(temp.Path, ".litespec", "record.yaml"), fields));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message == message);
        Assert.Equal("flow/loan-desk", finding.Subject);
    }

    /// <summary>The flow with its branches replaced by the given list.</summary>
    /// <param name="branches">The branches list written into the flow.</param>
    /// <returns>The flow fields as JSON.</returns>
    static string WithBranches(string branches) =>
        Flow.Replace("""[{"step": "s1", "condition": "The copy is damaged.", "nextStep": "end"}]""", branches);

    /// <summary>A project model holding the one flow, written from the given fields.</summary>
    /// <param name="root">The temporary project root.</param>
    /// <param name="fields">The flow fields as JSON.</param>
    /// <returns>The project model.</returns>
    static ProjectModel Model(string root, string fields) =>
        TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("flow", "flow/loan-desk", Path.Combine(root, ".litespec", "record.yaml"), fields));
}
