using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Handlers;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a derived not-found or rule-not-stated result carries the product refusal of the rule that triggers it, and the checks treat that code as declared.</summary>
public sealed class DerivedRefusalCodeTests
{
    /// <summary>A derived not-found result carries the refusal of the product-level rule its id input takes.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void DerivedNotFoundCarriesProductRuleRefusal()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        WriteProductRule(temp.Path, "unknown-copy", "rule/loan.unknown-copy", "refusal/library.unknown-copy");
        WriteBorrowCopy(temp.Path, "rule/loan.unknown-copy");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var derived = ById(model, "result/borrow-copy.not-found");

        Assert.True(derived.Derived);
        Assert.Equal("refusal/library.unknown-copy", derived.Field("refusal"));
        Assert.Equal("library.unknown-copy", derived.Field("code"));
        Assert.Equal("Not found", derived.Title);
        Assert.Equal("The record the input names does not exist.", derived.Field("meaning"));
    }

    /// <summary>A handler throwing the carried product code reports no undeclared refusal or result.</summary>
    /// <proves>litespec/backend/application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void ProductCodeViaRuleCoversThrownCode()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        WriteProductRule(temp.Path, "unknown-copy", "rule/loan.unknown-copy", "refusal/library.unknown-copy");
        WriteBorrowCopy(temp.Path, "rule/loan.unknown-copy");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        Throw(model, temp.Path, "library.unknown-copy");

        Assert.Empty(RefusalUndeclared(model));
        Assert.Empty(ResultUndeclared(model));
    }

    /// <summary>A product-level rule stating no refusal keeps the generic code on the derived result.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void RuleWithoutRefusalKeepsGenericCode()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        WriteProductRule(temp.Path, "unknown-copy", "rule/loan.unknown-copy", null);
        WriteBorrowCopy(temp.Path, "rule/loan.unknown-copy");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var derived = ById(model, "result/borrow-copy.not-found");

        Assert.True(derived.Derived);
        Assert.Null(derived.Field("refusal"));
        Assert.Equal("not-found", derived.Field("code"));
    }

    /// <summary>A handler throwing another code still reports undeclared refusal and result.</summary>
    /// <proves>litespec/backend/application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void DifferentThrownCodeStillReports()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        WriteProductRule(temp.Path, "unknown-copy", "rule/loan.unknown-copy", "refusal/library.unknown-copy");
        WriteBorrowCopy(temp.Path, "rule/loan.unknown-copy");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        Throw(model, temp.Path, "lending.other-refusal");

        var refusal = Assert.Single(RefusalUndeclared(model));
        Assert.Contains("lending.other-refusal", refusal.Message);
        var result = Assert.Single(ResultUndeclared(model));
        Assert.Contains("lending.other-refusal", result.Message);
    }

    /// <summary>A derived rule-not-stated result carries the refusal of the product-level rule its input takes.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void DerivedRuleNotStatedCarriesProductRuleRefusal()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        WriteProductRule(temp.Path, "branch-limit", "rule/loan.branch-limit", "refusal/library.rule-not-stated");
        Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/copies/use-cases/borrow-copy/borrow-copy.yaml", """
            type: use-case
            id: use-case/lending.borrow-copy
            title: Borrow copy
            kind: command
            input:
              - name: branch
                meaning: The branch lending the copy.
                type: text
                required: true
                rules:
                  - rule/loan.branch-limit
                origin: caller
                classification: internal
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var derived = ById(model, "result/borrow-copy.rule-not-stated");

        Assert.True(derived.Derived);
        Assert.Equal("refusal/library.rule-not-stated", derived.Field("refusal"));
        Assert.Equal("library.rule-not-stated", derived.Field("code"));

        Throw(model, temp.Path, "library.rule-not-stated");

        Assert.Empty(RefusalUndeclared(model));
        Assert.Empty(ResultUndeclared(model));
    }

    /// <summary>A module rule keeps its written result: the derived result keeps the generic code and the module code still reports.</summary>
    /// <proves>litespec/backend/application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void ModuleRuleRefusalStaysUndeclared()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/copies/rules/copy-closed/copy-closed.yaml", """
            type: rule
            id: rule/loan.copy-closed
            title: Copy closed
            kind: precondition
            statement: A reader borrows a copy only while it circulates.
            refusal: refusal/lending.copy-closed
            """);
        WriteBorrowCopy(temp.Path, "rule/loan.copy-closed");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var derived = ById(model, "result/borrow-copy.not-found");

        Assert.True(derived.Derived);
        Assert.Null(derived.Field("refusal"));
        Assert.Equal("not-found", derived.Field("code"));

        Throw(model, temp.Path, "lending.copy-closed");

        Assert.Single(RefusalUndeclared(model));
        Assert.Single(ResultUndeclared(model));
    }

    /// <summary>A handler throwing the generic code still passes while the derived result carries the product code.</summary>
    /// <proves>litespec/backend/application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void GenericCodeStillCoversWhenDerivedCarriesProduct()
    {
        using var temp = TestCli.Temp("derived-codes");
        WriteProject(temp.Path);
        WriteProductRule(temp.Path, "unknown-copy", "rule/loan.unknown-copy", "refusal/library.unknown-copy");
        WriteBorrowCopy(temp.Path, "rule/loan.unknown-copy");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        Throw(model, temp.Path, "not-found");

        Assert.Empty(RefusalUndeclared(model));
    }

    /// <summary>Runs the refusal declaration check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The refusal-undeclared findings.</returns>
    static List<Finding> RefusalUndeclared(ProjectModel model)
    {
        var findings = new List<Finding>();
        RefusalDeclarationChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "refusal-undeclared").ToList();
    }

    /// <summary>Runs the result check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The result-undeclared findings.</returns>
    static List<Finding> ResultUndeclared(ProjectModel model)
    {
        var findings = new List<Finding>();
        ResultChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "result-undeclared").ToList();
    }

    /// <summary>Adds a handler throwing the code to the model.</summary>
    /// <param name="model">The project model the handler row is added to.</param>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="code">The refusal code the handler produces.</param>
    static void Throw(ProjectModel model, string root, string code)
    {
        var file = Path.Combine(root, "BorrowCopyCommandHandler.cs");
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Refusals: [new CodeRefusal(code, file, 9)]));
    }

    /// <summary>The record holding the id, failing the test when no record holds it.</summary>
    /// <param name="model">The project model to read.</param>
    /// <param name="id">The record id.</param>
    /// <returns>The record holding the id.</returns>
    static LiteSpec.Cli.Model.Records.Record ById(ProjectModel model, string id) =>
        model.ById.TryGetValue(id, out var record)
            ? record
            : throw new InvalidOperationException($"No record holds '{id}'.");

    /// <summary>Writes the use case with an id input taking the rule.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="rule">The rule identifier the id input takes.</param>
    static void WriteBorrowCopy(string root, string rule) =>
        Write(root, ".litespec/behavior/modules/lending/aggregates/copies/use-cases/borrow-copy/borrow-copy.yaml", $$"""
            type: use-case
            id: use-case/lending.borrow-copy
            title: Borrow copy
            kind: command
            input:
              - name: copyId
                meaning: The copy the reader borrows.
                type: id
                identifies: aggregate/lending.copy
                required: true
                rules:
                  - {{rule}}
                origin: caller
                classification: internal
            """);

    /// <summary>Writes the product-level rule, with its refusal when one is named.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="name">The rule folder name.</param>
    /// <param name="id">The rule identifier.</param>
    /// <param name="refusal">The refusal identifier the rule states, or null for none.</param>
    static void WriteProductRule(string root, string name, string id, string? refusal) =>
        Write(root, $".litespec/behavior/rules/{name}/{name}.yaml", $$"""
            type: rule
            id: {{id}}
            title: Product rule
            kind: precondition
            statement: A lookup for a named record that is absent is refused.
            {{(refusal is null ? "" : $"refusal: {refusal}")}}
            """);

    static void WriteProject(string root) =>
        Write(root, ".litespec/project.yaml", """{"shape": "client-only", "stacks": []}""");

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.Replace("\r\n", "\n"));
    }
}
