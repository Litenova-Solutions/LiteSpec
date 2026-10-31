using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for documentation paths and page frontmatter.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>Documentation root that is on disk.</summary>
    /// <proves>litespec/core/authoring#classify-every-specification-file</proves>
    [Fact(DisplayName = "documentation root that is on disk")]
    public void DocumentationRootThatIsOnDisk()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => PathsOf(project)["docs"] = "docs");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Documentation root that is not on disk.</summary>
    /// <proves>litespec/core/authoring#classify-every-specification-file</proves>
    [Fact(DisplayName = "documentation root that is not on disk")]
    public void DocumentationRootThatIsNotOnDisk()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => PathsOf(project)["docs"] = "documentation");
        AssertProblem(RunConsumer(folder.Path), "paths.docs 'documentation' does not exist");
    }

    /// <summary>Documentation root that holds no Markdown.</summary>
    /// <proves>litespec/core/authoring#classify-every-specification-file</proves>
    [Fact(DisplayName = "documentation root that holds no Markdown")]
    public void DocumentationRootThatHoldsNoMarkdown()
    {
        using var folder = ConsumerFixture.Create();
        Directory.CreateDirectory(Path.Combine(folder.Path, "empty-docs"));
        ConsumerFixture.MutateProject(folder.Path, project => PathsOf(project)["docs"] = "empty-docs");
        AssertProblem(RunConsumer(folder.Path), "Scanned no Markdown files");
    }

    /// <summary>Project naming no backend stack.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "project naming no backend stack")]
    public void ProjectNamingNoBackendStack()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("react"));
        AssertProblem(RunConsumer(folder.Path), "stacks name 0 backend stacks; name exactly one of dotnet");
    }

    /// <summary>Project naming a stack the release does not ship.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "project naming a stack the release does not ship")]
    public void ProjectNamingUnknownStack()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["stacks"] = new JsonArray("dotnet", "cobol"));
        AssertProblem(RunConsumer(folder.Path), "stack 'cobol' is not a stack the pinned LiteSpec release ships");
    }

    /// <summary>Stack-scoped extension without its stack.</summary>
    /// <proves>litespec/core/scope#record-selected-extensions</proves>
    [Fact(DisplayName = "stack-scoped extension without its stack")]
    public void StackScopedExtensionWithoutItsStack()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["stacks"] = new JsonArray("svelte");
            project["selectedExtensions"] = new JsonArray(new JsonObject { ["id"] = "efcore", ["criterion"] = "orders need relational reporting", ["reviewBy"] = "2999-01-01" });
        });
        AssertProblem(RunConsumer(folder.Path), "extensions 'efcore' supports only the dotnet stack, which the project does not name in stacks");
    }

    /// <summary>Stack-scoped extension with its stack.</summary>
    /// <proves>litespec/core/scope#record-selected-extensions</proves>
    [Fact(DisplayName = "stack-scoped extension with its stack")]
    public void StackScopedExtensionWithItsStack()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
            project["selectedExtensions"] = new JsonArray(new JsonObject { ["id"] = "efcore", ["criterion"] = "orders need relational reporting", ["reviewBy"] = "2999-01-01" }));
        AssertAbsent(RunConsumer(folder.Path), "supports only the dotnet stack");
    }

    /// <summary>Frontend declaring the backend stack as its platform.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend declaring the backend stack as its platform")]
    public void FrontendDeclaringBackendStackAsPlatform()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => ((JsonArray)PathsOf(project)["frontends"]!).Add(new JsonObject { ["name"] = "web", ["path"] = "apps/web", ["platform"] = "dotnet" }));
        AssertProblem(RunConsumer(folder.Path), "frontend 'web' has unknown platform 'dotnet'");
    }

    /// <summary>Glossary path that is not on disk.</summary>
    /// <proves>litespec/core/authoring#record-the-project-vocabulary-as-data</proves>
    [Fact(DisplayName = "glossary path that is not on disk")]
    public void GlossaryPathThatIsNotOnDisk()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => PathsOf(project)["glossary"] = "docs/missing.json");
        AssertProblem(RunConsumer(folder.Path), "paths.glossary 'docs/missing.json' does not exist");
    }

    /// <summary>Frontend path that is not on disk.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend path that is not on disk")]
    public void FrontendPathThatIsNotOnDisk()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            PathsOf(project)["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "web", ["path"] = "apps/missing", ["platform"] = "react" });
        });
        AssertProblem(RunConsumer(folder.Path), "frontend 'web' path 'apps/missing' does not exist");
    }

    /// <summary>Page under the documentation root is scanned.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "page under the documentation root is scanned")]
    public void PageUnderTheDocumentationRootIsScanned()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/behavior/modules/orders/unscanned.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"workflow","id":"unscanned","title":"Unscanned","owner":"fixture","status":"approved"}"""),
                "# Unscanned\n"));
        AssertProblem(RunConsumer(folder.Path), "matches 0 of");
    }

    /// <summary>Missing required title.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "missing required title")]
    public void MissingRequiredTitle()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/modules/orders/cancel-order.md",
            meta => meta.Remove("title"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'title'");
    }

    /// <summary>Removed hand-written status field.</summary>
    /// <proves>litespec/core/principles#separate-authority-from-implementation</proves>
    [Fact(DisplayName = "removed hand-written status field")]
    public void RemovedHandWrittenStatusField()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/modules/orders/cancel-order.md",
            meta => meta["implementationStatus"] = "planned");
        AssertProblem(RunConsumer(folder.Path), "unknown property 'implementationStatus'");
    }

    /// <summary>Status outside the lifecycle.</summary>
    /// <proves>litespec/core/principles#classify-record-authority</proves>
    [Fact(DisplayName = "status outside the lifecycle")]
    public void StatusOutsideTheLifecycle()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/product/brief.md",
            meta => meta["status"] = "final");
        AssertProblem(RunConsumer(folder.Path), "/status");
    }

    /// <summary>Use-case kind outside command and query.</summary>
    /// <proves>litespec/core/use-cases#give-each-use-case-a-command-or-query-kind</proves>
    [Fact(DisplayName = "use-case kind outside command and query")]
    public void UseCaseKindOutsideCommandAndQuery()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/modules/orders/cancel-order.md",
            meta => meta["kind"] = "mutation");
        AssertProblem(RunConsumer(folder.Path), "/kind");
    }

    /// <summary>Result entry without its outcome.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "result entry without its outcome")]
    public void ResultEntryWithoutItsOutcome()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/modules/orders/cancel-order.md",
            meta => ((JsonObject)((JsonArray)meta["records"]!)[0]!).Remove("outcome"));
        AssertProblem(RunConsumer(folder.Path), "/records/0");
    }

    /// <summary>Retired page type from the old model.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "retired page type from the old model")]
    public void RetiredPageTypeFromTheOldModel()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/modules/orders/cancel-order.md",
            meta => meta["type"] = "business-record");
        AssertProblem(RunConsumer(folder.Path), "matches 0 of");
    }

    /// <summary>Page with no frontmatter block.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "page with no frontmatter block")]
    public void PageWithNoFrontmatterBlock()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/behavior/modules/orders/plain.md",
            "# Plain\n\nA page with no record.\n");
        AssertProblem(RunConsumer(folder.Path), "no frontmatter block");
    }

    /// <summary>Page with a broken frontmatter block.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "page with a broken frontmatter block")]
    public void PageWithABrokenFrontmatterBlock()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/behavior/modules/orders/broken.md",
            "---\n{bad json}\n---\n\n# Broken\n");
        AssertProblem(RunConsumer(folder.Path), "JSON parse error");
    }
}
