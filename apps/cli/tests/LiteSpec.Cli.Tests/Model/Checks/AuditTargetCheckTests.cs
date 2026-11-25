using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the audit target checks: an audited use case that changes a record names its target input, the target resolves to an input carrying identifies, and the target kind matches what that input identifies.</summary>
public sealed class AuditTargetCheckTests
{
    /// <summary>An audited command changing a record with no target reports audit-target-missing.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void AuditedCommandChangingRecordWithoutTargetReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "audit": {"level": "required", "category": "monetary-action", "targetKind": "aggregate/spending.expense"}}"""));

        Assert.Contains("audit-target-missing", TestModel.Codes(model));
    }

    /// <summary>An audited command changing no record with no target passes the target rule.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void AuditedCommandWithoutWritesAndTargetPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.export-expenses", Path.Combine(temp.Path, "export.yaml"),
                """{"kind": "command", "audit": {"level": "required", "category": "bulk-export", "targetKind": "aggregate/spending.expense"}}"""));

        Assert.DoesNotContain("audit-target-missing", TestModel.Codes(model));
    }

    /// <summary>An exempt command passes the target rules.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void ExemptCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "audit": {"exemptBecause": "Approvals leave their trail in the expense states and events."}}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>A command with level none passes the target rules.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void LevelNoneCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"),
                """{"kind": "query", "audit": {"level": "none", "targetKind": "No audit entry."}}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>A target naming no input reports audit-target-unknown.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void TargetNamingNoInputReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": "payment", "targetKind": "aggregate/spending.expense"}}"""));

        Assert.Contains("audit-target-unknown", TestModel.Codes(model));
    }

    /// <summary>A target naming an input with no identifies reports audit-target-unknown.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void TargetNamingInputWithoutIdentifiesReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "input": [{"name": "note", "meaning": "The payment note.", "type": "text", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": "note", "targetKind": "aggregate/spending.expense"}}"""));

        Assert.Contains("audit-target-unknown", TestModel.Codes(model));
    }

    /// <summary>A target kind differing from what the target input identifies reports audit-target-kind-drift.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void TargetKindDifferingFromIdentifiesReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": "expense", "targetKind": "aggregate/spending.payment"}}"""));

        Assert.Contains("audit-target-kind-drift", TestModel.Codes(model));
    }

    /// <summary>A target resolving to an identified input with a matching kind passes every target rule.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void MatchingTargetPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": "expense", "targetKind": "aggregate/spending.expense"}}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>An audited shape without the audit extension selected passes the target rules.</summary>
    /// <proves>litespec/ext/audit#declare-which-commands-are-audited</proves>
    [Fact]
    public void AuditedShapeWithoutExtensionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "audit": {"level": "required", "category": "monetary-action", "targetKind": "aggregate/spending.expense"}}"""));

        var codes = TestModel.Codes(model);

        Assert.Contains("field-extension-unselected", codes);
        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>A composite target naming two identified inputs with a stated kind passes every target rule.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void CompositeTargetWithTwoIdentifiedInputsPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.attach-receipt", Path.Combine(temp.Path, "attach.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.receipt"], "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "classification": "internal"}, {"name": "receipt", "meaning": "The receipt.", "type": "id", "identifies": "aggregate/spending.receipt", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": ["expense", "receipt"], "targetKind": "aggregate/spending.receipt"}}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>A composite target naming an input with no identifies reports audit-target-unknown.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void CompositeTargetNamingInputWithoutIdentifiesReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.attach-receipt", Path.Combine(temp.Path, "attach.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.receipt"], "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "classification": "internal"}, {"name": "note", "meaning": "The note.", "type": "text", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": ["expense", "note"], "targetKind": "aggregate/spending.receipt"}}"""));

        Assert.Contains("audit-target-unknown", TestModel.Codes(model));
    }

    /// <summary>A composite target whose stated kind differs from each input still passes the kind rule.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void CompositeTargetWithStatedKindPassesDrift()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.attach-receipt", Path.Combine(temp.Path, "attach.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.receipt"], "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "classification": "internal"}, {"name": "receipt", "meaning": "The receipt.", "type": "id", "identifies": "aggregate/spending.receipt", "origin": "caller", "classification": "internal"}], "audit": {"level": "required", "category": "monetary-action", "target": ["expense", "receipt"], "targetKind": "aggregate/spending.attachment"}}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>A target read from an output field the handler determines passes every target rule.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void FromOutputTargetNamingOutputFieldPasses()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "output": [{"name": "expenseId", "meaning": "The submitted expense.", "type": "id", "identifies": "aggregate/spending.expense"}], "audit": {"level": "required", "category": "monetary-action", "target": {"from": "output", "field": "expenseId"}, "targetKind": "aggregate/spending.expense"}}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("audit-target-missing", codes);
        Assert.DoesNotContain("audit-target-unknown", codes);
        Assert.DoesNotContain("audit-target-kind-drift", codes);
    }

    /// <summary>A target read from an output field the use case carries no output for reports audit-target-unknown.</summary>
    /// <proves>litespec/core/use-cases#state-the-audit-position-of-every-command</proves>
    [Fact]
    public void FromOutputTargetNamingNoOutputFieldReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        SelectExtensions(temp.Path, "audit");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "writes": ["aggregate/spending.expense"], "output": [{"name": "expenseId", "meaning": "The submitted expense.", "type": "id", "identifies": "aggregate/spending.expense"}], "audit": {"level": "required", "category": "monetary-action", "target": {"from": "output", "field": "connectionId"}, "targetKind": "aggregate/spending.expense"}}"""));

        Assert.Contains("audit-target-unknown", TestModel.Codes(model));
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
