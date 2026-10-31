using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves one finding's severity follows the record's own status, the phase and the reader.</summary>
public sealed class SeverityTests
{
    /// <summary>A finding about a retired record stays silent, whatever its tier.</summary>
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
    [Theory]
    [InlineData("record-shape")]
    [InlineData("use-case-unimplemented")]
    [InlineData("prose-missing")]
    public void Retired_Subject_Stays_Silent(string code)
    {
        var decided = Severity.Of(At(code, "use-case/lending.list-loans"), "retired", false, "working", true);

        Assert.Equal(Outcome.Silent, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    /// <summary>An advisory finding reports, even about a live record under a qualified reader.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Advisory_Finding_Reports_And_Never_Blocks()
    {
        var decided = Severity.Of(At("prose-missing", "use-case/lending.list-loans"), "reviewed", false, "working", true);

        Assert.Equal(Outcome.Reported, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    /// <summary>An S finding about a draft record reports with the draft reason.</summary>
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
    [Fact]
    public void Draft_Subject_Reports_With_Draft_Reason()
    {
        var decided = Severity.Of(At("use-case-unimplemented", "use-case/lending.list-loans"), "draft", false, "working", true);

        Assert.Equal(Outcome.Reported, decided.Outcome);
        Assert.Equal("draft", decided.Reason);
    }

    /// <summary>A G finding about a draft record still blocks.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Draft_Subject_Keeps_General_Finding_Blocking()
    {
        var decided = Severity.Of(At("record-shape", "use-case/lending.list-loans"), "draft", false, "working", true);

        Assert.Equal(Outcome.Blocking, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    /// <summary>A proof finding reports with the frozen reason while the phase is frozen.</summary>
    /// <proves>litespec/core/model#hold-proof-findings-while-tests-are-frozen</proves>
    [Theory]
    [InlineData("proves-unconfirmed")]
    [InlineData("use-case-without-scenario")]
    public void Frozen_Phase_Reports_Proof_Finding(string code)
    {
        var decided = Severity.Of(At(code, "scenario/list-loans.listing"), "reviewed", false, "frozen", true);

        Assert.Equal(Outcome.Reported, decided.Outcome);
        Assert.Equal("frozen", decided.Reason);
    }

    /// <summary>A non-proof S finding still blocks while the phase is frozen, on a test path or off one.</summary>
    /// <proves>litespec/core/model#hold-proof-findings-while-tests-are-frozen</proves>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Frozen_Phase_Keeps_Code_Finding_Blocking(bool onTest)
    {
        var decided = Severity.Of(At("use-case-unimplemented", "use-case/lending.list-loans"), "reviewed", onTest, "frozen", true);

        Assert.Equal(Outcome.Blocking, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    /// <summary>An S finding reports under an unqualified reader, and blocks under a qualified one.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData(true, Outcome.Blocking)]
    [InlineData(false, Outcome.Reported)]
    public void Unqualified_Reader_Reports(bool readerQualified, Outcome outcome)
    {
        var decided = Severity.Of(At("use-case-unimplemented", "use-case/lending.list-loans"), "reviewed", false, "working", readerQualified);

        Assert.Equal(outcome, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    /// <summary>An S finding with no subject follows its tier: it blocks under a qualified reader.</summary>
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
    [Theory]
    [InlineData(true, Outcome.Blocking)]
    [InlineData(false, Outcome.Reported)]
    public void Missing_Subject_Follows_The_Tier(bool readerQualified, Outcome outcome)
    {
        var decided = Severity.Of(At("behavior-unrecorded", null), null, false, "working", readerQualified);

        Assert.Equal(outcome, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    /// <summary>A draft reason wins over the frozen phase, in the numbered rule order.</summary>
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
    [Fact]
    public void Draft_Wins_Over_Frozen()
    {
        var decided = Severity.Of(At("proves-unconfirmed", "scenario/list-loans.listing"), "draft", false, "frozen", true);

        Assert.Equal(Outcome.Reported, decided.Outcome);
        Assert.Equal("draft", decided.Reason);
    }

    /// <summary>A live G finding blocks under any reader.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void General_Finding_Blocks_Under_Any_Reader(bool readerQualified)
    {
        var decided = Severity.Of(At("record-shape", "use-case/lending.list-loans"), "approved", false, "working", readerQualified);

        Assert.Equal(Outcome.Blocking, decided.Outcome);
        Assert.Null(decided.Reason);
    }

    static Finding At(string code, string? subject) =>
        new Finding(code, "Something is wrong.", "docs/page.md", 3, subject);
}
