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
    public void TakesExactlyOneMatchesExpectedCount(CodeKind kind, bool expected) =>
        Assert.Equal(expected, MarkRules.TakesExactlyOne(kind));

    [Theory(DisplayName = "Verb is Proves for tests and Implements otherwise")]
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
