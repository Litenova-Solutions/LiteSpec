using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for project shape and stack selection.</summary>
public sealed partial class ConsumerValidatorTests
{
    static TempFolder HomeRequiringReactForSvelte() =>
        ConsumerFixture.CreateHome(manifest =>
        {
            var svelte = (JsonObject)((JsonObject)manifest["stacks"]!)["svelte"]!;
            svelte["requires"] = new JsonArray("react");
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
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "react"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Client-only project with blazor and no backend.</summary>
    /// <proves>litespec/core/scope#declare-a-consumer-that-has-no-backend</proves>
    [Fact(DisplayName = "client-only project with blazor and no backend")]
    public void ClientOnlyProjectWithBlazorAndNoBackend()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["shape"] = "client-only";
            project["stacks"] = new JsonArray("blazor");
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
            project["stacks"] = new JsonArray("blazor", "dotnet");
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
            PathsOf(project)["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "web", ["path"] = "apps/web", ["platform"] = "react" });
        });
        AssertProblem(RunConsumer(folder.Path), "platform 'react' is not a stack the project names in stacks");
    }

    /// <summary>Frontend platform the project names.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend platform the project names")]
    public void FrontendPlatformTheProjectNames()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "blazor");
            PathsOf(project)["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "desk", ["path"] = "apps/desk", ["platform"] = "blazor" });
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
        using var home = HomeRequiringReactForSvelte();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte"));
        AssertProblem(RunConsumer(folder.Path, home), "stack 'svelte' requires 'react'");
    }

    /// <summary>Stack whose requires the project names.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "stack whose requires the project names")]
    public void StackWhoseRequiresTheProjectNames()
    {
        using var folder = ConsumerFixture.Create();
        using var home = HomeRequiringReactForSvelte();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "svelte", "react"));
        AssertAbsent(RunConsumer(folder.Path, home), "requires 'react'");
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
