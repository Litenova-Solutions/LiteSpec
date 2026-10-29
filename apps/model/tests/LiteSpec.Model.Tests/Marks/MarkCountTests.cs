using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for the shared mark-count rule both hosts call.</summary>
public sealed class MarkCountTests
{
    [Fact(DisplayName = "An exactly-one kind with one mark is no violation")]
    public void ExactlyOneKindWithOneMarkIsOk()
    {
        var verdict = MarkRules.CheckCount(CodeKind.Handler, new[] { "use-case/spending.submit-expense" });

        Assert.False(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    [Theory(DisplayName = "An exactly-one kind with two marks is violated without a duplicate")]
    [InlineData(CodeKind.Handler)]
    [InlineData(CodeKind.AggregateMethod)]
    public void ExactlyOneKindWithTwoMarksIsViolated(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(
            kind,
            new[] { "use-case/spending.submit-expense", "use-case/spending.approve-expense" });

        Assert.True(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    [Theory(DisplayName = "An exactly-one kind naming one id twice reports the duplicate")]
    [InlineData(CodeKind.Handler)]
    [InlineData(CodeKind.AggregateMethod)]
    public void ExactlyOneKindNamingOneIdTwiceReportsDuplicate(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(
            kind,
            new[] { "use-case/spending.submit-expense", "use-case/spending.submit-expense" });

        Assert.True(verdict.Violated);
        Assert.Equal("use-case/spending.submit-expense", verdict.Duplicate);
    }

    [Theory(DisplayName = "A multi-mark kind with two different marks is no violation")]
    [InlineData(CodeKind.Port)]
    [InlineData(CodeKind.Adapter)]
    [InlineData(CodeKind.Test)]
    [InlineData(CodeKind.Technical)]
    public void MultiMarkKindWithTwoMarksIsOk(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(kind, new[] { "a/1", "a/2" });

        Assert.False(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    [Theory(DisplayName = "A multi-mark kind naming one id twice is violated")]
    [InlineData(CodeKind.Port)]
    [InlineData(CodeKind.Adapter)]
    [InlineData(CodeKind.Test)]
    [InlineData(CodeKind.Technical)]
    public void MultiMarkKindNamingOneIdTwiceIsViolated(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(kind, new[] { "a/1", "a/1" });

        Assert.True(verdict.Violated);
        Assert.Equal("a/1", verdict.Duplicate);
    }

    [Fact(DisplayName = "No marks is no violation: the missing mark is mark-missing")]
    public void EmptyMarksAreOk()
    {
        var verdict = MarkRules.CheckCount(CodeKind.Handler, Array.Empty<string>());

        Assert.False(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    [Fact(DisplayName = "CheckCount rejects an unknown kind")]
    public void CheckCountRejectsUnknownKind() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MarkRules.CheckCount((CodeKind)99, new[] { "use-case/spending.submit-expense" }));

    [Fact(DisplayName = "CheckCount rejects null record ids")]
    public void CheckCountRejectsNullIds() =>
        Assert.Throws<ArgumentNullException>(() => MarkRules.CheckCount(CodeKind.Handler, null!));
}
