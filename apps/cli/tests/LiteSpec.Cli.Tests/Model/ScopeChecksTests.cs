using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves every runbook answers something.</summary>
public sealed class ScopeChecksTests
{
    /// <summary>An unnamed runbook reports.</summary>
    /// <proves>litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void UnnamedRunbookReports()
    {
        using var temp = TestCli.Temp("checks");

        // The runbook codes belong to the delivery extension, so the project selects it.
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "selectedExtensions:\n  - id: delivery\n    criterion: The project runs alerts and runbooks in production.\n");

        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "runbook",
                "runbook/queue-drain",
                Path.Combine(temp.Path, "runbook.yaml"),
                """{"title": "Queue drain", "trigger": "Queue depth passes one hundred."}"""));

        Assert.Single(Checker.Check(model).Findings, finding => finding.Code == "runbook-unused");
    }

    /// <summary>A runbook a job names passes.</summary>
    /// <proves>litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void RunbookNamedByJobPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "runbook",
                "runbook/queue-drain",
                Path.Combine(temp.Path, "runbook.yaml"),
                """{"title": "Queue drain", "trigger": "Queue depth passes one hundred."}"""),
            TestModel.Record(
                "job",
                "job/drain-queue",
                Path.Combine(temp.Path, "job.yaml"),
                """{"title": "Drain queue", "runbook": "runbook/queue-drain"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "runbook-unused");
    }

    /// <summary>A runbook a use-case failure policy names passes.</summary>
    /// <proves>litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void RunbookNamedByFailurePolicyPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "runbook",
                "runbook/queue-drain",
                Path.Combine(temp.Path, "runbook.yaml"),
                """{"title": "Queue drain", "trigger": "Queue depth passes one hundred."}"""),
            TestModel.Record(
                "reaction",
                "reaction/queue-deep",
                Path.Combine(temp.Path, "reaction.yaml"),
                """{"title": "Queue deep", "failurePolicy": {"runbook": "runbook/queue-drain"}}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "runbook-unused");
    }

    /// <summary>A runbook an alert rule names passes.</summary>
    /// <proves>litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void RunbookNamedByAlertPasses()
    {
        using var temp = TestCli.Temp("checks");
        var alert = Path.Combine(temp.Path, "alerts", "queue.yaml");
        Directory.CreateDirectory(Path.Combine(temp.Path, "alerts"));
        File.WriteAllText(alert, """
            groups:
              - name: queue
                rules:
                  - alert: QueueDeep
                    expr: queue_depth > 100
                    labels:
                      runbook: runbook/queue-drain
            """);
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec", "project.yaml"),
            """{"paths": {"alerts": ["alerts"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "runbook",
                "runbook/queue-drain",
                Path.Combine(temp.Path, "runbook.yaml"),
                """{"title": "Queue drain", "trigger": "Queue depth passes one hundred."}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "runbook-unused");
    }
}
