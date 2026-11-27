using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Page titles, sections, links, and glossary cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Section order.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "section order")]
    public void SectionOrder() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## TEMP");
            page = StandardsFixture.ReplaceFirst(page, "## Conventions", "## Standards");
            page = StandardsFixture.ReplaceFirst(page, "## TEMP", "## Conventions");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.PageSectionOrder]);

    /// <summary>Broken link.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "broken link")]
    public void BrokenLink() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\nRead the [missing page](missing.md).\n"),
        [MaintainerDiagnostics.Codes.LinkBroken]);

    /// <summary>Broken anchor.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "broken anchor")]
    public void BrokenAnchor() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\nRead the [missing anchor](core/topic.md#missing).\n"),
        [MaintainerDiagnostics.Codes.AnchorBroken]);

    /// <summary>Missing H1.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "missing H1")]
    public void MissingH1() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard\n\n", "")),
        [MaintainerDiagnostics.Codes.PageTitleCount]);

    /// <summary>H1 case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "H1 case")]
    public void H1Case() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard", "# Topic standard")),
        [MaintainerDiagnostics.Codes.PageTitleCase]);

    /// <summary>Missing section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "missing section")]
    public void MissingSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(
            StandardsFixture.TopicPage,
            "## Conventions\n\n### Use the Default Location\n\n**ID:** `litespec/core/topic#use-the-default-location`\n\n**Strength:** default\n\n**Default:** Store the topic in its owning documentation area.\n\n**Replacement:** A consumer can name another location through an explicit local convention.\n\n**Reviewed for:** Does the topic live in its owning documentation area?\n",
            "")),
        [MaintainerDiagnostics.Codes.PageMissingSection]);

    /// <summary>Empty section.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section")]
    public void EmptySection() => Check(
        root => StandardsFixture.Write(root, "docs/packs/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions\n\nNone.\n", "## Conventions\n")),
        [MaintainerDiagnostics.Codes.PageEmptySection]);

    /// <summary>Standard page without a lead.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "standard page without a lead")]
    public void StandardPageWithoutLead() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.\n\n", "")),
        [MaintainerDiagnostics.Codes.PageLeadMissing]);

    /// <summary>Standard page with a lead and no Intent section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "standard page with a lead and no Intent section")]
    public void StandardPageWithLeadAndNoIntentSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.TopicPage),
        []);

    /// <summary>Unknown section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "unknown section")]
    public void UnknownSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## Notes\n\nOne informative note.\n\n## Standards")),
        [MaintainerDiagnostics.Codes.PageUnknownSection]);

    /// <summary>Reference example declaration.</summary>
    /// <proves>litespec/core/authoring#attach-examples-to-their-provisions</proves>
    [Fact(DisplayName = "reference example declaration")]
    public void ReferenceExampleDeclaration() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.TopicPage + "\n## Reference Example\n\nA topic example has no declared provision.\n"),
        [MaintainerDiagnostics.Codes.ReferenceExampleDeclaration]);

    /// <summary>Index without a lead.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "index without a lead")]
    public void IndexWithoutLead() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Overview\n\nUse this page to find repository documentation.\n"),
        [MaintainerDiagnostics.Codes.PageLeadMissing]);

    /// <summary>Glossary order.</summary>
    /// <proves>litespec/core/authoring#order-glossary-groups-alphabetically</proves>
    [Fact(DisplayName = "glossary order")]
    public void GlossaryOrder() => Check(
        root => StandardsFixture.Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Zeta\n\nThe last sample term.\n\n## Alpha\n\nThe first sample term.\n"),
        [MaintainerDiagnostics.Codes.GlossaryOrder]);

    /// <summary>Glossary definition.</summary>
    /// <proves>litespec/core/authoring#order-glossary-groups-alphabetically</proves>
    [Fact(DisplayName = "glossary definition")]
    public void GlossaryDefinition() => Check(
        root => StandardsFixture.Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Sample\n\nOne sample term. It has another definition sentence.\n"),
        [MaintainerDiagnostics.Codes.GlossaryDefinition]);

    /// <summary>A link property with no reads-as and read-back-as annotation.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact(DisplayName = "link property with no annotations")]
    public void LinkAnnotationMissing() => Check(
        root => StandardsFixture.Write(root, "schemas/record.schema.yaml", "oneOf:\n- $ref: '#/$defs/flowPage'\n$defs:\n  flowPage:\n    type: object\n    properties:\n      type:\n        const: flow\n      serves:\n        $ref: '#/$defs/goalId'\n  goalId:\n    type: string\n"),
        [MaintainerDiagnostics.Codes.LinkAnnotationMissing]);

    /// <summary>Glossary term order.</summary>
    /// <proves>litespec/core/authoring#order-glossary-groups-alphabetically</proves>
    [Fact(DisplayName = "glossary term order")]
    public void GlossaryTermOrder() => Check(
        root => StandardsFixture.Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Sample\n\nThe sample group.\n\n### Zeta\n\nThe last sample term.\n\n### Alpha\n\nThe first sample term.\n"),
        [MaintainerDiagnostics.Codes.GlossaryOrder]);

    /// <summary>Non-action heading.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "non-action heading")]
    public void NonActionHeading() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "### Keep the Topic Bounded", "### Topic boundary");
            page = page.Replace("litespec/core/topic#keep-the-topic-bounded", "litespec/core/topic#topic-boundary");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.HeadingAction]);

    /// <summary>Heading with no body.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "heading with no body")]
    public void HeadingWithNoBody() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", string.Join("\n", ["## Concepts", "", "### Empty concept", "", "### Second concept", "", "This concept has a body.", "", "## Standards"]))),
        [MaintainerDiagnostics.Codes.HeadingEmptyBody]);

    /// <summary>A starter README is the consumer's own file, so its numbered run steps are no LiteSpec index procedure.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "starter README is not a LiteSpec page")]
    public void StarterReadmeIsNotALiteSpecPage() => Check(
        root => StandardsFixture.Write(root, "templates/bootstrap/dotnet-svelte/README.md", string.Join("\n", ["# __PROJECT__", "", "The starter of __PROJECT__.", "", "## Run locally", "", "1. Start the stack.", ""])),
        [],
        [MaintainerDiagnostics.Codes.IndexContainsProcedure, MaintainerDiagnostics.Codes.PageHeadingCase]);

    /// <summary>Numbered procedure inside an index.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "numbered procedure inside an index")]
    public void NumberedProcedureInsideIndex() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", string.Join("\n", ["# Documentation", "", "Use this page to find repository documentation.", "", "## Activation", "", "1. Select the applicable document.", ""])),
        [MaintainerDiagnostics.Codes.IndexContainsProcedure]);

    /// <summary>Provision heading in sentence case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "provision heading in sentence case")]
    public void ProvisionHeadingSentenceCase() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "### Keep the Topic Bounded", "### Keep the topic boundary small");
            page = page.Replace("litespec/core/topic#keep-the-topic-bounded", "litespec/core/topic#keep-the-topic-boundary-small");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.PageHeadingCase]);

    /// <summary>Concept heading in sentence case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "concept heading in sentence case")]
    public void ConceptHeadingSentenceCase() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## Concepts\n\n### Topic boundary rule\n\nThe boundary rule is one sentence.\n\n## Standards")),
        [MaintainerDiagnostics.Codes.PageHeadingCase]);

    /// <summary>Empty section variant naming no content.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section variant naming no content")]
    public void EmptySectionVariant() => Check(
        root => StandardsFixture.Write(root, "docs/packs/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions\n\nNone.", "## Conventions\n\nThis stack adds no conventions.")),
        [MaintainerDiagnostics.Codes.PageEmptySectionText]);

    /// <summary>Empty section holding bare None.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section holding bare None")]
    public void EmptySectionBareNone() => Check(
        root => StandardsFixture.Write(root, "docs/packs/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions\n\nNone.", "## Conventions\n\nNone")),
        [MaintainerDiagnostics.Codes.PageEmptySectionText]);

    /// <summary>Empty section holding None with more.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section holding None with more")]
    public void EmptySectionNoneWithMore() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "## Options\n\nNone.", "## Options\n\nNone. Everything passes through arguments.")),
        [MaintainerDiagnostics.Codes.PageEmptySectionText]);
}
