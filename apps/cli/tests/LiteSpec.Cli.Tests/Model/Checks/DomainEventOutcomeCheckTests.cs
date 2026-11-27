using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves each domain event is consumed by a reaction or a translating event, or states that nothing consumes it.</summary>
public sealed class DomainEventOutcomeCheckTests
{
    /// <summary>A domain event no consumer names and not stated ignored reports outcome missing.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void DomainEventWithNoConsumerReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Undecided(temp.Path));

        Assert.Contains("domain-event-outcome-missing", TestModel.Codes(model));
    }

    /// <summary>A domain event named by a reaction trigger passes.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void DomainEventWithReactionConsumerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Undecided(temp.Path),
            TestModel.Record("reaction", "reaction/lending.notify-reader-on-borrow", Path.Combine(temp.Path, "notify.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"]}"""));

        Assert.DoesNotContain("domain-event-outcome-missing", TestModel.Codes(model));
        Assert.DoesNotContain("domain-event-outcome-drift", TestModel.Codes(model));
    }

    /// <summary>A domain event translated by a module event passes.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void DomainEventWithModuleEventTranslationPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Undecided(temp.Path),
            TestModel.Record("module-event", "module-event/lending.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"translates": "domain-event/loan.loan-borrowed"}"""));

        Assert.DoesNotContain("domain-event-outcome-missing", TestModel.Codes(model));
        Assert.DoesNotContain("domain-event-outcome-drift", TestModel.Codes(model));
    }

    /// <summary>A domain event translated by an integration event passes.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void DomainEventWithIntegrationEventTranslationPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Undecided(temp.Path),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, "external.yaml"),
                """{"direction": "send", "translates": "domain-event/loan.loan-borrowed"}"""));

        Assert.DoesNotContain("domain-event-outcome-missing", TestModel.Codes(model));
        Assert.DoesNotContain("domain-event-outcome-drift", TestModel.Codes(model));
    }

    /// <summary>A domain event stating ignored with no consumer passes.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void IgnoredDomainEventWithNoConsumerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": [], "ignored": true}"""));

        Assert.DoesNotContain("domain-event-outcome-missing", TestModel.Codes(model));
        Assert.DoesNotContain("domain-event-outcome-drift", TestModel.Codes(model));
    }

    /// <summary>A domain event stating ignored that a reaction names reports outcome drift.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void IgnoredDomainEventWithConsumerReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": [], "ignored": true}"""),
            TestModel.Record("reaction", "reaction/lending.notify-reader-on-borrow", Path.Combine(temp.Path, "notify.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"]}"""));

        var drift = Checker.Check(model).Findings.Single(finding => finding.Code == "domain-event-outcome-drift");

        Assert.Contains("domain-event/loan.loan-borrowed", drift.Message, StringComparison.Ordinal);
        Assert.Contains("reaction/lending.notify-reader-on-borrow", drift.Message, StringComparison.Ordinal);
    }

    /// <summary>A domain event stating ignored that a translating event names reports outcome drift.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void IgnoredDomainEventWithTranslationReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": [], "ignored": true}"""),
            TestModel.Record("module-event", "module-event/lending.loan-borrowed", Path.Combine(temp.Path, "module.yaml"),
                """{"translates": "domain-event/loan.loan-borrowed"}"""));

        Assert.Contains("domain-event-outcome-drift", TestModel.Codes(model));
    }

    static LiteSpec.Cli.Model.Records.Record Undecided(string root) => TestModel.Record(
        "domain-event",
        "domain-event/loan.loan-borrowed",
        Path.Combine(root, "borrowed.yaml"),
        """{"meaning": "A copy was lent.", "fields": []}""");
}
