using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Tests.Validate;

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
        [MaintainerDiagnostics.Codes.PatternInvalid]);

    /// <summary>Pattern page whose heading names the pattern rather than an action.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "pattern page whose heading names the pattern rather than an action")]
    public void PatternHeadingNamesThePattern() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/patterns/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.PatternPage, "## Patterns\n\n### Record Header", "## Standards\n\n### Header layout")),
        [MaintainerDiagnostics.Codes.HeadingAction]);

    /// <summary>Tutorial that omits what the reader built.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "tutorial that omits what the reader built")]
    public void TutorialOmitsWhatReaderBuilt() => Check(
        root => StandardsFixture.Write(root, "docs/tutorial/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.TutorialPage, "## What You Built", "## Verification")),
        [MaintainerDiagnostics.Codes.PageMissingSection, MaintainerDiagnostics.Codes.PageUnknownSection]);

    /// <summary>How-to that ends in verification.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "how-to that ends in verification")]
    public void HowToEndingInVerification() => Check(
        root => StandardsFixture.Write(root, "docs/guide/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.GuidePage, "## Check", "## Verification")),
        [MaintainerDiagnostics.Codes.PageMissingSection, MaintainerDiagnostics.Codes.PageUnknownSection]);

    /// <summary>Analyzer rule page and analyzer procedure pass.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "analyzer rule page and analyzer procedure pass")]
    public void AnalyzerPagesPass() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/analyzers/rules/sample.md", StandardsFixture.ReferencePage);
            StandardsFixture.Write(root, "docs/analyzers/sample.md", StandardsFixture.GuidePage);
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(extra: "- [Rule](analyzers/rules/sample.md)\n- [Procedure](analyzers/sample.md)\n"));
        },
        []);

    /// <summary>Analyzer rule page written as a procedure.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "analyzer rule page written as a procedure")]
    public void AnalyzerRuleAsProcedure() => Check(
        root => StandardsFixture.Write(root, "docs/analyzers/rules/sample.md", StandardsFixture.GuidePage),
        [MaintainerDiagnostics.Codes.PageMissingSection]);

    /// <summary>Example page without an Intent.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "example page without an Intent")]
    public void ExamplePageWithoutIntent() => Check(
        root => StandardsFixture.Write(root, "docs/example/sample.md", "# Sample Example\n\n## The product\n\nA library lends copies.\n"),
        [MaintainerDiagnostics.Codes.PageMissingSection]);

    /// <summary>Reference page carrying an unknown section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "reference page carrying an unknown section")]
    public void ReferenceWithUnknownSection() => Check(
        root => StandardsFixture.Write(root, "docs/reference/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReferencePage, "## Notes", "## Procedure")),
        [MaintainerDiagnostics.Codes.PageUnknownSection]);

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
        [MaintainerDiagnostics.Codes.PageSectionOrder]);

    /// <summary>Command page hiding its mechanism.</summary>
    /// <proves>litespec/core/authoring#name-the-escape-from-every-abstraction</proves>
    [Fact(DisplayName = "command page hiding its mechanism")]
    public void CommandHidingMechanism() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "## Underneath\n\nNone.\n", "")),
        [MaintainerDiagnostics.Codes.PageMissingSection]);

    /// <summary>Command page without exit codes.</summary>
    /// <proves>litespec/core/authoring#name-the-escape-from-every-abstraction</proves>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "command page without exit codes")]
    public void CommandWithoutExitCodes() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "## Exit Codes", "## Results")),
        [MaintainerDiagnostics.Codes.PageMissingSection, MaintainerDiagnostics.Codes.PageUnknownSection]);

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
        [MaintainerDiagnostics.Codes.PageUnknownSection, MaintainerDiagnostics.Codes.PageSectionOrder]);

    /// <summary>Per-stack section outside frontend pages.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "per-stack section outside frontend pages")]
    public void PerStackOutsideFrontend() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## React\n\n- React names the binding the rule below forbids.\n\n## Standards")),
        [MaintainerDiagnostics.Codes.PageUnknownSection]);

    /// <summary>Stack page carrying a composition section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "stack page carrying a composition section")]
    public void StackWithCompositionSection() => Check(
        root => StandardsFixture.Write(root, "docs/backend/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions", "## Composition\n\n- [Topic standard](../../core/topic.md)\n\n## Conventions")),
        [MaintainerDiagnostics.Codes.PageUnknownSection]);

    /// <summary>Per-stack section after standards.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "per-stack section after standards")]
    public void PerStackAfterStandards() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/web/sample.md", StandardsFixture.TopicPage.Replace("core/topic", "frontend/web/sample") + "\n## React\n\n- React names the binding the rule above forbids.\n"),
        [MaintainerDiagnostics.Codes.PageUnknownSection, MaintainerDiagnostics.Codes.PageSectionOrder]);

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
        [MaintainerDiagnostics.Codes.IndexMissingRouting]);

    /// <summary>Routing block after the first navigation group.</summary>
    /// <proves>litespec/core/authoring#route-the-reader-before-listing-pages</proves>
    [Fact(DisplayName = "routing block after the first navigation group")]
    public void RoutingAfterNavigation() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nUse this page to find repository documentation.\n\n## Core\n\n- [Topic](core/topic.md)\n\n" + StandardsFixture.IndexRouting),
        [MaintainerDiagnostics.Codes.IndexMissingRouting]);

    /// <summary>Stale provision index.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "stale provision index")]
    public void StaleProvisionIndex() => Check(
        root => StandardsFixture.Write(root, Provisions.IndexPath, "# Provision Index\n\n## Intent\n\nThis page is out of date.\n"),
        [MaintainerDiagnostics.Codes.ProvisionsStale]);

    /// <summary>Missing provision index.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "missing provision index")]
    public void MissingProvisionIndex() => Check(
        root => StandardsFixture.Delete(root, Provisions.IndexPath),
        [MaintainerDiagnostics.Codes.ProvisionsStale]);

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

    /// <summary>Every emitted diagnostic code is declared in MaintainerDiagnostics.Codes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "every emitted diagnostic code is declared in MaintainerDiagnostics.Codes")]
    public void EmittedCodesAreDeclared()
    {
        var declared = typeof(MaintainerDiagnostics.Codes)
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
            MaintainerDiagnostics.Codes.AnchorBroken,
            MaintainerDiagnostics.Codes.CheckedByUnknown,
            MaintainerDiagnostics.Codes.ConventionDefaultSentence,
            MaintainerDiagnostics.Codes.ConventionMissingDefault,
            MaintainerDiagnostics.Codes.ConventionMissingReplacement,
            MaintainerDiagnostics.Codes.ConventionReplacementDeviation,
            MaintainerDiagnostics.Codes.ConventionReplacementSentence,
            MaintainerDiagnostics.Codes.ExtensionActivation,
            MaintainerDiagnostics.Codes.ExtensionBaseline,
            MaintainerDiagnostics.Codes.ExtensionDependencies,
            MaintainerDiagnostics.Codes.ExtensionKind,
            MaintainerDiagnostics.Codes.ExtensionReplacesEnforced,
            MaintainerDiagnostics.Codes.ExtensionReplacesUnknown,
            MaintainerDiagnostics.Codes.ExtensionLocation,
            MaintainerDiagnostics.Codes.ExtensionScope,
            MaintainerDiagnostics.Codes.ExtensionUnlisted,
            MaintainerDiagnostics.Codes.FieldNameShape,
            MaintainerDiagnostics.Codes.LayerUnreachable,
            MaintainerDiagnostics.Codes.ShapePage,
            MaintainerDiagnostics.Codes.StackFolder,
            MaintainerDiagnostics.Codes.StackReference,
            MaintainerDiagnostics.Codes.GlossaryDefinition,
            MaintainerDiagnostics.Codes.GlossaryOrder,
            MaintainerDiagnostics.Codes.HeadingAction,
            MaintainerDiagnostics.Codes.HeadingEmptyBody,
            MaintainerDiagnostics.Codes.IdDuplicate,
            MaintainerDiagnostics.Codes.IdInHeading,
            MaintainerDiagnostics.Codes.IdLineMissing,
            MaintainerDiagnostics.Codes.IdLineMismatch,
            MaintainerDiagnostics.Codes.IdPageFilename,
            MaintainerDiagnostics.Codes.IdUnknownReference,
            MaintainerDiagnostics.Codes.IndexContainsProcedure,
            MaintainerDiagnostics.Codes.IndexIntent,
            MaintainerDiagnostics.Codes.CheckedByFindingMissing,
            MaintainerDiagnostics.Codes.CommandOptionUnknown,
            MaintainerDiagnostics.Codes.ExampleRecordUnknown,
            MaintainerDiagnostics.Codes.FindingCodeUnknown,
            MaintainerDiagnostics.Codes.IndexMissingRouting,
            MaintainerDiagnostics.Codes.IndexPageMissing,
            MaintainerDiagnostics.Codes.PatternOptionCount,
            MaintainerDiagnostics.Codes.TemplateCitedUnknown,
            MaintainerDiagnostics.Codes.LayerUnlisted,
            MaintainerDiagnostics.Codes.LinkBroken,
            MaintainerDiagnostics.Codes.ManifestAnalyzerPin,
            MaintainerDiagnostics.Codes.ManifestAnalyzerRule,
            MaintainerDiagnostics.Codes.ManifestAnchor,
            MaintainerDiagnostics.Codes.ManifestDuplicatePath,
            MaintainerDiagnostics.Codes.ManifestPath,
            MaintainerDiagnostics.Codes.ManifestYaml,
            MaintainerDiagnostics.Codes.MarkMissing,
            MaintainerDiagnostics.Codes.MarkUnresolved,
            MaintainerDiagnostics.Codes.PageEmptySection,
            MaintainerDiagnostics.Codes.PageEmptySectionText,
            MaintainerDiagnostics.Codes.PageHeadingCase,
            MaintainerDiagnostics.Codes.PageMissingSection,
            MaintainerDiagnostics.Codes.PageSectionOrder,
            MaintainerDiagnostics.Codes.PageTitleCase,
            MaintainerDiagnostics.Codes.PageTitleCount,
            MaintainerDiagnostics.Codes.PageUnknownSection,
            MaintainerDiagnostics.Codes.PatternInvalid,
            MaintainerDiagnostics.Codes.ProseNonAscii,
            MaintainerDiagnostics.Codes.ProseNormativeLocation,
            MaintainerDiagnostics.Codes.ProvisionRestatesHeading,
            MaintainerDiagnostics.Codes.ProvisionsStale,
            MaintainerDiagnostics.Codes.RecordDecisionUncited,
            MaintainerDiagnostics.Codes.RecordExplanationMissing,
            MaintainerDiagnostics.Codes.RecordExplanationOrphaned,
            MaintainerDiagnostics.Codes.RecordIdMismatch,
            MaintainerDiagnostics.Codes.RecordSchema,
            MaintainerDiagnostics.Codes.RecordSource,
            MaintainerDiagnostics.Codes.RecordUnknownReference,
            MaintainerDiagnostics.Codes.ReferenceExampleDeclaration,
            MaintainerDiagnostics.Codes.SchemaId,
            MaintainerDiagnostics.Codes.SchemaInvalid,
            MaintainerDiagnostics.Codes.SchemaUnsupportedKeyword,
            MaintainerDiagnostics.Codes.SchemaYaml,
            MaintainerDiagnostics.Codes.StandardDeviationNormative,
            MaintainerDiagnostics.Codes.StandardDeviationSentence,
            MaintainerDiagnostics.Codes.StandardExampleLabel,
            MaintainerDiagnostics.Codes.StandardInformativeNormative,
            MaintainerDiagnostics.Codes.StandardLabelDuplicate,
            MaintainerDiagnostics.Codes.StandardLabelInvalid,
            MaintainerDiagnostics.Codes.StandardLabelOrder,
            MaintainerDiagnostics.Codes.StandardMissingDeviation,
            MaintainerDiagnostics.Codes.StandardMissingRequirement,
            MaintainerDiagnostics.Codes.StandardModalCount,
            MaintainerDiagnostics.Codes.StandardModalVocabulary,
            MaintainerDiagnostics.Codes.StandardRequirementCount,
            MaintainerDiagnostics.Codes.StandardSentenceCount,
            MaintainerDiagnostics.Codes.StandardUnexpectedDeviation,
            MaintainerDiagnostics.Codes.StandardUnlabeledContent,
            MaintainerDiagnostics.Codes.TemplateIndexPath,
            MaintainerDiagnostics.Codes.TemplateSchema,
            MaintainerDiagnostics.Codes.TemplateCitationUnknown,
            MaintainerDiagnostics.Codes.StandardStrengthModal,
            MaintainerDiagnostics.Codes.VerifyCommand,
            MaintainerDiagnostics.Codes.VerifyDuplicateEvidence,
            MaintainerDiagnostics.Codes.VerifyDuplicateId,
            MaintainerDiagnostics.Codes.VerifyGenericEvidence,
            MaintainerDiagnostics.Codes.VerifyHeader,
            MaintainerDiagnostics.Codes.VerifyMissingId,
            MaintainerDiagnostics.Codes.VerifyOrder,
            MaintainerDiagnostics.Codes.VerifyTemplatedEvidence,
            MaintainerDiagnostics.Codes.ValueStyle,
            MaintainerDiagnostics.Codes.GeneratedStale,
            MaintainerDiagnostics.Codes.ScopeArtifact,
            MaintainerDiagnostics.Codes.ScopeBlock,
            MaintainerDiagnostics.Codes.ScopeNarrow,
            MaintainerDiagnostics.Codes.ScopeValue,
            MaintainerDiagnostics.Codes.StrengthLine,
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
            .Where(diagnostic => diagnostic.Code == MaintainerDiagnostics.Codes.IdPageFilename)
            .Select(diagnostic => diagnostic.Relative)
            .ToList();

        Assert.Equal(["docs/reference/B.md", "docs/reference/aB.md"], relatives);
    }
}
