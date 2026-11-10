using LiteSpec.Tests.Consumer;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Conformance;

/// <summary>Proves <c>litespec model check</c> reports only the findings of the claimed conformance level.</summary>
public sealed class CheckByLevelTests
{
    /// <summary>A record finding blocks at every claimed level.</summary>
    /// <param name="level">The claimed conformance level.</param>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Theory]
    [InlineData("records")]
    [InlineData("traced")]
    [InlineData("full")]
    public void Record_Finding_Blocks_At_Every_Level(string level)
    {
        using var fixture = ModelFixture.Prepare("large", "record-invalid");
        SetConformance(fixture.Path, level);

        var result = Cli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.Equal(1, result.Status);
        Assert.Contains("record-invalid", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A traced finding is silent at records and blocks from traced.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Traced_Finding_Is_Silent_At_Records()
    {
        using var records = ModelFixture.Prepare("large", "use-case-unimplemented");
        SetConformance(records.Path, "records");

        var atRecords = Cli.Run(["model", "check", "--root", records.Path, "--format", "json"]);

        Assert.Equal(0, atRecords.Status);
        Assert.DoesNotContain("use-case-unimplemented", atRecords.Stdout, StringComparison.Ordinal);

        using var traced = ModelFixture.Prepare("large", "use-case-unimplemented");
        SetConformance(traced.Path, "traced");

        var atTraced = Cli.Run(["model", "check", "--root", traced.Path, "--format", "json"]);

        Assert.Equal(1, atTraced.Status);
        Assert.Contains("use-case-unimplemented", atTraced.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A layout finding is silent at records and traced and blocks only at full.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Layout_Finding_Is_Silent_Below_Full()
    {
        using var traced = ModelFixture.Prepare("large");
        AddStrayFolder(traced.Path);
        SetConformance(traced.Path, "traced");

        var atTraced = Cli.Run(["model", "check", "--root", traced.Path, "--format", "json"]);

        Assert.Equal(0, atTraced.Status);
        Assert.DoesNotContain("code-placement-drift", atTraced.Stdout, StringComparison.Ordinal);

        using var full = ModelFixture.Prepare("large");
        AddStrayFolder(full.Path);
        SetConformance(full.Path, "full");

        var atFull = Cli.Run(["model", "check", "--root", full.Path, "--format", "json"]);

        Assert.Equal(1, atFull.Status);
        Assert.Contains("code-placement-drift", atFull.Stdout, StringComparison.Ordinal);
    }

    /// <summary>At records the clean small fixture passes: each finding reports and runs at records, so no code reader runs.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Records_Check_Passes_Without_Code()
    {
        using var fixture = ModelFixture.Prepare("small");
        SetConformance(fixture.Path, "records");

        var result = Cli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.True(result.Status == 0, result.Output);
        using var document = JsonDocument.Parse(result.Stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            Assert.NotEqual("blocking", finding.GetProperty("outcome").GetString());
            Assert.True(
                ConformanceLevels.Runs(finding.GetProperty("code").GetString()!, ConformanceLevel.Records),
                $"Finding '{finding.GetProperty("code").GetString()}' runs above records.");
        }
    }

    /// <summary>Adds one folder naming no declared module under the backend root, so only a layout finding follows.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void AddStrayFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Library.Application", "Archive");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "readme.txt"), "This folder names no declared module, so the placement check reports it.\n");
    }

    /// <summary>Writes the claimed conformance level into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="level">The level name: records, traced or full.</param>
    static void SetConformance(string root, string level)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(file)!.AsObject();
        project["layers"] = ConsumerFixture.Layers(level);
        File.WriteAllText(file, project.ToJsonString());
    }
}
