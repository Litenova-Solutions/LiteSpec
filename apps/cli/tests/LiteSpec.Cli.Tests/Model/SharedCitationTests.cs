using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves shared provision citations resolve against the release the tool carries.</summary>
public sealed class SharedCitationTests
{
    /// <summary>The version the throwaway home pins and the fixture reviews, so the release check passes.</summary>
    const string PinnedVersion = "9.9.9";

    /// <summary>A new-form citation that the carried release holds exits 0.</summary>
    /// <proves>litespec/backend/identifiers#resolve-every-identifier-citation</proves>
    [Fact]
    public void Shared_Citation_That_Resolves_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        using var home = WriteFixtureHome();
        PinReviewedVersion(temp.Path);
        AppendCitation(temp.Path, "litespec/core/principles#retire-records-with-replaces");
        var result = Cli.Run(
            ["model", "check", "--root", temp.Path, "--format", "json"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>A new-form citation that the carried release lacks exits 1 with reference-unknown.</summary>
    /// <proves>litespec/backend/identifiers#resolve-every-identifier-citation</proves>
    [Fact]
    public void Shared_Citation_Without_A_Provision_Reports_Reference_Unresolved()
    {
        using var temp = ModelFixture.Prepare("small");
        using var home = WriteFixtureHome();
        PinReviewedVersion(temp.Path);
        AppendCitation(temp.Path, "litespec/core/principles#no-such-provision");
        var result = Cli.Run(
            ["model", "check", "--root", temp.Path, "--format", "json"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });
        Assert.Equal(1, result.Status);
        Assert.Contains("reference-unknown", result.Output);
    }

    static TempFolder WriteFixtureHome()
    {
        var home = Cli.Temp("model-home");
        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(Cli.Repository, "litespec.manifest.json")))!;
        manifest["version"] = PinnedVersion;
        File.WriteAllText(Path.Combine(home.Path, "litespec.manifest.json"), manifest.ToJsonString() + "\n");
        var schemas = Path.Combine(home.Path, "schemas");
        Directory.CreateDirectory(schemas);
        foreach (var schema in Directory.GetFiles(Path.Combine(Cli.Repository, "schemas")))
        {
            File.Copy(schema, Path.Combine(schemas, Path.GetFileName(schema)));
        }

        var folder = Path.Combine(home.Path, "docs", "core");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "principles.md"), PinnedPage);
        var backend = Path.Combine(home.Path, "docs", "backend");
        Directory.CreateDirectory(backend);
        File.WriteAllText(Path.Combine(backend, "application.md"), PinnedDeviationTarget);
        return home;
    }

    static void PinReviewedVersion(string root)
    {
        var file = Path.Combine(root, ".litespec", "project.json");
        var project = (JsonObject)JsonNode.Parse(File.ReadAllText(file))!;
        project["reviewedLiteSpecVersion"] = PinnedVersion;
        File.WriteAllText(file, project.ToJsonString() + "\n");
    }

    static void AppendCitation(string root, string citation)
    {
        var page = Path.Combine(root, ".litespec", "intent", "product.md");
        File.AppendAllText(page, $"\nSee {citation}.\n");
    }

    const string PinnedPage = """
        # Principles

        ## Standards

        ### Retire records with replaces

        A retired record stays readable.
        """;

    /// <summary>The provision the fixture's exception cites, so pinning stays complete.</summary>
    const string PinnedDeviationTarget = """
        # Application Layer

        ## Standards

        ### Enforce target authorization in the pipeline

        Authorization runs once per command.
        """;
}
