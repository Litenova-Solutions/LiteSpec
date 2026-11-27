using System.Text.Json.Nodes;
using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Tests.Standards;

/// <summary>Folder-based layout cases: shipped stacks and the pages a selection applies.</summary>
public sealed class LayoutTests
{
    static JsonObject Manifest(string json) => (JsonObject)JsonNode.Parse(json)!;

    static void Page(string root, string relative) => StandardsFixture.Write(root, relative, "# Sample\n");

    static readonly string TreesManifest = """
        {
          "stacks": {
            "dotnet": { "folder": "docs/packs/dotnet", "kind": "backend" },
            "svelte": { "folder": "docs/packs/svelte", "kind": "frontend" },
            "android": { "folder": "docs/packs/android", "kind": "frontend" }
          },
          "shapes": {
            "server": { "backendStacks": 1 },
            "client-only": { "backendStacks": 0, "backendPages": ["docs/core/backend-architecture.md", "docs/core/backend-domain.md"] }
          },
          "extensions": {
            "delivery": { "path": "docs/ext/delivery.md" },
            "native": { "path": "docs/packs/svelte/ext/native.md" }
          }
        }
        """;

    static void Trees(string root)
    {
        Page(root, "docs/core/system.md");
        Page(root, "docs/core/backend-architecture.md");
        Page(root, "docs/core/backend-domain.md");
        Page(root, "docs/workspace/structure.md");
        Page(root, "docs/quality/security.md");
        Page(root, "docs/README.md");
        Page(root, "docs/packs/dotnet/stack.md");
        Page(root, "docs/packs/dotnet/domain.md");
        Page(root, "docs/packs/svelte/stack.md");
        Page(root, "docs/packs/svelte/data.md");
        Page(root, "docs/packs/svelte/ext/native.md");
        Page(root, "docs/packs/android/stack.md");
        Page(root, "docs/ext/delivery.md");
        StandardsFixture.Write(root, "docs/packs/dotnet/notes.txt", "not a page\n");
    }

    /// <summary>Stacks read explicit kinds and pairing rules.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stacks read explicit kinds and pairing rules")]
    public void StacksReadExplicitKindsAndPairingRules()
    {
        var stacks = StackSelection.Stacks(Manifest("""
            {
              "stacks": {
                "dotnet": { "folder": "docs/packs/dotnet", "kind": "backend" },
                "svelte": { "folder": "docs/packs/svelte", "kind": "frontend", "requires": ["dotnet"], "pairsWith": ["dotnet"] },
                "stray": { "folder": "docs/core" }
              }
            }
            """));

        Assert.Equal(3, stacks.Count);
        Assert.Equal("dotnet", stacks[0].Id);
        Assert.Equal("docs/packs/dotnet", stacks[0].Folder);
        Assert.Equal("backend", stacks[0].Kind);
        Assert.Empty(stacks[0].Requires);
        Assert.Empty(stacks[0].PairsWith);
        Assert.Equal("svelte", stacks[1].Id);
        Assert.Equal("docs/packs/svelte", stacks[1].Folder);
        Assert.Equal("frontend", stacks[1].Kind);
        Assert.Equal(["dotnet"], stacks[1].Requires);
        Assert.Equal(["dotnet"], stacks[1].PairsWith);
        Assert.Equal("stray", stacks[2].Id);
        Assert.Equal("", stacks[2].Kind);
        Assert.Empty(StackSelection.Stacks(Manifest("{}")));
    }

    /// <summary>Stack kinds derive from pack folders.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack kinds derive from pack folders")]
    public void StackKindsDeriveFromPackFolders()
    {
        Assert.Equal("backend", StackSelection.KindFor("docs/packs/dotnet"));
        Assert.Equal("frontend", StackSelection.KindFor("docs/packs/svelte"));
        Assert.Equal("frontend", StackSelection.KindFor("docs/packs/android"));
        Assert.Equal("", StackSelection.KindFor("docs/core"));
        Assert.Equal("", StackSelection.KindFor("docs/packs"));
    }

    /// <summary>Applied pages for a server project.</summary>
    /// <proves>litespec/core/stacks#apply-the-complete-stack</proves>
    [Fact(DisplayName = "applied pages for a server project")]
    public void AppliedPagesForAServerProject()
    {
        using var temp = Cli.Temp("layers");
        Trees(temp.Path);

        var pages = StackSelection.AppliedPages(temp.Path, Manifest(TreesManifest), "server", ["dotnet", "svelte"], []);

        string[] expected =
        [
            "docs/core/backend-architecture.md",
            "docs/core/backend-domain.md",
            "docs/core/system.md",
            "docs/packs/dotnet/domain.md",
            "docs/packs/dotnet/stack.md",
            "docs/packs/svelte/data.md",
            "docs/packs/svelte/stack.md",
            "docs/quality/security.md",
            "docs/workspace/structure.md",
        ];
        Assert.Equal(expected, pages);
    }

    /// <summary>Applied pages for a client-only project.</summary>
    /// <proves>litespec/core/stacks#apply-the-complete-stack</proves>
    [Fact(DisplayName = "applied pages for a client-only project")]
    public void AppliedPagesForAClientOnlyProject()
    {
        using var temp = Cli.Temp("layers");
        Trees(temp.Path);

        var pages = StackSelection.AppliedPages(temp.Path, Manifest(TreesManifest), "client-only", ["svelte"], []);

        string[] expected =
        [
            "docs/core/backend-architecture.md",
            "docs/core/backend-domain.md",
            "docs/core/system.md",
            "docs/packs/svelte/data.md",
            "docs/packs/svelte/stack.md",
            "docs/quality/security.md",
            "docs/workspace/structure.md",
        ];
        Assert.Equal(expected, pages);
    }

    /// <summary>Applied pages include selected extensions.</summary>
    /// <proves>litespec/core/stacks#apply-the-complete-stack</proves>
    [Fact(DisplayName = "applied pages include selected extensions")]
    public void AppliedPagesIncludeSelectedExtensions()
    {
        using var temp = Cli.Temp("layers");
        Trees(temp.Path);

        var pages = StackSelection.AppliedPages(temp.Path, Manifest(TreesManifest), "server", ["dotnet"], ["delivery", "native"]);

        string[] expected =
        [
            "docs/core/backend-architecture.md",
            "docs/core/backend-domain.md",
            "docs/core/system.md",
            "docs/ext/delivery.md",
            "docs/packs/dotnet/domain.md",
            "docs/packs/dotnet/stack.md",
            "docs/packs/svelte/ext/native.md",
            "docs/quality/security.md",
            "docs/workspace/structure.md",
        ];
        Assert.Equal(expected, pages);
    }

    /// <summary>Applied pages ignore unknown stacks extensions and shapes.</summary>
    /// <proves>litespec/core/stacks#apply-the-complete-stack</proves>
    [Fact(DisplayName = "applied pages ignore unknown stacks extensions and shapes")]
    public void AppliedPagesIgnoreUnknownSelections()
    {
        using var temp = Cli.Temp("layers");
        Trees(temp.Path);

        var pages = StackSelection.AppliedPages(temp.Path, Manifest(TreesManifest), "unknown", ["cobol"], ["nope"]);

        string[] expected =
        [
            "docs/core/backend-architecture.md",
            "docs/core/backend-domain.md",
            "docs/core/system.md",
            "docs/quality/security.md",
            "docs/workspace/structure.md",
        ];
        Assert.Equal(expected, pages);
    }

    /// <summary>A companion folder applies with its pack, and an ext folder applies only through its extension.</summary>
    /// <proves>litespec/core/stacks#apply-the-complete-stack</proves>
    [Fact(DisplayName = "a companion folder applies with its pack and an ext folder applies only through its extension")]
    public void CompanionFolderAppliesWithItsPack()
    {
        using var temp = Cli.Temp("layers");
        Trees(temp.Path);
        Page(temp.Path, "docs/packs/svelte/widgets/inline.md");
        Page(temp.Path, "docs/packs/dotnet/guides/errors.md");

        var svelte = StackSelection.AppliedPages(temp.Path, Manifest(TreesManifest), "client-only", ["svelte"], []);
        var dotnet = StackSelection.AppliedPages(temp.Path, Manifest(TreesManifest), "server", ["dotnet"], []);

        Assert.Contains("docs/packs/svelte/widgets/inline.md", svelte);
        Assert.DoesNotContain("docs/packs/dotnet/guides/errors.md", svelte);
        Assert.DoesNotContain("docs/packs/svelte/ext/native.md", svelte);
        Assert.Contains("docs/packs/dotnet/guides/errors.md", dotnet);
        Assert.DoesNotContain("docs/packs/svelte/widgets/inline.md", dotnet);
        Assert.Equal(
            ["docs/packs/svelte/widgets"],
            StackSelection.CompanionFolders(temp.Path, "docs/packs/svelte"));
    }
}
