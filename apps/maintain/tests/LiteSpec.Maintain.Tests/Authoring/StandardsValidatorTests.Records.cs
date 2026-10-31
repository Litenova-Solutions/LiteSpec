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
            StandardsFixture.WriteResearch(root);
            StandardsFixture.WriteDecision(root, "docs/intent/decisions/0001-sample-choice");
        },
        []);

    /// <summary>Implementation decision passes.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "implementation decision passes")]
    public void ImplementationDecisionPasses() => Check(
        root =>
        {
            StandardsFixture.WriteResearch(root);
            StandardsFixture.WriteDecision(root, "docs/implementation/decisions/0001-sample-choice");
        },
        []);

    /// <summary>Research record breaking its schema.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "research record breaking its schema")]
    public void ResearchRecordBreakingItsSchema() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{\"researchPage\":{\"type\":\"object\",\"required\":[\"type\",\"id\",\"conclusion\"],\"properties\":{\"type\":{\"const\":\"research\"},\"id\":{\"type\":\"string\"},\"conclusion\":{\"type\":\"string\"}}},\"decisionPage\":{\"type\":\"object\"}}}");
            var record = StandardsFixture.ReplaceFirst(StandardsFixture.ResearchJson, "\"conclusion\": \"The sample shows one finding.\",\n", "");
            StandardsFixture.WriteResearch(root, record);
        },
        ["RECORD_SCHEMA"]);

    /// <summary>Record identifier not matching its file.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "record identifier not matching its file")]
    public void RecordIdentifierNotMatchingItsFile() => Check(
        root => StandardsFixture.WriteResearch(root, StandardsFixture.ReplaceFirst(StandardsFixture.ResearchJson, "\"id\": \"research/sample-study\"", "\"id\": \"research/other-study\"")),
        ["RECORD_ID_MISMATCH"]);

    /// <summary>Source without a read date.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "source without a read date")]
    public void SourceWithoutAReadDate() => Check(
        root => StandardsFixture.WriteResearch(root, StandardsFixture.ReplaceFirst(StandardsFixture.ResearchJson, ",\n      \"readOn\": \"2026-10-30\"", "")),
        ["RECORD_SOURCE"]);

    /// <summary>Source path naming no file.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "source path naming no file")]
    public void SourcePathNamingNoFile() => Check(
        root => StandardsFixture.WriteResearch(root, StandardsFixture.ReplaceFirst(StandardsFixture.ResearchJson, "\"url\": \"https://example.com/sample\"", "\"path\": \"docs/missing-evidence.md\"")),
        ["RECORD_SOURCE"]);

    /// <summary>Decision based on an unknown record.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "decision based on an unknown record")]
    public void DecisionBasedOnAnUnknownRecord() => Check(
        root =>
        {
            StandardsFixture.WriteResearch(root);
            StandardsFixture.WriteDecision(root, "docs/intent/decisions/0001-sample-choice", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionJson, "\"basedOn\": [\"research/sample-study\"]", "\"basedOn\": [\"research/missing-study\"]"));
        },
        ["RECORD_UNKNOWN_REFERENCE"]);

    /// <summary>Prose citing an unknown record.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "prose citing an unknown record")]
    public void ProseCitingAnUnknownRecord() => Check(
        root =>
        {
            StandardsFixture.WriteResearch(root);
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "The boundary gives reviewers one testable result.", "The boundary gives reviewers one testable result, as `research/missing-study` shows."));
        },
        ["RECORD_UNKNOWN_REFERENCE"]);

    /// <summary>Approved decision cited by no provision.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "approved decision cited by no provision")]
    public void ApprovedDecisionCitedByNoProvision() => Check(
        root =>
        {
            StandardsFixture.WriteResearch(root);
            StandardsFixture.WriteDecision(root, "docs/intent/decisions/0001-sample-choice", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionJson, "\"status\": \"draft\"", "\"status\": \"approved\""));
        },
        ["RECORD_DECISION_UNCITED"]);

    /// <summary>Approved decision cited by a provision.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "approved decision cited by a provision")]
    public void ApprovedDecisionCitedByAProvision() => Check(
        root =>
        {
            StandardsFixture.WriteResearch(root);
            StandardsFixture.WriteDecision(root, "docs/intent/decisions/0001-sample-choice", StandardsFixture.ReplaceFirst(StandardsFixture.DecisionJson, "\"status\": \"draft\"", "\"status\": \"approved\""));
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "The boundary gives reviewers one testable result.", "The boundary gives reviewers one testable result. Settled by `decision/0001-sample-choice`."));
        },
        []);

    /// <summary>Research carrying an invented section.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "research carrying an invented section")]
    public void ResearchCarryingAnInventedSection() => Check(
        root => StandardsFixture.WriteResearch(root, explanation: StandardsFixture.ReplaceFirst(StandardsFixture.ResearchExplanation, "## Notes on Sources", "## Conclusion\n\nAn invented section.\n\n## Notes on Sources")),
        ["PAGE_UNKNOWN_SECTION"]);

    /// <summary>Decision carrying an invented section.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "decision carrying an invented section")]
    public void DecisionCarryingAnInventedSection() => Check(
        root =>
        {
            StandardsFixture.WriteResearch(root);
            StandardsFixture.WriteDecision(root, "docs/intent/decisions/0001-sample-choice", explanation: StandardsFixture.ReplaceFirst(StandardsFixture.DecisionExplanation, "## Decision", "## Verdict\n\nAn invented section.\n\n## Decision"));
        },
        ["PAGE_UNKNOWN_SECTION"]);

    /// <summary>Record without an explanation file.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "record without an explanation file")]
    public void RecordWithoutAnExplanationFile() => Check(
        root => StandardsFixture.Write(root, "docs/intent/research/sample-study.yaml", StandardsFixture.Yaml(StandardsFixture.ResearchJson)),
        ["RECORD_EXPLANATION_MISSING"]);

    /// <summary>Explanation without a record file.</summary>
    /// <proves>litespec/core/maintaining#record-the-reasoning-behind-each-release</proves>
    [Fact(DisplayName = "explanation without a record file")]
    public void ExplanationWithoutARecordFile() => Check(
        root => StandardsFixture.Write(root, "docs/intent/research/sample-study.md", StandardsFixture.ResearchExplanation),
        ["RECORD_EXPLANATION_ORPHANED"]);
}
