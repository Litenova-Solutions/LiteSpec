using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Authoring;

/// <summary>Identifier, Standard, and Convention cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Duplicate slug on one page.</summary>
    /// <proves>litespec/core/authoring#name-the-owning-page-in-the-identifier</proves>
    [Fact(DisplayName = "duplicate slug on one page")]
    public void DuplicateId() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Conventions", "### Keep the Topic Bounded\n\n**Statement:** Consumers MUST keep the topic inside its declared boundary.\n\n## Conventions")),
        [MaintainerDiagnostics.Codes.IdDuplicate]);

    /// <summary>Identifier text in a provision heading.</summary>
    /// <proves>litespec/core/authoring#keep-identifiers-out-of-provision-headings</proves>
    [Fact(DisplayName = "identifier text in a provision heading")]
    public void IdInHeading() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "### Keep the Topic Bounded", "### Keep the Topic Bounded (litespec/core/topic#keep-the-topic-bounded)")),
        [MaintainerDiagnostics.Codes.IdInHeading]);

    /// <summary>Page file name that is not kebab case.</summary>
    /// <proves>litespec/core/authoring#name-the-owning-page-in-the-identifier</proves>
    [Fact(DisplayName = "page file name that is not kebab case")]
    public void PageFileNameNotKebab() => Check(
        root => StandardsFixture.Write(root, "docs/core/TwoWords.md", StandardsFixture.TopicPage),
        [MaintainerDiagnostics.Codes.IdPageFilename]);

    /// <summary>Missing statement.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "missing statement")]
    public void MissingStatement() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Statement:** Consumers MUST keep the topic inside its declared boundary.\n\n", "")),
        [MaintainerDiagnostics.Codes.StandardMissingRequirement]);

    /// <summary>Multiple modals.</summary>
    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "multiple modals")]
    public void MultipleModals() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST write the topic and MUST review the topic.")),
        [MaintainerDiagnostics.Codes.StandardModalCount]);

    /// <summary>Missing deviation.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "missing deviation")]
    public void MissingDeviation() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep")),
        [MaintainerDiagnostics.Codes.StandardMissingDeviation]);

    /// <summary>Missing convention default.</summary>
    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "missing convention default")]
    public void MissingConventionDefault() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Default:** Store the topic in its owning documentation area.\n\n", "")),
        [MaintainerDiagnostics.Codes.ConventionMissingDefault]);

    /// <summary>Normative convention rationale.</summary>
    /// <proves>litespec/core/authoring#restrict-normative-language-to-standards-and-conventions</proves>
    [Fact(DisplayName = "normative convention rationale")]
    public void NormativeConventionRationale() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Replacement:** A consumer can name another location through an explicit local convention.", "**Replacement:** A consumer can name another location through an explicit local convention.\n\n**Rationale:** Consumers MUST record the replacement.")),
        [MaintainerDiagnostics.Codes.StandardInformativeNormative]);

    /// <summary>Modal in a convention Default is allowed.</summary>
    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "modal in a convention Default is allowed")]
    public void ModalInConventionDefaultAllowed() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Store the topic in its owning documentation area.", "Consumers MUST store the topic in its owning documentation area."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Modal in a convention Replacement is allowed.</summary>
    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "modal in a convention Replacement is allowed")]
    public void ModalInConventionReplacementAllowed() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer MUST name another location through an explicit local convention.")),
        []);

    /// <summary>Convention without a verification line.</summary>
    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "convention without a verification line")]
    public void ConventionWithoutVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Reviewed for:** Does the topic live in its owning documentation area?\n", "")),
        [MaintainerDiagnostics.Codes.VerifyMissingId]);

    /// <summary>ID line naming another page.</summary>
    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "ID line naming another page")]
    public void WrongIdLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`", "**ID:** `litespec/core/other#keep-the-topic-bounded`")),
        [MaintainerDiagnostics.Codes.IdLineMismatch]);

    /// <summary>ID line outside the identifier grammar.</summary>
    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "ID line outside the identifier grammar")]
    public void MalformedIdLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`", "**ID:** `not-an-id`")),
        [MaintainerDiagnostics.Codes.IdLineMismatch]);

    /// <summary>Heading reworded above a kept ID.</summary>
    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "heading reworded above a kept ID")]
    public void HeadingRewordedAboveKeptId() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "### Keep the Topic Bounded", "### Keep the Topic Inside Its Boundary"));
            Directory.Delete(Path.Combine(root, "docs", "reference"), true);
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(omit: ["reference/sample.md", "reference/glossary.md"]));
        },
        []);

    /// <summary>Duplicate ID line on one page.</summary>
    /// <proves>litespec/core/authoring#name-the-owning-page-in-the-identifier</proves>
    [Fact(DisplayName = "duplicate ID line on one page")]
    public void DuplicateStatedId() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Conventions", "### Keep the Topic Narrow\n\n**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n**Statement:** Consumers MUST keep the topic narrow enough to review.\n\n**Rationale:** A narrow topic is reviewable.\n\n**Checked by:** `litespec-maintain validate` confirms the topic is narrow.\n\n## Conventions")),
        [MaintainerDiagnostics.Codes.IdDuplicate]);

    /// <summary>Missing ID line.</summary>
    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "missing ID line")]
    public void MissingIdLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n", "")),
        [MaintainerDiagnostics.Codes.IdLineMissing]);

    /// <summary>ID line after the statement.</summary>
    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "ID line after the statement")]
    public void IdLineAfterStatement() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n", "");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.IdLineMissing]);

    /// <summary>Unknown active ID.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "unknown active ID")]
    public void UnknownActiveId() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nThe page cites litespec/core/topic#unknown-rule.\n"),
        [MaintainerDiagnostics.Codes.IdUnknownReference]);

    /// <summary>Stale citation.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "stale citation")]
    public void StaleCitation() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nThe page cites litespec/core/topic#keep-the-topic-bounded-elsewhere.\n"),
        [MaintainerDiagnostics.Codes.IdUnknownReference]);

    /// <summary>Grammar notation is not a citation.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "grammar notation is not a citation")]
    public void GrammarNotationIsNotCitation() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme("An identifier uses litespec/<page>#<slug> as its grammar.")),
        []);

    /// <summary>Unsupported modal vocabulary.</summary>
    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "unsupported modal vocabulary")]
    public void UnsupportedModalVocabulary() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST keep the topic bounded and SHALL record its owner.")),
        [MaintainerDiagnostics.Codes.StandardModalVocabulary]);

    /// <summary>Multiple statement sentences.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "multiple statement sentences")]
    public void MultipleStatementSentences() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST keep the topic inside its boundary. Reviewers inspect the result.")),
        [MaintainerDiagnostics.Codes.StandardSentenceCount]);

    /// <summary>Duplicate Statement.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "duplicate Statement")]
    public void DuplicateStatement() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Statement:** Consumers MUST name the topic owner.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        [MaintainerDiagnostics.Codes.StandardRequirementCount]);

    /// <summary>Unexpected deviation.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "unexpected deviation")]
    public void UnexpectedDeviation() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** No deviation applies.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        [MaintainerDiagnostics.Codes.StandardUnexpectedDeviation]);

    /// <summary>Deviation sentence count.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "deviation sentence count")]
    public void DeviationSentenceCount() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** A local decision permits another boundary. The decision names its owner.\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.StandardDeviationSentence]);

    /// <summary>Unlabeled example.</summary>
    /// <proves>litespec/core/authoring#attach-examples-to-their-provisions</proves>
    [Fact(DisplayName = "unlabeled example")]
    public void UnlabeledExample() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Conventions", "```text\nexample\n```\n\n## Conventions")),
        [MaintainerDiagnostics.Codes.StandardExampleLabel]);

    /// <summary>Normative rationale.</summary>
    /// <proves>litespec/core/authoring#restrict-normative-language-to-standards-and-conventions</proves>
    [Fact(DisplayName = "normative rationale")]
    public void NormativeRationale() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "The boundary gives reviewers one testable result.", "Reviewers MUST record the boundary result.")),
        [MaintainerDiagnostics.Codes.StandardInformativeNormative]);

    /// <summary>Duplicate label.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "duplicate label")]
    public void DuplicateLabel() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Rationale:** The boundary gives reviewers one testable result.\n\n**Rationale:** Another explanation.")),
        [MaintainerDiagnostics.Codes.StandardLabelDuplicate]);

    /// <summary>Invalid label.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "invalid label")]
    public void InvalidLabel() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Default:** Keep another default.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        [MaintainerDiagnostics.Codes.StandardLabelInvalid]);

    /// <summary>Label order.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "label order")]
    public void LabelOrder() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Example:** One example.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        [MaintainerDiagnostics.Codes.StandardLabelOrder]);

    /// <summary>Unlabeled provision content.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "unlabeled provision content")]
    public void UnlabeledProvisionContent() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "Additional constraint.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        [MaintainerDiagnostics.Codes.StandardUnlabeledContent]);

    /// <summary>Missing convention replacement.</summary>
    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "missing convention replacement")]
    public void MissingConventionReplacement() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Replacement:** A consumer can name another location through an explicit local convention.\n", "")),
        [MaintainerDiagnostics.Codes.ConventionMissingReplacement]);

    /// <summary>Default sentence count.</summary>
    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "default sentence count")]
    public void DefaultSentenceCount() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Store the topic in its owning documentation area.", "Store the topic in its owning documentation area. Record the path.")),
        [MaintainerDiagnostics.Codes.ConventionDefaultSentence]);

    /// <summary>Replacement sentence count.</summary>
    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "replacement sentence count")]
    public void ReplacementSentenceCount() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer can name another location. The local convention records it.")),
        [MaintainerDiagnostics.Codes.ConventionReplacementSentence]);

    /// <summary>Normative deviation.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "normative deviation")]
    public void NormativeDeviation() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** Consumers MAY keep another boundary.\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.StandardDeviationNormative]);

    /// <summary>Replacement naming an exception.</summary>
    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "replacement naming an exception")]
    public void ReplacementNamingAnException() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer replaces this default by recording an Exception for this provision.")),
        [MaintainerDiagnostics.Codes.ConventionReplacementDeviation]);

    /// <summary>Replacement naming a deviation.</summary>
    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "replacement naming a deviation")]
    public void ReplacementNamingADeviation() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer replaces this default by recording a deviation with its reason.")),
        [MaintainerDiagnostics.Codes.ConventionReplacementDeviation]);
}
