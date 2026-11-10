using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for the shared UI package and API contracts.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>Controlled frontend without its shared package.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact(DisplayName = "controlled frontend without its shared package")]
    public void ControlledFrontendWithoutItsSharedPackage()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "react");
            project["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "web", ["path"] = "apps/web", ["platform"] = "react" });
        });
        AssertProblem(RunConsumer(folder.Path), "missing required property 'ui'");
    }

    /// <summary>Frontend outside the controlled contract stays quiet.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact(DisplayName = "frontend outside the controlled contract stays quiet")]
    public void FrontendOutsideTheControlledContractStaysQuiet()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "blazor");
            project["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "desk", ["path"] = "apps/desk", ["platform"] = "blazor" });
        });
        AssertAbsent(RunConsumer(folder.Path), "controlled UI");
    }

    /// <summary>Child validator findings merge with their prefix.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact(DisplayName = "child validator findings merge with their prefix")]
    public void ChildValidatorFindingsMergeWithTheirPrefix()
    {
        using var folder = ConsumerFixture.Create();
        // A controlled frontend with an unreadable shared package fails the child
        // validator, and its findings merge into this validator's list.
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("dotnet", "react");
            project["frontends"] = new JsonArray(
                new JsonObject
                {
                    ["name"] = "web",
                    ["path"] = "apps/web",
                    ["platform"] = "react",
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
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact(DisplayName = "failing run writes its problems as an array")]
    public void FailingRunWritesItsProblemsAsAnArray()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            ".litespec/product/decisions/0001-launch.md",
            raw => ReplaceFirst(raw, "## Context", "## Notes"));
        var result = RunConsumer(folder.Path, "--format", "json");
        Assert.Equal(1, result.Status);
        var parsed = (JsonObject)JsonNode.Parse(result.Stdout)!;
        var messages = parsed["findings"]!.AsArray()
            .Select(finding => (JsonObject)finding!)
            .Where(finding => (string?)finding["code"] == "page-section-missing")
            .Select(finding => (string?)finding["message"] ?? string.Empty);
        Assert.Contains(messages, message => message.Contains("requires an H2 'Context'", StringComparison.Ordinal));
    }

    /// <summary>Machine-readable problems carry their code and level.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact(DisplayName = "machine-readable problems carry their code and level")]
    public void MachineReadableProblemsCarryTheirCodeAndLevel()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            ".litespec/product/decisions/0001-launch.md",
            raw => ReplaceFirst(raw, "## Context", "## Notes"));
        var result = RunConsumer(folder.Path, "--format", "json");
        Assert.Equal(1, result.Status);
        var parsed = (JsonObject)JsonNode.Parse(result.Stdout)!;
        var findings = parsed["findings"]!.AsArray()
            .Select(finding => (JsonObject)finding!)
            .Where(finding => (string?)finding["code"] == "page-section-missing")
            .ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, finding =>
        {
            Assert.Equal("G", (string?)finding["level"]);
            Assert.Equal("page-section-missing", (string?)finding["code"]);
            Assert.Equal("blocking", (string?)finding["outcome"]);
            Assert.Contains("requires an H2 'Context'", (string?)finding["message"] ?? string.Empty, StringComparison.Ordinal);
            Assert.Equal(".litespec/product/decisions/0001-launch.md", (string?)finding["path"]);
            Assert.Equal(1, (int?)finding["line"]);
            Assert.Equal("decision/0001-launch", (string?)finding["subject"]);
        });
    }

    /// <summary>Page with a leading byte-order mark.</summary>
    /// <proves>litespec/core/model#declare-structured-specification-metadata</proves>
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
