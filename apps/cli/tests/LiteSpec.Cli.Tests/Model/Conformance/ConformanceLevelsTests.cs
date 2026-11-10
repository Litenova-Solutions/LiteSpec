using LiteSpec.Model;

namespace LiteSpec.Tests.Model.Conformance;

/// <summary>Proves the lowest level of each finding code follows what its check reads.</summary>
public sealed class ConformanceLevelsTests
{
    /// <summary>The layout group holds the two code layout codes, each running only at full.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Layout_Group_Runs_Only_At_Full()
    {
        Assert.Equal(
            new[] { "code-placement-drift", "mark-folder-drift" },
            ConformanceLevels.Layout.OrderBy(code => code, StringComparer.Ordinal).ToArray());
        Assert.All(
            ConformanceLevels.Layout,
            code => Assert.Equal(ConformanceLevel.Full, ConformanceLevels.LowestOf(code)));
    }

    /// <summary>A record-only code runs at records even when its tier is S, because its check reads no code.</summary>
    /// <param name="code">The record-only code.</param>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Theory]
    [InlineData("use-case-scenario-missing")]
    [InlineData("effect-timing-invalid")]
    [InlineData("record-invalid")]
    [InlineData("intent-unreached")]
    public void Record_Only_Code_Runs_At_Records(string code)
    {
        Assert.Equal(ConformanceLevel.Records, ConformanceLevels.LowestOf(code));
    }

    /// <summary>A code whose check reads code, screens or measure results runs at traced even when its tier is G or A.</summary>
    /// <param name="code">The reader code.</param>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Theory]
    [InlineData("screen-serves-drift")]
    [InlineData("step-screen-missing")]
    [InlineData("requirement-measure-missing")]
    [InlineData("constituent-parent-missing")]
    [InlineData("analyzer-lowering-unrecorded")]
    [InlineData("use-case-unimplemented")]
    public void Reader_Code_Runs_At_Traced(string code)
    {
        Assert.Equal(ConformanceLevel.Traced, ConformanceLevels.LowestOf(code));
    }

    /// <summary>An operations, dependency or controlled UI code runs at full even when its tier is G or A.</summary>
    /// <param name="code">The full-only code.</param>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Theory]
    [InlineData("quality-alert-missing")]
    [InlineData("alert-link-missing")]
    [InlineData("dependency-undeclared")]
    [InlineData("repository-unimplemented")]
    [InlineData("contract-break")]
    [InlineData("ui-package-invalid")]
    public void Full_Only_Code_Runs_At_Full(string code)
    {
        Assert.Equal(ConformanceLevel.Full, ConformanceLevels.LowestOf(code));
    }

    /// <summary>An unknown code runs at full, so a new code stays silent below full until its inputs place it.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Unknown_Code_Runs_At_Full()
    {
        Assert.Equal(ConformanceLevel.Full, ConformanceLevels.LowestOf("no-such-finding-code"));
        Assert.False(ConformanceLevels.Runs("no-such-finding-code", ConformanceLevel.Traced));
        Assert.True(ConformanceLevels.Runs("no-such-finding-code", ConformanceLevel.Full));
    }

    /// <summary>The levels nest: records runs only records codes, traced adds traced codes, full runs every code.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Levels_Nest()
    {
        Assert.True(ConformanceLevels.Runs("record-invalid", ConformanceLevel.Records));
        Assert.False(ConformanceLevels.Runs("use-case-unimplemented", ConformanceLevel.Records));
        Assert.False(ConformanceLevels.Runs("code-placement-drift", ConformanceLevel.Records));
        Assert.True(ConformanceLevels.Runs("use-case-unimplemented", ConformanceLevel.Traced));
        Assert.False(ConformanceLevels.Runs("code-placement-drift", ConformanceLevel.Traced));
        Assert.True(ConformanceLevels.Runs("code-placement-drift", ConformanceLevel.Full));
    }

    /// <summary>Each planned-Intent code runs at records, so its local and pull-request outcomes apply at every level.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Planned_Intent_Runs_At_Records()
    {
        Assert.All(
            Severity.PlannedIntent,
            code => Assert.Equal(ConformanceLevel.Records, ConformanceLevels.LowestOf(code)));
    }
}
