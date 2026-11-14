using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the parse cascade guard: a child of a module whose record file could not be read reports no missing parent, at tier G.</summary>
public sealed class ParseCascadeTests
{
    /// <summary>Use Case With Unreadable Module Stays Silent.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void UseCaseWithUnreadableModuleStaysSilent()
    {
        using var temp = TestCli.Temp("checks");
        var moduleFile = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""));
        var scan = RecordFiles.Scan(moduleFile, "type: module\ntitle: Lending: renewals\ndependsOn: []\n");
        Assert.NotEmpty(scan.Problems);
        model.Problems.AddRange(scan.Problems);

        var findings = Checker.Check(model).Findings;
        Assert.Contains(findings, found => found.Code == "record-invalid" && found.Path == moduleFile);
        Assert.DoesNotContain(findings, found => found.Code == "record-parent-missing");
    }

    /// <summary>Rule With Unreadable Module Stays Silent.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void RuleWithUnreadableModuleStaysSilent()
    {
        using var temp = TestCli.Temp("checks");
        var moduleFile = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/lending.copy-barcode-unique", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "rules", "barcodes.yaml"), """{"kind": "invariant", "statement": "A copy barcode is unique.", "governs": ["aggregate/lending.loan"]}"""));
        var scan = RecordFiles.Scan(moduleFile, "type: module\ntitle: Lending: renewals\ndependsOn: []\n");
        Assert.NotEmpty(scan.Problems);
        model.Problems.AddRange(scan.Problems);

        var findings = Checker.Check(model).Findings;
        Assert.Contains(findings, found => found.Code == "record-invalid" && found.Path == moduleFile);
        Assert.DoesNotContain(findings, found => found.Code == "record-parent-missing");
    }

    /// <summary>Child Of Readable Missing Module Still Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ChildOfReadableMissingModuleStillReports()
    {
        using var temp = TestCli.Temp("checks");
        var moduleFile = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/archive.return-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "aggregates", "loans", "return-copy.yaml"), """{"kind": "command"}"""));
        var scan = RecordFiles.Scan(moduleFile, "type: module\ntitle: Lending: renewals\ndependsOn: []\n");
        Assert.NotEmpty(scan.Problems);
        model.Problems.AddRange(scan.Problems);

        var findings = Checker.Check(model).Findings;
        var finding = Assert.Single(findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/archive.return-copy' has no parent and its home 'archive' names no module.", finding.Message);
        Assert.Equal("use-case/archive.return-copy", finding.Subject);
    }
}
