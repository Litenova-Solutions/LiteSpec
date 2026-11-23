using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;

using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a use case reading knowledge derives the generic rule-not-stated refusal, and only then.</summary>
public sealed class DerivedRefusalTests
{
    /// <summary>An input rule refused as the generic rule-not-stated derives it.</summary>
    /// <proves>litespec/backend/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void InputRuleRefusedAsRuleNotStatedDerives()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "input": [{"name": "branch", "meaning": "The branch lending the copy.", "type": "text", "required": true, "rules": ["rule/loan.limit-per-branch"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.limit-per-branch",
            Path.Combine(temp.Path, "limit.yaml"),
            """{"kind": "precondition", "statement": "A loan takes the limit of its branch.", "refusal": "refusal/library.rule-not-stated"}""");

        Assert.Contains("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase, rule)));
    }

    /// <summary>A taken rule citing a knowledge table derives rule-not-stated.</summary>
    /// <proves>litespec/backend/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void TakenRuleCitingKnowledgeTableDerives()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "steps": [{"statement": "Lending reads the loan limit in force for the branch.", "cites": ["rule/loan.limit-per-branch"]}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.limit-per-branch",
            Path.Combine(temp.Path, "limit.yaml"),
            """{"kind": "precondition", "statement": "A loan reads the limit row in force for the branch from reference-data/lending.loan-limits."}""");
        var table = TestModel.Record(
            "reference-data",
            "reference-data/lending.loan-limits",
            Path.Combine(temp.Path, "limits.yaml"),
            """{"summary": "The loan limit of each branch.", "unit": "loans"}""");
        var area = TestModel.Record(
            "knowledge-area",
            "knowledge-area/lending",
            Path.Combine(temp.Path, "area.yaml"),
            """{"meaning": "The loan rules of each branch."}""");

        Assert.Contains("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase, rule, table, area)));
    }

    /// <summary>A use case reading no knowledge derives no rule-not-stated.</summary>
    /// <proves>litespec/backend/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void UseCaseWithoutKnowledgeReadsDerivesNone()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "query"}""");

        Assert.DoesNotContain("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase)));
    }

    /// <summary>An input rule refused as an ordinary refusal derives no rule-not-stated.</summary>
    /// <proves>litespec/backend/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void InputRuleWithOrdinaryRefusalDerivesNone()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "input": [{"name": "branch", "meaning": "The branch lending the copy.", "type": "text", "required": true, "rules": ["rule/loan.limit-per-branch"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.limit-per-branch",
            Path.Combine(temp.Path, "limit.yaml"),
            """{"kind": "precondition", "statement": "A loan takes the limit of its branch.", "refusal": "refusal/lending.limit-exceeded"}""");

        Assert.DoesNotContain("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase, rule)));
    }

    /// <summary>A taken rule citing a table no knowledge area owns derives no rule-not-stated.</summary>
    /// <proves>litespec/backend/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void TakenRuleCitingModuleTableDerivesNone()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "steps": [{"statement": "Lending reads the loan limit in force for the branch.", "cites": ["rule/loan.limit-per-branch"]}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.limit-per-branch",
            Path.Combine(temp.Path, "limit.yaml"),
            """{"kind": "precondition", "statement": "A loan reads the limit row in force for the branch from reference-data/lending.loan-limits."}""");
        var table = TestModel.Record(
            "reference-data",
            "reference-data/lending.loan-limits",
            Path.Combine(temp.Path, "limits.yaml"),
            """{"summary": "The loan limit of each branch.", "unit": "loans"}""");

        Assert.DoesNotContain("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase, rule, table)));
    }

    private static IReadOnlyDictionary<string, Record> ById(params Record[] records) =>
        records.ToDictionary(record => record.Id, StringComparer.Ordinal);
}
