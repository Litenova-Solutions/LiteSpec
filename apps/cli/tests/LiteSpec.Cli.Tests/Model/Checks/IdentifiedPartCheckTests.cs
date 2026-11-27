using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves an identifies naming an aggregate part resolves to a declared identified part.</summary>
public sealed class IdentifiedPartCheckTests
{
    /// <summary>Identifies Naming A Declared Identified Part Passes.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdentifiesNamingADeclaredIdentifiedPartPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", Path.Combine(temp.Path, "change.yaml"), """{"kind": "command", "steps": [{"statement": "The reader changes one line of the loan."}], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "aggregate/lending.loan#line-item", "required": true}]}"""));

        Assert.False(Reports(model, "reference-unknown", "#line-item"));
    }

    /// <summary>Identifies Naming A Missing Part Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdentifiesNamingAMissingPartReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", Path.Combine(temp.Path, "change.yaml"), """{"kind": "command", "steps": [{"statement": "The reader changes one line of the loan."}], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "aggregate/lending.loan#no-such-part", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names part 'no-such-part' the aggregate does not declare"));
    }

    /// <summary>Identifies Naming A Part Without Identity Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdentifiesNamingAPartWithoutIdentityReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item"}]}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", Path.Combine(temp.Path, "change.yaml"), """{"kind": "command", "steps": [{"statement": "The reader changes one line of the loan."}], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "aggregate/lending.loan#line-item", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names part 'line-item' the aggregate declares without identity"));
    }

    /// <summary>Identifies Naming A Part Of No Aggregate Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdentifiesNamingAPartOfNoAggregateReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person"}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", Path.Combine(temp.Path, "change.yaml"), """{"kind": "command", "steps": [{"statement": "The reader changes one line of the loan."}], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "actor/reader#line-item", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names a part of 'actor/reader', which is no aggregate"));
    }

    /// <summary>Aggregate Declaring One Part Name Twice Reports.</summary>
    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void AggregateDeclaringOnePartNameTwiceReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}, {"name": "line-item"}]}"""));

        Assert.True(Reports(model, "record-invalid", "declares part 'line-item' more than once"));
    }

    /// <summary>Command Taking A Part Identifier Returned Through Same Entry Point Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingAPartIdentifierReturnedThroughSameEntryPointPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", UseCaseFile(temp.Path, "change-line.yaml"), """{"kind": "command", "via": ["entry-point/library-app"], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "aggregate/lending.loan#line-item", "required": true, "origin": "caller", "classification": "internal"}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "via": ["entry-point/library-app"], "outputs": {"include": {"loan": {"fields": [{"name": "lineItemId", "meaning": "The read line.", "type": "id", "identifies": "aggregate/lending.loan#line-item"}]}}}}"""));

        Assert.False(Reports(model, "caller-identifier-unreachable", "change-line"));
        Assert.False(Reports(model, "reference-unknown", "#line-item"));
    }

    /// <summary>Command Taking A Part Identifier With No Returning Query Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingAPartIdentifierWithNoReturningQueryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", UseCaseFile(temp.Path, "change-line.yaml"), """{"kind": "command", "via": ["entry-point/library-app"], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "aggregate/lending.loan#line-item", "required": true, "origin": "caller", "classification": "internal"}]}"""));

        Assert.True(Reports(model, "caller-identifier-unreachable", "aggregate/lending.loan#line-item"));
        Assert.False(Reports(model, "reference-unknown", "#line-item"));
    }

    /// <summary>Command Taking A Part Identifier Returned Only As The Whole Aggregate Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingAPartIdentifierReturnedOnlyAsTheWholeAggregateReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.change-line", UseCaseFile(temp.Path, "change-line.yaml"), """{"kind": "command", "via": ["entry-point/library-app"], "inputs": [{"name": "line", "meaning": "The line to change.", "type": "id", "identifies": "aggregate/lending.loan#line-item", "required": true, "origin": "caller", "classification": "internal"}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "via": ["entry-point/library-app"], "outputs": {"include": {"loan": {"fields": [{"name": "loanId", "meaning": "The read loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""));

        Assert.True(Reports(model, "caller-identifier-unreachable", "aggregate/lending.loan#line-item"));
    }

    /// <summary>One use case file in the lending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    static string UseCaseFile(string root, string file) =>
        Path.Combine(root, ".litespec", "behavior", "modules", "lending", file);

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <param name="code">The finding code to look for.</param>
    /// <param name="text">The text the finding message must contain.</param>
    /// <returns>True when a finding carries the code and the text.</returns>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
