using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the refusal-not-thrown check reads a code the project composes from a key at run time.</summary>
public sealed class ComposedRefusalTests
{
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Composed_Code_Whose_Key_Is_In_Code_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = BarcodeModel(temp.Path);
        model.Code.KeyLiterals.Add("copy-barcode");

        Assert.DoesNotContain("refusal-not-thrown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Composed_Code_Whose_Key_No_Code_Holds_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = BarcodeModel(temp.Path);
        model.Code.KeyLiterals.Add("copy-shelfmark");

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "refusal-not-thrown").Message;
        Assert.Contains("uniqueness.copy-barcode-taken", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Code_Outside_Every_Template_Still_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = BarcodeModel(temp.Path, "lending.copy-barcode-taken");
        model.Code.KeyLiterals.Add("copy-barcode");

        Assert.Contains("refusal-not-thrown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Theory]
    [InlineData("uniqueness.{key}-taken", true)]
    [InlineData("{key}-taken", false)]
    [InlineData("uniqueness.taken", false)]
    [InlineData("uniqueness.{key}-{key}", false)]
    public void Template_Needs_One_Key_After_A_Prefix(string template, bool wellFormed)
    {
        Assert.Equal(wellFormed, RefusalTemplate.IsWellFormed(template));
    }

    static ProjectModel BarcodeModel(string root, string code = "uniqueness.copy-barcode-taken")
    {
        File.WriteAllText(Path.Combine(root, "litespec.project.json"), """{"refusals": {"composed": ["uniqueness.{key}-taken"]}}""");
        var file = Path.Combine(root, "register-copy.md");

        return TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("use-case", "use-case/lending.register-copy", file, """{"status": "reviewed", "kind": "command"}"""),
            TestModel.Record("result", "result/register-copy.barcode-taken", file, "{\"status\": \"reviewed\", \"outcome\": \"refusal\", \"code\": \"" + code + "\", \"meaning\": \"Another copy holds the barcode.\"}"));
    }
}
