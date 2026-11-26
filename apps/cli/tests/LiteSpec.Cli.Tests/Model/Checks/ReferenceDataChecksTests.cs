using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the reference-data content checks: closed values, resolving keys, played roles, covered dates, and agreeing sign conventions.</summary>
public sealed class ReferenceDataChecksTests
{
    /// <summary>Row values within the closed set pass the value rule.</summary>
    /// <proves>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</proves>
    [Fact]
    public void RowValuesWithinClosedSetPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "membership-kind", """{"summary": "The membership kinds.", "choices": ["child", "adult", "senior"], "rows": [""" + Row("\"child\"", "2026-01-01", null) + ", " + Row("\"senior\"", "2026-01-01", null) + "]}"));

        Assert.Empty(Findings(model, "reference-data-value-invalid"));
    }

    /// <summary>A row value outside the closed set reports reference-data-value-invalid at tier G.</summary>
    /// <proves>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</proves>
    [Fact]
    public void RowValueOutsideClosedSetReportsValueInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "membership-kind", """{"summary": "The membership kinds.", "choices": ["child", "adult", "senior"], "rows": [""" + Row("\"guest\"", "2026-01-01", null) + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-value-invalid"));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/spending.membership-kind", finding.Subject);
        Assert.Contains("row 0 with value 'guest' outside its choices", finding.Message);
    }

    /// <summary>A number matches its closed set across written forms: 5.0 is the listed 5.</summary>
    /// <proves>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</proves>
    [Fact]
    public void NumberValueMatchesAcrossWrittenForms()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "choices": [5], "rows": [""" + Row("5.0", "2026-01-01", null) + "]}"));

        Assert.Empty(Findings(model, "reference-data-value-invalid"));
    }

    /// <summary>A table without a closed set holds any value.</summary>
    /// <proves>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</proves>
    [Fact]
    public void TableWithoutClosedSetPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", null) + "]}"));

        Assert.Empty(Findings(model, "reference-data-value-invalid"));
    }

    /// <summary>A cross-reference to a key the named table holds passes.</summary>
    /// <proves>litespec/core/knowledge#validate-reference-data-before-serving</proves>
    [Fact]
    public void CrossReferenceToHeldKeyPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", null, key: "target-2026") + "]}"),
            Table(temp.Path, "reminder-target", """{"summary": "The reminder targets.", "rows": [""" + Row("3", "2026-01-01", null, refersTo: "reference-data/spending.decision-target#target-2026") + "]}"));

        Assert.Empty(Findings(model, "reference-data-key-unknown"));
    }

    /// <summary>A cross-reference to a key no row of the named table holds reports reference-data-key-unknown at tier G.</summary>
    /// <proves>litespec/core/knowledge#validate-reference-data-before-serving</proves>
    [Fact]
    public void CrossReferenceToMissingKeyReportsKeyUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", null, key: "target-2026") + "]}"),
            Table(temp.Path, "reminder-target", """{"summary": "The reminder targets.", "rows": [""" + Row("3", "2026-01-01", null, refersTo: "reference-data/spending.decision-target#target-2030") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-key-unknown"));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/spending.reminder-target", finding.Subject);
        Assert.Contains("which names a key no row of 'reference-data/spending.decision-target' holds", finding.Message);
    }

    /// <summary>A cross-reference to a table no record holds is left to the citation check, so one defect draws one finding.</summary>
    /// <proves>litespec/core/knowledge#validate-reference-data-before-serving</proves>
    [Fact]
    public void CrossReferenceToMissingTableIsLeftToTheCitationCheck()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "reminder-target", """{"summary": "The reminder targets.", "rows": [""" + Row("3", "2026-01-01", null, refersTo: "reference-data/spending.unknown#standard") + "]}"));

        Assert.Empty(Findings(model, "reference-data-key-unknown"));
        Assert.Contains(Findings(model, "reference-unknown"), found => found.Message.Contains("reference-data/spending.unknown"));
    }

    /// <summary>A cross-reference naming no row reports record-invalid.</summary>
    /// <proves>litespec/core/knowledge#validate-reference-data-before-serving</proves>
    [Fact]
    public void MalformedCrossReferenceReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "reminder-target", """{"summary": "The reminder targets.", "rows": [""" + Row("3", "2026-01-01", null, refersTo: "target-2026") + "]}"));

        Assert.Contains(Findings(model, "record-invalid"), found => found.Message.Contains("which names no reference-data row"));
    }

    /// <summary>Two rows of one table sharing a key across disjoint spans pass: one key holds the dated history.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void SharedKeyAcrossDisjointSpansPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", "2026-12-31", key: "target") + ", " + Row("3", "2027-01-01", null, key: "target") + "]}"));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Message.Contains("holds key 'target'"));
        Assert.Empty(Findings(model, "reference-data-period-invalid"));
    }

    /// <summary>Every required role played by a row passes the role rule.</summary>
    /// <proves>litespec/core/knowledge#name-a-role-not-a-row</proves>
    [Fact]
    public void EveryRequiredRolePlayedPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "requiredRoles": ["standard"], "rows": [""" + Row("5", "2026-01-01", null, role: "standard") + "]}"));

        Assert.Empty(Findings(model, "reference-data-role-missing"));
    }

    /// <summary>A required role played by no row reports reference-data-role-missing at tier G.</summary>
    /// <proves>litespec/core/knowledge#name-a-role-not-a-row</proves>
    [Fact]
    public void RequiredRolePlayedByNoRowReportsRoleMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "requiredRoles": ["standard", "expedited"], "rows": [""" + Row("5", "2026-01-01", null, role: "standard") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-role-missing"));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/spending.decision-target", finding.Subject);
        Assert.Contains("requires role 'expedited', which no row plays", finding.Message);
    }

    /// <summary>Rows running contiguously across a year boundary pass the coverage rule.</summary>
    /// <proves>litespec/core/domain-model#cover-every-date-in-use</proves>
    [Fact]
    public void ContiguousRowsPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", "2026-12-31") + ", " + Row("3", "2027-01-01", null) + "]}"));

        Assert.Empty(Findings(model, "reference-data-coverage-missing"));
    }

    /// <summary>Rows leaving dates with no row in force report reference-data-coverage-missing at tier G.</summary>
    /// <proves>litespec/core/domain-model#cover-every-date-in-use</proves>
    [Fact]
    public void RowsLeavingAGapReportCoverageMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", "2026-06-30") + ", " + Row("3", "2027-01-01", null) + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-coverage-missing"));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/spending.decision-target", finding.Subject);
        Assert.Contains("leaving dates between '2026-06-30' and '2027-01-01' with no row in force", finding.Message);
    }

    /// <summary>A row in force on the date the cast declares as today passes the coverage rule.</summary>
    /// <proves>litespec/core/domain-model#cover-every-date-in-use</proves>
    [Fact]
    public void CastTodayCoveredPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("cast", "cast/spending", Path.Combine(temp.Path, ".litespec", "cast.yaml"), """{"summary": "The cast.", "today": "2027-06-15"}"""),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("3", "2027-01-01", null) + "]}"));

        Assert.Empty(Findings(model, "reference-data-coverage-missing"));
    }

    /// <summary>No row in force on the date the cast declares as today reports reference-data-coverage-missing.</summary>
    /// <proves>litespec/core/domain-model#cover-every-date-in-use</proves>
    [Fact]
    public void CastTodayUncoveredReportsCoverageMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("cast", "cast/spending", Path.Combine(temp.Path, ".litespec", "cast.yaml"), """{"summary": "The cast.", "today": "2027-06-15"}"""),
            Table(temp.Path, "decision-target", """{"summary": "The decision targets.", "rows": [""" + Row("5", "2026-01-01", "2026-12-31") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-coverage-missing"));
        Assert.Equal("reference-data/spending.decision-target", finding.Subject);
        Assert.Contains("holds no row in force on '2027-06-15'", finding.Message);
    }

    /// <summary>Two use cases computing one value with one sign convention pass the sign rule.</summary>
    /// <proves>litespec/core/domain-model#state-the-sign-of-every-amount</proves>
    [Fact]
    public void SharedAmountWithOneConventionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Calculation(temp.Path),
            UseCase(temp.Path, "pay-fine", """{"name": "amount", "meaning": "The fine.", "type": "money", "required": true, "origin": "caller", "classification": "internal", "sign": "money-in-positive"}"""),
            Query(temp.Path, "report-fines", """{"name": "amount", "meaning": "The fine.", "type": "money", "sign": "money-in-positive"}"""));

        Assert.Empty(Findings(model, "amount-sign-drift"));
    }

    /// <summary>Two use cases computing one value with differing sign conventions report amount-sign-drift at tier A.</summary>
    /// <proves>litespec/core/domain-model#state-the-sign-of-every-amount</proves>
    [Fact]
    public void SharedAmountWithDifferingConventionsReportsSignDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Calculation(temp.Path),
            UseCase(temp.Path, "pay-fine", """{"name": "amount", "meaning": "The fine.", "type": "money", "required": true, "origin": "caller", "classification": "internal", "sign": "money-in-positive"}""", """{"name": "amount", "meaning": "The fine.", "type": "money", "sign": "money-in-positive"}"""),
            Query(temp.Path, "report-fines", """{"name": "amount", "meaning": "The fine.", "type": "money", "sign": "money-out-positive"}"""));

        var finding = Assert.Single(Findings(model, "amount-sign-drift"));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("rule/spending.fine-total", finding.Subject);
        Assert.Contains("computes 'amount' from use cases stating differing sign conventions", finding.Message);
    }

    /// <summary>Two use cases computing one value where one states no sign report amount-sign-drift.</summary>
    /// <proves>litespec/core/domain-model#state-the-sign-of-every-amount</proves>
    [Fact]
    public void SharedAmountWithMissingConventionReportsSignDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Calculation(temp.Path),
            UseCase(temp.Path, "pay-fine", """{"name": "amount", "meaning": "The fine.", "type": "money", "required": true, "origin": "caller", "classification": "internal", "sign": "money-in-positive"}"""),
            Query(temp.Path, "report-fines", """{"name": "amount", "meaning": "The fine.", "type": "money"}"""));

        var finding = Assert.Single(Findings(model, "amount-sign-drift"));
        Assert.Contains("'use-case/spending.report-fines' states none", finding.Message);
    }

    /// <summary>An amount no second use case computes states no sign without a finding.</summary>
    /// <proves>litespec/core/domain-model#state-the-sign-of-every-amount</proves>
    [Fact]
    public void UnsharedAmountWithoutSignPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Calculation(temp.Path),
            UseCase(temp.Path, "pay-fine", """{"name": "amount", "meaning": "The fine.", "type": "money", "required": true, "origin": "caller", "classification": "internal"}"""));

        Assert.Empty(Findings(model, "amount-sign-drift"));
    }

    /// <summary>A sign on a field that is no amount reports record-invalid.</summary>
    /// <proves>litespec/core/domain-model#state-the-sign-of-every-amount</proves>
    [Fact]
    public void SignOnTextFieldReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path, "pay-fine", """{"name": "note", "meaning": "The note.", "type": "text", "required": true, "origin": "caller", "classification": "internal", "sign": "money-in-positive"}"""));

        Assert.Contains(Findings(model, "record-invalid"), found => found.Message.Contains("with a sign on type 'text'"));
    }

    /// <summary>A sign outside the sign conventions reports record-invalid.</summary>
    /// <proves>litespec/core/domain-model#state-the-sign-of-every-amount</proves>
    [Fact]
    public void UnknownSignReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path, "pay-fine", """{"name": "amount", "meaning": "The fine.", "type": "money", "required": true, "origin": "caller", "classification": "internal", "sign": "positive"}"""));

        Assert.Contains(Findings(model, "record-invalid"), found => found.Message.Contains("with sign 'positive' outside money-in-positive, money-out-positive"));
    }

    /// <summary>A table with no rows yet and a summary saying what rows will state passes.</summary>
    /// <proves>litespec/core/domain-model#state-what-an-empty-table-will-hold</proves>
    [Fact]
    public void EmptyTableWithSummaryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "accepted-providers", """{"summary": "The providers each jurisdiction accepts; no jurisdiction has stated its providers yet.", "rows": []}"""));

        Assert.DoesNotContain(Findings(model, "record-invalid"), found => found.Subject == "reference-data/spending.accepted-providers" && found.Message.Contains("holds no rows"));
        Assert.Empty(Findings(model, "reference-data-coverage-missing"));
    }

    /// <summary>A table with no rows yet and no summary reports record-invalid.</summary>
    /// <proves>litespec/core/domain-model#state-what-an-empty-table-will-hold</proves>
    [Fact]
    public void EmptyTableWithoutSummaryReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, "accepted-providers", """{"rows": []}"""));

        var findings = Findings(model, "record-invalid")
            .Where(found => found.Subject == "reference-data/spending.accepted-providers")
            .ToList();
        Assert.Contains(findings, found => found.Message.Contains("holds no rows and states no summary"));
    }

    /// <summary>The findings of a model that carry one code.</summary>
    /// <param name="model">The model under test.</param>
    /// <param name="code">The finding code.</param>
    /// <returns>The findings carrying the code.</returns>
    static List<Finding> Findings(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(found => found.Code == code).ToList();

    /// <summary>One reference-data row as JSON, with its value, dates, source, and optional key, role and cross-reference.</summary>
    /// <param name="value">The row value as JSON.</param>
    /// <param name="from">The date the value takes effect.</param>
    /// <param name="to">The date the value stops applying, or null while it stays in force.</param>
    /// <param name="key">The stable key of the row, or null.</param>
    /// <param name="role">The role the row plays, or null.</param>
    /// <param name="refersTo">The row of another table this row reads, or null.</param>
    /// <returns>The row entry as JSON.</returns>
    static string Row(string value, string from, string? to, string? key = null, string? role = null, string? refersTo = null) =>
        "{\"value\": " + value + ", \"effectiveFrom\": \"" + from + "\""
        + (to is null ? "" : ", \"effectiveTo\": \"" + to + "\"")
        + (key is null ? "" : ", \"key\": \"" + key + "\"")
        + (role is null ? "" : ", \"role\": \"" + role + "\"")
        + (refersTo is null ? "" : ", \"refersTo\": \"" + refersTo + "\"")
        + ", \"source\": \"research/approval-time-benchmarks\"}";

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

    /// <summary>The fine-total calculation rule of the spending module.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The rule record.</returns>
    static Record Calculation(string root) => TestModel.Record(
        "rule",
        "rule/spending.fine-total",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "fine-total", "fine-total.yaml"),
        """{"kind": "calculation", "statement": "A fine totals its lines.", "governs": ["use-case/spending.pay-fine", "use-case/spending.report-fines"]}""");

    /// <summary>One command of the spending module computing the fine total, with one input field and an optional output field.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="name">The use-case name.</param>
    /// <param name="input">The input field as JSON.</param>
    /// <param name="output">The output field as JSON, or null for no command output.</param>
    /// <returns>The use-case record.</returns>
    static Record UseCase(string root, string name, string input, string? output = null) => TestModel.Record(
        "use-case",
        $"use-case/spending.{name}",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "use-cases", name, name + ".yaml"),
        """{"summary": "Pay the fine.", "kind": "command", "exposure": "A reader pays their own fine.", "reachedThrough": ["entry-point/spending-app"], "computedBy": ["rule/spending.fine-total"], "input": [""" + input + "]"
        + (output is null ? "" : ", \"output\": [" + output + "]")
        + """, "steps": [{"statement": "The reader pays the fine."}]}""");

    /// <summary>One query of the spending module computing the fine total, with one output field in its output group.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="name">The use-case name.</param>
    /// <param name="output">The output field as JSON.</param>
    /// <returns>The use-case record.</returns>
    static Record Query(string root, string name, string output) => TestModel.Record(
        "use-case",
        $"use-case/spending.{name}",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "use-cases", name, name + ".yaml"),
        """{"summary": "Report the fines.", "kind": "query", "exposure": "A reader reads their own fines.", "reachedThrough": ["entry-point/spending-app"], "computedBy": ["rule/spending.fine-total"], "output": {"include": {"all": {"fields": [""" + output + """], "classification": "internal", "reads": ["aggregate/spending.expense"], "freshness": {"mode": "immediate"}}}}, "steps": [{"statement": "The reader reads the fines."}]}""");
}
