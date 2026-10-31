using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the record shape checks: the unified edge grammar, the idempotency grammar, the structured failure policy, the renamed fields and the layer status vocabulary.</summary>
public sealed class RecordShapeCheckTests
{
    /// <proves>litespec/core/use-cases#keep-the-effect-graph-free-of-cycles</proves>
    [Fact]
    public void Automation_Effects_Runs_Edge_Feeds_Effect_Loop()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.return-copy", Path.Combine(temp.Path, "return.md"), """{"status": "reviewed", "kind": "command", "raises": ["event/loan.loan-returned"]}"""),
            TestModel.Record("event", "event/loan.loan-returned", Path.Combine(temp.Path, "loan.md"), """{"status": "reviewed", "meaning": "A copy was returned.", "carries": []}"""),
            TestModel.Record("automation", "automation/lending.return-again-on-loan-returned", Path.Combine(temp.Path, "again.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-returned"}, "effects": [{"runs": "use-case/lending.return-copy", "timing": "async"}]}"""));

        Assert.Contains("effect-loop-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/edges#describe-every-notification-with-audience-channel-and-consent</proves>
    [Fact]
    public void Automation_Effects_Sends_Edge_Satisfies_Notification_Sender()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}]}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.md"), """{"status": "reviewed"}"""));

        Assert.DoesNotContain("notification-sender-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/edges#describe-every-notification-with-audience-channel-and-consent</proves>
    [Fact]
    public void Scheduled_Automation_Effects_Sends_Edge_Satisfies_Notification_Sender()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"schedule": {"every": "PT1H"}}, "effects": [{"sends": "notification/lending.loan-due", "timing": "async"}]}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.md"), """{"status": "reviewed"}"""));

        Assert.DoesNotContain("notification-sender-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void Automation_With_Top_Level_Runs_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "runs": "use-case/lending.notify-overdue"}"""));

        Assert.True(Reports(model, "record-invalid", "top-level runs"));
    }

    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void Automation_With_Top_Level_Sends_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "sends": "notification/lending.loan-due"}"""));

        Assert.True(Reports(model, "record-invalid", "top-level sends"));
    }

    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void Scheduled_Automation_With_Top_Level_Runs_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"schedule": {"every": "PT1H"}}, "runs": ["use-case/lending.mark-due"]}"""));

        Assert.True(Reports(model, "record-invalid", "top-level runs"));
    }

    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void Automation_Without_Effects_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-borrowed"}}"""));

        Assert.True(Reports(model, "record-invalid", "has no effects"));
    }

    /// <proves>litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing</proves>
    [Fact]
    public void Automation_Effects_Without_Timing_Pass_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}]}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "meaning": "A copy was lent.", "carries": []}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01"}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void Key_Placeholder_Naming_No_Input_Field_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "copyId", "meaning": "The copy."}], "idempotency": {"mode": "key", "key": "loan:{copy}", "scope": "tenant"}}"""));

        Assert.True(Reports(model, "record-invalid", "placeholder 'copy' names no input field"));
        Assert.True(Reports(model, "record-invalid", "input fields in scope: copyId"));
    }

    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void Key_Without_Placeholder_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "copyId", "meaning": "The copy."}], "idempotency": {"mode": "key", "key": "notice per loan", "scope": "tenant"}}"""));

        Assert.True(Reports(model, "record-invalid", "with no {field} placeholder"));
        Assert.True(Reports(model, "record-invalid", "input fields in scope: copyId"));
    }

    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void Unknown_Scope_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "reachedThrough": ["entry-point/library-app"], "idempotency": {"mode": "key", "key": "loan:{copyId}", "scope": "branch"}}"""));

        Assert.True(Reports(model, "record-invalid", "tenant, account, login, global"));
    }

    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void Valid_Key_And_Scope_Pass_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "copyId", "meaning": "The copy."}], "idempotency": {"mode": "key", "key": "loan:{copyId}", "scope": "login"}}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-the-idempotency-of-every-use-case</proves>
    [Fact]
    public void Automation_Key_Names_Event_Fields_Unchecked()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}], "idempotency": {"mode": "key", "key": "notice:{loan}", "scope": "global"}}"""));

        Assert.False(Reports(model, "record-invalid", "idempotency key"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Automation_Prose_OnFailure_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": "Retry a few times, then give up."}"""));

        Assert.True(Reports(model, "record-invalid", "as prose"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Scheduled_Automation_Prose_OnFailure_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": "Retry a few times, then give up."}"""));

        Assert.True(Reports(model, "record-invalid", "as prose"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Missing_Mode_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": {}}"""));

        Assert.True(Reports(model, "record-invalid", "onFailure with no mode"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Unknown_Mode_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": {"mode": "give-up"}}"""));

        Assert.True(Reports(model, "record-invalid", "mode 'give-up' outside retry, skip-and-log, discard"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Malformed_Retry_Delay_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5, "initialDelay": "5 seconds", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "initialDelay '5 seconds', which is not an ISO 8601 duration"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Missing_InitialDelay_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5}, "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "retry initialDelay with no duration"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Missing_MaxAttempts_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": {"mode": "retry", "retry": {"initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "no positive maxAttempts"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Missing_Retry_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": {"mode": "retry", "then": "dead-letter"}}"""));

        Assert.True(Reports(model, "record-invalid", "onFailure with no retry"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Missing_Then_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5, "initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}}}"""));

        Assert.True(Reports(model, "record-invalid", "retry with no then"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Unknown_Then_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"schedule": {"every": "PT1H"}}, "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5, "initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "requeue"}}"""));

        Assert.True(Reports(model, "record-invalid", "then 'requeue' outside dead-letter"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Structured_OnFailure_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"sends": "notification/lending.loan-due"}], "onFailure": {"mode": "retry", "retry": {"maxAttempts": 10, "initialDelay": "PT5S", "maxDelay": "PT1M", "backoff": "exponential"}, "then": "dead-letter"}}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "meaning": "A copy was lent.", "carries": []}"""),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "due.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01"}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Skip_And_Log_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"manual": true}, "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}], "onFailure": {"mode": "skip-and-log"}}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Discard_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "triggeredBy": {"manual": true}, "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}], "onFailure": {"mode": "discard"}}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Effect_Prose_OnFailure_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "effects": [{"runs": "use-case/lending.notify-reader", "timing": "async", "onFailure": "retry (5, PT1M) then dead-letter"}]}"""));

        Assert.True(Reports(model, "record-invalid", "effect onFailure 'retry (5, PT1M) then dead-letter' as prose"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Effect_Retry_Without_Attempts_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "effects": [{"runs": "use-case/lending.notify-reader", "timing": "async", "onFailure": {"mode": "retry", "retry": {"initialDelay": "PT1M", "maxDelay": "PT5M", "backoff": "exponential"}, "then": "dead-letter"}}]}"""));

        Assert.True(Reports(model, "record-invalid", "effect onFailure retry with no positive maxAttempts"));
    }

    /// <proves>litespec/core/automation#state-the-failure-handling-of-every-automation</proves>
    [Fact]
    public void Effect_Structured_OnFailure_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "command", "effects": [{"runs": "use-case/lending.notify-reader", "timing": "async", "onFailure": {"mode": "discard"}}]}"""));

        Assert.False(Reports(model, "record-invalid", "onFailure"));
    }

    [Fact]
    public void Reservation_Without_ReleasedBy_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.md"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"]}"""));

        Assert.True(Reports(model, "reservation-release-missing", "reserves with no releasedBy"));
    }

    [Fact]
    public void Reservation_With_Never_Object_And_Reason_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.md"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": {"mode": "never", "reason": "Holds never expire."}}"""));

        Assert.DoesNotContain("reservation-release-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Reservation_With_Never_Object_And_No_Reason_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.md"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": {"mode": "never"}}"""));

        Assert.True(Reports(model, "reservation-release-missing", "names never with no reason"));
    }

    [Fact]
    public void Reservation_With_Use_Case_Id_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.md"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": "use-case/lending.release-hold"}"""));

        Assert.DoesNotContain("reservation-release-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Reservation_With_Legacy_Never_String_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/copy.hold-reservation", Path.Combine(temp.Path, "copy.md"), """{"kind": "reservation", "statement": "A placed hold reserves its copy.", "governs": ["use-case/lending.place-hold"], "releasedBy": "never (the hold never expires)"}"""));

        Assert.True(Reports(model, "field-invalid", "neither a record id nor a never object"));
    }

    /// <proves>litespec/core/intent#state-each-goal-with-its-measure</proves>
    [Fact]
    public void Goal_With_DueBy_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2027-01-31", "statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#state-each-goal-with-its-measure</proves>
    [Fact]
    public void Goal_With_Only_By_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.md"), """{"status": "approved", "statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "by": "Pilot launch"}"""));

        Assert.True(Reports(model, "record-invalid", "has no dueBy"));
    }

    /// <proves>litespec/core/model#use-the-layer-status-vocabulary</proves>
    [Fact]
    public void Screen_With_Reviewed_Passes_Status()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.md"), """{"status": "reviewed", "entryPoint": "entry-point/library-app", "address": "/loans"}"""),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.md"), """{"status": "reviewed", "kind": "browser"}"""));

        Assert.DoesNotContain("field-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#use-the-layer-status-vocabulary</proves>
    [Fact]
    public void Screen_With_Approved_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.md"), """{"status": "approved", "entryPoint": "entry-point/library-app", "address": "/loans"}"""),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.md"), """{"status": "reviewed", "kind": "browser"}"""));

        Assert.True(Reports(model, "field-invalid", "has status 'approved'"));
        Assert.True(Reports(model, "field-invalid", "draft, reviewed or retired"));
    }

    /// <proves>litespec/core/model#use-the-layer-status-vocabulary</proves>
    [Fact]
    public void Exception_With_Approved_Passes_Status()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/loan-screens-uncovered", Path.Combine(temp.Path, "exceptions.md"), """{"status": "approved", "kind": "gap", "covers": ["record-invalid"], "subject": ["scenario/no-such-case.no-such-scenario"], "expiresOn": "2035-06-01", "reason": "The screens land after the pilot."}"""));

        Assert.DoesNotContain("field-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#use-the-layer-status-vocabulary</proves>
    [Fact]
    public void Exception_With_Reviewed_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/loan-screens-uncovered", Path.Combine(temp.Path, "exceptions.md"), """{"status": "reviewed", "kind": "gap", "covers": ["record-invalid"], "subject": ["scenario/no-such-case.no-such-scenario"], "expiresOn": "2035-06-01", "reason": "The screens land after the pilot."}"""));

        Assert.True(Reports(model, "field-invalid", "has status 'reviewed'"));
        Assert.True(Reports(model, "field-invalid", "draft, approved or retired"));
    }

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
