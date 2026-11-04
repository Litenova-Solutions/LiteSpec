using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the record name-shape checks: template keys, setting units and published event names.</summary>
public sealed class RecordNameCheckTests
{
    /// <summary>Template Key Ending In Its Audience Passes.</summary>
    /// <proves>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</proves>
    [Fact]
    public void Template_Key_Ending_In_Its_Audience_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "notifications", "loan-due.yaml"), """{"template": "loan-due-reader", "addresses": ["actor/reader"]}"""));

        Assert.DoesNotContain("template-key-invalid", TestModel.Codes(model));
    }

    /// <summary>Comma Separated Template Keys Pass With None Skipped.</summary>
    /// <proves>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</proves>
    [Fact]
    public void Comma_Separated_Template_Keys_Pass_With_None_Skipped()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "notifications", "loan-due.yaml"), """{"template": "loan-due-reader, loan-due-staff, none (no message)", "addresses": ["actor/reader"]}"""));

        Assert.DoesNotContain("template-key-invalid", TestModel.Codes(model));
    }

    /// <summary>Template Key Missing Its Audience Reports.</summary>
    /// <proves>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</proves>
    [Fact]
    public void Template_Key_Missing_Its_Audience_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "notifications", "loan-due.yaml"), """{"template": "loan-due", "addresses": ["actor/reader"]}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "template-key-invalid").Message;
        Assert.Contains("loan-due", message, StringComparison.Ordinal);
    }

    /// <summary>Setting With A Unit Passes.</summary>
    /// <proves>litespec/core/use-cases#state-the-unit-of-every-quantity-setting</proves>
    [Fact]
    public void Setting_With_A_Unit_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("setting", "setting/lending.loan-period", Path.Combine(temp.Path, ".litespec", "operations", "limits.yaml"), """{"valueType": "integer", "unit": "days"}"""),
            TestModel.Record("setting", "setting/lending.late-fee", Path.Combine(temp.Path, ".litespec", "operations", "limits.yaml"), """{"valueType": "money"}"""));

        Assert.DoesNotContain("setting-unit-missing", TestModel.Codes(model));
    }

    /// <summary>Quantity Setting Without A Unit Reports.</summary>
    /// <proves>litespec/core/use-cases#state-the-unit-of-every-quantity-setting</proves>
    [Fact]
    public void Quantity_Setting_Without_A_Unit_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("setting", "setting/lending.loan-period", Path.Combine(temp.Path, ".litespec", "operations", "limits.yaml"), """{"valueType": "integer"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "setting-unit-missing").Message;
        Assert.Contains("setting/lending.loan-period", message, StringComparison.Ordinal);
    }

    /// <summary>Outbound Event Named For Its Fact Passes.</summary>
    /// <proves>litespec/core/edges#name-published-events-in-the-domains-words</proves>
    [Fact]
    public void Outbound_Event_Named_For_Its_Fact_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "events", "loan-borrowed.yaml"), """{"direction": "out"}"""),
            TestModel.Record("integration-event", "integration-event/claim-staged", Path.Combine(temp.Path, ".litespec", "behavior", "events", "claim-staged.yaml"), """{"direction": "in"}"""));

        Assert.DoesNotContain("integration-event-name-invalid", TestModel.Codes(model));
    }

    /// <summary>Outbound Event Naming Machinery Reports.</summary>
    /// <proves>litespec/core/edges#name-published-events-in-the-domains-words</proves>
    [Fact]
    public void Outbound_Event_Naming_Machinery_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/refund-staged", Path.Combine(temp.Path, ".litespec", "behavior", "events", "refund-staged.yaml"), """{"direction": "out"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "integration-event-name-invalid").Message;
        Assert.Contains("staged", message, StringComparison.Ordinal);
    }

    static void WriteProject(string root, string json) =>
        File.WriteAllText(Path.Combine(root, ".litespec/project.json"), json);
}
