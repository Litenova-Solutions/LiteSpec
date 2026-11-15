using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the reaction subscription checks against hand-built models.</summary>
public sealed class ReactionSubscriptionCheckTests
{
    /// <summary>Handler Subscribed To An Undeclared Event Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers</proves>
    [Fact]
    public void HandlerSubscribedToAnUndeclaredEventReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModelWithHandler(temp.Path, ["domain-event/loan.loan-borrowed"], "LoanReturnedEvent");

        var drift = Checker.Check(model).Findings.Single(finding => finding.Code == "reaction-subscription-drift");

        Assert.Contains("NotifyReaderOnCopyBorrowedHandler", drift.Message, StringComparison.Ordinal);
        Assert.Contains("domain-event/loan.loan-returned", drift.Message, StringComparison.Ordinal);
    }

    /// <summary>Declared Trigger With No Subscribed Handler Reports Unhandled.</summary>
    /// <proves>litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers</proves>
    [Fact]
    public void DeclaredTriggerWithNoSubscribedHandlerReportsUnhandled()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModelWithHandler(temp.Path, ["domain-event/loan.loan-borrowed", "domain-event/loan.loan-returned"], "LoanBorrowedEvent");

        var findings = Checker.Check(model).Findings;
        var unhandled = Assert.Single(findings, finding => finding.Code == "reaction-trigger-unhandled");

        Assert.Contains("domain-event/loan.loan-returned", unhandled.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(findings, finding => finding.Code == "reaction-subscription-drift");
    }

    /// <summary>Handler Subscribed To Its Declared Trigger Passes.</summary>
    /// <proves>litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers</proves>
    [Fact]
    public void HandlerSubscribedToItsDeclaredTriggerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModelWithHandler(temp.Path, ["domain-event/loan.loan-borrowed"], "LoanBorrowedEvent");

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("reaction-subscription-drift", codes);
        Assert.DoesNotContain("reaction-trigger-unhandled", codes);
    }

    /// <summary>A model with one reaction, its two domain events, and one marked handler subscribed to the given event type.</summary>
    /// <param name="root">The consumer root folder holding the record files.</param>
    /// <param name="triggers">The event identifiers the reaction names in triggeredBy.</param>
    /// <param name="eventType">The simple name of the event type the handler subscribes to.</param>
    /// <returns>The project model holding the records and the scanned handler.</returns>
    static ProjectModel ModelWithHandler(string root, string[] triggers, string eventType)
    {
        var triggerList = string.Join(", ", triggers.Select(trigger => $"\"{trigger}\""));
        var model = TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("reaction", "reaction/lending.notify-reader-on-borrow", Path.Combine(root, "notify.yaml"), """{"status": "reviewed", "triggeredBy": [""" + triggerList + """], "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(root, "borrowed.yaml"), """{"status": "reviewed", "meaning": "A copy was lent.", "carries": []}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-returned", Path.Combine(root, "returned.yaml"), """{"status": "reviewed", "meaning": "A copy was returned.", "carries": []}"""));

        var file = Path.Combine(root, "NotifyReaderOnCopyBorrowedHandler.cs");
        model.Code.Events.Add(new CodeDomainEvent("LoanBorrowedEvent", Path.Combine(root, "LoanBorrowedEvent.cs"), 3, "domain-event/loan.loan-borrowed"));
        model.Code.Events.Add(new CodeDomainEvent("LoanReturnedEvent", Path.Combine(root, "LoanReturnedEvent.cs"), 3, "domain-event/loan.loan-returned"));
        model.Code.Reactions.Add(new CodeReaction(
            "NotifyReaderOnCopyBorrowedHandler", file, 5, null, "reaction/lending.notify-reader-on-borrow", EventType: eventType));

        return model;
    }
}
