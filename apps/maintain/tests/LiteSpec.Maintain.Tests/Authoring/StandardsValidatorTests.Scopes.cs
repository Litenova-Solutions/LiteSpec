using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Scope block, Strength line, Applies to line, and generated counts cases.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Page owning provisions without a Scope block.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "page owning provisions without a Scope block")]
    public void MissingScopeBlock() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any\n**Layer:** records\n\n", "")),
        ["SCOPE_BLOCK"]);

    /// <summary>Scope block naming no layer.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "scope block naming no layer")]
    public void ScopeBlockMissingLayer() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any\n**Layer:** records", "**Stack:** any")),
        ["SCOPE_BLOCK"]);

    /// <summary>Scope block on a page without provisions is ignored.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "scope block on a page without provisions is ignored")]
    public void ScopeBlockOnProvisionlessPage() => Check(
        root => StandardsFixture.Write(root, "docs/guide/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.GuidePage, "# Sample Guide\n\n", "# Sample Guide\n\n**Stack:** any\n**Layer:** records\n\n")),
        []);

    /// <summary>Scope block naming an unknown stack.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope block naming an unknown stack")]
    public void ScopeBlockUnknownStack() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any", "**Stack:** cobol")),
        ["SCOPE_VALUE"]);

    /// <summary>Scope block naming an unknown layer.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope block naming an unknown layer")]
    public void ScopeBlockUnknownLayer() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Layer:** records", "**Layer:** melted-core")),
        ["SCOPE_VALUE"]);

    /// <summary>Applies to line naming an unknown artifact.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming an unknown artifact")]
    public void AppliesToUnknownArtifact() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** artifact: receipt\n\n**Statement:**")),
        ["SCOPE_VALUE"]);

    /// <summary>Applies to line naming a record artifact.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming a record artifact")]
    public void AppliesToRecordArtifact() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** artifact: record:use-case\n\n**Statement:**"), "**Strength:** default\n\n**Default:**", "**Strength:** default\n\n**Applies to:** artifact: record:use-case\n\n**Default:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line as wide as its page.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line as wide as its page")]
    public void AppliesToRepeatingPageScope() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** layer: records\n\n**Statement:**")),
        ["SCOPE_NARROW"]);

    /// <summary>Applies to line widening its page.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line widening its page")]
    public void AppliesToWideningPageScope() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** layer: domain\n\n**Statement:**")),
        ["SCOPE_NARROW"]);

    /// <summary>Applies to line narrowing to an artifact.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line narrowing to an artifact")]
    public void AppliesToArtifactNarrows() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** artifact: aggregate\n\n**Statement:**"), "**Strength:** default\n\n**Default:**", "**Strength:** default\n\n**Applies to:** artifact: aggregate\n\n**Default:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>A provision with no artifact on a page where another provision names one.</summary>
    /// <proves>litespec/core/scopes#name-the-artifact-of-every-provision-on-a-page-that-names-one</proves>
    [Fact(DisplayName = "provision with no artifact beside one that names an artifact")]
    public void ArtifactMissingBesideANamedArtifact() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** artifact: aggregate\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        ["SCOPE_ARTIFACT"]);

    /// <summary>Applies to line narrowing a page stack of any.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line narrowing a page stack of any")]
    public void AppliesToStackNarrowsAny() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** stack: dotnet\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line repeating a page stack of any.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line repeating a page stack of any")]
    public void AppliesToStackRepeatingAny() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** stack: any\n\n**Statement:**")),
        ["SCOPE_NARROW"]);

    /// <summary>Applies to line narrowing a layer subset.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line narrowing a layer subset")]
    public void AppliesToLayerSubsetNarrows() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Layer:** records", "**Layer:** domain, application");
            page = StandardsFixture.ReplaceFirst(page, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** layer: domain\n\n**Statement:**");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming a conformance level that differs from its page.</summary>
    /// <proves>litespec/core/scopes#give-every-provision-a-conformance-level</proves>
    [Fact(DisplayName = "applies to line naming a conformance level that differs from its page")]
    public void AppliesToConformanceOverride() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** conformance: traced\n\n**Statement:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming a conformance level beside an artifact.</summary>
    /// <proves>litespec/core/scopes#give-every-provision-a-conformance-level</proves>
    [Fact(DisplayName = "applies to line naming a conformance level beside an artifact")]
    public void AppliesToArtifactAndConformance() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** artifact: aggregate; conformance: traced\n\n**Statement:**"), "**Strength:** default\n\n**Default:**", "**Strength:** default\n\n**Applies to:** artifact: aggregate\n\n**Default:**"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming an unknown conformance level.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming an unknown conformance level")]
    public void AppliesToUnknownConformance() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** conformance: partial\n\n**Statement:**")),
        ["SCOPE_VALUE"]);

    /// <summary>Applies to line naming two conformance levels.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming two conformance levels")]
    public void AppliesToTwoConformances() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** conformance: traced, full\n\n**Statement:**")),
        ["SCOPE_VALUE"]);

    /// <summary>Applies to line stating its page conformance level.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line stating its page conformance level")]
    public void AppliesToRepeatingPageConformance() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** conformance: records\n\n**Statement:**")),
        ["SCOPE_NARROW"]);

    /// <summary>Applies to line stating its page conformance level beside an artifact.</summary>
    /// <proves>litespec/core/scopes#state-applies-to-only-when-narrower</proves>
    [Fact(DisplayName = "applies to line stating its page conformance level beside an artifact")]
    public void AppliesToArtifactRepeatingPageConformance() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** artifact: aggregate; conformance: records\n\n**Statement:**")),
        ["SCOPE_NARROW"]);

    /// <summary>Scope vocabulary page naming the conformance levels.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "scope vocabulary page naming the conformance levels")]
    public void ScopesVocabularyNamesConformances()
    {
        var vocabulary = Scopes.Load(Cli.Repository);
        Assert.Equal(["full", "records", "traced"], vocabulary.Conformances.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.Contains("traced", Scopes.ConformanceValues);
    }

    /// <summary>Provision without a Strength line.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision without a Strength line")]
    public void MissingStrengthLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n", "")),
        ["STRENGTH_LINE"]);

    /// <summary>Strength line naming an outside value.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "strength line naming an outside value")]
    public void StrengthLineUnknownValue() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule", "**Strength:** Law")),
        ["STRENGTH_LINE"]);

    /// <summary>Backticked scope values read as their inner text.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "backticked scope values read as their inner text")]
    public void BacktickedScopeValues() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any\n**Layer:** records", "**Stack:** `any`\n**Layer:** `records`");
            page = StandardsFixture.ReplaceFirst(page, "**Strength:** rule", "**Strength:** `rule`");
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
        ["GENERATED_STALE"]);

    /// <summary>Generated counts region naming an unknown source.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region naming an unknown source")]
    public void GeneratedCountsUnknownSource() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:spells -->\nThis tree states 5 provisions.\n<!-- /generated:counts -->\n"),
        ["GENERATED_STALE"]);

    /// <summary>Generated counts region stating no count.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region stating no count")]
    public void GeneratedCountsMissingNumber() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:provisions -->\nThis tree states many provisions.\n<!-- /generated:counts -->\n"),
        ["GENERATED_STALE"]);

    /// <summary>Generated counts region for analyzer rule pages.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "generated counts region for analyzer rule pages")]
    public void GeneratedCountsAnalyzerRules() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/analyzers/rules/ls1001.md", StandardsFixture.ReferencePage);
            StandardsFixture.Write(root, "docs/analyzers/rules/ls1002.md", StandardsFixture.ReferencePage);
            StandardsFixture.Write(root, "docs/README.md", BaseReadme(root) + "\n<!-- generated:counts:analyzer-rules -->\nThis tree ships 2 analyzer rules.\n<!-- /generated:counts -->\n");
        },
        []);

    /// <summary>Missing provision data file.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "missing provision data file")]
    public void MissingProvisionData() => Check(
        root => StandardsFixture.Delete(root, Provisions.JsonPath),
        ["PROVISIONS_STALE"]);

    /// <summary>Stale provision data file.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "stale provision data file")]
    public void StaleProvisionData() => Check(
        root => StandardsFixture.Write(root, Provisions.JsonPath, "{ \"provisions\": [] }\n"),
        ["PROVISIONS_STALE"]);

    /// <summary>Scope block naming several values per field.</summary>
    /// <proves>litespec/core/scopes#declare-the-page-scope</proves>
    [Fact(DisplayName = "scope block naming several values per field")]
    public void MultiValueScopeBlock() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Layer:** records", "**Layer:** records, tests"));
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
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Stack:** any", "**Stack:** cobol"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Applies to line naming no field.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming no field")]
    public void EmptyAppliesToLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:**\n\n**Statement:**")),
        ["SCOPE_VALUE"]);

    /// <summary>Applies to line naming an unknown field.</summary>
    /// <proves>litespec/core/scopes#use-only-declared-scope-values</proves>
    [Fact(DisplayName = "applies to line naming an unknown field")]
    public void AppliesToUnknownField() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** rule\n\n**Statement:**", "**Strength:** rule\n\n**Applies to:** region: core\n\n**Statement:**")),
        ["SCOPE_VALUE"]);

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
        Assert.True(Scopes.IsLayerWide("any"));
        Assert.Equal(["command-handler", "query-handler"], Scopes.ArtifactValues("command-handler, query-handler"));
    }

    static string BaseReadme(string root) =>
        File.ReadAllText(Path.Combine(root, "docs", "README.md")).Replace("\r\n", "\n");
}
