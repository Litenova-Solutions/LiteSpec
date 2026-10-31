namespace LiteSpec.Tests.Authoring;

/// <summary>Inline verification cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "duplicate verification line")]
    public void DuplicateVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.")),
        ["VERIFY_DUPLICATE_ID"]);

    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "leftover verification section and table")]
    public void LeftoverVerificationSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.TopicPage + "\n## Verification\n\n| ID | Mark | Evidence |\n|:---|:---|:---|\n| litespec/core/topic#keep-the-topic-bounded | checkable | `litespec-maintain validate` confirms the topic stays inside its declared boundary. |\n"),
        ["PAGE_UNKNOWN_SECTION", "VERIFY_HEADER"]);

    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "missing verification line")]
    public void MissingVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n", "")),
        ["VERIFY_MISSING_ID"]);

    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "verification line before the statement")]
    public void VerificationLineBeforeStatement() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n## Conventions", "## Conventions");
            page = StandardsFixture.ReplaceFirst(page, "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n", "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        ["VERIFY_ORDER"]);

    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "content after the verification line")]
    public void ContentAfterVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\nAdditional constraint.")),
        ["VERIFY_ORDER"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming a missing file")]
    public void CheckableEvidenceMissingFile() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`tools/validate-missing.mjs` confirms the topic stays inside its declared boundary.")),
        ["CHECKED_BY_UNKNOWN"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming no command")]
    public void CheckableEvidenceNoCommand() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "A reviewer confirms the topic stays inside its declared boundary.")),
        ["VERIFY_COMMAND"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming a removed command")]
    public void CheckableEvidenceRemovedCommand() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec patterns check` confirms the topic stays inside its declared boundary.")),
        ["CHECKED_BY_UNKNOWN"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming an unknown finding code")]
    public void CheckableEvidenceUnknownFindingCode() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `topic-cites-missing` for a topic naming no reason.")),
        ["CHECKED_BY_UNKNOWN"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming an unknown maintainer code")]
    public void CheckableEvidenceUnknownMaintainerCode() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec-maintain validate` emits no `TOPIC_UNKNOWN_CODE` diagnostic.")),
        ["CHECKED_BY_UNKNOWN"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming an unshipped analyzer rule")]
    public void CheckableEvidenceUnshippedAnalyzerRule() => Check(
        root =>
        {
            AnalyzerPinFiles(root, "3.0.0");
            StandardsFixture.Write(root, "apps/analyzers/src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md", AnalyzerShippedFile);
            CiteAnalyzerRule(root, "LS9999");
        },
        ["CHECKED_BY_UNKNOWN"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming codes that exist")]
    public void CheckableEvidenceKnownCodes() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `record-invalid` for a record outside its home."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming a consumer finding code")]
    public void CheckableEvidenceConsumerFindingCode() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec consumer` reports no `GLOSSARY_REJECTED_SYNONYM` finding."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming the consumer-side tool path")]
    public void CheckableEvidenceConsumerToolPath() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec consumer` confirms the topic stays inside its declared boundary."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "checkable evidence naming a consumer-side suite path")]
    public void CheckableEvidenceConsumerSuitePath() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`tests/routes/every-route.spec.ts` proves the topic boundary in a browser."));
            StandardsFixture.Write(root, "tests/routes/every-route.spec.ts", "// Fixture stub so the consumer suite path resolves.\n");
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <proves>litespec/core/authoring#mark-every-provision-checkable-or-guidance</proves>
    [Fact(DisplayName = "guidance evidence names no command")]
    public void GuidanceEvidenceNoCommand() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Reviewed for:** Does the topic stay inside its declared boundary?"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "generic verification evidence")]
    public void GenericVerificationEvidence() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "confirms the topic stays inside its declared boundary.", "runs, then inspect evidence for the topic boundary.")),
        ["VERIFY_GENERIC_EVIDENCE"]);

    /// <proves>litespec/core/authoring#restrict-normative-language-to-standards-and-conventions</proves>
    [Fact(DisplayName = "normative verification evidence")]
    public void NormativeVerificationEvidence() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Does the topic live in its owning documentation area?", "Reviewers MUST record where the topic lives.")),
        ["STANDARD_INFORMATIVE_NORMATIVE"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "provision restates its heading")]
    public void ProvisionRestatesHeading() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Statement:** Consumers MUST keep the topic inside its declared boundary.", "**Statement:** Consumers MUST keep the topic bounded.")),
        ["PROVISION_RESTATES_HEADING"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "evidence repeats its provision heading")]
    public void EvidenceRepeatsHeading() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "Pull request review asserts `keep the topic bounded` in the owning specification.")),
        ["VERIFY_TEMPLATED_EVIDENCE"]);

    /// <proves>litespec/core/authoring#name-observable-evidence-in-verification-lines</proves>
    [Fact(DisplayName = "verification lines sharing evidence")]
    public void VerificationLinesSharingEvidence() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Does the topic live in its owning documentation area?", "`litespec-maintain validate` confirms the topic stays inside its declared boundary.")),
        ["VERIFY_DUPLICATE_EVIDENCE"]);

    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "pattern provision without a verification line")]
    public void PatternWithoutVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/patterns/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.PatternPage, "**Reviewed for:** Does the top of each record page match the default option or the one its catalog records?\n\n", "")),
        ["VERIFY_MISSING_ID"]);
}
