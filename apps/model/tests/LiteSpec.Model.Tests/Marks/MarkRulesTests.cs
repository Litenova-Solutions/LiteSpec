using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for the per-kind mark rules and the mark finding table.</summary>
public sealed class MarkRulesTests
{
    /// <summary>RecordTypes names the expected record types for every kind.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "RecordTypes names the expected record types for every kind")]
    [InlineData(CodeKind.AggregateRoot, "aggregate")]
    [InlineData(CodeKind.AggregateMethod, "use-case")]
    [InlineData(CodeKind.DomainEvent, "domain-event")]
    [InlineData(CodeKind.RuleException, "rule")]
    [InlineData(CodeKind.Handler, "use-case")]
    [InlineData(CodeKind.EventHandler, "reaction")]
    [InlineData(CodeKind.Job, "job")]
    [InlineData(CodeKind.Port, "external-system")]
    [InlineData(CodeKind.Adapter, "external-system")]
    [InlineData(CodeKind.Endpoint, "use-case")]
    [InlineData(CodeKind.FrontendModule, "use-case")]
    [InlineData(CodeKind.Test, "scenario,rule,requirement,decision,external-system,module-event,integration-event,litespec")]
    [InlineData(CodeKind.Technical, "aggregate,use-case,reaction,job,external-system,domain-event,rule,notification,setting,module-event,integration-event,read-model")]
    public void RecordTypesNamesExpectedTypes(CodeKind kind, string expected)
    {
        var types = MarkRules.RecordTypes(kind);

        Assert.Equal(expected.Split(','), types);
    }

    /// <summary>TakesExactlyOne matches the expected count for every kind.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <param name="expected">The expected result for the case.</param>
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

    /// <summary>Verb is Proves for tests and Implements otherwise, including technical classes.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <param name="expected">The expected result for the case.</param>
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

    /// <summary>RecordTypes rejects an unknown kind.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "RecordTypes rejects an unknown kind")]
    public void RecordTypesRejectsUnknownKind() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkRules.RecordTypes((CodeKind)99));

    /// <summary>Accepts takes an identifier of the kind's type.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Accepts takes an identifier of the kind's type")]
    public void AcceptsTakesMatchingIdentifier()
    {
        Assert.True(MarkRules.Accepts(CodeKind.Handler, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "scenario/submit-expense.succeeds"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "rule/expense.approver-is-not-submitter"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "requirement/spending.month-end-submissions"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "decision/0002-keep-expenses-as-documents"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "external-system/payout-bank"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "integration-event/expense-submitted"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "litespec/backend/architecture#point-dependencies-inward"));
        Assert.False(MarkRules.Accepts(CodeKind.Test, "aggregate/spending.expense"));
        Assert.False(MarkRules.Accepts(CodeKind.Test, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "aggregate/spending.expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.EventHandler, "reaction/spending.remind-approver"));
        Assert.False(MarkRules.Accepts(CodeKind.EventHandler, "job/spending.remind-approvers"));
        Assert.True(MarkRules.Accepts(CodeKind.Job, "job/spending.remind-approvers"));
        Assert.False(MarkRules.Accepts(CodeKind.Job, "reaction/spending.remind-approver"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "reaction/spending.remind-approver"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "job/spending.remind-approvers"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "external-system/payments"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "domain-event/expense.submitted"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "rule/spending.receipt-required"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "notification/spending.approval-reminder"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "setting/spending.max-open-expenses"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "integration-event/expense-submitted"));
        Assert.False(MarkRules.Accepts(CodeKind.Technical, "decision/0002-keep-expenses-as-documents"));
        Assert.False(MarkRules.Accepts(CodeKind.Technical, "waiver/export-review-bypass"));
        Assert.False(MarkRules.Accepts(CodeKind.Technical, "litespec/backend/persistence#commit-each-command-pipeline-once"));
    }

    /// <summary>FollowsRecordTypes names the provision namespace, decision and waiver.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "FollowsRecordTypes names the provision namespace, decision and waiver")]
    public void FollowsRecordTypesNamesExpectedTypes() =>
        Assert.Equal(["litespec", "decision", "waiver"], MarkRules.FollowsRecordTypes);

    /// <summary>AcceptsFollows takes a provision, decision or waiver and rejects a Behavior record.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "AcceptsFollows takes a provision, decision or waiver and rejects a Behavior record")]
    public void AcceptsFollowsTakesChoices()
    {
        Assert.True(MarkRules.AcceptsFollows("litespec/backend/persistence#commit-each-command-pipeline-once"));
        Assert.True(MarkRules.AcceptsFollows("decision/0002-keep-expenses-as-documents"));
        Assert.True(MarkRules.AcceptsFollows("waiver/export-review-bypass"));
        Assert.False(MarkRules.AcceptsFollows("use-case/spending.submit-expense"));
        Assert.False(MarkRules.AcceptsFollows("decision/"));
        Assert.False(MarkRules.AcceptsFollows("decision"));
    }

    /// <summary>TakesVerb takes implements or follows for technical classes and one verb otherwise.</summary>
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

    /// <summary>Accepts rejects another type, an empty name, and a prefix without a slash.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Accepts rejects another type, an empty name, and a prefix without a slash")]
    public void AcceptsRejectsOtherTypes()
    {
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "domain-event/expense.submitted"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-case/"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-case"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-casex/spending.submit-expense"));
    }

    /// <summary>FindingCodes lists the eight mark findings in rule order.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "FindingCodes lists the eight mark findings in rule order")]
    public void FindingCodesListsMarkFindings() =>
        Assert.Equal(
            [
                "mark-missing",
                "mark-unknown",
                "mark-count-invalid",
                "mark-style-invalid",
                "runtime-value-drift",
                "partial-missing",
                "act-event-undeclared",
                "act-event-unimplemented",
            ],
            MarkRules.FindingCodes);

}
