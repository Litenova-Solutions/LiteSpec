using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the erasure and retention checks against hand-built models.</summary>
public sealed class ErasureCheckTests
{
    /// <summary>Personal Class Without Erasure Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void PersonalClassWithoutErasureReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """
                {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
                 "dataClasses": [{"name": "Borrower identity", "classification": "personal"}]}
                """));

        Assert.Contains("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Personal Field Without Cover Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void PersonalFieldWithoutCoverReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Loan(temp.Path, "personal"));

        Assert.Contains("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Sensitive Field Reports As Personal.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void SensitiveFieldReportsAsPersonal()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Loan(temp.Path, "sensitive"));

        Assert.Contains("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Public And Internal Fields Pass.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void PublicAndInternalFieldsPass()
    {
        using var temp = TestCli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}], "data": [
              {"name": "shelf", "meaning": "The home shelf.", "classification": "public"},
              {"name": "branch", "meaning": "The owning branch.", "classification": "internal"}
            ]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), json));

        Assert.DoesNotContain("erasure-undeclared", TestModel.Codes(model));
    }

    /// <summary>Erasure Position With Enforcement Covers The Data.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void ErasurePositionWithEnforcementCoversTheData()
    {
        using var temp = TestCli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
             "data": [{"name": "borrower-email", "meaning": "The address notices go to.", "classification": "personal"}],
             "dataClasses": [{"name": "Borrower identity", "classification": "personal", "fields": ["borrower-email"],
               "onErasure": "erase", "basis": "business", "enforcedBy": "record-expiry"}]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), json));

        Assert.DoesNotContain("erasure-undeclared", TestModel.Codes(model));
        Assert.DoesNotContain("retention-enforcement-missing", TestModel.Codes(model));
    }

    /// <summary>Per Field Exception Covers The Field.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void PerFieldExceptionCoversTheField()
    {
        using var temp = TestCli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
             "data": [{"name": "borrower-email", "meaning": "The address notices go to.", "classification": "personal"}],
             "dataClasses": [{"name": "Borrower identity", "classification": "personal", "fields": ["shelf"],
               "onErasure": "erase", "basis": "business", "enforcedBy": "record-expiry",
               "exceptions": [{"field": "borrower-email", "onErasure": "pseudonymize"}]}]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), json));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "erasure-undeclared");
    }

    /// <summary>Retention Without Enforcement Reports.</summary>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void RetentionWithoutEnforcementReports()
    {
        using var temp = TestCli.Temp("checks");
        var json = """
            {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
             "dataClasses": [{"name": "Borrower identity", "classification": "personal",
               "onErasure": "erase", "basis": "business"}]}
            """;
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), json));

        Assert.Contains("retention-enforcement-missing", TestModel.Codes(model));
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
}
