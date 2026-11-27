using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves each domain event type carries one outbox decision in code, matching the outcome the records state.</summary>
public sealed class OutboxDecisionCheckTests
{
    /// <summary>A domain event type with no outbox decision reports decision missing.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void EventTypeWithNoDecisionReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "outbox");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": []}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(temp.Path, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));

        var missing = Checker.Check(model).Findings.Single(finding => finding.Code == "outbox-decision-missing");

        Assert.Contains("LoanBorrowedEvent", missing.Message, StringComparison.Ordinal);
    }

    /// <summary>A domain event type translated in code while its record states ignored reports decision drift.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void TranslatedTypeStatedIgnoredReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "outbox");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": [], "ignored": true}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(temp.Path, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));
        model.Code.OutboxDecisions.Add(new CodeOutboxDecision("LoanBorrowedEvent", Path.Combine(temp.Path, "OutboxComposition.cs"), 5, Ignored: false));

        var drift = Checker.Check(model).Findings.Single(finding => finding.Code == "outbox-decision-drift");

        Assert.Contains("LoanBorrowedEvent", drift.Message, StringComparison.Ordinal);
        Assert.Contains("ignored", drift.Message, StringComparison.Ordinal);
    }

    /// <summary>A domain event type ignored in code while a reaction consumes it reports decision drift.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void IgnoredTypeWithReactionConsumerReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "outbox");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.notify-reader-on-borrow", Path.Combine(temp.Path, "notify.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"]}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(temp.Path, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));
        model.Code.OutboxDecisions.Add(new CodeOutboxDecision("LoanBorrowedEvent", Path.Combine(temp.Path, "OutboxComposition.cs"), 5, Ignored: true));

        var drift = Checker.Check(model).Findings.Single(finding => finding.Code == "outbox-decision-drift");

        Assert.Contains("LoanBorrowedEvent", drift.Message, StringComparison.Ordinal);
        Assert.Contains("reaction/lending.notify-reader-on-borrow", drift.Message, StringComparison.Ordinal);
    }

    /// <summary>A domain event type translated in code with a reaction consumer passes.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void TranslatedTypeWithConsumerPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "outbox");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.notify-reader-on-borrow", Path.Combine(temp.Path, "notify.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"]}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(temp.Path, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));
        model.Code.OutboxDecisions.Add(new CodeOutboxDecision("LoanBorrowedEvent", Path.Combine(temp.Path, "OutboxComposition.cs"), 5, Ignored: false));

        Assert.DoesNotContain("outbox-decision-drift", TestModel.Codes(model));
        Assert.DoesNotContain("outbox-decision-missing", TestModel.Codes(model));
    }

    /// <summary>A domain event type ignored in code with an ignored record and no consumer passes.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void IgnoredTypeStatedIgnoredPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "outbox");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": [], "ignored": true}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(temp.Path, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));
        model.Code.OutboxDecisions.Add(new CodeOutboxDecision("LoanBorrowedEvent", Path.Combine(temp.Path, "OutboxComposition.cs"), 5, Ignored: true));

        Assert.DoesNotContain("outbox-decision-drift", TestModel.Codes(model));
        Assert.DoesNotContain("outbox-decision-missing", TestModel.Codes(model));
    }

    /// <summary>A domain event type with no outbox decision passes when the outbox extension is not selected.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void EventTypeWithNoDecisionPassesWithoutOutbox()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": []}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(temp.Path, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));

        Assert.DoesNotContain("outbox-decision-drift", TestModel.Codes(model));
        Assert.DoesNotContain("outbox-decision-missing", TestModel.Codes(model));
    }

    /// <summary>A simple event name two records claim reports neither drift nor missing.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void AmbiguousEventNameReportsNeitherDriftNorMissing()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "outbox");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "borrowed.yaml"),
                """{"meaning": "A copy was lent.", "fields": []}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-returned", Path.Combine(temp.Path, "returned.yaml"),
                """{"meaning": "A copy was returned.", "fields": []}"""));
        model.Code.Events.Add(new CodeDomainEvent("LoanEvent", Path.Combine(temp.Path, "Borrowing", "LoanEvent.cs"), 3, "domain-event/loan.loan-borrowed"));
        model.Code.Events.Add(new CodeDomainEvent("LoanEvent", Path.Combine(temp.Path, "Returns", "LoanEvent.cs"), 3, "domain-event/loan.loan-returned"));

        Assert.DoesNotContain("outbox-decision-drift", TestModel.Codes(model));
        Assert.DoesNotContain("outbox-decision-missing", TestModel.Codes(model));
    }

    /// <summary>Selects the extensions in the consumer declaration the config reads.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="ids">The extension ids to select.</param>
    static void SelectExtensions(string root, params string[] ids)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));

        var entries = string.Join(
            "\n",
            ids.Select(id => $"  - id: {id}\n    criterion: The product needs the {id} extension."));

        File.WriteAllText(Path.Combine(root, ".litespec", "project.yaml"), "selectedExtensions:\n" + entries + "\n");
    }
}
