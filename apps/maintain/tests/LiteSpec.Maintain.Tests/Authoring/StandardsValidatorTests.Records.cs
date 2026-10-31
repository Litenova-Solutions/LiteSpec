using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>LiteSpec record cases: one fixture per record code, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Record pages pass.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "record pages pass")]
    public void RecordPagesPass() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/intent/decisions/0001-sample-choice.md", StandardsFixture.DecisionPage);
        },
        []);

    /// <summary>Implementation decision passes.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "implementation decision passes")]
    public void ImplementationDecisionPasses() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/implementation/decisions/0001-sample-choice.md", StandardsFixture.DecisionPage);
        },
        []);

    /// <summary>Research frontmatter breaking its schema.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "research frontmatter breaking its schema")]
    public void ResearchFrontmatterBreakingItsSchema() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{\"researchPage\":{\"type\":\"object\",\"required\":[\"type\",\"id\",\"conclusion\"],\"properties\":{\"type\":{\"const\":\"research\"},\"id\":{\"type\":\"string\"},\"conclusion\":{\"type\":\"string\"}}},\"decisionPage\":{\"type\":\"object\"}}}");
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.ResearchPage, "\"conclusion\": \"The sample shows one finding.\",\n", "");
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", page);
        },
        ["RECORD_SCHEMA"]);

    /// <summary>Record identifier not matching its file.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "record identifier not matching its file")]
    public void RecordIdentifierNotMatchingItsFile() => Check(
        root => StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ReplaceFirst(StandardsFixture.ResearchPage, "\"id\": \"research/sample-study\"", "\"id\": \"research/other-study\"")),
        ["RECORD_ID_MISMATCH"]);

    /// <summary>Source without a read date.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "source without a read date")]
    public void SourceWithoutAReadDate() => Check(
        root => StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ReplaceFirst(StandardsFixture.ResearchPage, ",\n      \"readOn\": \"2026-10-30\"", "")),
        ["RECORD_SOURCE"]);

    /// <summary>Source path naming no file.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "source path naming no file")]
    public void SourcePathNamingNoFile() => Check(
        root => StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ReplaceFirst(StandardsFixture.ResearchPage, "\"url\": \"https://example.com/sample\"", "\"path\": \"docs/missing-evidence.md\"")),
        ["RECORD_SOURCE"]);

    /// <summary>Decision based on an unknown record.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "decision based on an unknown record")]
    public void DecisionBasedOnAnUnknownRecord() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/intent/decisions/0001-sample-choice.md", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionPage, "\"basedOn\": [\"research/sample-study\"]", "\"basedOn\": [\"research/missing-study\"]"));
        },
        ["RECORD_UNKNOWN_REFERENCE"]);

    /// <summary>Prose citing an unknown record.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "prose citing an unknown record")]
    public void ProseCitingAnUnknownRecord() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "The boundary gives reviewers one testable result.", "The boundary gives reviewers one testable result, as `research/missing-study` shows."));
        },
        ["RECORD_UNKNOWN_REFERENCE"]);

    /// <summary>Approved decision cited by no provision.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "approved decision cited by no provision")]
    public void ApprovedDecisionCitedByNoProvision() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/intent/decisions/0001-sample-choice.md", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionPage, "\"status\": \"draft\"", "\"status\": \"approved\""));
        },
        ["RECORD_DECISION_UNCITED"]);

    /// <summary>Approved decision cited by a provision.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "approved decision cited by a provision")]
    public void ApprovedDecisionCitedByAProvision() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/intent/decisions/0001-sample-choice.md", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionPage, "\"status\": \"draft\"", "\"status\": \"approved\""));
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "The boundary gives reviewers one testable result.", "The boundary gives reviewers one testable result. Settled by `decision/0001-sample-choice`."));
        },
        []);

    /// <summary>Research carrying an invented section.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "research carrying an invented section")]
    public void ResearchCarryingAnInventedSection() => Check(
        root => StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ReplaceFirst(StandardsFixture.ResearchPage, "## Notes on Sources", "## Conclusion\n\nAn invented section.\n\n## Notes on Sources")),
        ["PAGE_UNKNOWN_SECTION"]);

    /// <summary>Decision carrying an invented section.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "decision carrying an invented section")]
    public void DecisionCarryingAnInventedSection() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchPage);
            StandardsFixture.Write(root, "docs/intent/decisions/0001-sample-choice.md", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionPage, "## Consequences", "## Verdict\n\nAn invented section.\n\n## Consequences"));
        },
        ["PAGE_UNKNOWN_SECTION"]);
}
