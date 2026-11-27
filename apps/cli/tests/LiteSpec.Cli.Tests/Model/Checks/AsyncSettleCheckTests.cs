using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the async settle checks against hand-built models: a declared tracksState on a reaction or job running a use case writing an aggregate names the pending, done and refused states the run tracks, entered and distinct.</summary>
public sealed class AsyncSettleCheckTests
{
    /// <summary>The finding codes the async settle checks report.</summary>
    static readonly string[] AsyncSettleCodes = ["async-pending-missing", "async-refused-unrecorded"];

    /// <summary>A reaction settling distinct pending, done and refused states entered in order reports no async settle finding.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void SettledReactionWithThreeStatesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."},
                  {"name": "Lapsed", "meaning": "The hold lapsed with no copy."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.place-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"},
                  {"from": "Placed", "to": "Lapsed", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.place-hold", Path.Combine(temp.Path, "place.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.hold", "pending": "Placed", "done": "Ready", "refused": "Lapsed"},
                 "outcomes": [
                  {"name": "completed", "meaning": "The hold was confirmed."},
                  {"name": "failed", "meaning": "The hold lapsed with no copy."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.DoesNotContain(TestModel.Codes(model), code => AsyncSettleCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>A job settling pending and done with a declared retry and no refused state reports no async settle finding.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void SettledJobWithRetryAndNoRefusedStatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """
                {"states": [
                  {"name": "Active", "meaning": "The loan is open."},
                  {"name": "Erased", "meaning": "The borrower link was erased."}],
                 "transitions": [
                  {"from": "-", "to": "Active", "movedBy": "use-case/lending.borrow-copy"},
                  {"from": "Active", "to": "Erased", "movedBy": "job/lending.erase-expired-borrower"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.loan"]}
                """),
            TestModel.Record("use-case", "use-case/lending.erase-expired-loan", Path.Combine(temp.Path, "erase.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.loan"]}
                """),
            TestModel.Record("job", "job/lending.erase-expired-borrower", Path.Combine(temp.Path, "job.yaml"), """
                {"kind": "scheduled", "schedule": {"every": "P1D"},
                 "effects": [{"runs": "use-case/lending.erase-expired-loan", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.loan", "pending": "Active", "done": "Erased"},
                 "outcomes": [
                  {"name": "completed", "meaning": "Expired loans were erased."},
                  {"name": "nothing-selected", "meaning": "No loan was past retention."},
                  {"name": "retry-scheduled", "meaning": "The run failed and another attempt is scheduled."}],
                 "onFailure": {"mode": "retry", "retry": {"maxAttempts": 3, "initialDelay": "PT5M", "maxDelay": "PT1H", "backoff": "exponential"}, "ifExhausted": "dead-letter"}}
                """));

        Assert.DoesNotContain(TestModel.Codes(model), code => AsyncSettleCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>A reaction running no use case settles nothing and reports no async settle finding without tracksState.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void SendOnlyReactionWithoutTracksStatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.yaml"), """
                {"meaning": "A copy was lent.", "fields": []}
                """),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "notice.yaml"), """
                {"summary": "The due notice.", "template": "loan-due", "audience": ["actor/reader"], "channels": ["email"],
                 "via": "external-system/mail-relay", "frequency": "once", "deduplicationKey": "loan-due-{loanId}",
                 "parameters": [], "locale": "en"}
                """),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "remind.yaml"), """
                {"triggeredBy": ["domain-event/loan.loan-borrowed"],
                 "effects": [{"sends": "notification/lending.loan-due", "timing": "async"}],
                 "outcomes": [{"name": "completed", "meaning": "The notice was sent."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.DoesNotContain(TestModel.Codes(model), code => AsyncSettleCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>A reaction running a use case that writes nothing settles nothing and reports no async settle finding without tracksState.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void ReactionRunningAUseCaseThatWritesNothingPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.record-scan", Path.Combine(temp.Path, "scan.yaml"), """
                {"kind": "command", "writes": []}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.record-scan-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.record-scan", "timing": "async"}],
                 "outcomes": [{"name": "completed", "meaning": "The scan was recorded."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.DoesNotContain(TestModel.Codes(model), code => AsyncSettleCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>A reaction running a use case that writes an aggregate without tracksState states nothing and reports no async settle finding.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void RunWithoutTracksStatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."}],
                 "transitions": [{"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "outcomes": [{"name": "completed", "meaning": "The hold was confirmed."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.DoesNotContain(TestModel.Codes(model), code => AsyncSettleCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>TracksState naming one state as both pending and done reports async-pending-missing.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void TracksStateNamingOneStateTwiceReportsPendingMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.place-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.place-hold", Path.Combine(temp.Path, "place.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.hold", "pending": "Ready", "done": "Ready"},
                 "outcomes": [
                  {"name": "completed", "meaning": "The hold was confirmed."},
                  {"name": "retry-scheduled", "meaning": "Another attempt is scheduled."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.Contains("async-pending-missing", TestModel.Codes(model));
    }

    /// <summary>TracksState naming a pending state that only the settled run enters reports async-pending-missing.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void PendingEnteredOnlyByTheSettledRunReportsPendingMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."},
                  {"name": "Lapsed", "meaning": "The hold lapsed with no copy."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.confirm-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.hold", "pending": "Placed", "done": "Ready", "refused": "Lapsed"},
                 "outcomes": [{"name": "completed", "meaning": "The hold was confirmed."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.Contains("async-pending-missing", TestModel.Codes(model));
    }

    /// <summary>TracksState naming a state the aggregate does not hold reports reference-unknown.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void TracksStateNamingAnUnknownStateReportsReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.place-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.place-hold", Path.Combine(temp.Path, "place.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.hold", "pending": "Queued", "done": "Ready"},
                 "outcomes": [
                  {"name": "completed", "meaning": "The hold was confirmed."},
                  {"name": "retry-scheduled", "meaning": "Another attempt is scheduled."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.Contains("reference-unknown", TestModel.Codes(model));
    }

    /// <summary>TracksState naming an aggregate the run does not write reports async-pending-missing.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void TracksStateNamingAnotherAggregateReportsPendingMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.place-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.place-hold", Path.Combine(temp.Path, "place.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.loan", "pending": "Placed", "done": "Ready"},
                 "outcomes": [{"name": "completed", "meaning": "The hold was confirmed."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        var codes = TestModel.Codes(model);
        Assert.Contains("async-pending-missing", codes);
        Assert.Contains("reference-unknown", codes);
    }

    /// <summary>A settled run with no refused state and no declared retry reports async-refused-unrecorded.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void SettledRunWithoutRefusedStateOrRetryReportsRefusedUnrecorded()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.place-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.place-hold", Path.Combine(temp.Path, "place.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.hold", "pending": "Placed", "done": "Ready"},
                 "outcomes": [
                  {"name": "completed", "meaning": "The hold was confirmed."},
                  {"name": "failed", "meaning": "The hold could not be confirmed."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.Contains("async-refused-unrecorded", TestModel.Codes(model));
    }

    /// <summary>TracksState naming the done state again as refused reports async-refused-unrecorded.</summary>
    /// <proves>litespec/core/automation#settle-every-async-outcome-in-visible-states</proves>
    [Fact]
    public void RefusedRepeatingTheDoneStateReportsRefusedUnrecorded()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.yaml"), """
                {"states": [
                  {"name": "Placed", "meaning": "The hold waits for its copy."},
                  {"name": "Ready", "meaning": "The copy waits on the shelf."}],
                 "transitions": [
                  {"from": "-", "to": "Placed", "movedBy": "use-case/lending.place-hold"},
                  {"from": "Placed", "to": "Ready", "movedBy": "use-case/lending.confirm-hold"}]}
                """),
            TestModel.Record("use-case", "use-case/lending.place-hold", Path.Combine(temp.Path, "place.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("use-case", "use-case/lending.confirm-hold", Path.Combine(temp.Path, "confirm.yaml"), """
                {"kind": "command", "writes": ["aggregate/lending.hold"]}
                """),
            TestModel.Record("domain-event", "domain-event/copy.copy-returned", Path.Combine(temp.Path, "returned.yaml"), """
                {"meaning": "A copy was returned.", "fields": []}
                """),
            TestModel.Record("reaction", "reaction/lending.confirm-hold-on-copy-returned", Path.Combine(temp.Path, "reaction.yaml"), """
                {"triggeredBy": ["domain-event/copy.copy-returned"],
                 "effects": [{"runs": "use-case/lending.confirm-hold", "timing": "async"}],
                 "tracksState": {"aggregate": "aggregate/lending.hold", "pending": "Placed", "done": "Ready", "refused": "Ready"},
                 "outcomes": [{"name": "completed", "meaning": "The hold was confirmed."}],
                 "onFailure": {"mode": "skip-and-log"}}
                """));

        Assert.Contains("async-refused-unrecorded", TestModel.Codes(model));
    }
}
