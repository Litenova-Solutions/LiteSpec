using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that a nested use-case reference to another module needs a declared dependency.</summary>
public sealed class UseCaseModuleReferenceCheckTests
{
    /// <summary>An input naming another module's record without dependsOn reports.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void InputToAnotherModuleWithoutDependencyReports()
    {
        var model = ModelWith("""{"kind": "command", "input": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine"}]}""", []);

        var finding = Assert.Single(Findings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("use-case/lending.record-repayment", finding.Subject);
        Assert.Contains("'aggregate/finance.overdue-fine' of module 'finance'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A steps entry naming another module's rule without dependsOn reports.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void StepCitingAnotherModuleWithoutDependencyReports()
    {
        var model = ModelWith("""{"kind": "command", "steps": [{"statement": "Record the fine.", "cites": ["rule/finance.fine-is-due"]}]}""", []);

        Assert.Single(Findings(model));
    }

    /// <summary>An output naming another module's record without dependsOn reports.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void OutputToAnotherModuleWithoutDependencyReports()
    {
        var model = ModelWith("""{"kind": "query", "output": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine"}]}""", []);

        Assert.Single(Findings(model));
    }

    /// <summary>An input held opaque that names another module's aggregate without dependsOn passes.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void OpaqueInputWithoutDependencyPasses()
    {
        var model = ModelWith("""{"kind": "command", "input": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine", "held": "opaque"}]}""", []);

        Assert.Empty(Findings(model));
    }

    /// <summary>An output held opaque that names another module's aggregate without dependsOn passes.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void OpaqueOutputWithoutDependencyPasses()
    {
        var model = ModelWith("""{"kind": "command", "output": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine", "held": "opaque"}]}""", []);

        Assert.Empty(Findings(model));
    }

    /// <summary>An opaque input still reports when the use case reads the named aggregate.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void OpaqueInputWithReadOfAggregateReports()
    {
        var model = ModelWith("""{"kind": "command", "reads": ["aggregate/finance.overdue-fine"], "input": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine", "held": "opaque"}]}""", []);

        var finding = Assert.Single(Findings(model));
        Assert.Contains("in 'input'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>An opaque input still reports when a step cites the named aggregate.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void OpaqueInputWithStepCitingAggregateReports()
    {
        var model = ModelWith("""{"kind": "command", "input": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine", "held": "opaque"}], "steps": [{"statement": "Record the fine.", "cites": ["aggregate/finance.overdue-fine"]}]}""", []);

        var finding = Assert.Single(Findings(model));
        Assert.Contains("in 'input'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>The same references pass once the module declares the dependency.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void ReferencesWithDeclaredDependencyPass()
    {
        var model = ModelWith(
            """
            {"kind": "command", "input": [{"name": "fine", "type": "id", "identifies": "aggregate/finance.overdue-fine"}], "steps": [{"statement": "Record the fine.", "cites": ["rule/finance.fine-is-due"]}]}
            """,
            ["module/finance"]);

        Assert.Empty(Findings(model));
    }

    /// <summary>A reference to a record of the use case's own module passes.</summary>
    /// <proves>litespec/core/model#declare-cross-module-references-in-depends-on</proves>
    [Fact]
    public void ReferenceToOwnModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.record-repayment", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "record-repayment", "record-repayment.yaml"), """{"kind": "command", "input": [{"name": "loan", "type": "id", "identifies": "aggregate/lending.loan"}]}"""));

        Assert.Empty(Findings(model));
    }

    /// <summary>A model with two modules and one use case naming a finance record.</summary>
    /// <param name="useCase">The use-case record fields as JSON.</param>
    /// <param name="dependsOn">The dependencies the lending module declares.</param>
    /// <returns>The hand-built project model.</returns>
    static ProjectModel ModelWith(string useCase, string[] dependsOn)
    {
        var temp = TestCli.Temp("checks");
        var depends = string.Join(", ", dependsOn.Select(name => "\"" + name + "\""));

        return TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), "{\"dependsOn\": [" + depends + "]}"),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/finance.overdue-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "aggregates", "fines", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/finance.fine-is-due", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "rules", "fine-is-due", "fine-is-due.yaml"), """{"kind": "invariant", "statement": "A fine is due.", "governs": []}"""),
            TestModel.Record("use-case", "use-case/lending.record-repayment", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "record-repayment", "record-repayment.yaml"), useCase));
    }

    /// <summary>The module-reference findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is module-reference-undeclared.</returns>
    static List<Finding> Findings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "module-reference-undeclared").ToList();
}
