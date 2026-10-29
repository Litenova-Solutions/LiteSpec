namespace LiteSpec.Tests.Authoring;

/// <summary>Inline verification cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    [Fact(DisplayName = "duplicate verification line")]
    public void DuplicateVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.")),
        ["VERIFY_DUPLICATE_ID"]);

    [Fact(DisplayName = "leftover verification section and table")]
    public void LeftoverVerificationSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.TopicPage + "\n## Verification\n\n| ID | Mark | Evidence |\n|:---|:---|:---|\n| litespec/core/topic#keep-the-topic-bounded | checkable | `litespec-maintain validate` confirms the topic stays inside its declared boundary. |\n"),
        ["PAGE_UNKNOWN_SECTION", "VERIFY_HEADER"]);

    [Fact(DisplayName = "missing verification line")]
    public void MissingVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n", "")),
        ["VERIFY_MISSING_ID"]);

    [Fact(DisplayName = "verification line before the statement")]
    public void VerificationLineBeforeStatement() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n## Conventions", "## Conventions");
            page = StandardsFixture.ReplaceFirst(page, "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n", "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\n");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        ["VERIFY_ORDER"]);

    [Fact(DisplayName = "content after the verification line")]
    public void ContentAfterVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.\n\nAdditional constraint.")),
        ["VERIFY_ORDER"]);

    [Fact(DisplayName = "checkable evidence naming a missing file")]
    public void CheckableEvidenceMissingFile() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`tools/validate-missing.mjs` confirms the topic stays inside its declared boundary.")),
        ["VERIFY_COMMAND"]);

    [Fact(DisplayName = "checkable evidence naming no command")]
    public void CheckableEvidenceNoCommand() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "A reviewer confirms the topic stays inside its declared boundary.")),
        ["VERIFY_COMMAND"]);

    [Fact(DisplayName = "checkable evidence naming a removed command")]
    public void CheckableEvidenceRemovedCommand() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec patterns check` confirms the topic stays inside its declared boundary.")),
        ["VERIFY_COMMAND"]);

    [Fact(DisplayName = "checkable evidence naming the consumer-side tool path")]
    public void CheckableEvidenceConsumerToolPath() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec consumer` confirms the topic stays inside its declared boundary."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    [Fact(DisplayName = "checkable evidence naming a consumer-side suite path")]
    public void CheckableEvidenceConsumerSuitePath() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`tests/routes/every-route.spec.ts` proves the topic boundary in a browser."));
            StandardsFixture.Write(root, "tests/routes/every-route.spec.ts", "// Fixture stub so the consumer suite path resolves.\n");
            StandardsFixture.RebuildIndex(root);
        },
        []);

    [Fact(DisplayName = "guidance evidence names no command")]
    public void GuidanceEvidenceNoCommand() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Reviewed for:** Does the topic stay inside its declared boundary?"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    [Fact(DisplayName = "generic verification evidence")]
    public void GenericVerificationEvidence() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "confirms the topic stays inside its declared boundary.", "runs, then inspect evidence for the topic boundary.")),
        ["VERIFY_GENERIC_EVIDENCE"]);

    [Fact(DisplayName = "normative verification evidence")]
    public void NormativeVerificationEvidence() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Does the topic live in its owning documentation area?", "Reviewers MUST record where the topic lives.")),
        ["STANDARD_INFORMATIVE_NORMATIVE"]);

    [Fact(DisplayName = "provision restates its heading")]
    public void ProvisionRestatesHeading() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Statement:** Consumers MUST keep the topic inside its declared boundary.", "**Statement:** Consumers MUST keep the topic bounded.")),
        ["PROVISION_RESTATES_HEADING"]);

    [Fact(DisplayName = "evidence repeats its provision heading")]
    public void EvidenceRepeatsHeading() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "Pull request review asserts `keep the topic bounded` in the owning specification.")),
        ["VERIFY_TEMPLATED_EVIDENCE"]);

    [Fact(DisplayName = "verification lines sharing evidence")]
    public void VerificationLinesSharingEvidence() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Does the topic live in its owning documentation area?", "`litespec-maintain validate` confirms the topic stays inside its declared boundary.")),
        ["VERIFY_DUPLICATE_EVIDENCE"]);

    [Fact(DisplayName = "pattern provision without a verification line")]
    public void PatternWithoutVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/frontend/patterns/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.PatternPage, "**Reviewed for:** Does the top of each record page match the default option or the one its catalog records?\n\n", "")),
        ["VERIFY_MISSING_ID"]);
}
