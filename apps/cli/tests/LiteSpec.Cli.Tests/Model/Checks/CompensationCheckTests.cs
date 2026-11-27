using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the compensation checks against hand-built models: a compensating reaction names a command it does not run, and answers an event the undone step does not raise.</summary>
public sealed class CompensationCheckTests
{
    /// <summary>The finding codes the compensation checks report.</summary>
    static readonly string[] CompensationCodes = ["compensation-target-invalid", "compensation-trigger-invalid"];

    /// <summary>A compensating reaction naming a command it does not run, answering the later failure, reports no compensation finding.</summary>
    /// <proves>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</proves>
    [Fact]
    public void CompensatingReactionWithACommandTargetAndAFailureTriggerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.reserve-copy", Path.Combine(temp.Path, "reserve.yaml"), """{"kind": "command", "raises": ["domain-event/reservation.reserved"]}"""),
            TestModel.Record("use-case", "use-case/lending.record-charge-failure", Path.Combine(temp.Path, "failure.yaml"), """{"kind": "command", "raises": ["domain-event/charge.charge-failed"]}"""),
            TestModel.Record("use-case", "use-case/lending.release-copy", Path.Combine(temp.Path, "release.yaml"), """{"kind": "command"}"""),
            TestModel.Record("domain-event", "domain-event/reservation.reserved", Path.Combine(temp.Path, "reserved.yaml"), """{"meaning": "A copy was reserved.", "fields": []}"""),
            TestModel.Record("domain-event", "domain-event/charge.charge-failed", Path.Combine(temp.Path, "failed.yaml"), """{"meaning": "The charge failed.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.release-copy-on-charge-failed", Path.Combine(temp.Path, "compensate.yaml"), """{"triggeredBy": ["domain-event/charge.charge-failed"], "effects": [{"runs": "use-case/lending.release-copy", "timing": "async"}], "compensates": "use-case/lending.reserve-copy"}"""));

        Assert.DoesNotContain(TestModel.Codes(model), code => CompensationCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>A reaction without compensates reports no compensation finding.</summary>
    /// <proves>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</proves>
    [Fact]
    public void ReactionWithoutCompensatesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.notify-overdue", Path.Combine(temp.Path, "notify.yaml"), """{"kind": "command"}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.yaml"), """{"meaning": "A copy was lent.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.yaml"), """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""));

        Assert.DoesNotContain(TestModel.Codes(model), code => CompensationCodes.Contains(code, StringComparer.Ordinal));
    }

    /// <summary>Compensates naming a query reports compensation-target-invalid, because only a command commits a step.</summary>
    /// <proves>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</proves>
    [Fact]
    public void CompensatesNamingAQueryReportsTargetInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-copies", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query"}"""),
            TestModel.Record("use-case", "use-case/lending.release-copy", Path.Combine(temp.Path, "release.yaml"), """{"kind": "command"}"""),
            TestModel.Record("domain-event", "domain-event/charge.charge-failed", Path.Combine(temp.Path, "failed.yaml"), """{"meaning": "The charge failed.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.release-copy-on-charge-failed", Path.Combine(temp.Path, "compensate.yaml"), """{"triggeredBy": ["domain-event/charge.charge-failed"], "effects": [{"runs": "use-case/lending.release-copy", "timing": "async"}], "compensates": "use-case/lending.list-copies"}"""));

        Assert.Contains("compensation-target-invalid", TestModel.Codes(model));
    }

    /// <summary>Compensates naming the use case the reaction runs reports compensation-target-invalid, because the reaction would redo the step instead of undoing it.</summary>
    /// <proves>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</proves>
    [Fact]
    public void CompensatesNamingTheRunUseCaseReportsTargetInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.reserve-copy", Path.Combine(temp.Path, "reserve.yaml"), """{"kind": "command", "raises": ["domain-event/reservation.reserved"]}"""),
            TestModel.Record("domain-event", "domain-event/charge.charge-failed", Path.Combine(temp.Path, "failed.yaml"), """{"meaning": "The charge failed.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.reserve-copy-on-charge-failed", Path.Combine(temp.Path, "compensate.yaml"), """{"triggeredBy": ["domain-event/charge.charge-failed"], "effects": [{"runs": "use-case/lending.reserve-copy", "timing": "async"}], "compensates": "use-case/lending.reserve-copy"}"""));

        Assert.Contains("compensation-target-invalid", TestModel.Codes(model));
    }

    /// <summary>A compensating reaction answering an event the compensated use case raises reports compensation-trigger-invalid.</summary>
    /// <proves>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</proves>
    [Fact]
    public void TriggerRaisedByTheCompensatedUseCaseReportsTriggerInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.reserve-copy", Path.Combine(temp.Path, "reserve.yaml"), """{"kind": "command", "raises": ["domain-event/reservation.reserved"]}"""),
            TestModel.Record("use-case", "use-case/lending.release-copy", Path.Combine(temp.Path, "release.yaml"), """{"kind": "command"}"""),
            TestModel.Record("domain-event", "domain-event/reservation.reserved", Path.Combine(temp.Path, "reserved.yaml"), """{"meaning": "A copy was reserved.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.release-copy-on-reserved", Path.Combine(temp.Path, "compensate.yaml"), """{"triggeredBy": ["domain-event/reservation.reserved"], "effects": [{"runs": "use-case/lending.release-copy", "timing": "async"}], "compensates": "use-case/lending.reserve-copy"}"""));

        Assert.Contains("compensation-trigger-invalid", TestModel.Codes(model));
    }

    /// <summary>Compensates naming no written record reports reference-unknown through the citation checks.</summary>
    /// <proves>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</proves>
    [Fact]
    public void CompensatesNamingAnUnknownUseCaseReportsReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.release-copy", Path.Combine(temp.Path, "release.yaml"), """{"kind": "command"}"""),
            TestModel.Record("domain-event", "domain-event/charge.charge-failed", Path.Combine(temp.Path, "failed.yaml"), """{"meaning": "The charge failed.", "fields": []}"""),
            TestModel.Record("reaction", "reaction/lending.release-copy-on-charge-failed", Path.Combine(temp.Path, "compensate.yaml"), """{"triggeredBy": ["domain-event/charge.charge-failed"], "effects": [{"runs": "use-case/lending.release-copy", "timing": "async"}], "compensates": "use-case/lending.no-such-step"}"""));

        Assert.Contains("reference-unknown", TestModel.Codes(model));
    }
}
