using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.RecordShape;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves how the checks treat a vision: its fields, its one home at product level, the links that may name it, and its exemption from reach.</summary>
public sealed class VisionCheckTests
{
    /// <summary>A vision in its product-level folder passes the folder and placement checks.</summary>
    /// <proves>litespec/core/intent#state-each-vision-in-its-fields</proves>
    [Fact]
    public void VisionInItsFolderPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "visions", "contractor-expenses.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Vision(file, """{"summary": "Contractors pay work costs too.", "statement": "Contractors submit expenses as employees do."}"""));

        var findings = new List<Finding>();
        FolderChecks.Run(model, findings);

        Assert.DoesNotContain(findings, found => found.Code is "folder-invalid" or "record-placement-invalid");
    }

    /// <summary>A vision under a module sits outside its one home and reports its placement.</summary>
    /// <proves>litespec/core/intent#state-each-vision-in-its-fields</proves>
    [Fact]
    public void VisionUnderModuleReportsPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "spending", "module.yaml");
        var file = PathAt(temp.Path, ".litespec", "intent", "modules", "spending", "visions", "contractor-expenses.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", moduleFile, """{"dependsOn": []}"""),
            Vision(file, """{"summary": "Contractors pay work costs too.", "statement": "Contractors submit expenses as employees do."}"""));

        var findings = new List<Finding>();
        FolderChecks.Run(model, findings);

        Assert.Contains(findings, found => found.Code == "record-placement-invalid" && found.Message.Contains("vision/contractor-expenses", StringComparison.Ordinal));
    }

    /// <summary>A vision without its statement is an invalid record.</summary>
    /// <proves>litespec/core/intent#state-each-vision-in-its-fields</proves>
    [Fact]
    public void VisionWithoutStatementReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "visions", "contractor-expenses.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Vision(file, """{"summary": "Contractors pay work costs too."}"""));

        Assert.Contains(Checker.Check(model).Findings, found =>
            found.Code == "record-invalid" && found.Message.Contains("statement", StringComparison.Ordinal));
    }

    /// <summary>A vision nothing reaches reports no planned-Intent finding, because it promises nothing.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void VisionReachingNothingReportsNoIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "visions", "contractor-expenses.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Vision(file, """{"summary": "Contractors pay work costs too.", "statement": "Contractors submit expenses as employees do."}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found =>
            found.Code is "intent-unreached" or "goal-mover-missing" && found.Subject == "vision/contractor-expenses");
    }

    /// <summary>A decision and a requirement may name a vision, and both links resolve.</summary>
    /// <proves>litespec/core/intent#cite-the-vision-a-decision-keeps-open</proves>
    [Fact]
    public void DecisionAndRequirementNamingVisionResolve()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Vision(PathAt(temp.Path, ".litespec", "intent", "visions", "contractor-expenses.yaml"),
                """{"summary": "Contractors pay work costs too.", "statement": "Contractors submit expenses as employees do."}"""),
            TestModel.Record("decision", "decision/0001-submitter-as-person", PathAt(temp.Path, ".litespec", "intent", "decisions", "0001-submitter-as-person.yaml"),
                """{"basedOn": ["vision/contractor-expenses"], "decides": ["requirement/approval.submitter-named"]}"""),
            TestModel.Record("requirement", "requirement/approval.submitter-named", PathAt(temp.Path, ".litespec", "intent", "modules", "spending", "features", "approval.yaml"),
                """{"kind": "functional", "statement": "Each expense names the person who submitted it.", "cites": ["vision/contractor-expenses"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found =>
            found.Code is "reference-unknown" or "link-unknown" && found.Message.Contains("vision/contractor-expenses", StringComparison.Ordinal));
    }

    /// <summary>Builds a vision record at the file given.</summary>
    /// <param name="file">The record file path.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The vision record.</returns>
    static Record Vision(string file, string json) =>
        TestModel.Record("vision", "vision/contractor-expenses", file, json);

    /// <summary>The path of a file under the root, with its folders created.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="parts">The path segments below the root.</param>
    /// <returns>The full path.</returns>
    static string PathAt(string root, params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }
}
