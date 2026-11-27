using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a data class enforcement job reaches the aggregate holding the data class.</summary>
public sealed class EnforcementJobCheckTests
{
    /// <summary>An enforcement job writing no use case for the aggregate reports.</summary>
    [Fact]
    public void EnforcementJobWritingNoAggregateUseCaseReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Loan(temp.Path, "job/lending.erase-expired-borrower"),
            Job(temp.Path, "use-case/lending.erase-expired-loan"),
            TestModel.Record("use-case", "use-case/lending.erase-expired-loan", Path.Combine(temp.Path, "other.yaml"),
                """{"kind": "command", "writes": ["aggregate/lending.reservation"]}"""));

        Assert.Contains("enforcement-job-unreached", TestModel.Codes(model));
    }

    /// <summary>An enforcement job running a use case that writes the aggregate passes.</summary>
    [Fact]
    public void EnforcementJobWritingAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Loan(temp.Path, "job/lending.erase-expired-borrower"),
            Job(temp.Path, "use-case/lending.erase-expired-loan"),
            TestModel.Record("use-case", "use-case/lending.erase-expired-loan", Path.Combine(temp.Path, "erase.yaml"),
                """{"kind": "command", "writes": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("enforcement-job-unreached", TestModel.Codes(model));
    }

    /// <summary>An enforcement use case writing the aggregate passes without a job.</summary>
    [Fact]
    public void EnforcementUseCaseWritingAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Loan(temp.Path, "use-case/lending.erase-expired-loan"));

        Assert.DoesNotContain("enforcement-job-unreached", TestModel.Codes(model));
    }

    private static LiteSpec.Cli.Model.Records.Record Loan(string root, string enforcer) => TestModel.Record(
        "aggregate",
        "aggregate/lending.loan",
        Path.Combine(root, "loan.yaml"),
        $$"""
        {"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is open."}],
         "dataClasses": [{"name": "Borrower identity", "classification": "personal", "fields": [],
           "onErasure": "erase", "basedOn": ["constraint/borrower-privacy"],
           "purpose": "Keep the borrower identity for the loan file.", "period": "P7Y",
           "periodStartsAt": "domain-event/lending.loan-borrowed", "enforcedBy": ["{{enforcer}}"]}]}
        """);

    private static LiteSpec.Cli.Model.Records.Record Job(string root, string runs) => TestModel.Record(
        "job",
        "job/lending.erase-expired-borrower",
        Path.Combine(root, "job.yaml"),
        $$"""
        {"kind": "scheduled", "schedule": {"every": "PT1H"},
         "effects": [{"runs": "{{runs}}", "timing": "async"}]}
        """);
}
