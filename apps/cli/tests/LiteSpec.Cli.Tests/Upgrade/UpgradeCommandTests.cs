using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Upgrade;

/// <summary>Proves <c>litespec upgrade --check</c> reports the release changes that apply.</summary>
public sealed class UpgradeCommandTests
{
    /// <summary>Upgrade check on a project pinned to the carried release lists no changes and exits 0.</summary>
    [Fact]
    public void UpgradeCheckOnPinnedProjectListsNoChanges()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("upgrade", "--check", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("no newer changes", result.Stdout);
        Assert.DoesNotContain("Field renames:", result.Stdout);
    }

    /// <summary>Upgrade check on a project pinned to 6.0.0 lists the 7.0.0 changes and exits 1.</summary>
    [Fact]
    public void UpgradeCheckOnOlderProjectListsChangesAndExitsOne()
    {
        using var temp = ModelFixture.Prepare("small");
        PinVersion(temp.Path, "6.0.0");

        var result = TestCli.Run("upgrade", "--check", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains($"Release {CarriedVersion()}:", result.Stdout);
        Assert.Contains("generated-drift", result.Stdout);
        Assert.Contains("sync-drift", result.Stdout);
        Assert.Contains("litespec sync", result.Stdout);
        Assert.Contains("Field renames:", result.Stdout);
        Assert.Contains("Finding codes:", result.Stdout);
        Assert.Contains("Analyzer IDs:", result.Stdout);
        Assert.Contains("Retired commands:", result.Stdout);
        Assert.Contains("Removed extensions:", result.Stdout);
        Assert.Contains("Removed stacks:", result.Stdout);
    }

    /// <summary>Upgrade check JSON on a project pinned to 6.0.0 lists the migrations and exits 1.</summary>
    [Fact]
    public void UpgradeCheckJsonListsMigrations()
    {
        using var temp = ModelFixture.Prepare("small");
        PinVersion(temp.Path, "6.0.0");

        var result = TestCli.Run("upgrade", "--check", "--root", temp.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        var report = JsonNode.Parse(result.Stdout) as JsonObject;
        Assert.NotNull(report);
        Assert.Equal("6.0.0", report["reviewed"]!.GetValue<string>());
        Assert.Equal(CarriedVersion(), report["carried"]!.GetValue<string>());

        var migrations = report["migrations"] as JsonArray;
        Assert.NotNull(migrations);
        Assert.True(migrations.Count > 0, "an older project has applying migrations");

        var latest = migrations.OfType<JsonObject>().First(migration => migration["version"]!.GetValue<string>() == CarriedVersion());
        var renames = (latest["fieldRenames"] as JsonArray)!.OfType<JsonObject>().ToList();
        Assert.True(renames.Count > 0, "the migration holds field renames");
        Assert.Contains(renames, rename =>
            rename["from"]!.GetValue<string>() == "records" && rename["to"]!.GetValue<string>() == "results");
        var findingRenames = (latest["findingRenames"] as JsonArray)!.OfType<JsonObject>().ToList();
        Assert.Contains(findingRenames, rename =>
            rename["from"]!.GetValue<string>() == "generated-drift" && rename["to"]!.GetValue<string>() == "sync-drift");
        Assert.True((latest["findingAdded"] as JsonArray)!.Count > 0, "the migration holds new finding codes");
        Assert.True((latest["analyzerRemoved"] as JsonArray)!.Count > 0, "the migration holds removed analyzer IDs");
    }

    /// <summary>Upgrade check JSON on a pinned project holds no migrations and exits 0.</summary>
    [Fact]
    public void UpgradeCheckJsonOnPinnedProjectHoldsNoMigrations()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("upgrade", "--check", "--root", temp.Path, "--format", "json");

        Assert.Equal(0, result.Status);
        var report = JsonNode.Parse(result.Stdout) as JsonObject;
        Assert.NotNull(report);
        Assert.Empty((report["migrations"] as JsonArray)!);
    }

    /// <summary>Upgrade without --check is a usage error and exits 2.</summary>
    [Fact]
    public void UpgradeWithoutCheckIsAUsageError()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("upgrade", "--root", temp.Path);

        Assert.Equal(2, result.Status);
    }

    /// <summary>Upgrade help prints the usage text and exits 0.</summary>
    [Fact]
    public void UpgradeHelpPrintsUsage()
    {
        using var temp = ModelFixture.Prepare("small");

        var result = TestCli.Run("upgrade", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("litespec upgrade --check", result.Stdout);
    }

    /// <summary>Rewrites the reviewed LiteSpec version the fixture project records.</summary>
    /// <param name="root">The prepared fixture root.</param>
    /// <param name="version">The version to record as reviewed.</param>
    static void PinVersion(string root, string version)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        var text = File.ReadAllText(project);
        File.WriteAllText(
            project,
            Regex.Replace(text, @"reviewedLiteSpecVersion:\s*\S+", $"reviewedLiteSpecVersion: {version}"));
    }

    /// <summary>The carried LiteSpec release version from the repository manifest.</summary>
    /// <returns>The carried release version.</returns>
    static string CarriedVersion()
    {
        var text = File.ReadAllText(Path.Combine(TestCli.Repository, "litespec.manifest.yaml"));
        var match = Regex.Match(text, @"^version:\s*(\S+)", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : "7.0.0";
    }
}
