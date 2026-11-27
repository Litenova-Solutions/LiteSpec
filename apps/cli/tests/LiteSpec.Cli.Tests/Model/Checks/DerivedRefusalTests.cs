using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;

using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the generic refusals a use case derives, and the product refusals its derived results carry.</summary>
public sealed class DerivedRefusalTests
{
    /// <summary>An input rule refused as the generic rule-not-stated derives it.</summary>
    /// <proves>litespec/core/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void InputRuleRefusedAsRuleNotStatedDerives()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "branch", "meaning": "The branch lending the copy.", "type": "text", "required": true, "rules": ["rule/loan.limit-per-branch"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.limit-per-branch",
            Path.Combine(temp.Path, "limit.yaml"),
            """{"kind": "precondition", "statement": "A loan takes the limit of its branch.", "refusal": "refusal/library.rule-not-stated"}""");

        Assert.Contains("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase, rule)));
    }

    /// <summary>A taken rule citing a knowledge table derives rule-not-stated.</summary>
    /// <proves>litespec/core/knowledge#refuse-a-missing-rule-with-a-reason</proves>
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
    /// <proves>litespec/core/knowledge#refuse-a-missing-rule-with-a-reason</proves>
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
    /// <proves>litespec/core/knowledge#refuse-a-missing-rule-with-a-reason</proves>
    [Fact]
    public void InputRuleWithOrdinaryRefusalDerivesNone()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "branch", "meaning": "The branch lending the copy.", "type": "text", "required": true, "rules": ["rule/loan.limit-per-branch"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.limit-per-branch",
            Path.Combine(temp.Path, "limit.yaml"),
            """{"kind": "precondition", "statement": "A loan takes the limit of its branch.", "refusal": "refusal/lending.limit-exceeded"}""");

        Assert.DoesNotContain("rule-not-stated", RefusalSetReader.DeriveFor(useCase, ById(useCase, rule)));
    }

    /// <summary>A taken rule citing a table no knowledge area owns derives no rule-not-stated.</summary>
    /// <proves>litespec/core/knowledge#refuse-a-missing-rule-with-a-reason</proves>
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

    /// <summary>A product-level rule taken on an id input is carried by the derived not-found result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void ProductRuleRefusalCarriedForNotFound()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "copyId", "meaning": "The copy the reader borrows.", "type": "id", "identifies": "aggregate/lending.copy", "required": true, "rules": ["rule/loan.unknown-copy"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.unknown-copy",
            Path.Combine(temp.Path, "unknown.yaml"),
            """{"kind": "precondition", "statement": "A lookup for a copy that is absent is refused.", "refusal": "refusal/library.unknown-copy"}""");

        Assert.Equal("refusal/library.unknown-copy", RefusalSetReader.ProductRefusalFor(useCase, "not-found", ById(useCase, rule)));
    }

    /// <summary>A module rule taken on an id input is not carried by the derived not-found result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void ModuleRuleRefusalIgnoredForNotFound()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "copyId", "meaning": "The copy the reader borrows.", "type": "id", "identifies": "aggregate/lending.copy", "required": true, "rules": ["rule/loan.copy-closed"], "origin": "caller", "classification": "internal"}]}""");
        var rule = WithModule(
            TestModel.Record(
                "rule",
                "rule/loan.copy-closed",
                Path.Combine(temp.Path, "closed.yaml"),
                """{"kind": "precondition", "statement": "A reader borrows a copy only while it circulates.", "refusal": "refusal/lending.copy-closed"}"""),
            "lending");

        Assert.Null(RefusalSetReader.ProductRefusalFor(useCase, "not-found", ById(useCase, rule)));
    }

    /// <summary>A product-level rule taken on a text input is carried by the derived not-found result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void TextInputRuleCarriedForNotFound()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "branch", "meaning": "The branch lending the copy.", "type": "text", "required": true, "rules": ["rule/loan.unknown-copy"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.unknown-copy",
            Path.Combine(temp.Path, "unknown.yaml"),
            """{"kind": "precondition", "statement": "A lookup for a copy that is absent is refused.", "refusal": "refusal/library.unknown-copy"}""");

        Assert.Equal("refusal/library.unknown-copy", RefusalSetReader.ProductRefusalFor(useCase, "not-found", ById(useCase, rule)));
    }

    /// <summary>A rule stating no refusal is carried by no derived result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void RuleWithoutRefusalCarriesNone()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "copyId", "meaning": "The copy the reader borrows.", "type": "id", "identifies": "aggregate/lending.copy", "required": true, "rules": ["rule/loan.unknown-copy"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.unknown-copy",
            Path.Combine(temp.Path, "unknown.yaml"),
            """{"kind": "precondition", "statement": "A lookup for a copy that is absent is refused."}""");

        Assert.Null(RefusalSetReader.ProductRefusalFor(useCase, "not-found", ById(useCase, rule)));
    }

    /// <summary>A module rule naming the generic rule-not-stated refusal is carried by the derived result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void NameMatchModuleRuleCarriedForRuleNotStated()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "branch", "meaning": "The branch lending the copy.", "type": "text", "required": true, "rules": ["rule/loan.limit-per-branch"], "origin": "caller", "classification": "internal"}]}""");
        var rule = WithModule(
            TestModel.Record(
                "rule",
                "rule/loan.limit-per-branch",
                Path.Combine(temp.Path, "limit.yaml"),
                """{"kind": "precondition", "statement": "A loan takes the limit of its branch.", "refusal": "refusal/lending.rule-not-stated"}"""),
            "lending");

        Assert.Equal("refusal/lending.rule-not-stated", RefusalSetReader.ProductRefusalFor(useCase, "rule-not-stated", ById(useCase, rule)));
    }

    /// <summary>A product-level rule citing a knowledge table is carried by the derived rule-not-stated result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void KnowledgeTableProductRuleCarriedForRuleNotStated()
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
            """{"kind": "precondition", "statement": "A loan reads the limit row in force for the branch from reference-data/lending.loan-limits.", "refusal": "refusal/library.rule-not-stated"}""");
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

        Assert.Equal("refusal/library.rule-not-stated", RefusalSetReader.ProductRefusalFor(useCase, "rule-not-stated", ById(useCase, rule, table, area)));
    }

    /// <summary>A generic refusal no rule triggers carries no product refusal.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void OtherGenericCarriesNone()
    {
        using var temp = TestCli.Temp("derived");
        var useCase = TestModel.Record(
            "use-case",
            "use-case/lending.borrow-copy",
            Path.Combine(temp.Path, "copy.yaml"),
            """{"kind": "command", "inputs": [{"name": "copyId", "meaning": "The copy the reader borrows.", "type": "id", "identifies": "aggregate/lending.copy", "required": true, "rules": ["rule/loan.unknown-copy"], "origin": "caller", "classification": "internal"}]}""");
        var rule = TestModel.Record(
            "rule",
            "rule/loan.unknown-copy",
            Path.Combine(temp.Path, "unknown.yaml"),
            """{"kind": "precondition", "statement": "A lookup for a copy that is absent is refused.", "refusal": "refusal/library.unknown-copy"}""");

        Assert.Null(RefusalSetReader.ProductRefusalFor(useCase, "forbidden", ById(useCase, rule)));
        Assert.Null(RefusalSetReader.ProductRefusalFor(useCase, "invalid-input", ById(useCase, rule)));
        Assert.Null(RefusalSetReader.ProductRefusalFor(useCase, "conflict", ById(useCase, rule)));
    }

    private static IReadOnlyDictionary<string, Record> ById(params Record[] records) =>
        records.ToDictionary(record => record.Id, StringComparer.Ordinal);

    /// <summary>The record with its module set, so a rule reads as standing under that module.</summary>
    /// <param name="record">The record to place.</param>
    /// <param name="module">The module the record stands under.</param>
    /// <returns>The record with its module set.</returns>
    private static Record WithModule(Record record, string? module) => new()
    {
        Type = record.Type,
        Id = record.Id,
        Folder = record.Folder,
        Module = module,
        Title = record.Title,
        Derived = record.Derived,
        File = record.File,
        Line = record.Line,
        ExplanationFile = record.ExplanationFile,
        Explanation = record.Explanation,
        IsEntry = record.IsEntry,
        PageId = record.PageId,
        Raw = record.Raw,
    };
}
