using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a carried field names an existing type its aggregate declares: an object type or row type naming a part resolves to a declared part, and a code type name passes.</summary>
public sealed class DomainEventCarryCheckTests
{
    /// <summary>Carry Existing Type Naming A Declared Part Without Identity Passes.</summary>
    /// <proves>litespec/core/domain-model#state-each-event-with-meaning-and-carries</proves>
    [Fact]
    public void CarryExistingTypeNamingADeclaredPartWithoutIdentityPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item"}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"carries": [{"name": "line", "type": "aggregate/lending.loan#line-item"}]}"""));

        Assert.False(Reports(model, "reference-unknown", "holds carries field"));
    }

    /// <summary>Carry Existing Type Naming A Missing Part Reports.</summary>
    /// <proves>litespec/core/domain-model#state-each-event-with-meaning-and-carries</proves>
    [Fact]
    public void CarryExistingTypeNamingAMissingPartReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"carries": [{"name": "line", "type": "aggregate/lending.loan#no-such-part"}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names part 'no-such-part' the aggregate does not declare"));
    }

    /// <summary>Carry Existing Type Naming A Part Of No Record Reports.</summary>
    /// <proves>litespec/core/domain-model#state-each-event-with-meaning-and-carries</proves>
    [Fact]
    public void CarryExistingTypeNamingAPartOfNoRecordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"carries": [{"name": "line", "type": "aggregate/lending.no-such-loan#line-item"}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names a part of 'aggregate/lending.no-such-loan', which no record holds"));
    }

    /// <summary>Carry Existing Type Naming A Part Of No Aggregate Reports.</summary>
    /// <proves>litespec/core/domain-model#state-each-event-with-meaning-and-carries</proves>
    [Fact]
    public void CarryExistingTypeNamingAPartOfNoAggregateReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person"}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"carries": [{"name": "line", "type": "actor/reader#line-item"}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names a part of 'actor/reader', which is no aggregate"));
    }

    /// <summary>Carry Row Type Naming A Missing Part Reports.</summary>
    /// <proves>litespec/core/domain-model#state-each-event-with-meaning-and-carries</proves>
    [Fact]
    public void CarryRowTypeNamingAMissingPartReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "line-item", "identity": true}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"carries": [{"name": "fines", "type": "list", "rowShape": {"fields": [{"name": "amount", "type": "decimal", "scale": 2, "required": true, "meaning": "The fine amount."}, {"name": "note", "type": "text", "required": false, "meaning": "The fine note."}]}, "rowType": "aggregate/lending.loan#no-such-part"}]}"""));

        Assert.True(Reports(model, "reference-unknown", "with rowType 'aggregate/lending.loan#no-such-part'"));
    }

    /// <summary>Carry With A Code Type Name And An Optional Flag Passes.</summary>
    /// <proves>litespec/core/domain-model#state-each-event-with-meaning-and-carries</proves>
    [Fact]
    public void CarryWithACodeTypeNameAndAnOptionalFlagPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"carries": [{"name": "slip", "type": "DueSlip", "required": false}, {"name": "note", "type": "text", "required": false}]}"""));

        Assert.False(Reports(model, "reference-unknown", "holds carries field"));
        Assert.False(Reports(model, "record-invalid", "holds carries field"));
    }

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <param name="code">The finding code to look for.</param>
    /// <param name="text">The text the finding message must contain.</param>
    /// <returns>True when a finding carries the code and the text.</returns>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
