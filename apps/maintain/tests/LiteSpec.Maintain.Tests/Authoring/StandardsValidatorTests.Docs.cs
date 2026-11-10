using LiteSpec.Authoring;
using LiteSpec.Maintain.Tests.Support;

namespace LiteSpec.Maintain.Tests.Authoring;

/// <summary>Cases that bind the documentation to the tools, the templates, the example, the index and the pattern budget.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>A page cites a finding code the table lacks.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a page cites a finding code the table lacks")]
    public void CitedFindingCodeUnknown() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "The model reports `made-up-missing` for an unknown record.\n\n## Standards")),
        [Diagnostics.Codes.FindingCodeUnknown]);

    /// <summary>A page cites a finding code the table holds.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a page cites a finding code the table holds")]
    public void CitedFindingCodeKnown() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "The model reports `record-invalid` for an unknown record.\n\n## Standards")),
        []);

    /// <summary>A research record names a finding code that was weighed and never adopted.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a research record may name a finding code the table lacks")]
    public void ResearchRecordMayNameUnadoptedCode()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/intent/research/sample-evidence.md", "# Sample Evidence\n\nThe study weighed `made-up-missing` and kept none.\n");
        var result = StandardsValidator.Validate(temp.Path);
        Assert.DoesNotContain(result.Diagnostics, item => item.Relative == "docs/intent/research/sample-evidence.md" && item.Code == Diagnostics.Codes.FindingCodeUnknown);
    }

    /// <summary>A Checked by line for the model check names no finding code.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a Checked by line for the model check names no finding code")]
    public void CheckedByModelCheckWithoutCode() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` confirms the topic stays inside its declared boundary.")),
        [Diagnostics.Codes.CheckedByFindingMissing]);

    /// <summary>A Checked by line for the model check names a finding code.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a Checked by line for the model check names a finding code")]
    public void CheckedByModelCheckWithCode() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `record-invalid` for a topic outside its boundary."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>A page cites a template the templates folder lacks.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a page cites a template the templates folder lacks")]
    public void CitedTemplateUnknown() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "Start from `templates/consumer/missing.yaml`.\n\n## Standards")),
        [Diagnostics.Codes.TemplateCitedUnknown]);

    /// <summary>An example page cites a record ID the example lacks.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "an example page cites a record ID the example lacks")]
    public void CitedExampleRecordUnknown() => Check(
        root => StandardsFixture.Write(root, "docs/example/sample.md", "# Sample Example\n\n## Intent\n\nThe example submits `use-case/spending.submit-expense`.\n"),
        [Diagnostics.Codes.ExampleRecordUnknown]);

    /// <summary>An example page cites a record ID the example holds, and a placeholder names no record.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "an example page cites a record ID the example holds")]
    public void CitedExampleRecordKnown() => Check(
        root =>
        {
            StandardsFixture.Write(root, "examples/claimwick/.litespec/use-case.yaml", "id: use-case/spending.submit-expense\n");
            StandardsFixture.Write(root, "docs/example/sample.md", "# Sample Example\n\n## Intent\n\nThe example submits `use-case/spending.submit-expense` and names `use-case/<module>.<name>`.\n");
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(extra: "- [Example](example/sample.md)\n"));
        },
        []);

    /// <summary>A page that no index page reaches.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a page that no index page reaches")]
    public void UnindexedPage() => Check(
        root => StandardsFixture.Write(root, "docs/guide/orphan.md", "# Orphan Guide\n\n## Purpose\n\nNo index links this page.\n"),
        [Diagnostics.Codes.IndexPageMissing]);

    /// <summary>A pattern that offers more than three options.</summary>
    /// <proves>litespec/frontend/patterns#keep-one-pattern-catalog</proves>
    [Fact(DisplayName = "a pattern that offers more than three options")]
    public void PatternOfFourOptions() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/patterns/sample.md", StandardsFixture.PatternPage + "\n#### `three`: Three Actions\n\n**Rationale:** A third choice.\n\n**Cost:** More labels.\n\n#### `four`: Four Actions\n\n**Rationale:** A fourth choice.\n\n**Cost:** More labels still.\n"),
        [Diagnostics.Codes.PatternOptionCount]);

    /// <summary>A manifest page without its pin table region is stale.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "a manifest page without its pin table region is stale")]
    public void ManifestPageWithoutPinRegion() => Check(
        root => StandardsFixture.Write(root, "docs/reference/manifest.md", "# Release Manifest\n\n## Intent\n\nThe manifest lists the pins.\n"),
        [Diagnostics.Codes.ProvisionsStale]);

    /// <summary>The provisions command writes the generated tables, and validate then finds them current.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "the provisions command writes the generated tables the validator then finds current")]
    public void GeneratedTablesCurrentAfterWrite()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/reference/manifest.md", "# Release Manifest\n\n## Intent\n\nThe manifest lists the pins.\n\n<!-- generated:manifest-pins -->\n<!-- /generated:manifest-pins -->\n");
        Assert.Equal(0, Cli.Run("provisions", "--root", temp.Path).Status);
        Assert.Equal(0, Cli.Run("provisions", "--check", "--root", temp.Path).Status);
        var codes = StandardsValidator.Validate(temp.Path).Diagnostics.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain(Diagnostics.Codes.ProvisionsStale, codes);
    }
}
