using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that a reaction or job states failure handling on the record, not in an effect entry.</summary>
public sealed class OnFailurePlacementCheckTests
{
    /// <summary>An effect entry onFailure on a reaction reports.</summary>
    /// <proves>litespec/core/automation#keep-failure-handling-on-the-reaction-or-job</proves>
    [Fact]
    public void ReactionEffectOnFailureReports()
    {
        var model = ModelWith("reaction", """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.borrow-copy", "onFailure": {"mode": "discard"}}], "onFailure": {"mode": "discard"}}""");

        var finding = Assert.Single(Findings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reaction/lending.react-on-borrow", finding.Subject);
        Assert.Contains("states onFailure in an effect entry", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>An effect entry onFailure on a job reports once for the whole record.</summary>
    /// <proves>litespec/core/automation#keep-failure-handling-on-the-reaction-or-job</proves>
    [Fact]
    public void JobEffectOnFailureReportsOnce()
    {
        var model = ModelWith("job", """{"kind": "scheduled", "effects": [{"runs": "use-case/lending.borrow-copy", "onFailure": {"mode": "discard"}}, {"sends": "notification/lending.notice", "onFailure": {"mode": "discard"}}], "onFailure": {"mode": "discard"}}""");

        Assert.Single(Findings(model));
    }

    /// <summary>A reaction with failure handling only on the record passes.</summary>
    /// <proves>litespec/core/automation#keep-failure-handling-on-the-reaction-or-job</proves>
    [Fact]
    public void ReactionRecordOnFailurePasses()
    {
        var model = ModelWith("reaction", """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.borrow-copy"}], "onFailure": {"mode": "discard"}}""");

        Assert.Empty(Findings(model));
    }

    /// <summary>A use-case effect keeps its own onFailure: the home rule covers only reactions and jobs.</summary>
    /// <proves>litespec/core/automation#keep-failure-handling-on-the-reaction-or-job</proves>
    [Fact]
    public void UseCaseEffectOnFailurePasses()
    {
        var model = ModelWith("use-case", """{"kind": "command", "effects": [{"sends": "notification/lending.notice", "timing": "after-commit", "onFailure": {"mode": "discard"}}]}""");

        Assert.Empty(Findings(model));
    }

    /// <summary>A model holding one holder record of the given type.</summary>
    /// <param name="type">The record type: reaction, job, or use-case.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The hand-built project model.</returns>
    static ProjectModel ModelWith(string type, string json)
    {
        var temp = TestCli.Temp("checks");
        var id = type == "use-case" ? "use-case/lending.borrow-copy" : type + "/lending.react-on-borrow";

        return TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                type,
                id,
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "records.yaml"),
                json));
    }

    /// <summary>The on-failure placement findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is on-failure-placement-invalid.</returns>
    static List<Finding> Findings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "on-failure-placement-invalid").ToList();
}
