namespace LiteSpec.Tests.Authoring;

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
        ["PAGE_SECTION_ORDER"]);

    /// <summary>Broken link.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "broken link")]
    public void BrokenLink() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nRead the [missing page](missing.md).\n"),
        ["LINK_BROKEN"]);

    /// <summary>Broken anchor.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "broken anchor")]
    public void BrokenAnchor() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nRead the [missing anchor](core/topic.md#missing).\n"),
        ["ANCHOR_BROKEN"]);

    /// <summary>Missing H1.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "missing H1")]
    public void MissingH1() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard\n\n", "")),
        ["PAGE_TITLE_COUNT"]);

    /// <summary>H1 case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "H1 case")]
    public void H1Case() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard", "# Topic standard")),
        ["PAGE_TITLE_CASE"]);

    /// <summary>Missing section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "missing section")]
    public void MissingSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Intent\n\n", "")),
        ["PAGE_MISSING_SECTION"]);

    /// <summary>Empty section.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section")]
    public void EmptySection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.\n\n", "")),
        ["PAGE_EMPTY_SECTION"]);

    /// <summary>Unknown section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "unknown section")]
    public void UnknownSection() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## Notes\n\nOne informative note.\n\n## Standards")),
        ["PAGE_UNKNOWN_SECTION"]);

    /// <summary>Reference example declaration.</summary>
    /// <proves>litespec/core/authoring#attach-examples-to-their-provisions</proves>
    [Fact(DisplayName = "reference example declaration")]
    public void ReferenceExampleDeclaration() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.TopicPage + "\n## Reference Example\n\nA topic example has no declared provision.\n"),
        ["REFERENCE_EXAMPLE_DECLARATION"]);

    /// <summary>Index without Intent.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "index without Intent")]
    public void IndexWithoutIntent() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", "# Documentation\n\n## Overview\n\nUse this page to find repository documentation.\n"),
        ["INDEX_INTENT"]);

    /// <summary>Glossary order.</summary>
    /// <proves>litespec/core/authoring#order-glossary-groups-alphabetically</proves>
    [Fact(DisplayName = "glossary order")]
    public void GlossaryOrder() => Check(
        root => StandardsFixture.Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Zeta\n\nThe last sample term.\n\n## Alpha\n\nThe first sample term.\n"),
        ["GLOSSARY_ORDER"]);

    /// <summary>Glossary definition.</summary>
    /// <proves>litespec/core/authoring#order-glossary-groups-alphabetically</proves>
    [Fact(DisplayName = "glossary definition")]
    public void GlossaryDefinition() => Check(
        root => StandardsFixture.Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Topic\n\nA bounded standards subject. It has another definition sentence.\n"),
        ["GLOSSARY_DEFINITION"]);

    /// <summary>Glossary term order.</summary>
    /// <proves>litespec/core/authoring#order-glossary-groups-alphabetically</proves>
    [Fact(DisplayName = "glossary term order")]
    public void GlossaryTermOrder() => Check(
        root => StandardsFixture.Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Topic\n\nThe sample group.\n\n### Zeta\n\nThe last sample term.\n\n### Alpha\n\nThe first sample term.\n"),
        ["GLOSSARY_ORDER"]);

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
        ["HEADING_ACTION"]);

    /// <summary>Heading with no body.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "heading with no body")]
    public void HeadingWithNoBody() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", string.Join("\n", ["## Concepts", "", "### Empty concept", "", "### Second concept", "", "This concept has a body.", "", "## Standards"]))),
        ["HEADING_EMPTY_BODY"]);

    /// <summary>Numbered procedure inside an index.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "numbered procedure inside an index")]
    public void NumberedProcedureInsideIndex() => Check(
        root => StandardsFixture.Write(root, "docs/README.md", string.Join("\n", ["# Documentation", "", "## Intent", "", "Use this page to find repository documentation.", "", "## Activation", "", "1. Select the applicable document.", ""])),
        ["INDEX_CONTAINS_PROCEDURE"]);

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
        ["PAGE_HEADING_CASE"]);

    /// <summary>Concept heading in sentence case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "concept heading in sentence case")]
    public void ConceptHeadingSentenceCase() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## Concepts\n\n### Topic boundary rule\n\nThe boundary rule is one sentence.\n\n## Standards")),
        ["PAGE_HEADING_CASE"]);

    /// <summary>Empty section variant naming no content.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section variant naming no content")]
    public void EmptySectionVariant() => Check(
        root => StandardsFixture.Write(root, "docs/backend/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions\n\nNone.", "## Conventions\n\nThis stack adds no conventions.")),
        ["PAGE_EMPTY_SECTION_TEXT"]);

    /// <summary>Empty section holding bare None.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section holding bare None")]
    public void EmptySectionBareNone() => Check(
        root => StandardsFixture.Write(root, "docs/backend/sample/stack.md", StandardsFixture.ReplaceFirst(StandardsFixture.StackPage, "## Conventions\n\nNone.", "## Conventions\n\nNone")),
        ["PAGE_EMPTY_SECTION_TEXT"]);

    /// <summary>Empty section holding None with more.</summary>
    /// <proves>litespec/core/authoring#write-none-in-an-empty-required-section</proves>
    [Fact(DisplayName = "empty section holding None with more")]
    public void EmptySectionNoneWithMore() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "## Options\n\nNone.", "## Options\n\nNone. Everything passes through arguments.")),
        ["PAGE_EMPTY_SECTION_TEXT"]);
}
