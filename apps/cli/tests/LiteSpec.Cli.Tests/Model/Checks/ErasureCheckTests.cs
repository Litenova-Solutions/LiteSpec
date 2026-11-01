using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the erasure, retention, acceptance and exemption checks against hand-built models.</summary>
public sealed class ErasureCheckTests
{
    /// <summary>Personal Class Without Erasure Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void Personal_Class_Without_Erasure_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), """
                {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
                 "dataClasses": [{"name": "Borrower identity", "classification": "personal"}]}
                """));

        Assert.Contains("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Personal Field Without Cover Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void Personal_Field_Without_Cover_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Loan(temp.Path, "personal"));

        Assert.Contains("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Sensitive Field Reports As Personal.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void Sensitive_Field_Reports_As_Personal()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Loan(temp.Path, "sensitive"));

        Assert.Contains("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Public And Internal Fields Pass.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void Public_And_Internal_Fields_Pass()
    {
        using var temp = Cli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}], "data": [
              {"name": "shelf", "meaning": "The home shelf.", "classification": "public"},
              {"name": "branch", "meaning": "The owning branch.", "classification": "internal"}
            ]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), json));

        Assert.DoesNotContain("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Erasure Position With Enforcement Covers The Data.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void Erasure_Position_With_Enforcement_Covers_The_Data()
    {
        using var temp = Cli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
             "data": [{"name": "borrower-email", "meaning": "The address notices go to.", "classification": "personal"}],
             "dataClasses": [{"name": "Borrower identity", "classification": "personal", "fields": ["borrower-email"],
               "onErasure": "erase", "basis": "business", "enforcedBy": "record-expiry"}]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), json));

        Assert.DoesNotContain("erasure-undeclared", TestModel.Codes(model));
        Assert.DoesNotContain("retention-enforcement-missing", TestModel.Codes(model));
    }

    /// <summary>Per Field Exception Covers The Field.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void Per_Field_Exception_Covers_The_Field()
    {
        using var temp = Cli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
             "data": [{"name": "borrower-email", "meaning": "The address notices go to.", "classification": "personal"}],
             "dataClasses": [{"name": "Borrower identity", "classification": "personal", "fields": ["shelf"],
               "onErasure": "erase", "basis": "business", "enforcedBy": "record-expiry",
               "exceptions": [{"field": "borrower-email", "onErasure": "pseudonymize"}]}]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), json));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "erasure-undeclared");
    }

    /// <summary>Retention Without Enforcement Reports.</summary>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void Retention_Without_Enforcement_Reports()
    {
        using var temp = Cli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
             "dataClasses": [{"name": "Borrower identity", "classification": "personal",
               "onErasure": "erase", "basis": "business"}]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), json));

        Assert.Contains("retention-enforcement-missing", TestModel.Codes(model));
    }

    /// <summary>Retain Without Acceptance Reports.</summary>
    /// <proves>litespec/core/privacy#accept-every-retained-personal-data-class-in-intent</proves>
    [Fact]
    public void Retain_Without_Acceptance_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Kept(temp.Path, null));

        Assert.Contains("risk-acceptance-missing", TestModel.Codes(model));
    }

    /// <summary>Retain With An Intent Decision Passes.</summary>
    /// <proves>litespec/core/privacy#accept-every-retained-personal-data-class-in-intent</proves>
    [Fact]
    public void Retain_With_An_Intent_Decision_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Kept(temp.Path, "decision/0001-loan-period"));

        Assert.DoesNotContain("risk-acceptance-missing", TestModel.Codes(model));
    }

    /// <summary>Pseudonymize Without Acceptance Reports.</summary>
    /// <proves>litespec/core/privacy#accept-every-retained-personal-data-class-in-intent</proves>
    [Fact]
    public void Pseudonymize_Without_Acceptance_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "aggregate",
                "aggregate/lending.loan",
                Path.Combine(temp.Path, "loan.md"),
                """
                {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
                 "dataClasses": [{"name": "Borrower identity", "classification": "personal",
                   "onErasure": "pseudonymize", "basis": "business", "enforcedBy": "record-expiry"}]}
                """));

        Assert.Contains("risk-acceptance-missing", TestModel.Codes(model));
    }

    /// <summary>Pseudonymize With An Intent Decision Passes.</summary>
    /// <proves>litespec/core/privacy#accept-every-retained-personal-data-class-in-intent</proves>
    [Fact]
    public void Pseudonymize_With_An_Intent_Decision_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "aggregate",
                "aggregate/lending.loan",
                Path.Combine(temp.Path, "loan.md"),
                """
                {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
                 "dataClasses": [{"name": "Borrower identity", "classification": "personal",
                   "onErasure": "pseudonymize", "basis": "business", "enforcedBy": "record-expiry",
                   "acceptedBy": ["decision/0001-loan-period"]}]}
                """),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                Path.Combine(temp.Path, "loan-period.md"),
                """{"status": "approved", "options": [{"name": "Keep", "pros": ["Tax."], "cons": ["None."]}], "choice": "Keep", "reopenedWhen": "The law changes.", "decides": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("risk-acceptance-missing", TestModel.Codes(model));
    }

    /// <summary>Exempt Without Review Date Reports.</summary>
    /// <proves>litespec/core/privacy#review-every-erasure-exemption-before-its-date</proves>
    [Fact]
    public void Exempt_Without_Review_Date_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Exempt(temp.Path, null));

        Assert.Contains("exemption-expired", TestModel.Codes(model));
    }

    /// <summary>Exempt Past Review Reports.</summary>
    /// <proves>litespec/core/privacy#review-every-erasure-exemption-before-its-date</proves>
    [Fact]
    public void Exempt_Past_Review_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Exempt(temp.Path, "2020-01-01"));

        Assert.Contains("exemption-expired", TestModel.Codes(model));
    }

    /// <summary>Exempt Before Review Passes.</summary>
    /// <proves>litespec/core/privacy#review-every-erasure-exemption-before-its-date</proves>
    [Fact]
    public void Exempt_Before_Review_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Exempt(temp.Path, "2035-01-01"));

        Assert.DoesNotContain("exemption-expired", TestModel.Codes(model));
    }

    static Record Loan(string root, string classification) =>
        TestModel.Record(
            "aggregate",
            "aggregate/lending.loan",
            Path.Combine(root, "loan.md"),
            $$"""
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}], "data": [
              {"name": "borrower-email", "meaning": "The address notices go to.", "classification": "{{classification}}"}
            ]}
            """);

    static Record Kept(string root, string? acceptedBy) =>
        TestModel.Record(
            "aggregate",
            "aggregate/lending.loan",
            Path.Combine(root, "loan.md"),
            "{\"status\": \"reviewed\", \"states\": [{\"name\": \"Active\", \"meaning\": \"The loan is open.\"}],"
            + "\"dataClasses\": [{\"name\": \"Borrower identity\", \"classification\": \"personal\","
            + "\"onErasure\": \"retain\", \"basis\": \"statute\", \"enforcedBy\": \"record-expiry\""
            + (acceptedBy is null ? "" : ", \"acceptedBy\": [\"" + acceptedBy + "\"]")
            + "}]}");

    static Record Exempt(string root, string? reviewBy) =>
        TestModel.Record(
            "aggregate",
            "aggregate/lending.loan",
            Path.Combine(root, "loan.md"),
            "{\"status\": \"reviewed\", \"states\": [{\"name\": \"Active\", \"meaning\": \"The loan is open.\"}],"
            + "\"dataClasses\": [{\"name\": \"Borrower identity\", \"classification\": \"personal\","
            + "\"onErasure\": \"exempt\", \"basis\": \"statute\", \"enforcedBy\": \"record-expiry\","
            + "\"acceptedBy\": [\"decision/0001-loan-period\"]"
            + (reviewBy is null ? "" : ", \"reviewBy\": \"" + reviewBy + "\"")
            + "}]}");
}
