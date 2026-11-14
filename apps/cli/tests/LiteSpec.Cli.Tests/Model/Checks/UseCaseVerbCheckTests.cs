using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the job naming check: a job carries the verb of the use case it runs.</summary>
public sealed class UseCaseVerbCheckTests
{
    /// <summary>Job Carries The Verb Of The Use Case It Runs.</summary>
    /// <proves>litespec/core/automation#name-each-job-for-the-use-case-it-runs</proves>
    [Fact]
    public void JobCarriesTheVerbOfTheUseCaseItRuns()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.expire-holds", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "holds", "expire-holds.yaml"), """{"kind": "command"}"""),
            TestModel.Record("job", "job/lending.expire-lapsed-holds", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "jobs", "expire-lapsed-holds.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"runs": "use-case/lending.expire-holds"}]}"""),
            TestModel.Record("job", "job/lending.sweep-holds", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "jobs", "sweep-holds.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"runs": "use-case/lending.expire-holds"}]}"""));

        var form = Checker.Check(model).Findings.Where(finding => finding.Code == "job-name-invalid").ToList();

        Assert.Single(form);
        Assert.Contains("sweep-holds", form[0].Message);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), "{}");
}
