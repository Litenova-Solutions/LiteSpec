using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.PlanWaves;

/// <summary>Proves <c>litespec plan-waves</c> prints conflict-free parallel slices and waves.</summary>
public sealed class PlanWavesCommandTests
{
    /// <summary>Plan-waves prints one slice per aggregate of the small fixture, with waves and frozen paths.</summary>
    [Fact]
    public void PlanWavesListsOneSlicePerAggregate()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("plan-waves", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Slices (3):", result.Stdout);
        Assert.Contains("aggregate/lending.copy", result.Stdout);
        Assert.Contains("aggregate/lending.hold", result.Stdout);
        Assert.Contains("aggregate/lending.loan", result.Stdout);
        Assert.Contains("use-case/lending.borrow-copy", result.Stdout);
        Assert.Contains("Waves (", result.Stdout);
        Assert.Contains("Frozen:", result.Stdout);
        Assert.Contains("Library.Kernel", result.Stdout);
    }

    /// <summary>Plan-waves gives every slice exclusive path globs: no glob prints on two slices.</summary>
    [Fact]
    public void PlanWavesSlicePathsAreDisjoint()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("plan-waves", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains(".litespec/behavior/modules/lending/aggregates/copies/**", result.Stdout);
        Assert.Contains(".litespec/behavior/modules/lending/aggregates/holds/**", result.Stdout);
        Assert.Contains(".litespec/behavior/modules/lending/aggregates/loans/**", result.Stdout);
        Assert.Contains("apps/api/src/Modules/Library.Lending/Application/Copies/**", result.Stdout);
        Assert.Contains("apps/api/src/Modules/Library.Lending/WebApi/Loans/**", result.Stdout);
        Assert.Contains("apps/api/src/Modules/Library.Lending/Infrastructure/Holds/**", result.Stdout);

        var globs = result.Stdout
            .Split('\n')
            .Select(line => line.Trim().TrimStart('-').Trim())
            .Where(line => line.EndsWith("/**", StringComparison.Ordinal))
            .ToList();

        Assert.True(globs.Count > 0, "plan-waves prints path globs");
        Assert.Equal(globs.Count, globs.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Plan-waves orders the waves by dependency: copies print on an earlier wave than loans.</summary>
    [Fact]
    public void PlanWavesOrdersDependenciesInWaves()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("plan-waves", "--root", temp.Path, "--format", "json");

        Assert.Equal(0, result.Status);
        var report = JsonNode.Parse(result.Stdout) as JsonObject;
        Assert.NotNull(report);
        var waves = report["waves"] as JsonArray;
        Assert.NotNull(waves);
        Assert.True(waves.Count >= 2, "copies and loans print on different waves");

        var waveOf = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var wave = 0; wave < waves.Count; wave++)
        {
            foreach (var id in waves[wave]!.AsArray().Select(entry => entry!.GetValue<string>()))
            {
                waveOf[id] = wave;
            }
        }

        Assert.True(waveOf["aggregate/lending.copy"] < waveOf["aggregate/lending.loan"], "copies print before loans");
        Assert.True(waveOf["aggregate/lending.copy"] < waveOf["aggregate/lending.hold"], "copies print before holds");
    }

    /// <summary>Plan-waves JSON reports each slice with its records, paths, waves and frozen paths.</summary>
    [Fact]
    public void PlanWavesJsonReportsSlicesWavesAndFrozen()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("plan-waves", "--root", temp.Path, "--format", "json");

        Assert.Equal(0, result.Status);
        var report = JsonNode.Parse(result.Stdout) as JsonObject;
        Assert.NotNull(report);
        var slices = report["slices"] as JsonArray;
        Assert.NotNull(slices);
        Assert.Equal(3, slices.Count);

        var paths = new List<string>();
        foreach (var slice in slices.OfType<JsonObject>())
        {
            Assert.True(slice["records"] is JsonArray { Count: > 0 });
            var slicePaths = (slice["paths"] as JsonArray)!.Select(entry => entry!.GetValue<string>()).ToList();
            Assert.True(slicePaths.Count > 0, "every slice holds paths");
            paths.AddRange(slicePaths);
        }

        Assert.Equal(paths.Count, paths.Distinct(StringComparer.Ordinal).Count());
        var frozen = (report["frozen"] as JsonArray)!.Select(entry => entry!.GetValue<string>()).ToList();
        Assert.Contains(frozen, path => path.Contains("Library.Kernel", StringComparison.Ordinal));
    }

    /// <summary>Plan-waves help prints the usage text and exits 0.</summary>
    [Fact]
    public void PlanWavesHelpPrintsUsage()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("plan-waves", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("litespec plan-waves", result.Stdout);
    }
}
