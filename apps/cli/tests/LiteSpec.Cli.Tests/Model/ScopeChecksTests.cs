using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the scope checks against hand-built models.</summary>
public sealed class ScopeChecksTests
{
    /// <summary>Record Under a Markets Intent Area Reports Blocking.</summary>
    /// <proves>litespec/core/scope#reject-company-areas-in-intent</proves>
    [Fact]
    public void Record_Under_A_Markets_Intent_Area_Reports_Blocking()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("topic", "topic/pricing", Path.Combine(temp.Path, ".litespec", "intent", "markets", "pricing.yaml"), """{"status": "approved"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "intent-area-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Intent area 'markets' holds record 'topic/pricing' under a company area folder.", finding.Message);
        Assert.Equal("topic/pricing", finding.Subject);
    }

    /// <summary>Record Under a Roadmap Intent Area Reports.</summary>
    /// <proves>litespec/core/scope#reject-company-areas-in-intent</proves>
    [Fact]
    public void Record_Under_A_Roadmap_Intent_Area_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("topic", "topic/launch", Path.Combine(temp.Path, ".litespec", "intent", "roadmap", "launch.yaml"), """{"status": "approved"}"""));

        Assert.Contains("intent-area-invalid", TestModel.Codes(model));
    }

    /// <summary>Record Under a Product Intent Area Passes.</summary>
    /// <proves>litespec/core/scope#reject-company-areas-in-intent</proves>
    [Fact]
    public void Record_Under_A_Product_Intent_Area_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("topic", "topic/spending", Path.Combine(temp.Path, ".litespec", "intent", "spending", "expenses.yaml"), """{"status": "approved"}"""));

        Assert.DoesNotContain("intent-area-invalid", TestModel.Codes(model));
    }

    /// <summary>Record Directly Under Intent Passes.</summary>
    /// <proves>litespec/core/scope#reject-company-areas-in-intent</proves>
    [Fact]
    public void Record_Directly_Under_Intent_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("product", "product", Path.Combine(temp.Path, ".litespec", "intent", "brief.yaml"), """{"status": "approved"}"""));

        Assert.DoesNotContain("intent-area-invalid", TestModel.Codes(model));
    }

    /// <summary>Unnamed Runbook Reports Advisory.</summary>
    /// <proves>litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void Unnamed_Runbook_Reports_Advisory()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("runbook", "runbook/clear-stuck-refunds", Path.Combine(temp.Path, ".litespec", "behavior", "runbooks", "clear-stuck-refunds.yaml"), """{"status": "approved", "trigger": "Refunds stop moving."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "runbook-unused");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Runbook 'runbook/clear-stuck-refunds' is named by no alert, job, reaction or failure policy.", finding.Message);
        Assert.Equal("runbook/clear-stuck-refunds", finding.Subject);
    }

    /// <summary>Runbook Named by a Job Passes.</summary>
    /// <proves>litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void Runbook_Named_By_A_Job_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("runbook", "runbook/clear-stuck-refunds", Path.Combine(temp.Path, ".litespec", "behavior", "runbooks", "clear-stuck-refunds.yaml"), """{"status": "approved", "trigger": "Refunds stop moving."}"""),
            TestModel.Record("job", "spending.retry-refunds", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "retry-refunds.yaml"), """{"status": "approved", "runbook": "runbook/clear-stuck-refunds"}"""));

        Assert.DoesNotContain("runbook-unused", TestModel.Codes(model));
    }

    /// <summary>Runbook Named by a Failure Policy Passes.</summary>
    /// <proves>litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void Runbook_Named_By_A_Failure_Policy_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("runbook", "runbook/clear-stuck-refunds", Path.Combine(temp.Path, ".litespec", "behavior", "runbooks", "clear-stuck-refunds.yaml"), """{"status": "approved", "trigger": "Refunds stop moving."}"""),
            TestModel.Record("reaction", "spending.retry-refund-on-payment-failed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "retry-refund.yaml"), """{"status": "approved", "onFailure": {"mode": "skip-and-log", "runbook": "runbook/clear-stuck-refunds"}}"""));

        Assert.DoesNotContain("runbook-unused", TestModel.Codes(model));
    }

    /// <summary>Runbook Named by an Alert Passes.</summary>
    /// <proves>litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void Runbook_Named_By_An_Alert_Passes()
    {
        using var temp = Cli.Temp("checks");
        var alerts = Path.Combine(temp.Path, "alerts.yaml");
        File.WriteAllText(alerts, "groups:\n  - name: spending\n    rules:\n      - alert: refunds-stuck\n        labels:\n          requirement: requirement/spending.refunds\n          runbook: runbook/clear-stuck-refunds\n");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), """{"paths": {"alerts": ["alerts.yaml"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("runbook", "runbook/clear-stuck-refunds", Path.Combine(temp.Path, ".litespec", "behavior", "runbooks", "clear-stuck-refunds.yaml"), """{"status": "approved", "trigger": "Refunds stop moving."}"""));

        Assert.DoesNotContain("runbook-unused", TestModel.Codes(model));
    }

    /// <summary>Retired Runbook Passes When Unnamed.</summary>
    /// <proves>litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</proves>
    [Fact]
    public void Retired_Runbook_Passes_When_Unnamed()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("runbook", "runbook/clear-stuck-refunds", Path.Combine(temp.Path, ".litespec", "behavior", "runbooks", "clear-stuck-refunds.yaml"), """{"status": "retired", "trigger": "Refunds stop moving."}"""));

        Assert.DoesNotContain("runbook-unused", TestModel.Codes(model));
    }
}
