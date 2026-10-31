using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for the shared mark-count-invalid rule both hosts call.</summary>
public sealed class MarkCountTests
{
    /// <summary>An exactly-one kind with one mark is no violation.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An exactly-one kind with one mark is no violation")]
    public void ExactlyOneKindWithOneMarkIsOk()
    {
        var verdict = MarkRules.CheckCount(CodeKind.Handler, ["use-case/spending.submit-expense"]);

        Assert.False(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    /// <summary>An exactly-one kind with two marks is violated without a duplicate.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "An exactly-one kind with two marks is violated without a duplicate")]
    [InlineData(CodeKind.Handler)]
    [InlineData(CodeKind.AggregateMethod)]
    public void ExactlyOneKindWithTwoMarksIsViolated(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(
            kind,
            ["use-case/spending.submit-expense", "use-case/spending.approve-expense"]);

        Assert.True(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    /// <summary>An exactly-one kind naming one id twice reports the duplicate.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "An exactly-one kind naming one id twice reports the duplicate")]
    [InlineData(CodeKind.Handler)]
    [InlineData(CodeKind.AggregateMethod)]
    public void ExactlyOneKindNamingOneIdTwiceReportsDuplicate(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(
            kind,
            ["use-case/spending.submit-expense", "use-case/spending.submit-expense"]);

        Assert.True(verdict.Violated);
        Assert.Equal("use-case/spending.submit-expense", verdict.Duplicate);
    }

    /// <summary>A multi-mark kind with two different marks is no violation.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "A multi-mark kind with two different marks is no violation")]
    [InlineData(CodeKind.Port)]
    [InlineData(CodeKind.Adapter)]
    [InlineData(CodeKind.Test)]
    [InlineData(CodeKind.Technical)]
    public void MultiMarkKindWithTwoMarksIsOk(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(kind, ["a/1", "a/2"]);

        Assert.False(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    /// <summary>A multi-mark kind naming one id twice is violated.</summary>
    /// <param name="kind">The code kind under test.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "A multi-mark kind naming one id twice is violated")]
    [InlineData(CodeKind.Port)]
    [InlineData(CodeKind.Adapter)]
    [InlineData(CodeKind.Test)]
    [InlineData(CodeKind.Technical)]
    public void MultiMarkKindNamingOneIdTwiceIsViolated(CodeKind kind)
    {
        var verdict = MarkRules.CheckCount(kind, ["a/1", "a/1"]);

        Assert.True(verdict.Violated);
        Assert.Equal("a/1", verdict.Duplicate);
    }

    /// <summary>No marks is no violation: the missing mark is mark-missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "No marks is no violation: the missing mark is mark-missing")]
    public void EmptyMarksAreOk()
    {
        var verdict = MarkRules.CheckCount(CodeKind.Handler, Array.Empty<string>());

        Assert.False(verdict.Violated);
        Assert.Null(verdict.Duplicate);
    }

    /// <summary>CheckCount rejects an unknown kind.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "CheckCount rejects an unknown kind")]
    public void CheckCountRejectsUnknownKind() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MarkRules.CheckCount((CodeKind)99, ["use-case/spending.submit-expense"]));

    /// <summary>CheckCount rejects null record ids.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "CheckCount rejects null record ids")]
    public void CheckCountRejectsNullIds() =>
        Assert.Throws<ArgumentNullException>(() => MarkRules.CheckCount(CodeKind.Handler, null!));
}
