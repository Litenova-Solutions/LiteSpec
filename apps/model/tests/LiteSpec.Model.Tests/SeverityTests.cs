using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves one finding's severity follows its tier and the reader.</summary>
public sealed class SeverityTests
{
    /// <summary>An advisory finding reports under any reader.</summary>
    /// <param name="readerQualified">Whether the reader is qualified for the finding.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AdvisoryFindingReportsAndNeverBlocks(bool readerQualified)
    {
        var decided = Severity.Of(At("prose-missing", "use-case/lending.list-loans"), readerQualified, continuousIntegration: false);

        Assert.Equal(Outcome.Reported, decided.Outcome);
    }

    /// <summary>A general finding blocks under any reader.</summary>
    /// <param name="readerQualified">Whether the reader is qualified for the finding.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneralFindingBlocksUnderAnyReader(bool readerQualified)
    {
        var decided = Severity.Of(At("record-invalid", "use-case/lending.list-loans"), readerQualified, continuousIntegration: false);

        Assert.Equal(Outcome.Blocking, decided.Outcome);
    }

    /// <summary>A stack finding blocks under a qualified reader and reports otherwise.</summary>
    /// <param name="readerQualified">Whether the reader is qualified for the finding.</param>
    /// <param name="outcome">The expected severity outcome.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData(true, Outcome.Blocking)]
    [InlineData(false, Outcome.Reported)]
    public void StackFindingFollowsReaderQualification(bool readerQualified, Outcome outcome)
    {
        var decided = Severity.Of(At("use-case-unimplemented", "use-case/lending.list-loans"), readerQualified, continuousIntegration: false);

        Assert.Equal(outcome, decided.Outcome);
    }

    /// <summary>A stack finding with no subject follows its tier: it blocks under a qualified reader.</summary>
    /// <param name="readerQualified">Whether the reader is qualified for the finding.</param>
    /// <param name="outcome">The expected severity outcome.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData(true, Outcome.Blocking)]
    [InlineData(false, Outcome.Reported)]
    public void MissingSubjectFollowsTheTier(bool readerQualified, Outcome outcome)
    {
        var decided = Severity.Of(At("behavior-unrecorded", null), readerQualified, continuousIntegration: false);

        Assert.Equal(outcome, decided.Outcome);
    }

    /// <summary>A finding graded apart from its code follows the override tier.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void TierOverrideDecidesTheSeverity()
    {
        var lifted = Severity.Of(Graded("result-scenario-missing", "G"), true, continuousIntegration: false);
        var lowered = Severity.Of(Graded("requirement-measure-missing", "A"), true, continuousIntegration: false);

        Assert.Equal(Outcome.Blocking, lifted.Outcome);
        Assert.Equal(Outcome.Reported, lowered.Outcome);
    }

    static Finding At(string code, string? subject) =>
        new Finding(code, "Something is wrong.", "docs/page.md", 3, subject);

    static Finding Graded(string code, string tier) =>
        new Finding(code, "Something is wrong.", "docs/page.md", 3, null) { TierOverride = tier };
}
