using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves every stated value meaning names a value its choices list.</summary>
public sealed class ValueMeaningsTests
{
    /// <summary>Table meanings naming listed choices pass.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TableMeaningsNamingListedChoicesPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "membership-kind", """{"summary": "The membership kinds.", "choices": ["child", "adult"], "valueMeanings": {"child": "A reader below twelve.", "adult": "A reader of twelve or older."}, "rows": []}"""));

        Assert.DoesNotContain(MeaningsFindings(model), found => found.Message.Contains("value meaning"));
    }

    /// <summary>A table meaning naming no listed choice reports record-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TableMeaningNamingNoChoiceReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "membership-kind", """{"summary": "The membership kinds.", "choices": ["child", "adult"], "valueMeanings": {"guest": "A reader passing through."}, "rows": []}"""));

        var finding = Assert.Single(MeaningsFindings(model));
        Assert.Equal("reference-data/spending.membership-kind", finding.Subject);
        Assert.Contains("holds a value meaning for 'guest', which its choices do not list", finding.Message);
    }

    /// <summary>A table meaning that is not a sentence reports record-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TableMeaningWithoutASentenceReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "membership-kind", """{"summary": "The membership kinds.", "choices": ["child"], "valueMeanings": {"child": 5}, "rows": []}"""));

        var finding = Assert.Single(MeaningsFindings(model));
        Assert.Contains("holds a value meaning for 'child' that is not a sentence", finding.Message);
    }

    /// <summary>A table without meanings passes.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TableWithoutMeaningsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "membership-kind", """{"summary": "The membership kinds.", "choices": ["child", "adult"], "rows": []}"""));

        Assert.DoesNotContain(MeaningsFindings(model), found => found.Message.Contains("value meaning"));
    }

    /// <summary>A numeric choice matches the meaning keyed by its written text.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NumericChoiceMatchesItsWrittenKey()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "choices": [5], "valueMeanings": {"5": "Five business days."}, "rows": []}"""));

        Assert.DoesNotContain(MeaningsFindings(model), found => found.Message.Contains("value meaning"));
    }

    /// <summary>Setting meanings naming listed choices pass.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SettingMeaningsNamingListedChoicesPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Setting(temp.Path, "receipt-mode", """{"valueType": "choice", "setBy": "operator", "configurationPath": "Spending:ReceiptMode", "choices": ["scan", "upload"], "valueMeanings": {"scan": "The camera scans it.", "upload": "A file is uploaded."}}"""));

        Assert.DoesNotContain(MeaningsFindings(model), found => found.Message.Contains("value meaning"));
    }

    /// <summary>A setting meaning naming no listed choice reports record-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SettingMeaningNamingNoChoiceReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Setting(temp.Path, "receipt-mode", """{"valueType": "choice", "setBy": "operator", "configurationPath": "Spending:ReceiptMode", "choices": ["scan", "upload"], "valueMeanings": {"fax": "A fax arrives."}}"""));

        var finding = Assert.Single(MeaningsFindings(model));
        Assert.Equal("setting/spending.receipt-mode", finding.Subject);
        Assert.Contains("holds a value meaning for 'fax', which its choices do not list", finding.Message);
    }

    /// <summary>The record-invalid findings about value meanings.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings about value meanings.</returns>
    static List<Finding> MeaningsFindings(ProjectModel model) =>
        Checker.Check(model).Findings
            .Where(found => found.Code == "record-invalid" && found.Message.Contains("value meaning"))
            .ToList();

    /// <summary>One reference-data table of the spending module.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="name">The table name.</param>
    /// <param name="json">The table fields as JSON.</param>
    /// <returns>The reference-data record.</returns>
    static Record Table(string root, string name, string json) => TestModel.Record(
        "reference-data",
        $"reference-data/spending.{name}",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "reference-data", name, name + ".yaml"),
        json);

    /// <summary>One setting of the spending module.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="name">The setting name.</param>
    /// <param name="json">The setting fields as JSON.</param>
    /// <returns>The setting record.</returns>
    static Record Setting(string root, string name, string json) => TestModel.Record(
        "setting",
        $"setting/spending.{name}",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "settings", name, name + ".yaml"),
        json);
}
