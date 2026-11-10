using LiteSpec.Authoring;
using LiteSpec.Maintain.Tests.Support;

namespace LiteSpec.Maintain.Tests.Authoring;

/// <summary>Scope block, Strength line, Applies to line, and generated counts cases.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Page owning provisions without a Scope block.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "page owning provisions without a Scope block")]
    public void MissingScopeBlock() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any\n**Concern:** records\n\n", "")),
        [Diagnostics.Codes.ScopeBlock]);

    /// <summary>Scope block naming no concern.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "scope block naming no concern")]
    public void ScopeBlockMissingConcern() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any\n**Concern:** records", "**Stack:** any")),
        [Diagnostics.Codes.ScopeBlock]);

    /// <summary>Scope block on a page without provisions is ignored.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "scope block on a page without provisions is ignored")]
    public void ScopeBlockOnProvisionlessPage() => Check(
        root => StandardsFixture.Write(root, "docs/guide/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.GuidePage, "# Sample Guide\n\n", "# Sample Guide\n\n**Stack:** any\n**Concern:** records\n\n")),
        []);

    /// <summary>Scope block naming an unknown stack.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope block naming an unknown stack")]
    public void ScopeBlockUnknownStack() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any", "**Stack:** cobol")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Scope block naming an unknown concern.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope block naming an unknown concern")]
    public void ScopeBlockUnknownConcern() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Concern:** records", "**Concern:** melted-core")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Applies to line naming an unknown artifact.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming an unknown artifact")]
    public void AppliesToUnknownArtifact() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: receipt\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Applies to line naming a record artifact.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming a record artifact")]
    public void AppliesToRecordArtifact() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: record:use-case\n\n**Statement:**"), "**Strength:** default\n\n**Default:**", "**Strength:** default\n\n**Applies to:** artifact: record:use-case\n\n**Default:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line as wide as its page.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line as wide as its page")]
    public void AppliesToRepeatingPageScope() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** stack: any\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeNarrow]);

    /// <summary>Applies to line widening its page.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line widening its page")]
    public void AppliesToWideningPageScope() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any", "**Stack:** dotnet"), "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** stack: react\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeNarrow]);

    /// <summary>Applies to line narrowing to an artifact.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line narrowing to an artifact")]
    public void AppliesToArtifactNarrows() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: aggregate\n\n**Statement:**"), "**Strength:** default\n\n**Default:**", "**Strength:** default\n\n**Applies to:** artifact: aggregate\n\n**Default:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>A provision with no artifact on a page where another provision names one.</summary>
    /// <proves>litespec/core/scopes#name-the-artifact-of-every-provision-on-a-page-that-names-one</proves>
    [Fact(DisplayName = "provision with no artifact beside one that names an artifact")]
    public void ArtifactMissingBesideANamedArtifact() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: aggregate\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        [Diagnostics.Codes.ScopeArtifact]);

    /// <summary>Applies to line narrowing a page stack of any.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line narrowing a page stack of any")]
    public void AppliesToStackNarrowsAny() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** stack: dotnet\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line repeating a page stack of any.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line repeating a page stack of any")]
    public void AppliesToStackRepeatingAny() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** stack: any\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeNarrow]);
    /// <summary>Applies to line naming a layer that differs from its page.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line naming a layer that differs from its page")]
    public void AppliesToLayerDiffers() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** layer: implementation\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming a trace that differs from its page.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line naming a trace that differs from its page")]
    public void AppliesToTraceDiffers() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** trace: yes\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming a layer and a trace beside an artifact.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line naming a layer and a trace beside an artifact")]
    public void AppliesToArtifactLayerAndTrace() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: aggregate; layer: implementation; trace: yes\n\n**Statement:**"), "**Strength:** default\n\n**Default:**", "**Strength:** default\n\n**Applies to:** artifact: aggregate\n\n**Default:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming an unknown layer.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming an unknown layer")]
    public void AppliesToUnknownLayer() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** layer: partial\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Applies to line naming a trace outside yes and no.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming a trace outside yes and no")]
    public void AppliesToUnknownTrace() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** trace: maybe\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Applies to line naming two traces.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming two traces")]
    public void AppliesToTwoTraces() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** trace: yes, no\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Applies to line stating its page layer.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line stating its page layer")]
    public void AppliesToRepeatingPageLayer() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** layer: any\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeNarrow]);

    /// <summary>Applies to line stating its page trace of no.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line stating its page trace of no")]
    public void AppliesToRepeatingPageTrace() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** trace: no\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeNarrow]);

    /// <summary>Applies to line stating its page layer beside an artifact.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line stating its page layer beside an artifact")]
    public void AppliesToArtifactRepeatingPageLayer() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: aggregate; layer: any\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeNarrow]);

    /// <summary>Scope vocabulary page naming the record layers and the concerns.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope vocabulary page naming the record layers")]
    public void ScopesVocabularyNamesLayers()
    {
        var vocabulary = Scopes.Load(Cli.Repository);
        Assert.Equal(["any", "behavior", "implementation", "intent", "proof"], vocabulary.Layers.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.Contains("tests", vocabulary.Concerns);
        Assert.Equal(["no", "yes"], Scopes.TraceValues.OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }


    /// <summary>Provision without a Strength line.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision without a Strength line")]
    public void MissingStrengthLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n", "")),
        [Diagnostics.Codes.StrengthLine]);

    /// <summary>Strength line naming an outside value.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "strength line naming an outside value")]
    public void StrengthLineUnknownValue() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required", "**Strength:** Law")),
        [Diagnostics.Codes.StrengthLine]);

    /// <summary>Backticked scope values read as their inner text.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "backticked scope values read as their inner text")]
    public void BacktickedScopeValues() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any\n**Concern:** records", "**Stack:** `any`\n**Concern:** `records`");
            page = StandardsFixture.ReplaceFirst(page, "**Strength:** required", "**Strength:** `required`");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        []);

    /// <summary>Generated counts region stating its source count.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region stating its source count")]
    public void GeneratedCountsFresh() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:provisions -->\nThis tree states 5 provisions.\n<!-- /generated:counts -->\n"),
        []);

    /// <summary>Generated counts region stating a stale count.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region stating a stale count")]
    public void GeneratedCountsStale() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:provisions -->\nThis tree states 4 provisions.\n<!-- /generated:counts -->\n"),
        [Diagnostics.Codes.GeneratedStale]);

    /// <summary>Generated counts region naming an unknown source.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region naming an unknown source")]
    public void GeneratedCountsUnknownSource() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:spells -->\nThis tree states 5 provisions.\n<!-- /generated:counts -->\n"),
        [Diagnostics.Codes.GeneratedStale]);

    /// <summary>Generated counts region stating no count.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region stating no count")]
    public void GeneratedCountsMissingNumber() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:provisions -->\nThis tree states many provisions.\n<!-- /generated:counts -->\n"),
        [Diagnostics.Codes.GeneratedStale]);

    /// <summary>Generated counts region for analyzer rule pages.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region for analyzer rule pages")]
    public void GeneratedCountsAnalyzerRules() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/analyzers/rules/ls1001.md", StandardsFixture.ReferencePage);
            StandardsFixture.Write(root, "docs/analyzers/rules/ls1002.md", StandardsFixture.ReferencePage);
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(extra: "- [LS1001](analyzers/rules/ls1001.md)\n- [LS1002](analyzers/rules/ls1002.md)\n") + "\n<!-- generated:counts:analyzer-rules -->\nThis tree ships 2 analyzer rules.\n<!-- /generated:counts -->\n");
        },
        []);

    /// <summary>Missing provision data file.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "missing provision data file")]
    public void MissingProvisionData() => Check(
        root => StandardsFixture.Delete(root, Provisions.JsonPath),
        [Diagnostics.Codes.ProvisionsStale]);

    /// <summary>Stale provision data file.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "stale provision data file")]
    public void StaleProvisionData() => Check(
        root => StandardsFixture.Write(root, Provisions.JsonPath, "{ \"provisions\": [] }\n"),
        [Diagnostics.Codes.ProvisionsStale]);

    /// <summary>Scope block naming several values per field.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "scope block naming several values per field")]
    public void MultiValueScopeBlock() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Concern:** records", "**Concern:** records, tests"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Value checks skip a repository without a scope page.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "value checks skip a repository without a scope page")]
    public void MissingScopesPageSkipsValueChecks() => Check(
        root =>
        {
            StandardsFixture.Delete(root, "docs/core/scopes.md");
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(omit: ["core/scopes.md"]));
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(omit: ["core/scopes.md"]));
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any", "**Stack:** cobol"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming no field.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming no field")]
    public void EmptyAppliesToLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:**\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Applies to line naming an unknown field.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming an unknown field")]
    public void AppliesToUnknownField() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** region: core\n\n**Statement:**")),
        [Diagnostics.Codes.ScopeValue]);

    /// <summary>Scope vocabulary page naming the settled artifact kinds.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope vocabulary page naming the settled artifact kinds")]
    public void ScopesVocabularyNamesArtifacts()
    {
        var vocabulary = Scopes.Load(Cli.Repository);
        Assert.Contains("any", vocabulary.Artifacts);
        Assert.Contains("use-case", vocabulary.Artifacts);
        Assert.Contains("validator", vocabulary.Artifacts);
        Assert.Contains("reaction", vocabulary.Artifacts);
        Assert.Contains("port", vocabulary.Artifacts);
        Assert.Contains("repository", vocabulary.Artifacts);
        Assert.True(vocabulary.IsArtifact("any"));
        Assert.True(Scopes.IsConcernWide("any"));
        Assert.Equal(["command-handler", "query-handler"], Scopes.ArtifactValues("command-handler, query-handler"));
    }

    static string BaseReadme(string root) =>
        File.ReadAllText(Path.Combine(root, "docs", "README.md")).Replace("\r\n", "\n");
}
