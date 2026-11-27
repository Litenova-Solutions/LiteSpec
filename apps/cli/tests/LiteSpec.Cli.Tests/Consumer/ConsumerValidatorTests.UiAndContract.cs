using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for the shared UI package and API contracts.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>Controlled frontend without its shared package.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact(DisplayName = "controlled frontend without its shared package")]
    public void ControlledFrontendWithoutItsSharedPackage()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "svelte");
            project["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "web", ["path"] = "apps/web", ["platform"] = "svelte" });
        });
        AssertProblem(RunConsumer(folder.Path), "missing required property 'ui'");
    }

    /// <summary>Frontend outside the controlled contract stays quiet.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact(DisplayName = "frontend outside the controlled contract stays quiet")]
    public void FrontendOutsideTheControlledContractStaysQuiet()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "android");
            project["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "desk", ["path"] = "apps/desk", ["platform"] = "android" });
        });
        AssertAbsent(RunConsumer(folder.Path), "controlled UI");
    }

    /// <summary>Child validator findings merge with their prefix.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact(DisplayName = "child validator findings merge with their prefix")]
    public void ChildValidatorFindingsMergeWithTheirPrefix()
    {
        using var folder = ConsumerFixture.Create();
        // A controlled frontend with an unreadable shared package fails the child
        // validator, and its findings merge into this validator's list.
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "svelte");
            project["frontends"] = new JsonArray(
                new JsonObject
                {
                    ["name"] = "web",
                    ["path"] = "apps/web",
                    ["platform"] = "svelte",
                    ["ui"] = new JsonObject { ["profile"] = "application-balanced" },
                });
            project["ui"] = new JsonObject { ["package"] = new JsonObject
            {
                ["name"] = "fixture-ui",
                ["path"] = "packages/ui",
                ["componentsJson"] = "packages/ui/components.json",
                ["primitives"] = "packages/ui/src",
                ["tokens"] = "packages/ui/tokens.css",
                ["sourceLock"] = "packages/ui/lock.json",
                ["designContract"] = "packages/ui/DESIGN.md",
                ["publicExports"] = new JsonArray("fixture-ui"),
            } };
        });
        ConsumerFixture.WriteFile(folder.Path, "apps/web/package.json", "{}\n");
        ConsumerFixture.WriteFile(folder.Path, "packages/ui/components.json", "{}\n");
        Directory.CreateDirectory(Path.Combine(folder.Path, "packages", "ui", "src"));
        ConsumerFixture.WriteFile(folder.Path, "packages/ui/tokens.css", string.Empty);
        ConsumerFixture.WriteFile(folder.Path, "packages/ui/lock.json", "{}\n");
        ConsumerFixture.WriteFile(folder.Path, "packages/ui/DESIGN.md", "# Design\n");
        AssertProblem(RunConsumer(folder.Path), "controlled UI:");
    }

    /// <summary>Malformed project file keeps the machine-readable contract.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "malformed project file keeps the machine-readable contract")]
    public void MalformedProjectFileKeepsTheMachineReadableContract()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(folder.Path, ".litespec/project.yaml", "{\n");
        var result = RunConsumer(folder.Path, "--format", "json");
        Assert.Equal(2, result.Status);
        Assert.Contains("not valid YAML or JSON", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Failing run writes its problems as an array.</summary>
    /// <proves>litespec/core/intent#state-each-decision-with-context-and-choice</proves>
    [Fact(DisplayName = "failing run writes its problems as an array")]
    public void FailingRunWritesItsProblemsAsAnArray()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, DecisionPage, meta => meta.Remove("consequences"));
        var result = RunConsumer(folder.Path, "--format", "json");
        Assert.Equal(1, result.Status);
        var parsed = (JsonObject)JsonNode.Parse(result.Stdout)!;
        var messages = parsed["findings"]!.AsArray()
            .Select(finding => (JsonObject)finding!)
            .Where(finding => (string?)finding["code"] == "record-invalid")
            .Select(finding => (string?)finding["message"] ?? string.Empty);
        Assert.Contains(messages, message => message.Contains("missing required property 'consequences'", StringComparison.Ordinal));
    }

    /// <summary>Machine-readable review problems carry their code and level.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact(DisplayName = "machine-readable review problems carry their code and level")]
    public void MachineReadableReviewProblemsCarryTheirCodeAndLevel()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            TutorialPage,
            raw => ReplaceFirst(raw, "## What You Built", "## Notes"));
        var result = RunReview(folder.Path, "--format", "json");
        Assert.Equal(0, result.Status);
        var parsed = (JsonObject)JsonNode.Parse(result.Stdout)!;
        var findings = parsed["findings"]!.AsArray()
            .Select(finding => (JsonObject)finding!)
            .Where(finding => (string?)finding["code"] == "page-section-missing")
            .ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, finding =>
        {
            Assert.Equal("R", (string?)finding["level"]);
            Assert.Equal("page-section-missing", (string?)finding["code"]);
            Assert.Equal("reported", (string?)finding["outcome"]);
            Assert.Contains("requires an H2 'What You Built'", (string?)finding["message"] ?? string.Empty, StringComparison.Ordinal);
            Assert.Equal(".litespec/implementation/first-run.md", (string?)finding["path"]);
            Assert.Equal(1, (int?)finding["line"]);
            Assert.Equal("first-run", (string?)finding["subject"]);
        });
    }

    /// <summary>Page with a leading byte-order mark.</summary>
    /// <proves>litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema</proves>
    [Fact(DisplayName = "page with a leading byte-order mark")]
    public void PageWithALeadingByteOrderMark()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            ".litespec/behavior/modules/orders/bom-page.md",
            char.ConvertFromUtf32(0xFEFF) + ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"reference","id":"bom-page","title":"BOM Page"}"""),
                """
                # BOM Page

                ## Intent

                This page carries a leading mark.

                ## Reference

                | Name | Value |
                |:---|:---|
                | `mark` | `strip` |

                """));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Project file with a leading byte-order mark.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "project file with a leading byte-order mark")]
    public void ProjectFileWithALeadingByteOrderMark()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            ".litespec/project.yaml",
            char.ConvertFromUtf32(0xFEFF) + ConsumerFixture.ReadFile(folder.Path, ".litespec/project.yaml"));
        AssertClean(RunConsumer(folder.Path));
    }

}
