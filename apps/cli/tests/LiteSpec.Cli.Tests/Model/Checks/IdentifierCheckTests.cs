using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the identifier drift checks: a module home matches its folder, a name is used once, and a decision number is used once, at tier G.</summary>
public sealed class IdentifierCheckTests
{
    /// <summary>Use Case In Its Module Passes.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void UseCaseInItsModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("identifier-folder-drift", TestModel.Codes(model));
    }

    /// <summary>Use Case In Another Module Reports.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void UseCaseInAnotherModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/archive", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "identifier-folder-drift");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' sits in module 'archive' but its identifier names module 'lending'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Aggregate In Another Module Reports.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void AggregateInAnotherModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/archive", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "loans", "aggregate.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "identifier-folder-drift");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Aggregate 'aggregate/lending.loan' sits in module 'archive' but its identifier names module 'lending'.", finding.Message);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <summary>Module Scoped Record In Another Module Reports.</summary>
    /// <param name="type">The module-homed record type.</param>
    /// <param name="id">The record identifier naming the lending module.</param>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Theory]
    [InlineData("reaction", "reaction/lending.send-due-notice")]
    [InlineData("job", "job/lending.scan-due")]
    [InlineData("notification", "notification/lending.loan-due")]
    [InlineData("setting", "setting/lending.max-renewals")]
    public void ModuleScopedRecordInAnotherModuleReports(string type, string id)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/archive", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record(type, id, Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "record.yaml"), """{}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "identifier-folder-drift");
        Assert.Equal("G", finding.Tier);
        Assert.Contains("'archive'", finding.Message);
        Assert.Contains("'lending'", finding.Message);
        Assert.Equal(id, finding.Subject);
    }

    /// <summary>Rule Keeps Its Segment.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void RuleKeepsItsSegment()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/archive", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""),
            TestModel.Record("rule", "rule/lending.copy-barcode-unique", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "rules", "barcodes.yaml"), """{"kind": "invariant", "statement": "A copy barcode is unique.", "governs": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("identifier-folder-drift", TestModel.Codes(model));
    }

    /// <summary>Undeclared Home Stays Parent Missing.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void UndeclaredHomeStaysParentMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/nope.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("identifier-folder-drift", TestModel.Codes(model));
        Assert.Contains("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Undeclared Folder Stays Silent.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void UndeclaredFolderStaysSilent()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("identifier-folder-drift", TestModel.Codes(model));
    }

    /// <summary>Record Outside Modules Stays Silent.</summary>
    /// <proves>litespec/core/model#move-each-identifier-with-its-module</proves>
    [Fact]
    public void RecordOutsideModulesStaysSilent()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("identifier-folder-drift", TestModel.Codes(model));
    }

    /// <summary>Distinct Names Pass.</summary>
    /// <proves>litespec/core/model#keep-each-name-unique-in-the-product</proves>
    [Fact]
    public void DistinctNamesPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "borrowing.yaml"), """{"solves": ["problem/overdue-loans-pile-up"]}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.yaml"), """{"solves": ["problem/overdue-loans-pile-up"]}"""));

        Assert.DoesNotContain("name-duplicate", TestModel.Codes(model));
    }

    /// <summary>Use Case Name Reused Reports.</summary>
    /// <proves>litespec/core/model#keep-each-name-unique-in-the-product</proves>
    [Fact]
    public void UseCaseNameReusedReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/archive", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/archive.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""));

        var findings = Checker.Check(model).Findings.Where(found => found.Code == "name-duplicate").ToList();
        Assert.Equal(2, findings.Count);
        var finding = Assert.Single(findings, found => found.Subject == "use-case/lending.borrow-copy");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' shares its name 'borrow-copy' with another use case.", finding.Message);
    }

    /// <summary>Aggregate Name Reused Reports.</summary>
    /// <proves>litespec/core/model#keep-each-name-unique-in-the-product</proves>
    [Fact]
    public void AggregateNameReusedReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/archive", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""),
            TestModel.Record("aggregate", "aggregate/archive.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "archive", "loans", "aggregate.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""));

        var findings = Checker.Check(model).Findings.Where(found => found.Code == "name-duplicate").ToList();
        Assert.Equal(2, findings.Count);
        var finding = Assert.Single(findings, found => found.Subject == "aggregate/lending.loan");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Aggregate 'aggregate/lending.loan' shares its name 'loan' with another aggregate.", finding.Message);
    }

    /// <summary>Same Name Across Types Passes.</summary>
    /// <proves>litespec/core/model#keep-each-name-unique-in-the-product</proves>
    [Fact]
    public void SameNameAcrossTypesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("permission", "permission/borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "permissions.yaml"), """{"meaning": "Borrow an available copy.", "scope": "self"}"""));

        Assert.DoesNotContain("name-duplicate", TestModel.Codes(model));
    }

    /// <summary>Same Identifier Twice Stays Identifier Duplicate.</summary>
    /// <proves>litespec/core/model#keep-each-name-unique-in-the-product</proves>
    [Fact]
    public void SameIdentifierTwiceStaysIdentifierDuplicate()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "permissions.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("permission", "permission/borrow-copy", file, 1, """{"meaning": "Borrow an available copy.", "scope": "self"}"""),
            TestModel.Record("permission", "permission/borrow-copy", file, 5, """{"meaning": "Borrow an available copy again.", "scope": "self"}"""));

        Assert.Contains("identifier-duplicate", TestModel.Codes(model));
        Assert.DoesNotContain("name-duplicate", TestModel.Codes(model));
    }

    /// <summary>Distinct Numbers Pass.</summary>
    /// <proves>litespec/core/model#take-decision-numbers-from-one-sequence</proves>
    [Fact]
    public void DistinctNumbersPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.yaml"), """{"decides": ["requirement/borrowing.loan-period-days"]}"""),
            TestModel.Record("decision", "decision/0002-hold-window", Path.Combine(temp.Path, "decisions.yaml"), """{"decides": ["requirement/borrowing.hold-pickup-window"]}"""));

        Assert.DoesNotContain("decision-number-duplicate", TestModel.Codes(model));
    }

    /// <summary>Number Reused Reports.</summary>
    /// <proves>litespec/core/model#take-decision-numbers-from-one-sequence</proves>
    [Fact]
    public void NumberReusedReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "loan.yaml"), """{"decides": ["requirement/borrowing.loan-period-days"]}"""),
            TestModel.Record("decision", "decision/0001-hold-window", Path.Combine(temp.Path, "hold.yaml"), """{"decides": ["requirement/borrowing.hold-pickup-window"]}"""));

        var findings = Checker.Check(model).Findings.Where(found => found.Code == "decision-number-duplicate").ToList();
        Assert.Equal(2, findings.Count);
        var finding = Assert.Single(findings, found => found.Subject == "decision/0001-loan-period");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Decision 'decision/0001-loan-period' shares its number '0001' with another decision.", finding.Message);
    }

    /// <summary>Number Reused Across Folders Reports.</summary>
    /// <proves>litespec/core/model#take-decision-numbers-from-one-sequence</proves>
    [Fact]
    public void NumberReusedAcrossFoldersReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, ".litespec", "intent", "decisions", "0001-loan-period.yaml"), """{"decides": ["requirement/borrowing.loan-period-days"]}"""),
            TestModel.Record("decision", "decision/0001-loan-store", Path.Combine(temp.Path, ".litespec", "implementation", "modules", "lending", "decisions", "0001-loan-store.yaml"), """{"decides": ["requirement/borrowing.loan-period-days"]}"""));

        var findings = Checker.Check(model).Findings.Where(found => found.Code == "decision-number-duplicate").ToList();
        Assert.Equal(2, findings.Count);
    }

    /// <summary>Same Decision Twice Stays Identifier Duplicate.</summary>
    /// <proves>litespec/core/model#take-decision-numbers-from-one-sequence</proves>
    [Fact]
    public void SameDecisionTwiceStaysIdentifierDuplicate()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "decisions.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", file, 1, """{"decides": ["requirement/borrowing.loan-period-days"]}"""),
            TestModel.Record("decision", "decision/0001-loan-period", file, 5, """{"decides": ["requirement/borrowing.loan-period-days"]}"""));

        Assert.Contains("identifier-duplicate", TestModel.Codes(model));
        Assert.DoesNotContain("decision-number-duplicate", TestModel.Codes(model));
    }

    /// <summary>Decision Without Number Stays Identifier Invalid.</summary>
    /// <proves>litespec/core/model#take-decision-numbers-from-one-sequence</proves>
    [Fact]
    public void DecisionWithoutNumberStaysIdentifierInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/loan-period", Path.Combine(temp.Path, "decisions.yaml"), """{"decides": ["requirement/borrowing.loan-period-days"]}"""));

        Assert.Contains("identifier-invalid", TestModel.Codes(model));
        Assert.DoesNotContain("decision-number-duplicate", TestModel.Codes(model));
    }
}
