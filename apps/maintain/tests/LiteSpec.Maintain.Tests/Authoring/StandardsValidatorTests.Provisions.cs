namespace LiteSpec.Tests.Authoring;

/// <summary>Identifier, Standard, and Convention cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <proves>litespec/core/authoring#name-the-owning-page-in-the-identifier</proves>
    [Fact(DisplayName = "duplicate slug on one page")]
    public void DuplicateId() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Conventions", "### Keep the Topic Bounded\n\n**Statement:** Consumers MUST keep the topic inside its declared boundary.\n\n## Conventions")),
        ["ID_DUPLICATE"]);

    /// <proves>litespec/core/authoring#keep-identifiers-out-of-provision-headings</proves>
    [Fact(DisplayName = "identifier text in a provision heading")]
    public void IdInHeading() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "### Keep the Topic Bounded", "### Keep the Topic Bounded (litespec/core/topic#keep-the-topic-bounded)")),
        ["ID_IN_HEADING"]);

    /// <proves>litespec/core/authoring#name-the-owning-page-in-the-identifier</proves>
    [Fact(DisplayName = "page file name that is not kebab case")]
    public void PageFileNameNotKebab() => Check(
        root => StandardsFixture.Write(root, "docs/core/TwoWords.md", StandardsFixture.TopicPage),
        ["ID_PAGE_FILENAME"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "missing statement")]
    public void MissingStatement() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Statement:** Consumers MUST keep the topic inside its declared boundary.\n\n", "")),
        ["STANDARD_MISSING_REQUIREMENT"]);

    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "multiple modals")]
    public void MultipleModals() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST write the topic and MUST review the topic.")),
        ["STANDARD_MODAL_COUNT"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "missing deviation")]
    public void MissingDeviation() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep")),
        ["STANDARD_MISSING_DEVIATION"]);

    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "missing convention default")]
    public void MissingConventionDefault() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Default:** Store the topic in its owning documentation area.\n\n", "")),
        ["CONVENTION_MISSING_DEFAULT"]);

    /// <proves>litespec/core/authoring#restrict-normative-language-to-standards-and-conventions</proves>
    [Fact(DisplayName = "normative convention rationale")]
    public void NormativeConventionRationale() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Replacement:** A consumer can name another location through an explicit local convention.", "**Replacement:** A consumer can name another location through an explicit local convention.\n\n**Rationale:** Consumers MUST record the replacement.")),
        ["STANDARD_INFORMATIVE_NORMATIVE"]);

    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "modal in a convention Default is allowed")]
    public void ModalInConventionDefaultAllowed() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Store the topic in its owning documentation area.", "Consumers MUST store the topic in its owning documentation area.")),
        []);

    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "modal in a convention Replacement is allowed")]
    public void ModalInConventionReplacementAllowed() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer MUST name another location through an explicit local convention.")),
        []);

    /// <proves>litespec/core/authoring#map-provisions-to-evidence</proves>
    [Fact(DisplayName = "convention without a verification line")]
    public void ConventionWithoutVerificationLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Reviewed for:** Does the topic live in its owning documentation area?\n", "")),
        ["VERIFY_MISSING_ID"]);

    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "wrong ID line")]
    public void WrongIdLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`", "**ID:** `litespec/core/topic#keep-the-topic-boundary`")),
        ["ID_LINE_MISMATCH"]);

    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "missing ID line")]
    public void MissingIdLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n", "")),
        ["ID_LINE_MISSING"]);

    /// <proves>litespec/core/authoring#place-the-id-under-every-provision-heading</proves>
    [Fact(DisplayName = "ID line after the statement")]
    public void IdLineAfterStatement() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n", "");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**ID:** `litespec/core/topic#keep-the-topic-bounded`\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        ["ID_LINE_MISSING"]);

    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "unknown active ID")]
    public void UnknownActiveId() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nThe page cites litespec/core/topic#unknown-rule.\n"),
        ["ID_UNKNOWN_REFERENCE"]);

    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "stale citation")]
    public void StaleCitation() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nThe page cites litespec/core/topic#keep-the-topic-bounded-elsewhere.\n"),
        ["ID_UNKNOWN_REFERENCE"]);

    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "grammar notation is not a citation")]
    public void GrammarNotationIsNotCitation() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nAn identifier uses litespec/<page>#<slug> as its grammar.\n\n" + StandardsFixture.IndexRouting),
        []);

    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "unsupported modal vocabulary")]
    public void UnsupportedModalVocabulary() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST keep the topic bounded and SHALL record its owner.")),
        ["STANDARD_MODAL_VOCABULARY"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "multiple statement sentences")]
    public void MultipleStatementSentences() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST keep the topic inside its boundary. Reviewers inspect the result.")),
        ["STANDARD_SENTENCE_COUNT"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "duplicate Statement")]
    public void DuplicateStatement() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Statement:** Consumers MUST name the topic owner.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        ["STANDARD_REQUIREMENT_COUNT"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "unexpected deviation")]
    public void UnexpectedDeviation() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** No deviation applies.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        ["STANDARD_UNEXPECTED_DEVIATION"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "deviation sentence count")]
    public void DeviationSentenceCount() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** A local decision permits another boundary. The decision names its owner.\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        ["STANDARD_DEVIATION_SENTENCE"]);

    /// <proves>litespec/core/authoring#attach-examples-to-their-provisions</proves>
    [Fact(DisplayName = "unlabeled example")]
    public void UnlabeledExample() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Conventions", "```text\nexample\n```\n\n## Conventions")),
        ["STANDARD_EXAMPLE_LABEL"]);

    /// <proves>litespec/core/authoring#restrict-normative-language-to-standards-and-conventions</proves>
    [Fact(DisplayName = "normative rationale")]
    public void NormativeRationale() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "The boundary gives reviewers one testable result.", "Reviewers MUST record the boundary result.")),
        ["STANDARD_INFORMATIVE_NORMATIVE"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "duplicate label")]
    public void DuplicateLabel() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Rationale:** The boundary gives reviewers one testable result.\n\n**Rationale:** Another explanation.")),
        ["STANDARD_LABEL_DUPLICATE"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "invalid label")]
    public void InvalidLabel() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Default:** Keep another default.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        ["STANDARD_LABEL_INVALID"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "label order")]
    public void LabelOrder() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "**Example:** One example.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        ["STANDARD_LABEL_ORDER"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "unlabeled provision content")]
    public void UnlabeledProvisionContent() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Rationale:** The boundary gives reviewers one testable result.", "Additional constraint.\n\n**Rationale:** The boundary gives reviewers one testable result.")),
        ["STANDARD_UNLABELED_CONTENT"]);

    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "missing convention replacement")]
    public void MissingConventionReplacement() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Replacement:** A consumer can name another location through an explicit local convention.\n", "")),
        ["CONVENTION_MISSING_REPLACEMENT"]);

    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "default sentence count")]
    public void DefaultSentenceCount() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Store the topic in its owning documentation area.", "Store the topic in its owning documentation area. Record the path.")),
        ["CONVENTION_DEFAULT_SENTENCE"]);

    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "replacement sentence count")]
    public void ReplacementSentenceCount() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer can name another location. The local convention records it.")),
        ["CONVENTION_REPLACEMENT_SENTENCE"]);

    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "normative deviation")]
    public void NormativeDeviation() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** Consumers MAY keep another boundary.\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        ["STANDARD_DEVIATION_NORMATIVE"]);

    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "replacement naming an exception")]
    public void ReplacementNamingAnException() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer replaces this default by recording an Exception for this provision.")),
        ["CONVENTION_REPLACEMENT_DEVIATION"]);

    /// <proves>litespec/core/authoring#identify-actionable-conventions</proves>
    [Fact(DisplayName = "replacement naming a deviation")]
    public void ReplacementNamingADeviation() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "A consumer can name another location through an explicit local convention.", "A consumer replaces this default by recording a deviation with its reason.")),
        ["CONVENTION_REPLACEMENT_DEVIATION"]);
}
