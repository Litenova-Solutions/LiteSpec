using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Strength and wording agreement cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Required statement holding MUST NOT.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "required statement holding MUST NOT")]
    public void RequiredStatementHoldingMustNot() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST NOT move the topic outside its declared boundary."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Required statement with a MAY folded beside MUST.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "required statement with a MAY folded beside MUST")]
    public void RequiredStatementWithFoldedMay() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST keep the topic inside its declared boundary, and MAY rely on neighboring pages otherwise."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Required statement with a MAY folded beside MUST NOT.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "required statement with a MAY folded beside MUST NOT")]
    public void RequiredStatementWithFoldedMayBesideMustNot() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST NOT move the topic outside its declared boundary, and MAY rely on neighboring pages otherwise."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Required statement holding only MAY.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "required statement holding only MAY")]
    public void RequiredStatementHoldingOnlyMay() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MAY keep the topic inside its declared boundary.")),
        [MaintainerDiagnostics.Codes.StandardStrengthModal]);

    /// <summary>Required statement holding only SHOULD.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "required statement holding only SHOULD")]
    public void RequiredStatementHoldingOnlyShould() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep", "Consumers SHOULD keep")),
        [MaintainerDiagnostics.Codes.StandardStrengthModal, MaintainerDiagnostics.Codes.StandardMissingDeviation]);

    /// <summary>Advisory statement holding MUST.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "advisory statement holding MUST")]
    public void AdvisoryStatementHoldingMust() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required", "**Strength:** advisory")),
        [MaintainerDiagnostics.Codes.StandardStrengthModal]);

    /// <summary>Advisory statement holding MUST NOT.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "advisory statement holding MUST NOT")]
    public void AdvisoryStatementHoldingMustNot() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST NOT move the topic outside its declared boundary."), "**Strength:** required", "**Strength:** advisory")),
        [MaintainerDiagnostics.Codes.StandardStrengthModal]);

    /// <summary>Advisory statement holding SHOULD.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "advisory statement holding SHOULD")]
    public void AdvisoryStatementHoldingShould() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required", "**Strength:** advisory");
            page = StandardsFixture.ReplaceFirst(page, "Consumers MUST keep", "Consumers SHOULD keep");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** A draft topic keeps its boundary by review.\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Default strength on a provision with no Default line.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "default strength on a provision with no Default line")]
    public void DefaultStrengthWithoutDefaultLine() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required", "**Strength:** default")),
        [MaintainerDiagnostics.Codes.StandardStrengthModal]);

    /// <summary>Advisory convention with no Statement.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "advisory convention with no Statement")]
    public void AdvisoryConventionWithoutStatement() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** default", "**Strength:** advisory"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Statement with a MAY folded beside SHOULD.</summary>
    /// <proves>litespec/core/authoring#use-one-normative-vocabulary</proves>
    [Fact(DisplayName = "statement with a MAY folded beside SHOULD")]
    public void StatementWithFoldedMayBesideShould() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required", "**Strength:** advisory");
            page = StandardsFixture.ReplaceFirst(page, "Consumers MUST keep the topic inside its declared boundary.", "Consumers SHOULD keep the topic inside its declared boundary, and MAY rely on neighboring pages otherwise.");
            page = StandardsFixture.ReplaceFirst(page, "**Rationale:** The boundary gives reviewers one testable result.", "**Deviation:** A draft topic keeps its boundary by review.\n\n**Rationale:** The boundary gives reviewers one testable result.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.StandardModalCount]);
}
