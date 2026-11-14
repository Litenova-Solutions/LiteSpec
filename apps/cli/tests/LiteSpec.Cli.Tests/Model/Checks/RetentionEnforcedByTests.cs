using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the retention schedule row enforcement check against hand-built models and schedule pages.</summary>
public sealed class RetentionEnforcedByTests
{
    /// <summary>Retention Row With Bare Enforcement Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void RetentionRowWithBareEnforcementReports()
    {
        using var temp = TestCli.Temp("checks");
        var schedule = WriteSchedule(temp.Path, "record-expiry");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "field-invalid" && found.Message.Contains("which is not a reaction or job identifier", StringComparison.Ordinal));

        Assert.Equal(schedule, finding.Path);
        Assert.Equal(10, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>Retention Row With Unknown Job Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void RetentionRowWithUnknownJobReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteSchedule(temp.Path, "job/lending.erase-expired-borrower");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.Contains(
            Checker.Check(model).Findings,
            finding => finding.Code == "reference-unknown" && finding.Message.Contains("holds enforcedBy 'job/lending.erase-expired-borrower', which no record holds", StringComparison.Ordinal));
    }

    /// <summary>Retention Row With Existing Job Passes.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void RetentionRowWithExistingJobPasses()
    {
        using var temp = TestCli.Temp("checks");
        WriteSchedule(temp.Path, "job/lending.erase-expired-borrower");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.erase-expired-borrower", Path.Combine(temp.Path, "job.yaml"), """{"summary": "Erase expired borrower data."}"""));

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            finding => finding.Code is "field-invalid" or "reference-unknown" && finding.Message.Contains("Retention schedule row", StringComparison.Ordinal));
    }

    /// <summary>Retention Row With Use Case Reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void RetentionRowWithUseCaseReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteSchedule(temp.Path, "use-case/lending.erase-borrower");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.erase-borrower", Path.Combine(temp.Path, "erase-borrower.yaml"), """{"summary": "Erase the borrower.", "kind": "command"}"""));

        Assert.Contains(
            Checker.Check(model).Findings,
            finding => finding.Code == "field-invalid" && finding.Message.Contains("links enforcedBy to 'use-case/lending.erase-borrower', which is a use-case", StringComparison.Ordinal));
    }

    /// <summary>Aggregate With Bare Enforcement Still Passes.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void AggregateWithBareEnforcementStillPasses()
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

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            finding => finding.Code is "field-invalid" or "reference-unknown" && finding.Message.Contains("enforcedBy", StringComparison.Ordinal));
    }

    static string WriteSchedule(string root, string enforcedBy)
    {
        var folder = Path.Combine(root, ".litespec", "behavior");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "retention-schedule.yaml");
        File.WriteAllText(file, $$"""
            type: retention-schedule
            title: Retention Schedule
            rows:
            - dataClass: borrower-identity
              covers:
              - aggregate/lending.loan
              basis: business
              purpose: Lending keeps the borrower identity for the loan file.
              period: P7Y
              enforcedBy: {{enforcedBy}}
            """);

        return file;
    }
}
