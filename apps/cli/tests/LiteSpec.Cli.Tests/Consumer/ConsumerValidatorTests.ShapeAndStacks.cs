using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for project shape and stack selection.</summary>
public sealed partial class ConsumerValidatorTests
{
    static TempFolder HomeRequiringAndroidForSvelte() =>
        ConsumerFixture.CreateHome(manifest =>
        {
            var svelte = (JsonObject)((JsonObject)manifest["stacks"]!)["svelte"]!;
            svelte["requires"] = new JsonArray("android");
        });

    static TempFolder HomePairingSvelteWith(params string[] backends) =>
        ConsumerFixture.CreateHome(manifest =>
        {
            var svelte = (JsonObject)((JsonObject)manifest["stacks"]!)["svelte"]!;
            var pairs = new JsonArray();
            foreach (var backend in backends)
            {
                pairs.Add(backend);
            }

            svelte["pairsWith"] = pairs;
        });

    /// <summary>Server project with a backend and a frontend.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "server project with a backend and a frontend")]
    public void ServerProjectWithABackendAndAFrontend()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Server project with a backend and no frontend.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "server project with a backend and no frontend")]
    public void ServerProjectWithABackendAndNoFrontend()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet");
            project.Remove("frontends");
        });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Records server project with no stacks and no frontends.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "intent and behavior server project with no stacks and no frontends")]
    public void IntentAndBehaviorServerProjectWithNoStacksOrFrontends()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["layers"] = ConsumerFixture.Layers(implementation: null, proof: null);
            project.Remove("stacks");
            project.Remove("frontends");
        });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Client-only project with android and no backend.</summary>
    /// <proves>litespec/core/scope#declare-a-consumer-that-has-no-backend</proves>
    [Fact(DisplayName = "client-only project with android and no backend")]
    public void ClientOnlyProjectWithAndroidAndNoBackend()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["shape"] = "client-only";
            project["stacks"] = new JsonArray("android");
        });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Client-only project naming a backend.</summary>
    /// <proves>litespec/core/scope#declare-a-consumer-that-has-no-backend</proves>
    [Fact(DisplayName = "client-only project naming a backend")]
    public void ClientOnlyProjectNamingABackend()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["shape"] = "client-only";
            project["stacks"] = new JsonArray("android", "dotnet");
        });
        AssertProblem(RunConsumer(folder.Path), "shape 'client-only' names backend stack 'dotnet'");
    }

    /// <summary>Frontend platform the project does not name.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend platform the project does not name")]
    public void FrontendPlatformTheProjectDoesNotName()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "web", ["path"] = "apps/web", ["platform"] = "svelte" });
        });
        AssertProblem(RunConsumer(folder.Path), "platform 'svelte' is not a stack the project names in stacks");
    }

    /// <summary>Frontend platform the project names.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend platform the project names")]
    public void FrontendPlatformTheProjectNames()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "android");
            project["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "desk", ["path"] = "apps/desk", ["platform"] = "android" });
        });
        Directory.CreateDirectory(Path.Combine(folder.Path, "apps", "desk"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Stack whose requires the project does not name.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "stack whose requires the project does not name")]
    public void StackWhoseRequiresTheProjectDoesNotName()
    {
        using var folder = ConsumerFixture.Create();
        using var home = HomeRequiringAndroidForSvelte();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte"));
        AssertProblem(RunConsumer(folder.Path, home), "stack 'svelte' requires 'android'");
    }

    /// <summary>Stack whose requires the project names.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "stack whose requires the project names")]
    public void StackWhoseRequiresTheProjectNames()
    {
        using var folder = ConsumerFixture.Create();
        using var home = HomeRequiringAndroidForSvelte();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte", "android"));
        AssertAbsent(RunConsumer(folder.Path, home), "requires 'android'");
    }

    /// <summary>Frontend stack whose pairsWith excludes the backend.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend stack whose pairsWith excludes the backend")]
    public void FrontendStackWhosePairsWithExcludesTheBackend()
    {
        using var folder = ConsumerFixture.Create();
        using var home = HomePairingSvelteWith("cobol");
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte"));
        AssertProblem(RunConsumer(folder.Path, home), "pairs only with cobol, not with backend stack 'dotnet'");
    }

    /// <summary>Frontend stack whose pairsWith includes the backend.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend stack whose pairsWith includes the backend")]
    public void FrontendStackWhosePairsWithIncludesTheBackend()
    {
        using var folder = ConsumerFixture.Create();
        using var home = HomePairingSvelteWith("dotnet");
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte"));
        AssertAbsent(RunConsumer(folder.Path, home), "pairs only with");
    }

}
