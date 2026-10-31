using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for the per-kind mark rules and the mark finding table.</summary>
public sealed class MarkRulesTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "RecordTypes names the expected record types for every kind")]
    [InlineData(CodeKind.AggregateRoot, "aggregate")]
    [InlineData(CodeKind.AggregateMethod, "use-case")]
    [InlineData(CodeKind.DomainEvent, "event")]
    [InlineData(CodeKind.RuleException, "rule")]
    [InlineData(CodeKind.Handler, "use-case")]
    [InlineData(CodeKind.EventHandler, "automation")]
    [InlineData(CodeKind.Job, "automation")]
    [InlineData(CodeKind.Port, "external-system")]
    [InlineData(CodeKind.Adapter, "external-system")]
    [InlineData(CodeKind.Endpoint, "use-case")]
    [InlineData(CodeKind.FrontendModule, "use-case")]
    [InlineData(CodeKind.Test, "scenario,rule,requirement,decision,external-system,integration-event,litespec")]
    [InlineData(CodeKind.Technical, "aggregate,use-case,automation,external-system,event,rule,notification,setting,integration-event")]
    public void RecordTypesNamesExpectedTypes(CodeKind kind, string expected)
    {
        var types = MarkRules.RecordTypes(kind);

        Assert.Equal(expected.Split(','), types);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "TakesExactlyOne matches the expected count for every kind")]
    [InlineData(CodeKind.AggregateRoot, true)]
    [InlineData(CodeKind.AggregateMethod, true)]
    [InlineData(CodeKind.DomainEvent, true)]
    [InlineData(CodeKind.RuleException, true)]
    [InlineData(CodeKind.Handler, true)]
    [InlineData(CodeKind.EventHandler, true)]
    [InlineData(CodeKind.Job, true)]
    [InlineData(CodeKind.Port, false)]
    [InlineData(CodeKind.Adapter, false)]
    [InlineData(CodeKind.Endpoint, true)]
    [InlineData(CodeKind.FrontendModule, true)]
    [InlineData(CodeKind.Test, false)]
    [InlineData(CodeKind.Technical, false)]
    public void TakesExactlyOneMatchesExpectedCount(CodeKind kind, bool expected) =>
        Assert.Equal(expected, MarkRules.TakesExactlyOne(kind));

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "Verb is Proves for tests and Implements otherwise, including technical classes")]
    [InlineData(CodeKind.AggregateRoot, MarkVerb.Implements)]
    [InlineData(CodeKind.AggregateMethod, MarkVerb.Implements)]
    [InlineData(CodeKind.DomainEvent, MarkVerb.Implements)]
    [InlineData(CodeKind.RuleException, MarkVerb.Implements)]
    [InlineData(CodeKind.Handler, MarkVerb.Implements)]
    [InlineData(CodeKind.EventHandler, MarkVerb.Implements)]
    [InlineData(CodeKind.Job, MarkVerb.Implements)]
    [InlineData(CodeKind.Port, MarkVerb.Implements)]
    [InlineData(CodeKind.Adapter, MarkVerb.Implements)]
    [InlineData(CodeKind.Endpoint, MarkVerb.Implements)]
    [InlineData(CodeKind.FrontendModule, MarkVerb.Implements)]
    [InlineData(CodeKind.Test, MarkVerb.Proves)]
    [InlineData(CodeKind.Technical, MarkVerb.Implements)]
    public void VerbMatchesExpectedVerb(CodeKind kind, MarkVerb expected) =>
        Assert.Equal(expected, MarkRules.Verb(kind));

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "RecordTypes rejects an unknown kind")]
    public void RecordTypesRejectsUnknownKind() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkRules.RecordTypes((CodeKind)99));

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Accepts takes an identifier of the kind's type")]
    public void AcceptsTakesMatchingIdentifier()
    {
        Assert.True(MarkRules.Accepts(CodeKind.Handler, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "scenario/submit-expense.succeeds"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "rule/expense.approver-is-not-submitter"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "requirement/spending.month-end-submissions"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "decision/0002-store-expenses-as-documents"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "external-system/payout-bank"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "integration-event/expense-submitted"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "litespec/backend/architecture#point-dependencies-inward"));
        Assert.False(MarkRules.Accepts(CodeKind.Test, "aggregate/spending.expense"));
        Assert.False(MarkRules.Accepts(CodeKind.Test, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "aggregate/spending.expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.EventHandler, "automation/spending.remind-approver"));
        Assert.True(MarkRules.Accepts(CodeKind.Job, "automation/spending.remind-approvers"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "automation/spending.remind-approver"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "external-system/payments"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "event/expense.submitted"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "rule/spending.receipt-required"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "notification/spending.approval-reminder"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "setting/spending.max-open-expenses"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "integration-event/expense-submitted"));
        Assert.False(MarkRules.Accepts(CodeKind.Technical, "decision/0002-store-expenses-as-documents"));
        Assert.False(MarkRules.Accepts(CodeKind.Technical, "deviation/export-review-bypass"));
        Assert.False(MarkRules.Accepts(CodeKind.Technical, "litespec/backend/persistence#commit-each-command-pipeline-once"));
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "FollowsRecordTypes names the provision namespace, decision and deviation")]
    public void FollowsRecordTypesNamesExpectedTypes() =>
        Assert.Equal(new[] { "litespec", "decision", "deviation" }, MarkRules.FollowsRecordTypes);

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "AcceptsFollows takes a provision, decision or deviation and rejects a Behavior record")]
    public void AcceptsFollowsTakesChoices()
    {
        Assert.True(MarkRules.AcceptsFollows("litespec/backend/persistence#commit-each-command-pipeline-once"));
        Assert.True(MarkRules.AcceptsFollows("decision/0002-store-expenses-as-documents"));
        Assert.True(MarkRules.AcceptsFollows("deviation/export-review-bypass"));
        Assert.False(MarkRules.AcceptsFollows("use-case/spending.submit-expense"));
        Assert.False(MarkRules.AcceptsFollows("decision/"));
        Assert.False(MarkRules.AcceptsFollows("decision"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "TakesVerb takes implements or follows for technical classes and one verb otherwise")]
    public void TakesVerbTakesEitherVerbForTechnical()
    {
        Assert.True(MarkRules.TakesVerb(CodeKind.Technical, MarkVerb.Implements));
        Assert.True(MarkRules.TakesVerb(CodeKind.Technical, MarkVerb.Follows));
        Assert.False(MarkRules.TakesVerb(CodeKind.Technical, MarkVerb.Proves));
        Assert.True(MarkRules.TakesVerb(CodeKind.Handler, MarkVerb.Implements));
        Assert.False(MarkRules.TakesVerb(CodeKind.Handler, MarkVerb.Follows));
        Assert.True(MarkRules.TakesVerb(CodeKind.Test, MarkVerb.Proves));
        Assert.False(MarkRules.TakesVerb(CodeKind.Test, MarkVerb.Implements));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Accepts rejects another type, an empty name, and a prefix without a slash")]
    public void AcceptsRejectsOtherTypes()
    {
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "event/expense.submitted"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-case/"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-case"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-casex/spending.submit-expense"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "FindingCodes lists the eight mark findings in rule order")]
    public void FindingCodesListsMarkFindings() =>
        Assert.Equal(
            new[]
            {
                "mark-missing",
                "mark-unknown",
                "mark-count-invalid",
                "mark-style-invalid",
                "runtime-value-drift",
                "partial-missing",
                "act-event-undeclared",
                "act-event-unimplemented",
            },
            MarkRules.FindingCodes);

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "RuleId prints the analyzer rule for every mark finding")]
    [InlineData("mark-missing", "LS5001")]
    [InlineData("mark-unknown", "LS5002")]
    [InlineData("mark-count-invalid", "LS5003")]
    [InlineData("mark-style-invalid", "LS5004")]
    [InlineData("runtime-value-drift", "LS5005")]
    [InlineData("partial-missing", "LS5006")]
    [InlineData("act-event-undeclared", "LS5007")]
    [InlineData("act-event-unimplemented", "LS5008")]
    public void RuleIdPrintsExpectedRule(string code, string expected) =>
        Assert.Equal(expected, MarkRules.RuleId(code));

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "RuleId rejects an unknown code")]
    public void RuleIdRejectsUnknownCode() =>
        Assert.Throws<ArgumentException>(() => MarkRules.RuleId("no-such-finding"));

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "Tier holds S for every mark finding")]
    [InlineData("mark-missing")]
    [InlineData("mark-unknown")]
    [InlineData("mark-count-invalid")]
    [InlineData("mark-style-invalid")]
    [InlineData("runtime-value-drift")]
    [InlineData("partial-missing")]
    [InlineData("act-event-undeclared")]
    [InlineData("act-event-unimplemented")]
    public void TierHoldsSForMarkFindings(string code) =>
        Assert.Equal("S", MarkRules.Tier(code));

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Tier rejects an unknown code")]
    public void TierRejectsUnknownCode() =>
        Assert.Throws<ArgumentException>(() => MarkRules.Tier("no-such-finding"));
}
