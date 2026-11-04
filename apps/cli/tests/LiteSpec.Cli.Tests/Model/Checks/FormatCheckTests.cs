using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the format check against hand-built models and files.</summary>
public sealed class FormatCheckTests
{
    /// <summary>A record file outside canonical form reports.</summary>
    /// <proves>litespec/core/model#write-every-record-file-in-canonical-form</proves>
    [Fact]
    public void Record_File_Outside_Canonical_Form_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "loan.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "title: Loan\ntype: aggregate\nid: aggregate/lending.loan\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"kind": "entity"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "format-drift");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Run 'litespec model format'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <summary>A record file in canonical form passes.</summary>
    /// <proves>litespec/core/model#write-every-record-file-in-canonical-form</proves>
    [Fact]
    public void Canonical_Record_File_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "loan.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, RecordFile.Write(JsonNode.Parse("""{"type": "aggregate", "id": "aggregate/lending.loan", "title": "Loan", "kind": "entity"}""")!));
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"kind": "entity"}"""));

        Assert.DoesNotContain("format-drift", TestModel.Codes(model));
    }

    /// <summary>A record file that is not YAML reports no format drift.</summary>
    /// <proves>litespec/core/model#write-every-record-file-in-canonical-form</proves>
    [Fact]
    public void Unparsable_Record_File_Reports_No_Format_Drift()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "loan.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "type: [unclosed\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"kind": "entity"}"""));

        Assert.DoesNotContain("format-drift", TestModel.Codes(model));
    }
}
