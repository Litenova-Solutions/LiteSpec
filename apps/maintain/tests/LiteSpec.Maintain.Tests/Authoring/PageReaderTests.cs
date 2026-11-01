using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Reader cases: structure comes from Markdig, each with a passing and a failing case.</summary>
public sealed class PageReaderTests
{
    static List<(string Code, int Line)> Diagnose(Action<string> mutate)
    {
        using var temp = Cli.Temp("reader");
        StandardsFixture.Base(temp.Path);
        mutate(temp.Path);
        return StandardsValidator.Validate(temp.Path).Diagnostics.Select(item => (item.Code, item.Line)).ToList();
    }

    static List<(string Code, int Line)> Diagnose() => Diagnose(_ => { });

    /// <summary>Reader passes a provision heading inside a fenced block.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "reader passes a provision heading inside a fenced block")]
    public void FencedHeadingPass()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Standards", "## Standards\n\n```text\n### Not a verb\n```\n")));
        Assert.Empty(found);
    }

    /// <summary>Reader fails a provision heading outside a fenced block.</summary>
    /// <proves>litespec/core/authoring#prefer-direct-action-headings</proves>
    [Fact(DisplayName = "reader fails a provision heading outside a fenced block")]
    public void FencedHeadingFail()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "## Conventions",
                "### Not a verb\n\n**ID:** `litespec/core/topic#not-a-verb`\n\n**Statement:** Consumers MUST record the test boundary.\n\n**Checked by:** `litespec-maintain validate` confirms the test boundary is recorded.\n\n## Conventions")));
        Assert.Contains(found, item => item.Code == "HEADING_ACTION");
    }

    /// <summary>Reader passes a Statement inside a code span.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "reader passes a Statement inside a code span")]
    public void StatementCodeSpanPass()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage,
                "**Rationale:** The boundary gives reviewers one testable result.",
                "**Rationale:** The boundary gives reviewers one testable result.\n\nThe `**Statement:**` label starts a rule.")));
        Assert.Empty(found);
    }

    /// <summary>Reader fails a second Statement outside a code span.</summary>
    /// <proves>litespec/core/authoring#write-atomic-standards-provisions</proves>
    [Fact(DisplayName = "reader fails a second Statement outside a code span")]
    public void StatementCodeSpanFail()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage,
                "**Rationale:** The boundary gives reviewers one testable result.",
                "**Statement:** Consumers MUST name the topic owner.\n\n**Rationale:** The boundary gives reviewers one testable result.")));
        Assert.Contains(found, item => item.Code == "STANDARD_REQUIREMENT_COUNT");
    }

    /// <summary>Reader passes a link inside a code span.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "reader passes a link inside a code span")]
    public void LinkCodeSpanPass()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage,
                "This page defines one repository topic.",
                "Read `[missing](missing.md)` for details.\n\nThis page defines one repository topic.")));
        Assert.Empty(found);
    }

    /// <summary>Reader fails a link outside a code span.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "reader fails a link outside a code span")]
    public void LinkCodeSpanFail()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage,
                "This page defines one repository topic.",
                "Read [missing](missing.md) for details.\n\nThis page defines one repository topic.")));
        Assert.Contains(found, item => item.Code == "LINK_BROKEN");
    }

    /// <summary>Reader passes a valid table cell link.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "reader passes a valid table cell link")]
    public void TableLinkPass()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage,
                "This page defines one repository topic.",
                "This page defines one repository topic.\n\n| Link | Target |\n|:---|:---|\n| [Docs](../README.md) | Documentation root. |")));
        Assert.Empty(found);
    }

    /// <summary>Reader fails a broken table cell link.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "reader fails a broken table cell link")]
    public void TableLinkFail()
    {
        var found = Diagnose(root => StandardsFixture.Write(root, "docs/core/topic.md",
            StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage,
                "This page defines one repository topic.",
                "This page defines one repository topic.\n\n| Link | Target |\n|:---|:---|\n| [Missing](missing.md) | Missing page. |")));
        Assert.Contains(found, item => item.Code == "LINK_BROKEN");
    }

    /// <summary>Reader passes a page with one H1.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "reader passes a page with one H1")]
    public void TwoH1Pass()
    {
        Assert.Empty(Diagnose());
    }

    /// <summary>Reader reports two H1s on the second H1 line.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "reader reports two H1s on the second H1 line")]
    public void TwoH1Fail()
    {
        using var temp = Cli.Temp("reader");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", StandardsFixture.TopicPage + "\n# Second Title\n");
        var result = StandardsValidator.Validate(temp.Path);
        var finding = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == "PAGE_TITLE_COUNT");
        var written = File.ReadAllText(Path.Combine(temp.Path, "docs", "core", "topic.md")).Split('\n');
        var expected = Array.FindIndex(written, line => line.Trim() == "# Second Title") + 1;
        Assert.Equal(expected, finding.Line);
    }
}
