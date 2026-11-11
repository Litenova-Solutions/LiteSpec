using LiteSpec.Cli.Model.Tests;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves the test run reads browser outcomes by title, so a browser proof can pass.</summary>
public sealed class BrowserTitleTests
{
    /// <summary>Passing Of Title Returns True For A Passed Title.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingOfTitleReturnsTrueForAPassedTitle()
    {
        var run = new TestRun();
        run.OutcomeByTitle["lists the expenses"] = "Passed";

        Assert.True(run.PassingOfTitle("lists the expenses"));
    }

    /// <summary>Passing Of Title Returns False When The Title Did Not Pass.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingOfTitleReturnsFalseWhenTheTitleDidNotPass()
    {
        var run = new TestRun();
        run.OutcomeByTitle["lists the expenses"] = "Failed";
        run.OutcomeByTitle["refuses an unknown expense"] = "Skipped";

        Assert.False(run.PassingOfTitle("lists the expenses"));
        Assert.False(run.PassingOfTitle("refuses an unknown expense"));
    }

    /// <summary>Passing Of Title Returns Null When The Run Holds No Outcome.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingOfTitleReturnsNullWhenTheRunHoldsNoOutcome()
    {
        var run = new TestRun();
        run.OutcomeByTitle["lists the expenses"] = "Passed";

        Assert.Null(run.PassingOfTitle("renews a loan"));
    }

    /// <summary>Browser Title Of Reads The Bare Title Through A Describe Chain And A Project.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void BrowserTitleOfReadsTheBareTitleThroughADescribeChainAndAProject()
    {
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("expenses.spec.ts", "lists the expenses"));
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("tests/expenses.spec.ts", "expenses list › lists the expenses"));
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("expenses.spec.ts", "[chromium] expenses list › lists the expenses"));
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("tests/expenses.spec.ts", "[chromium] lists the expenses"));
    }

    /// <summary>Browser Title Of Ignores A Backend Case And An Empty Name.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void BrowserTitleOfIgnoresABackendCaseAndAnEmptyName()
    {
        Assert.Null(TestRun.BrowserTitleOf("Library.Acceptance.Tests.JUnit.JUnitScenarios", "Borrowing_an_available_copy_succeeds"));
        Assert.Null(TestRun.BrowserTitleOf("Library.Acceptance.Tests.Trx.TrxScenarios", "Borrowing_an_available_copy_succeeds"));
        Assert.Null(TestRun.BrowserTitleOf("expenses.spec.ts", ""));
    }

    /// <summary>A Playwright Case Name Resolves Its Title Outcome.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void APlaywrightCaseNameResolvesItsTitleOutcome()
    {
        var run = new TestRun();
        (string Classname, string Name, string Outcome)[] cases =
        [
            ("tests/expenses.spec.ts", "expenses list › lists the expenses", "Passed"),
            ("expenses.spec.ts", "[chromium] refuses an unknown expense", "Failed"),
            ("Library.Acceptance.Tests.JUnit.JUnitScenarios", "Borrowing_an_available_copy_succeeds", "Passed"),
        ];

        foreach (var (classname, name, outcome) in cases)
        {
            if (TestRun.BrowserTitleOf(classname, name) is { } title)
            {
                run.OutcomeByTitle[title] = outcome;
            }
        }

        Assert.True(run.PassingOfTitle("lists the expenses"));
        Assert.False(run.PassingOfTitle("refuses an unknown expense"));
        Assert.Null(run.PassingOfTitle("renews a loan"));
        Assert.Null(run.PassingOfTitle("Borrowing_an_available_copy_succeeds"));
    }
}
