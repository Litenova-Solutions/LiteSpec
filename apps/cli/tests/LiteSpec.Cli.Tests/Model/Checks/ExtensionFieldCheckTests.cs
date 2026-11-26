using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the extension field checks: a command declares audit when the audit extension is selected, and an audit or conflicts field written while its extension is not selected is reported.</summary>
public sealed class ExtensionFieldCheckTests
{
    /// <summary>A command without audit reports audit-missing when the audit extension is selected.</summary>
    /// <proves>litespec/core/audit#declare-which-commands-are-audited</proves>
    [Fact]
    public void CommandWithoutAuditWithAuditSelectedReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command"}"""));

        Assert.Contains("audit-missing", TestModel.Codes(model));
    }

    /// <summary>A command with audit passes when the audit extension is selected.</summary>
    /// <proves>litespec/core/audit#declare-which-commands-are-audited</proves>
    [Fact]
    public void CommandWithAuditWithAuditSelectedPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "audit": {"level": "required", "targetKind": "expense"}}"""));

        Assert.DoesNotContain("audit-missing", TestModel.Codes(model));
        Assert.DoesNotContain("field-extension-unselected", TestModel.Codes(model));
    }

    /// <summary>A command without audit passes the audit rule when the audit extension is not selected.</summary>
    /// <proves>litespec/core/audit#declare-which-commands-are-audited</proves>
    [Fact]
    public void CommandWithoutAuditWithoutExtensionPassesMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("audit-missing", TestModel.Codes(model));
    }

    /// <summary>A use case writing audit passes without the audit extension selected, because audit is core.</summary>
    /// <proves>litespec/core/audit#declare-which-commands-are-audited</proves>
    [Fact]
    public void AuditWrittenWithoutExtensionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "audit": {"level": "none"}}"""));

        Assert.DoesNotContain("field-extension-unselected", TestModel.Codes(model));
    }

    /// <summary>A query writing audit passes without the audit extension selected, because audit is core.</summary>
    /// <proves>litespec/core/audit#declare-which-commands-are-audited</proves>
    [Fact]
    public void QueryWithAuditWithoutExtensionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.total-spending", Path.Combine(temp.Path, "total.yaml"), """{"kind": "query", "audit": {"level": "none"}}"""));

        Assert.DoesNotContain("field-extension-unselected", TestModel.Codes(model));
        Assert.DoesNotContain("audit-missing", TestModel.Codes(model));
    }

    /// <summary>A command writing conflicts passes without the concurrency extension selected, because concurrency is core.</summary>
    /// <proves>litespec/core/concurrency#document-write-conflict-behavior</proves>
    [Fact]
    public void ConflictsWrittenWithoutExtensionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "conflicts": [{"subject": "A second approval of the same expense.", "winner": "first-write", "refusal": "refusal/spending.approve-conflict"}]}"""));

        Assert.DoesNotContain("field-extension-unselected", TestModel.Codes(model));
    }

    /// <summary>A command writing conflicts passes when the concurrency extension is selected.</summary>
    /// <proves>litespec/core/concurrency#document-write-conflict-behavior</proves>
    [Fact]
    public void ConflictsWrittenWithConcurrencySelectedPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "concurrency");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "conflicts": [{"subject": "A second approval of the same expense.", "winner": "first-write", "refusal": "refusal/spending.approve-conflict"}]}"""));

        Assert.DoesNotContain("field-extension-unselected", TestModel.Codes(model));
    }

    /// <summary>Selects the extensions in the consumer declaration the config reads.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="ids">The extension ids to select.</param>
    static void SelectExtensions(string root, params string[] ids)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));

        var entries = string.Join(
            "\n",
            ids.Select(id => $"  - id: {id}\n    criterion: The product needs the {id} extension."));

        File.WriteAllText(Path.Combine(root, ".litespec", "project.yaml"), "selectedExtensions:\n" + entries + "\n");
    }
}
