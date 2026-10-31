using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Manifest, stack, shape, extension, and vocabulary cases.</summary>
public sealed partial class StandardsValidatorTests
{
    static readonly string SampleExtensionManifest =
        "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/sample.md\"," +
        "\"activationScope\":\"local\",\"applicableKinds\":[\"use-case\"]}}}";

    static readonly string LayerExtensionPage = """
        # Sample Extension

        ## Intent

        This extension adds one optional capability.

        ## Activation

        Activation scope: `local`.

        Applicable specification kinds: `use-case`.

        Enable this extension when the sample capability is required.

        ## Baseline Relationship

        This extension replaces no baseline provision.

        ## Standards

        ### Record Extension Activation

        **ID:** `litespec/ext/sample#record-extension-activation`

        **Statement:** Consumers MUST record activation on each applicable specification.

        **Checked by:** `litespec-maintain validate` confirms each applicable specification records its activation.

        ## Conventions

        None.

        ## Dependencies

        None.
        """.ReplaceLineEndings("\n") + "\n";

    static readonly string LayerTopicPage = """
        # Sample Topic

        ## Intent

        This page defines one repository topic.

        ## Standards

        ### Keep the Topic Bounded

        **ID:** `litespec/core/sample#keep-the-topic-bounded`

        **Statement:** Consumers MUST keep the topic inside its declared boundary.

        **Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.

        ## Conventions

        None.
        """.ReplaceLineEndings("\n") + "\n";

    static List<string> LayerCodes(Action<string> build)
    {
        using var temp = Cli.Temp("layers");
        build(temp.Path);
        return StandardsValidator.Validate(temp.Path).Diagnostics.Select(diagnostic => diagnostic.Code).ToList();
    }

    static void LayerPage(string root, string relative) => StandardsFixture.Write(root, relative, "# Sample\n");

    /// <summary>Manifest JSON.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "manifest JSON")]
    public void ManifestJson() => Check(
        root => StandardsFixture.Write(root, "litespec.manifest.json", "{"),
        ["MANIFEST_JSON"]);

    /// <summary>Manifest path.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "manifest path")]
    public void ManifestPath() => Check(
        root => StandardsFixture.Write(root, "litespec.manifest.json", "{\"agentsEntry\":\"missing.md\"}"),
        ["MANIFEST_PATH"]);

    /// <summary>Manifest anchor.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "manifest anchor")]
    public void ManifestAnchor() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", "# Topic\n\n## Intent\n\nText.\n");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"agentsEntry\":\"docs/core/topic.md#missing\"}");
        },
        ["MANIFEST_ANCHOR"]);

    /// <summary>Manifest duplicate path.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "manifest duplicate path")]
    public void ManifestDuplicatePath() => Check(
        root => StandardsFixture.Write(root, "litespec.manifest.json", "{\"shapes\":{\"server\":{\"backendStacks\":1,\"backendPages\":[\"docs/backend/topic.md\",\"docs/backend/topic.md\"]}}}"),
        ["MANIFEST_DUPLICATE_PATH"]);

    /// <summary>Stack folder that does not exist.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack folder that does not exist")]
    public void StackFolderMissing()
    {
        var codes = LayerCodes(root => StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"}}}"));

        Assert.Contains("STACK_FOLDER", codes);
    }

    /// <summary>Stack folder outside the stack areas.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack folder outside the stack areas")]
    public void StackFolderOutsideAreas()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/core/topic.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/core\"}}}");
        });

        Assert.Contains("STACK_FOLDER", codes);
    }

    /// <summary>Stack folder without a stack.md.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack folder without a stack.md")]
    public void StackFolderWithoutEntry()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/domain.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"}}}");
        });

        Assert.Contains("STACK_FOLDER", codes);
    }

    /// <summary>Stack folders holding stack.md.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack folders holding stack.md")]
    public void StackFoldersHoldingEntry()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/frontend/web/shared.md");
            LayerPage(root, "docs/frontend/web/sample/stack.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"},\"web\":{\"folder\":\"docs/frontend/web/sample\"}}}");
        });

        Assert.DoesNotContain("STACK_FOLDER", codes);
    }

    /// <summary>Stack requiring a stack the manifest does not ship.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack requiring a stack the manifest does not ship")]
    public void StackRequiringUnknownStack()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\",\"requires\":[\"cobol\"]}}}");
        });

        Assert.Contains("STACK_REFERENCE", codes);
    }

    /// <summary>Stack pairing with a stack the manifest does not ship.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack pairing with a stack the manifest does not ship")]
    public void StackPairingWithUnknownStack()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/frontend/web/sample/stack.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"web\":{\"folder\":\"docs/frontend/web/sample\",\"pairsWith\":[\"cobol\"]}}}");
        });

        Assert.Contains("STACK_REFERENCE", codes);
    }

    /// <summary>Stack pairing with a frontend stack.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack pairing with a frontend stack")]
    public void StackPairingWithFrontendStack()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/frontend/web/sample/stack.md");
            LayerPage(root, "docs/frontend/other/stack.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"web\":{\"folder\":\"docs/frontend/web/sample\",\"pairsWith\":[\"other\"]},\"other\":{\"folder\":\"docs/frontend/other\"}}}");
        });

        Assert.Contains("STACK_REFERENCE", codes);
    }

    /// <summary>Stack references naming shipped stacks.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack references naming shipped stacks")]
    public void StackReferencesNamingShippedStacks()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/frontend/web/sample/stack.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"},\"web\":{\"folder\":\"docs/frontend/web/sample\",\"requires\":[\"sample\"],\"pairsWith\":[\"sample\"]}}}");
        });

        Assert.DoesNotContain("STACK_REFERENCE", codes);
    }

    /// <summary>Extension outside every ext folder.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension outside every ext folder")]
    public void ExtensionOutsideExtFolders()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/core/sample.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/core/sample.md\"}}}");
        });

        Assert.Contains("EXTENSION_LOCATION", codes);
    }

    /// <summary>Extension file not naming the extension.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension file not naming the extension")]
    public void ExtensionFileNotNamingTheExtension()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/ext/other.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/other.md\"}}}");
        });

        Assert.Contains("EXTENSION_LOCATION", codes);
    }

    /// <summary>Located extensions.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "located extensions")]
    public void LocatedExtensions()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/ext/outbox.md");
            LayerPage(root, "docs/backend/sample/ext/efcore.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"}},\"extensions\":{\"outbox\":{\"path\":\"docs/ext/outbox.md\"},\"efcore\":{\"path\":\"docs/backend/sample/ext/efcore.md\"}}}");
        });

        Assert.DoesNotContain("EXTENSION_LOCATION", codes);
    }

    /// <summary>Ext page no extension lists.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "ext page no extension lists")]
    public void ExtPageNoExtensionLists()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/ext/stray.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{}}");
        });

        Assert.Contains("EXTENSION_UNLISTED", codes);
    }

    /// <summary>Every ext page listed.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "every ext page listed")]
    public void EveryExtPageListed()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/ext/outbox.md");
            LayerPage(root, "docs/backend/sample/ext/efcore.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"}},\"extensions\":{\"outbox\":{\"path\":\"docs/ext/outbox.md\"},\"efcore\":{\"path\":\"docs/backend/sample/ext/efcore.md\"}}}");
        });

        Assert.DoesNotContain("EXTENSION_UNLISTED", codes);
    }

    /// <summary>Page in a stack folder the manifest does not list.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "page in a stack folder the manifest does not list")]
    public void PageInUncoveredFolder()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/backend/stray/stack.md");
            LayerPage(root, "docs/backend/stray/orphan.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"}}}");
        });

        Assert.Contains("LAYER_UNREACHABLE", codes);
    }

    /// <summary>Page in a folder holding no stack applies with its parent.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "page in a folder holding no stack applies with its parent")]
    public void PageInNestedFolderOfAStack()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/backend/sample/nested/deep.md");
            LayerPage(root, "docs/frontend/patterns/record-detail.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"},\"web\":{\"folder\":\"docs/frontend/web\"}}}");
        });

        Assert.DoesNotContain("LAYER_UNREACHABLE", codes);
    }

    /// <summary>Pages in stack folders and their ancestors.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "pages in stack folders and their ancestors")]
    public void PagesInStackFoldersAndAncestors()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/architecture.md");
            LayerPage(root, "docs/backend/sample/stack.md");
            LayerPage(root, "docs/frontend/web/shared.md");
            LayerPage(root, "docs/frontend/web/react/stack.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"stacks\":{\"sample\":{\"folder\":\"docs/backend/sample\"},\"react\":{\"folder\":\"docs/frontend/web/react\"}}}");
        });

        Assert.DoesNotContain("LAYER_UNREACHABLE", codes);
    }

    /// <summary>Shape backend page in a stack folder.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "shape backend page in a stack folder")]
    public void ShapeBackendPageInAStackFolder()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/sample/domain.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"shapes\":{\"client-only\":{\"backendStacks\":0,\"backendPages\":[\"docs/backend/sample/domain.md\"]}}}");
        });

        Assert.Contains("SHAPE_PAGE", codes);
    }

    /// <summary>Shape backend page outside the backend area.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "shape backend page outside the backend area")]
    public void ShapeBackendPageOutsideBackend()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/core/topic.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"shapes\":{\"client-only\":{\"backendStacks\":0,\"backendPages\":[\"docs/core/topic.md\"]}}}");
        });

        Assert.Contains("SHAPE_PAGE", codes);
    }

    /// <summary>Shape backend pages directly in docs/backend.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "shape backend pages directly in docs/backend")]
    public void ShapeBackendPagesDirectlyInBackend()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/backend/architecture.md");
            LayerPage(root, "docs/backend/domain.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"shapes\":{\"client-only\":{\"backendStacks\":0,\"backendPages\":[\"docs/backend/architecture.md\",\"docs/backend/domain.md\"]}}}");
        });

        Assert.DoesNotContain("SHAPE_PAGE", codes);
    }

    /// <summary>Page outside the known areas.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "page outside the known areas")]
    public void PageOutsideKnownAreas()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/profile/leftover.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        });

        Assert.Contains("LAYER_UNLISTED", codes);
    }

    /// <summary>Every page inside a known area.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "every page inside a known area")]
    public void EveryPageInsideAKnownArea()
    {
        var codes = LayerCodes(root =>
        {
            LayerPage(root, "docs/core/topic.md");
            LayerPage(root, "docs/backend/architecture.md");
            LayerPage(root, "docs/guide/sample.md");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        });

        Assert.DoesNotContain("LAYER_UNLISTED", codes);
    }

    /// <summary>Extension scope mismatch.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension scope mismatch")]
    public void ExtensionScopeMismatch()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage);
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/sample.md\",\"activationScope\":\"project\"}}}");
        });

        Assert.Contains("EXTENSION_SCOPE", codes);
    }

    /// <summary>Extension kind mismatch.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension kind mismatch")]
    public void ExtensionKindMismatch()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage);
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/sample.md\",\"activationScope\":\"local\",\"applicableKinds\":[\"workflow\"]}}}");
        });

        Assert.Contains("EXTENSION_KIND", codes);
    }

    /// <summary>Missing extension activation.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "missing extension activation")]
    public void MissingExtensionActivation()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage.Replace("\nEnable this extension when the sample capability is required.\n", "\n"));
            StandardsFixture.Write(root, "litespec.manifest.json", SampleExtensionManifest);
        });

        Assert.Contains("EXTENSION_ACTIVATION", codes);
    }

    /// <summary>Empty baseline relationship.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "empty baseline relationship")]
    public void EmptyBaselineRelationship()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage.Replace("This extension replaces no baseline provision.\n", ""));
            StandardsFixture.Write(root, "litespec.manifest.json", SampleExtensionManifest);
        });

        Assert.Contains("EXTENSION_BASELINE", codes);
    }

    /// <summary>Empty extension dependencies.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "empty extension dependencies")]
    public void EmptyExtensionDependencies()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage.Replace("## Dependencies\n\nNone.", "## Dependencies\n"));
            StandardsFixture.Write(root, "litespec.manifest.json", SampleExtensionManifest);
        });

        Assert.Contains("EXTENSION_DEPENDENCIES", codes);
    }

    /// <summary>Extension replacing an unknown provision.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension replacing an unknown provision")]
    public void ExtensionReplacesUnknown()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage);
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/sample.md\",\"activationScope\":\"local\",\"applicableKinds\":[\"use-case\"],\"replaces\":[\"litespec/core/sample#no-such-provision\"]}}}");
        });

        Assert.Contains("EXTENSION_REPLACES_UNKNOWN", codes);
    }

    /// <summary>Extension enforcing a replaced provision.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension enforcing a replaced provision")]
    public void ExtensionReplacesEnforced()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage);
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/sample.md\",\"activationScope\":\"local\",\"applicableKinds\":[\"use-case\"],\"replaces\":[\"litespec/ext/sample#record-extension-activation\"]}}}");
        });

        Assert.Contains("EXTENSION_REPLACES_ENFORCED", codes);
    }

    /// <summary>Extension replacing a baseline provision.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "extension replacing a baseline provision")]
    public void ExtensionReplacesBaseline()
    {
        var codes = LayerCodes(root =>
        {
            StandardsFixture.Write(root, "docs/core/sample.md", LayerTopicPage);
            StandardsFixture.Write(root, "docs/ext/sample.md", LayerExtensionPage);
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"extensions\":{\"sample\":{\"path\":\"docs/ext/sample.md\",\"activationScope\":\"local\",\"applicableKinds\":[\"use-case\"],\"replaces\":[\"litespec/core/sample#keep-the-topic-bounded\"]}}}");
        });

        Assert.DoesNotContain("EXTENSION_REPLACES_UNKNOWN", codes);
        Assert.DoesNotContain("EXTENSION_REPLACES_ENFORCED", codes);
    }

    /// <summary>Page file name that is not lowercase kebab case.</summary>
    /// <proves>litespec/core/authoring#name-the-owning-page-in-the-identifier</proves>
    [Fact(DisplayName = "page file name that is not lowercase kebab case")]
    public void PageFileNameNotKebabCase()
    {
        var codes = LayerCodes(root => StandardsFixture.Write(root, "docs/core/TwoWords.md", LayerTopicPage));

        Assert.Contains("ID_PAGE_FILENAME", codes);
    }

    /// <summary>Lowercase workflow prose passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "lowercase workflow prose passes")]
    public void LowercaseWorkflowPasses() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nThe release workflow runs weekly.\n\n" + StandardsFixture.IndexRouting),
        []);

    static readonly string AnalyzerProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <Version>3.0.0</Version>
          </PropertyGroup>
        </Project>
        """ + "\n";

    static readonly string AnalyzerShippedFile = """
        ; Shipped analyzer releases
        ; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

        ## Release 3.0.0

        ### New Rules

        Rule ID | Category | Severity | Notes
        --------|----------|----------|-------
        LS1001 | LiteSpec.Domain | Error | Descriptors
        """ + "\n";

    static void AnalyzerPinFiles(string root, string pin)
    {
        StandardsFixture.Write(root, "litespec.manifest.json", "{\"packages\":{\"nuget\":{\"LiteSpec.Analyzers\":\"" + pin + "\"}}}");
        StandardsFixture.Write(root, "apps/analyzers/src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj", AnalyzerProjectFile);
    }

    static void CiteAnalyzerRule(string root, string rule)
    {
        var page = StandardsFixture.TopicPage.Replace(
            "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.",
            "**Checked by:** `dotnet build` reports `" + rule + "` for the topic.");
        StandardsFixture.Write(root, "docs/core/topic.md", page);
        StandardsFixture.RebuildIndex(root);
    }

    /// <summary>Analyzer pin behind the analyzer project.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "analyzer pin behind the analyzer project")]
    public void AnalyzerPinBehindProject() => Check(
        root => AnalyzerPinFiles(root, "2.0.0"),
        ["MANIFEST_ANALYZER_PIN"]);

    /// <summary>Analyzer pin matching the analyzer project.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "analyzer pin matching the analyzer project")]
    public void AnalyzerPinMatchingProject() => Check(
        root => AnalyzerPinFiles(root, "3.0.0"),
        []);

    /// <summary>Stack page citing a rule the pinned release does not ship.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack page citing a rule the pinned release does not ship")]
    public void AnalyzerRuleNotShipped() => Check(
        root =>
        {
            AnalyzerPinFiles(root, "3.0.0");
            StandardsFixture.Write(root, "apps/analyzers/src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md", AnalyzerShippedFile);
            CiteAnalyzerRule(root, "LS9999");
        },
        ["MANIFEST_ANALYZER_RULE"]);

    /// <summary>Stack page citing a rule the pinned release ships.</summary>
    /// <proves>litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest</proves>
    [Fact(DisplayName = "stack page citing a rule the pinned release ships")]
    public void AnalyzerRuleShipped() => Check(
        root =>
        {
            AnalyzerPinFiles(root, "3.0.0");
            StandardsFixture.Write(root, "apps/analyzers/src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md", AnalyzerShippedFile);
            CiteAnalyzerRule(root, "LS1001");
        },
        []);
}
