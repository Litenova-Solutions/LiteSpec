using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for the per-kind mark rules and the mark finding table.</summary>
public sealed class MarkRulesTests
{
    [Theory(DisplayName = "RecordTypes names the expected record types for every kind")]
    [InlineData(CodeKind.AggregateRoot, "aggregate")]
    [InlineData(CodeKind.AggregateMethod, "use-case")]
    [InlineData(CodeKind.DomainEvent, "event")]
    [InlineData(CodeKind.RuleException, "rule")]
    [InlineData(CodeKind.Handler, "use-case")]
    [InlineData(CodeKind.EventHandler, "reaction")]
    [InlineData(CodeKind.Job, "job")]
    [InlineData(CodeKind.Port, "external-system")]
    [InlineData(CodeKind.Adapter, "external-system")]
    [InlineData(CodeKind.Endpoint, "use-case")]
    [InlineData(CodeKind.FrontendModule, "use-case")]
    [InlineData(CodeKind.Test, "scenario,rule")]
    [InlineData(CodeKind.Technical, "aggregate,use-case,reaction,job,external-system,event,rule,notification,setting,integration-event")]
    public void RecordTypesNamesExpectedTypes(CodeKind kind, string expected)
    {
        var types = MarkRules.RecordTypes(kind);

        Assert.Equal(expected.Split(','), types);
    }

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

    [Fact(DisplayName = "RecordTypes rejects an unknown kind")]
    public void RecordTypesRejectsUnknownKind() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkRules.RecordTypes((CodeKind)99));

    [Fact(DisplayName = "Accepts takes an identifier of the kind's type")]
    public void AcceptsTakesMatchingIdentifier()
    {
        Assert.True(MarkRules.Accepts(CodeKind.Handler, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "scenario/submit-expense.succeeds"));
        Assert.True(MarkRules.Accepts(CodeKind.Test, "rule/expense.approver-is-not-submitter"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "aggregate/spending.expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "use-case/spending.submit-expense"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "reaction/spending.remind-approver"));
        Assert.True(MarkRules.Accepts(CodeKind.Technical, "job/spending.remind-approvers"));
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

    [Fact(DisplayName = "FollowsRecordTypes names the provision namespace, decision and deviation")]
    public void FollowsRecordTypesNamesExpectedTypes() =>
        Assert.Equal(new[] { "litespec", "decision", "deviation" }, MarkRules.FollowsRecordTypes);

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

    [Fact(DisplayName = "Accepts rejects another type, an empty name, and a prefix without a slash")]
    public void AcceptsRejectsOtherTypes()
    {
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "event/expense.submitted"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-case/"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-case"));
        Assert.False(MarkRules.Accepts(CodeKind.Handler, "use-casex/spending.submit-expense"));
    }

    [Fact(DisplayName = "FindingCodes lists the eight mark findings in rule order")]
    public void FindingCodesListsMarkFindings() =>
        Assert.Equal(
            new[]
            {
                "mark-missing",
                "mark-unresolved",
                "mark-count",
                "mark-style-mixed",
                "runtime-value-drift",
                "partial-missing",
                "act-event-undeclared",
                "act-event-unraised",
            },
            MarkRules.FindingCodes);

    [Theory(DisplayName = "RuleId prints the analyzer rule for every mark finding")]
    [InlineData("mark-missing", "LS5001")]
    [InlineData("mark-unresolved", "LS5002")]
    [InlineData("mark-count", "LS5003")]
    [InlineData("mark-style-mixed", "LS5004")]
    [InlineData("runtime-value-drift", "LS5005")]
    [InlineData("partial-missing", "LS5006")]
    [InlineData("act-event-undeclared", "LS5007")]
    [InlineData("act-event-unraised", "LS5008")]
    public void RuleIdPrintsExpectedRule(string code, string expected) =>
        Assert.Equal(expected, MarkRules.RuleId(code));

    [Fact(DisplayName = "RuleId rejects an unknown code")]
    public void RuleIdRejectsUnknownCode() =>
        Assert.Throws<ArgumentException>(() => MarkRules.RuleId("no-such-finding"));

    [Theory(DisplayName = "Tier holds S for every mark finding")]
    [InlineData("mark-missing")]
    [InlineData("mark-unresolved")]
    [InlineData("mark-count")]
    [InlineData("mark-style-mixed")]
    [InlineData("runtime-value-drift")]
    [InlineData("partial-missing")]
    [InlineData("act-event-undeclared")]
    [InlineData("act-event-unraised")]
    public void TierHoldsSForMarkFindings(string code) =>
        Assert.Equal("S", MarkRules.Tier(code));

    [Fact(DisplayName = "Tier rejects an unknown code")]
    public void TierRejectsUnknownCode() =>
        Assert.Throws<ArgumentException>(() => MarkRules.Tier("no-such-finding"));
}
