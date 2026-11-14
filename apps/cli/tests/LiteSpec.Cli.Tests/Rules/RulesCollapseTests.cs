using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves a code path collapses the record's provisions into one line per record.</summary>
public sealed class RulesCollapseTests
{
    /// <summary>A handler collapses the provisions matched only through its marked record.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesHandlerCollapsesRecordProvisions()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/spending.submit-expense: ", result.Output);
        Assert.Contains(" provisions about the record; model check checks ", result.Output);
        Assert.Contains("run rules use-case/spending.submit-expense for the rest", result.Output);
        Assert.DoesNotContain("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
    }

    /// <summary>A record identifier still prints the record provisions in full.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRecordIdPrintsRecordProvisionsInFull()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "use-case/spending.submit-expense");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
        Assert.DoesNotContain("provisions about the record", result.Output);
    }

    /// <summary>A record file still prints the record provisions in full.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRecordFilePrintsRecordProvisionsInFull()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            ".litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
        Assert.DoesNotContain("provisions about the record", result.Output);
    }

    /// <summary>A handler still prints the provisions matching its own kind one by one.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesHandlerKeepsCodeProvisionsIndividual()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/backend/application#keep-command-handlers-to-load-call-and-stage:", result.Output);
    }
}
