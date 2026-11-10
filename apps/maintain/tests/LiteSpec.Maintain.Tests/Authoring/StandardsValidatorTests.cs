using LiteSpec.Authoring;
using LiteSpec.Tests.Support;
using System.Linq;

namespace LiteSpec.Tests.Authoring;

/// <summary>
///     Standards validator cases: one test per validator rule, in both directions,
///     asserting the expected diagnostic codes. This part holds the runner,
///     the coverage gate, the usage case, and the page-class cases.
/// </summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>
    ///     Runs one case with a mutation and its expected and forbidden codes:
    ///     every expected code is present, every forbidden code is absent, and a
    ///     passing case reports no diagnostics.
    /// </summary>
    /// <param name="mutate">The change to apply to the fixture repository, or null for a passing case.</param>
    /// <param name="expected">The diagnostic codes the run must report.</param>
    /// <param name="forbidden">The diagnostic codes the run must not report.</param>
    static void Check(Action<string>? mutate, string[] expected, string[] forbidden)
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        mutate?.Invoke(temp.Path);
        var result = StandardsValidator.Validate(temp.Path);
        var codes = result.Diagnostics.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var code in expected)
        {
            Assert.Contains(code, codes);
        }

        foreach (var code in forbidden)
        {
            Assert.DoesNotContain(code, codes);
        }

        if (expected.Length == 0)
        {
            Assert.Empty(result.Diagnostics);
        }
    }

    static void Check(Action<string>? mutate, string[] expected) => Check(mutate, expected, []);

    /// <summary>All page classes pass.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "all page classes pass")]
    public void AllPageClassesPass() => Check(null, []);

    /// <summary>Pattern page with a broken wireframe.</summary>
    /// <proves>litespec/frontend/patterns/record-detail#show-one-primary-action-in-a-record-header</proves>
    [Fact(DisplayName = "pattern page with a broken wireframe")]
    public void PatternPageWithBrokenWireframe() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/patterns/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.PatternPage, "action \"Refund\"", "button \"Refund\"")),
        [Diagnostics.Codes.PatternInvalid]);

    /// <summary>Pattern page whose heading names the pattern rather than an action.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "pattern page whose heading names the pattern rather than an action")]
    public void PatternHeadingNamesThePattern() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/patterns/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.PatternPage, "## Patterns\n\n### Record Header", "## Standards\n\n### Header layout")),
        [Diagnostics.Codes.HeadingAction]);

    /// <summary>Tutorial that omits what the reader built.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "tutorial that omits what the reader built")]
    public void TutorialOmitsWhatReaderBuilt() => Check(
        root => StandardsFixture.Write(root, "docs/tutorial/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.TutorialPage, "## What You Built", "## Verification")),
        [Diagnostics.Codes.PageMissingSection, Diagnostics.Codes.PageUnknownSection]);

    /// <summary>How-to that ends in verification.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "how-to that ends in verification")]
    public void HowToEndingInVerification() => Check(
        root => StandardsFixture.Write(root, "docs/guide/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.GuidePage, "## Check", "## Verification")),
        [Diagnostics.Codes.PageMissingSection, Diagnostics.Codes.PageUnknownSection]);

    /// <summary>Analyzer rule page and analyzer procedure pass.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "analyzer rule page and analyzer procedure pass")]
    public void AnalyzerPagesPass() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/analyzers/rules/sample.md", StandardsFixture.ReferencePage);
            StandardsFixture.Write(root, "docs/analyzers/sample.md", StandardsFixture.GuidePage);
        },
        []);

    /// <summary>Analyzer rule page written as a procedure.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "analyzer rule page written as a procedure")]
    public void AnalyzerRuleAsProcedure() => Check(
        root => StandardsFixture.Write(root, "docs/analyzers/rules/sample.md", StandardsFixture.GuidePage),
        [Diagnostics.Codes.PageMissingSection]);

    /// <summary>Example page without an Intent.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "example page without an Intent")]
    public void ExamplePageWithoutIntent() => Check(
        root => StandardsFixture.Write(root, "docs/example/sample.md", "# Sample Example\n\n## The product\n\nA library lends copies.\n"),
        [Diagnostics.Codes.PageMissingSection]);

    /// <summary>Reference page carrying an unknown section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "reference page carrying an unknown section")]
    public void ReferenceWithUnknownSection() => Check(
        root => StandardsFixture.Write(root, "docs/reference/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReferencePage, "## Notes", "## Procedure")),
        [Diagnostics.Codes.PageUnknownSection]);

    /// <summary>Reference page out of order.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "reference page out of order")]
    public void ReferenceOutOfOrder() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.ReferencePage, "## Intent\n\nState the exact values that this repository publishes.\n\n", "");
            page = StandardsFixture.ReplaceFirst(page, "The values above come from the repository manifest.", "State the exact values that this repository publishes.\n\n## Intent\n\nThe values above come from the repository manifest.");
            StandardsFixture.Write(root, "docs/reference/sample.md", page);
        },
        [Diagnostics.Codes.PageSectionOrder]);

    /// <summary>Command page hiding its mechanism.</summary>
    /// <proves>litespec/core/authoring#name-the-escape-from-every-abstraction</proves>
    [Fact(DisplayName = "command page hiding its mechanism")]
    public void CommandHidingMechanism() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "## Underneath\n\nNone.\n", "")),
        [Diagnostics.Codes.PageMissingSection]);

    /// <summary>Command page without exit codes.</summary>
    /// <proves>litespec/core/authoring#name-the-escape-from-every-abstraction</proves>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "command page without exit codes")]
    public void CommandWithoutExitCodes() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "## Exit Codes", "## Results")),
        [Diagnostics.Codes.PageMissingSection, Diagnostics.Codes.PageUnknownSection]);

    /// <summary>Frontend standard page with per-stack sections rejected.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "frontend standard page with per-stack sections rejected")]
    public void FrontendWithPerStackSections() => Check(
        root =>
        {
            var page = StandardsFixture.TopicPage.Replace("core/topic", "frontend/web/sample");
            page = StandardsFixture.ReplaceFirst(page, "## Standards", "## React\n\n- React names the binding the rule below forbids.\n\n## Svelte\n\n- Svelte names the binding the rule below forbids.\n\n## Standards");
            StandardsFixture.Write(root, "docs/frontend/web/sample.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        [Diagnostics.Codes.PageUnknownSection, Diagnostics.Codes.PageSectionOrder]);

    /// <summary>Per-stack section outside frontend pages.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "per-stack section outside frontend pages")]
    public void PerStackOutsideFrontend() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## React\n\n- React names the binding the rule below forbids.\n\n## Standards")),
        [Diagnostics.Codes.PageUnknownSection]);

    /// <summary>Stack page carrying a composition section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "stack page carrying a composition section")]
    public void StackWithCompositionSection() => Check(
        root => StandardsFixture.Write(root, "docs/backend/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions", "## Composition\n\n- [Topic standard](../../core/topic.md)\n\n## Conventions")),
        [Diagnostics.Codes.PageUnknownSection]);

    /// <summary>Per-stack section after standards.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "per-stack section after standards")]
    public void PerStackAfterStandards() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/web/sample.md", StandardsFixture.TopicPage.Replace("core/topic", "frontend/web/sample") + "\n## React\n\n- React names the binding the rule above forbids.\n"),
        [Diagnostics.Codes.PageUnknownSection, Diagnostics.Codes.PageSectionOrder]);

    /// <summary>Stack page with a concepts section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "stack page with a concepts section")]
    public void StackWithConceptsSection() => Check(
        root => StandardsFixture.Write(root, "docs/backend/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Standards", "## Concepts\n\nThis stack defines one composition term.\n\n## Standards")),
        []);

    static void CheckActionHeading(string verb)
    {
        var lower = verb.ToLowerInvariant();
        Check(
            root =>
            {
                var page = StandardsFixture.TopicPage.Replace("Keep the Topic Bounded", $"{verb} the Topic Boundary");
                page = page.Replace("keep-the-topic-bounded", $"{lower}-the-topic-boundary");
                page = StandardsFixture.ReplaceFirst(page, "MUST keep the topic", $"MUST {lower} the topic");
                StandardsFixture.Write(root, "docs/core/topic.md", page);
                StandardsFixture.RebuildIndex(root);
            },
            []);
    }

    /// <summary>Action heading starting with 'answer'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'answer'")]
    public void ActionHeadingAnswer() => CheckActionHeading("Answer");

    /// <summary>Action heading starting with 'ask'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'ask'")]
    public void ActionHeadingAsk() => CheckActionHeading("Ask");

    /// <summary>Action heading starting with 'compute'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'compute'")]
    public void ActionHeadingCompute() => CheckActionHeading("Compute");

    /// <summary>Action heading starting with 'confirm'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'confirm'")]
    public void ActionHeadingConfirm() => CheckActionHeading("Confirm");

    /// <summary>Action heading starting with 'grant'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'grant'")]
    public void ActionHeadingGrant() => CheckActionHeading("Grant");

    /// <summary>Action heading starting with 'reach'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'reach'")]
    public void ActionHeadingReach() => CheckActionHeading("Reach");

    /// <summary>Action heading starting with 'refuse'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'refuse'")]
    public void ActionHeadingRefuse() => CheckActionHeading("Refuse");

    /// <summary>Action heading starting with 'show'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'show'")]
    public void ActionHeadingShow() => CheckActionHeading("Show");

    /// <summary>Action heading starting with 'author'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'author'")]
    public void ActionHeadingAuthor() => CheckActionHeading("Author");

    /// <summary>Action heading starting with 'claim'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'claim'")]
    public void ActionHeadingClaim() => CheckActionHeading("Claim");

    /// <summary>Action heading starting with 'end'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'end'")]
    public void ActionHeadingEnd() => CheckActionHeading("End");

    /// <summary>Action heading starting with 'explain'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'explain'")]
    public void ActionHeadingExplain() => CheckActionHeading("Explain");

    /// <summary>Action heading starting with 'illustrate'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'illustrate'")]
    public void ActionHeadingIllustrate() => CheckActionHeading("Illustrate");

    /// <summary>Action heading starting with 'shape'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'shape'")]
    public void ActionHeadingShape() => CheckActionHeading("Shape");

    /// <summary>Action heading starting with 'tabulate'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'tabulate'")]
    public void ActionHeadingTabulate() => CheckActionHeading("Tabulate");

    /// <summary>Action heading starting with 'trigger'.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "action heading starting with 'trigger'")]
    public void ActionHeadingTrigger() => CheckActionHeading("Trigger");

    /// <summary>Index without a routing block.</summary>
    /// <proves>litespec/core/authoring#route-the-reader-before-listing-pages</proves>
    [Fact(DisplayName = "index without a routing block")]
    public void IndexWithoutRouting() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nUse this page to find repository documentation.\n"),
        [Diagnostics.Codes.IndexMissingRouting]);

    /// <summary>Routing block after the first navigation group.</summary>
    /// <proves>litespec/core/authoring#route-the-reader-before-listing-pages</proves>
    [Fact(DisplayName = "routing block after the first navigation group")]
    public void RoutingAfterNavigation() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nUse this page to find repository documentation.\n\n## Core\n\n- [Topic](core/topic.md)\n\n" + StandardsFixture.IndexRouting),
        [Diagnostics.Codes.IndexMissingRouting]);

    /// <summary>Stale provision index.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "stale provision index")]
    public void StaleProvisionIndex() => Check(
        root => StandardsFixture.Write(root, Provisions.IndexPath, "# Provision Index\n\n## Intent\n\nThis page is out of date.\n"),
        [Diagnostics.Codes.ProvisionsStale]);

    /// <summary>Missing provision index.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "missing provision index")]
    public void MissingProvisionIndex() => Check(
        root => StandardsFixture.Delete(root, Provisions.IndexPath),
        [Diagnostics.Codes.ProvisionsStale]);

    /// <summary>Declared prose exclusions pass.</summary>
    /// <proves>litespec/core/authoring#keep-authored-prose-ascii-safe</proves>
    [Fact(DisplayName = "declared prose exclusions pass")]
    public void DeclaredProseExclusionsPass() => Check(
        root => StandardsFixture.Write(root, "templates/consumer/exclusions.md", "# Exclusion Fixture\n\n```json\n{\"metadata\":\"This obvious and/or non-ASCII value - \u2014 - is excluded because metadata and fenced code are not prose.\"}\n```\n\nRead the [source](https://example.com/an-obvious/and/or/very/long/link/destination).\n\nThe identifier `one.extremely-long.dotted-path-like-token/with-many-segments` counts as one word.\n"),
        []);

    /// <summary>Validate with two positionals is a usage error.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "validate with two positionals is a usage error")]
    public void ValidateUsageExits2()
    {
        var result = Cli.Run("validate", "one", "two");
        Assert.Equal(2, result.Status);
    }

    /// <summary>Every emitted diagnostic code is declared in Diagnostics.Codes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "every emitted diagnostic code is declared in Diagnostics.Codes")]
    public void EmittedCodesAreDeclared()
    {
        var declared = typeof(Diagnostics.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(declared.Count, StandardsValidator.StableDiagnosticCodes.Count);
        foreach (var code in StandardsValidator.StableDiagnosticCodes)
        {
            Assert.Contains(code, declared);
        }

        foreach (var code in declared)
        {
            Assert.Contains(code, StandardsValidator.StableDiagnosticCodes);
        }
    }

    /// <summary>Every stable diagnostic code has a failing case.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "every stable diagnostic code has a failing case")]
    public void StableCodesAreCovered()
    {
        var covered = new HashSet<string>(StringComparer.Ordinal)
        {
            Diagnostics.Codes.AnchorBroken,
            Diagnostics.Codes.CheckedByUnknown,
            Diagnostics.Codes.ConventionDefaultSentence,
            Diagnostics.Codes.ConventionMissingDefault,
            Diagnostics.Codes.ConventionMissingReplacement,
            Diagnostics.Codes.ConventionReplacementDeviation,
            Diagnostics.Codes.ConventionReplacementSentence,
            Diagnostics.Codes.ExtensionActivation,
            Diagnostics.Codes.ExtensionBaseline,
            Diagnostics.Codes.ExtensionDependencies,
            Diagnostics.Codes.ExtensionKind,
            Diagnostics.Codes.ExtensionReplacesEnforced,
            Diagnostics.Codes.ExtensionReplacesUnknown,
            Diagnostics.Codes.ExtensionLocation,
            Diagnostics.Codes.ExtensionScope,
            Diagnostics.Codes.ExtensionUnlisted,
            Diagnostics.Codes.FieldNameShape,
            Diagnostics.Codes.LayerUnreachable,
            Diagnostics.Codes.ShapePage,
            Diagnostics.Codes.StackFolder,
            Diagnostics.Codes.StackReference,
            Diagnostics.Codes.GlossaryDefinition,
            Diagnostics.Codes.GlossaryOrder,
            Diagnostics.Codes.HeadingAction,
            Diagnostics.Codes.HeadingEmptyBody,
            Diagnostics.Codes.IdDuplicate,
            Diagnostics.Codes.IdInHeading,
            Diagnostics.Codes.IdLineMissing,
            Diagnostics.Codes.IdLineMismatch,
            Diagnostics.Codes.IdPageFilename,
            Diagnostics.Codes.IdUnknownReference,
            Diagnostics.Codes.IndexContainsProcedure,
            Diagnostics.Codes.IndexIntent,
            Diagnostics.Codes.IndexMissingRouting,
            Diagnostics.Codes.LayerUnlisted,
            Diagnostics.Codes.LinkBroken,
            Diagnostics.Codes.ManifestAnalyzerPin,
            Diagnostics.Codes.ManifestAnalyzerRule,
            Diagnostics.Codes.ManifestAnchor,
            Diagnostics.Codes.ManifestDuplicatePath,
            Diagnostics.Codes.ManifestJson,
            Diagnostics.Codes.ManifestPath,
            Diagnostics.Codes.MarkMissing,
            Diagnostics.Codes.MarkUnresolved,
            Diagnostics.Codes.PageEmptySection,
            Diagnostics.Codes.PageEmptySectionText,
            Diagnostics.Codes.PageHeadingCase,
            Diagnostics.Codes.PageMissingSection,
            Diagnostics.Codes.PageSectionOrder,
            Diagnostics.Codes.PageTitleCase,
            Diagnostics.Codes.PageTitleCount,
            Diagnostics.Codes.PageUnknownSection,
            Diagnostics.Codes.PatternInvalid,
            Diagnostics.Codes.ProseNonAscii,
            Diagnostics.Codes.ProseNormativeLocation,
            Diagnostics.Codes.ProvisionRestatesHeading,
            Diagnostics.Codes.ProvisionsStale,
            Diagnostics.Codes.RecordDecisionUncited,
            Diagnostics.Codes.RecordExplanationMissing,
            Diagnostics.Codes.RecordExplanationOrphaned,
            Diagnostics.Codes.RecordIdMismatch,
            Diagnostics.Codes.RecordSchema,
            Diagnostics.Codes.RecordSource,
            Diagnostics.Codes.RecordUnknownReference,
            Diagnostics.Codes.ReferenceExampleDeclaration,
            Diagnostics.Codes.SchemaId,
            Diagnostics.Codes.SchemaInvalid,
            Diagnostics.Codes.SchemaJson,
            Diagnostics.Codes.SchemaUnsupportedKeyword,
            Diagnostics.Codes.StandardDeviationNormative,
            Diagnostics.Codes.StandardDeviationSentence,
            Diagnostics.Codes.StandardExampleLabel,
            Diagnostics.Codes.StandardInformativeNormative,
            Diagnostics.Codes.StandardLabelDuplicate,
            Diagnostics.Codes.StandardLabelInvalid,
            Diagnostics.Codes.StandardLabelOrder,
            Diagnostics.Codes.StandardMissingDeviation,
            Diagnostics.Codes.StandardMissingRequirement,
            Diagnostics.Codes.StandardModalCount,
            Diagnostics.Codes.StandardModalVocabulary,
            Diagnostics.Codes.StandardRequirementCount,
            Diagnostics.Codes.StandardSentenceCount,
            Diagnostics.Codes.StandardUnexpectedDeviation,
            Diagnostics.Codes.StandardUnlabeledContent,
            Diagnostics.Codes.TemplateIndexPath,
            Diagnostics.Codes.TemplateSchema,
            Diagnostics.Codes.VerifyCommand,
            Diagnostics.Codes.VerifyDuplicateEvidence,
            Diagnostics.Codes.VerifyDuplicateId,
            Diagnostics.Codes.VerifyGenericEvidence,
            Diagnostics.Codes.VerifyHeader,
            Diagnostics.Codes.VerifyMissingId,
            Diagnostics.Codes.VerifyOrder,
            Diagnostics.Codes.VerifyTemplatedEvidence,
            Diagnostics.Codes.ValueStyle,
            Diagnostics.Codes.GeneratedStale,
            Diagnostics.Codes.ScopeArtifact,
            Diagnostics.Codes.ScopeBlock,
            Diagnostics.Codes.ScopeNarrow,
            Diagnostics.Codes.ScopeValue,
            Diagnostics.Codes.StrengthLine,
        };

        foreach (var code in StandardsValidator.StableDiagnosticCodes)
        {
            Assert.Contains(code, covered);
        }
    }

    /// <summary>Diagnostics sort ordinally.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "diagnostics sort ordinally")]
    public void DiagnosticsSortOrdinally()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/reference/B.md", StandardsFixture.ReferencePage);
        StandardsFixture.Write(temp.Path, "docs/reference/aB.md", StandardsFixture.ReferencePage);

        var result = StandardsValidator.Validate(temp.Path);
        var relatives = result.Diagnostics
            .Where(diagnostic => diagnostic.Code == Diagnostics.Codes.IdPageFilename)
            .Select(diagnostic => diagnostic.Relative)
            .ToList();

        Assert.Equal(["docs/reference/B.md", "docs/reference/aB.md"], relatives);
    }
}
