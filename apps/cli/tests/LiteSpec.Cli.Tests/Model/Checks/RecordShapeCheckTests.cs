using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the record shape checks: the unified edge grammar, the idempotency grammar and the structured failure policy.</summary>
public sealed class RecordShapeCheckTests
{
    /// <summary>Reaction Effects Runs Edge Feeds Effect Loop.</summary>
    /// <proves>litespec/core/use-cases#keep-the-effect-graph-free-of-cycles</proves>
    [Fact]
    public void ReactionEffectsRunsEdgeFeedsEffectLoop()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.return-copy", Path.Combine(temp.Path, "return.yaml"), """{"kind": "command", "raises": ["event/loan.loan-returned"]}"""),
            TestModel.Record("event", "event/loan.loan-returned", Path.Combine(temp.Path, "loan.yaml"), """{"meaning": "A copy was returned.", "carries": []}"""),
            TestModel.Record("reaction", "reaction/lending.return-again-on-loan-returned", Path.Combine(temp.Path, "again.yaml"), """{"triggeredBy": ["event/loan.loan-returned"], "effects": [{"runs": "use-case/lending.return-copy", "timing": "async"}]}"""));

        Assert.Contains("effect-loop-invalid", TestModel.Codes(model));
    }

    /// <summary>Reaction Effects Sends Edge Satisfies Notification Sender.</summary>
    /// <proves>litespec/core/edges#describe-every-notification-with-audience-channel-and-consent</proves>
    [Fact]
    public void ReactionEffectsSendsEdgeSatisfiesNotificationSender()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}]}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.yaml"), """{}"""));

        Assert.DoesNotContain("notification-sender-missing", TestModel.Codes(model));
    }

    /// <summary>Scheduled Job Effects Sends Edge Satisfies Notification Sender.</summary>
    /// <proves>litespec/core/edges#describe-every-notification-with-audience-channel-and-consent</proves>
    [Fact]
    public void ScheduledJobEffectsSendsEdgeSatisfiesNotificationSender()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"sends": "notification/lending.loan-due", "timing": "async"}]}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.yaml"), """{}"""));

        Assert.DoesNotContain("notification-sender-missing", TestModel.Codes(model));
    }

    /// <summary>Reaction Without Effects Reports Record Shape.</summary>
    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void ReactionWithoutEffectsReportsRecordShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"]}"""));

        Assert.True(Reports(model, "record-invalid", "has no effects"));
    }

    /// <summary>Reaction Effects Without Timing Pass Shape.</summary>
    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void ReactionEffectsWithoutTimingPassShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}]}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.yaml"), """{"meaning": "A copy was lent.", "carries": []}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.yaml"), """{}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Key Placeholder Naming No Input Field Reports.</summary>
    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void KeyPlaceholderNamingNoInputFieldReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "copyId", "meaning": "The copy."}], "idempotency": {"mode": "key", "key": "loan:{copy}", "scope": "tenant"}}"""));

        Assert.True(Reports(model, "record-invalid", "placeholder 'copy' names no input field"));
        Assert.True(Reports(model, "record-invalid", "input fields in scope: copyId"));
    }

    /// <summary>Key Without Placeholder Reports.</summary>
    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void KeyWithoutPlaceholderReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "copyId", "meaning": "The copy."}], "idempotency": {"mode": "key", "key": "notice per loan", "scope": "tenant"}}"""));

        Assert.True(Reports(model, "record-invalid", "with no {field} placeholder"));
        Assert.True(Reports(model, "record-invalid", "input fields in scope: copyId"));
    }

    /// <summary>Unknown Scope Reports.</summary>
    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void UnknownScopeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "idempotency": {"mode": "key", "key": "loan:{copyId}", "scope": "branch"}}"""));

        Assert.True(Reports(model, "record-invalid", "tenant, account, login, global"));
    }

    /// <summary>Valid Key And Scope Pass Shape.</summary>
    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void ValidKeyAndScopePassShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "copyId", "meaning": "The copy."}], "idempotency": {"mode": "key", "key": "loan:{copyId}", "scope": "login"}}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Reaction Key Names Event Fields Unchecked.</summary>
    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void ReactionKeyNamesEventFieldsUnchecked()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}], "idempotency": {"mode": "key", "key": "notice:{loan}", "scope": "global"}}"""));

        Assert.False(Reports(model, "record-invalid", "idempotency key"));
    }

    /// <summary>Reaction Prose OnFailure Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void ReactionProseOnFailureReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": "Retry a few times, then give up."}"""));

        Assert.True(Reports(model, "record-invalid", "as prose"));
    }

    /// <summary>Scheduled Job Prose OnFailure Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void ScheduledJobProseOnFailureReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": "Retry a few times, then give up."}"""));

        Assert.True(Reports(model, "record-invalid", "as prose"));
    }

    /// <summary>Missing Mode Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void MissingModeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": {}}"""));

        Assert.True(Reports(model, "record-invalid", "onFailure with no mode"));
    }

    /// <summary>Unknown Mode Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void UnknownModeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": {"mode": "give-up"}}"""));

        Assert.True(Reports(model, "record-invalid", "mode 'give-up' outside retry, skip-and-log, discard"));
    }

    /// <summary>Malformed Retry Delay Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void MalformedRetryDelayReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5, "initialDelay": "5 seconds", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "initialDelay '5 seconds', which is not an ISO 8601 duration"));
    }

    /// <summary>Missing InitialDelay Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void MissingInitialDelayReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5}, "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "retry initialDelay with no duration"));
    }

    /// <summary>Missing MaxAttempts Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void MissingMaxAttemptsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": {"mode": "retry", "retry": {"initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "no positive maxAttempts"));
    }

    /// <summary>Missing Retry Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void MissingRetryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": {"mode": "retry", "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "onFailure with no retry"));
    }

    /// <summary>Missing Then Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void MissingThenReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5, "initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}}}"""));

        Assert.True(Reports(model, "record-invalid", "retry with no then"));
    }

    /// <summary>Unknown Then Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void UnknownThenReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5, "initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "requeue"}}"""));

        Assert.True(Reports(model, "record-invalid", "then 'requeue' outside dead-letter"));
    }

    /// <summary>Structured OnFailure Passes Shape.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void StructuredOnFailurePassesShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": {"mode": "retry", "retry": {"maxAttempts": 10, "initialDelay": "PT5S", "maxDelay": "PT1M", "backoff": "exponential"}, "then": "dead-letter"}}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.yaml"), """{"meaning": "A copy was lent.", "carries": []}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.yaml"), """{}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Skip And Log Passes Shape.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void SkipAndLogPassesShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "manual", "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}], "onFailure": {"mode": "skip-and-log"}}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Discard Passes Shape.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void DiscardPassesShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "manual", "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}], "onFailure": {"mode": "discard"}}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Effect Prose OnFailure Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void EffectProseOnFailureReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "effects": [{"runs": "use-case/lending.notify-reader", "timing": "async", "onFailure": "retry (5, PT1M) then dead-letter"}]}"""));

        Assert.True(Reports(model, "record-invalid", "effect onFailure 'retry (5, PT1M) then dead-letter' as prose"));
    }

    /// <summary>Effect Retry Without Attempts Reports.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void EffectRetryWithoutAttemptsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "effects": [{"runs": "use-case/lending.notify-reader", "timing": "async", "onFailure": {"mode": "retry", "retry": {"initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "dead-letter"}}]}"""));

        Assert.True(Reports(model, "record-invalid", "effect onFailure retry with no positive maxAttempts"));
    }

    /// <summary>Effect Structured OnFailure Passes Shape.</summary>
    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job</proves>
    [Fact]
    public void EffectStructuredOnFailurePassesShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "effects": [{"runs": "use-case/lending.notify-reader", "timing": "async", "onFailure": {"mode": "discard"}}]}"""));

        Assert.False(Reports(model, "record-invalid", "onFailure"));
    }

    /// <summary>Reservation Without Released By Reports.</summary>
    /// <proves>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</proves>
    [Fact]
    public void ReservationWithoutReleasedByReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"]}"""));

        Assert.True(Reports(model, "reservation-release-missing", "reserves with no releasedBy"));
    }

    /// <summary>Reservation With Never Object And Reason Passes.</summary>
    /// <proves>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</proves>
    [Fact]
    public void ReservationWithNeverObjectAndReasonPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": {"mode": "never", "reason": "Holds never expire."}}"""));

        Assert.DoesNotContain("reservation-release-missing", TestModel.Codes(model));
    }

    /// <summary>Reservation With Never Object And No Reason Reports.</summary>
    /// <proves>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</proves>
    [Fact]
    public void ReservationWithNeverObjectAndNoReasonReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": {"mode": "never"}}"""));

        Assert.True(Reports(model, "reservation-release-missing", "names never with no reason"));
    }

    /// <summary>Reservation With Use Case Id Passes.</summary>
    /// <proves>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</proves>
    [Fact]
    public void ReservationWithUseCaseIdPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": "use-case/lending.release-hold"}"""));

        Assert.DoesNotContain("reservation-release-missing", TestModel.Codes(model));
    }

    /// <summary>Reservation With Legacy Never String Reports.</summary>
    /// <proves>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</proves>
    [Fact]
    public void ReservationWithLegacyNeverStringReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.yaml"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": "never (the hold never expires)"}"""));

        Assert.True(Reports(model, "field-invalid", "neither a record id nor a never object"));
    }

    /// <summary>Goal With DueBy Passes Shape.</summary>
    /// <proves>litespec/core/intent#state-each-goal-with-its-measure</proves>
    [Fact]
    public void GoalWithDueByPassesShape()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.yaml"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <param name="code">The finding code to look for.</param>
    /// <param name="text">The text the finding message must contain.</param>
    /// <returns>True when a finding carries the code and the text.</returns>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
