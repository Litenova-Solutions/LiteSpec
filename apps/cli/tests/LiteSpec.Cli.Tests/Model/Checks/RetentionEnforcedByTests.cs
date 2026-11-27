using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the data class enforcer check against hand-built aggregates: an enforcer is a job or a use case that exists.</summary>
public sealed class RetentionEnforcedByTests
{
    /// <summary>A data class enforcer that is a bare word, not a job or use case, reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void DataClassWithBareEnforcementReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), DataClass("record-expiry")),
            PrivacyBasis(temp.Path),
            BorrowedDomainEvent(temp.Path));

        Assert.Contains(
            Checker.Check(model).Findings,
            finding => finding.Code == "reference-unknown" && finding.Message.Contains("names enforcer 'record-expiry', which no record holds", StringComparison.Ordinal));
    }

    /// <summary>A data class enforcer that names no record reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void DataClassWithUnknownJobReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), DataClass("job/lending.erase-expired-borrower")),
            PrivacyBasis(temp.Path),
            BorrowedDomainEvent(temp.Path));

        Assert.Contains(
            Checker.Check(model).Findings,
            finding => finding.Code == "reference-unknown" && finding.Message.Contains("names enforcer 'job/lending.erase-expired-borrower', which no record holds", StringComparison.Ordinal));
    }

    /// <summary>A data class enforcer that names an existing job passes.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void DataClassWithExistingJobPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), DataClass("job/lending.erase-expired-borrower")),
            TestModel.Record("job", "job/lending.erase-expired-borrower", Path.Combine(temp.Path, "job.yaml"), """{"summary": "Erase expired borrower data."}"""));

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            finding => finding.Code is "record-invalid" or "reference-unknown" && finding.Message.Contains("enforcer", StringComparison.Ordinal));
    }

    /// <summary>A data class enforcer that names a record of another type reports.</summary>
    /// <proves>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</proves>
    [Fact]
    public void DataClassEnforcedByAnAggregateReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), DataClass("aggregate/lending.loan")),
            PrivacyBasis(temp.Path),
            BorrowedDomainEvent(temp.Path));

        Assert.Contains(
            Checker.Check(model).Findings,
            finding => finding.Code == "record-invalid" && finding.Message.Contains("names enforcer 'aggregate/lending.loan', which is a aggregate", StringComparison.Ordinal));
    }

    private static string DataClass(string enforcer) =>
        $$"""
        {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
         "dataClasses": [{"name": "Borrower identity", "classification": "personal", "fields": [],
           "onErasure": "erase", "basedOn": ["constraint/borrower-privacy"], "purpose": "Keep the borrower identity for the loan file.",
           "period": "P7Y", "periodStartsAt": "domain-event/lending.loan-borrowed", "enforcedBy": ["{{enforcer}}"]}]}
        """;

    /// <summary>The constraint the data classes name as their retention basis.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The constraint record.</returns>
    private static Record PrivacyBasis(string root) => TestModel.Record(
        "constraint",
        "constraint/borrower-privacy",
        Path.Combine(root, "privacy.yaml"),
        """{"authority": "The library privacy law.", "sources": ["Library privacy act 2024"], "kind": "legal", "applies": true, "appliesBecause": "The library holds borrower identity."}""");

    /// <summary>The domain event the data classes count their retention period from.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The domain event record.</returns>
    private static Record BorrowedDomainEvent(string root) => TestModel.Record(
        "domain-event",
        "domain-event/lending.loan-borrowed",
        Path.Combine(root, "borrowed.yaml"),
        """{"meaning": "A copy was lent.", "fields": [{"name": "loanId", "type": "id"}]}""");
}
