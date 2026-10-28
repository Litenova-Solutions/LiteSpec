using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Tests for the shared act-event check both hosts call.</summary>
public sealed class ActEventsTests
{
    [Fact(DisplayName = "An event a method raises without a listing is undeclared on that method")]
    public void UndeclaredPerMethodAndEvent()
    {
        var findings = ActEvents.Check(
            new[] { "event/expense.submitted" },
            new[]
            {
                new ActMethodRaises(new[] { "event/expense.submitted", "event/expense.approved" }),
                new ActMethodRaises(new[] { "event/expense.approved", "event/expense.paid" }),
            });

        Assert.Equal(
            new[]
            {
                ("act-event-undeclared", 0, "event/expense.approved"),
                ("act-event-undeclared", 1, "event/expense.approved"),
                ("act-event-undeclared", 1, "event/expense.paid"),
            },
            Of(findings));
    }

    [Fact(DisplayName = "A listed event no method raises is unraised once on the first method")]
    public void UnraisedOnceOnFirstMethod()
    {
        var findings = ActEvents.Check(
            new[] { "event/expense.submitted" },
            new[]
            {
                new ActMethodRaises(Array.Empty<string>()),
                new ActMethodRaises(Array.Empty<string>()),
            });

        Assert.Equal(
            new[] { ("act-event-unraised", 0, "event/expense.submitted") },
            Of(findings));
    }

    [Fact(DisplayName = "A use case with no marked method yields nothing")]
    public void NoMethodsYieldsNothing()
    {
        var findings = ActEvents.Check(
            new[] { "event/expense.submitted" },
            Array.Empty<ActMethodRaises>());

        Assert.Empty(findings);
    }

    [Fact(DisplayName = "Declared events each raised yield nothing")]
    public void AllRaisedYieldsNothing()
    {
        var findings = ActEvents.Check(
            new[] { "event/expense.submitted", "event/expense.approved" },
            new[]
            {
                new ActMethodRaises(new[] { "event/expense.submitted" }),
                new ActMethodRaises(new[] { "event/expense.approved" }),
            });

        Assert.Empty(findings);
    }

    [Fact(DisplayName = "A listed event raised by another marked method yields nothing")]
    public void RaisedUnionAcrossMethods()
    {
        var findings = ActEvents.Check(
            new[] { "event/expense.submitted", "event/expense.approved" },
            new[]
            {
                new ActMethodRaises(new[] { "event/expense.submitted" }),
                new ActMethodRaises(Array.Empty<string>()),
            });

        Assert.Equal(
            new[] { ("act-event-unraised", 0, "event/expense.approved") },
            Of(findings));
    }

    [Fact(DisplayName = "A twice-listed unraised event is reported once")]
    public void DuplicateRaisesYieldOneUnraised()
    {
        var findings = ActEvents.Check(
            new[] { "event/expense.submitted", "event/expense.submitted" },
            new[] { new ActMethodRaises(Array.Empty<string>()) });

        Assert.Equal(
            new[] { ("act-event-unraised", 0, "event/expense.submitted") },
            Of(findings));
    }

    [Fact(DisplayName = "A twice-raised undeclared event is reported once per method")]
    public void DuplicateRaisedYieldOneUndeclaredPerMethod()
    {
        var findings = ActEvents.Check(
            Array.Empty<string>(),
            new[] { new ActMethodRaises(new[] { "event/expense.approved", "event/expense.approved" }) });

        Assert.Equal(
            new[] { ("act-event-undeclared", 0, "event/expense.approved") },
            Of(findings));
    }

    [Fact(DisplayName = "Check rejects null raises")]
    public void CheckRejectsNullRaises() =>
        Assert.Throws<ArgumentNullException>(
            () => ActEvents.Check(null!, Array.Empty<ActMethodRaises>()));

    [Fact(DisplayName = "Check rejects null methods")]
    public void CheckRejectsNullMethods() =>
        Assert.Throws<ArgumentNullException>(
            () => ActEvents.Check(Array.Empty<string>(), null!));

    private static (string Code, int Method, string EventId)[] Of(
        System.Collections.Generic.IReadOnlyList<ActEventFinding> findings)
    {
        var found = new (string Code, int Method, string EventId)[findings.Count];

        for (var index = 0; index < findings.Count; index++)
        {
            found[index] = (findings[index].Code, findings[index].Method, findings[index].EventId);
        }

        return found;
    }
}
