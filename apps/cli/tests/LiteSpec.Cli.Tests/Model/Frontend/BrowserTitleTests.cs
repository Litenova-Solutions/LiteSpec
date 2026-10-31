using LiteSpec.Model;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>Proves the test run reads browser outcomes by title, so a browser proof can pass.</summary>
public sealed class BrowserTitleTests
{
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Passing_Of_Title_Returns_True_For_A_Passed_Title()
    {
        var run = new TestRun();
        run.OutcomeByTitle["lists the expenses"] = "Passed";

        Assert.True(run.PassingOfTitle("lists the expenses"));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Passing_Of_Title_Returns_False_When_The_Title_Did_Not_Pass()
    {
        var run = new TestRun();
        run.OutcomeByTitle["lists the expenses"] = "Failed";
        run.OutcomeByTitle["refuses an unknown expense"] = "Skipped";

        Assert.False(run.PassingOfTitle("lists the expenses"));
        Assert.False(run.PassingOfTitle("refuses an unknown expense"));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Passing_Of_Title_Returns_Null_When_The_Run_Holds_No_Outcome()
    {
        var run = new TestRun();
        run.OutcomeByTitle["lists the expenses"] = "Passed";

        Assert.Null(run.PassingOfTitle("renews a loan"));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Browser_Title_Of_Reads_The_Bare_Title_Through_A_Describe_Chain_And_A_Project()
    {
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("expenses.spec.ts", "lists the expenses"));
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("tests/expenses.spec.ts", "expenses list › lists the expenses"));
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("expenses.spec.ts", "[chromium] expenses list › lists the expenses"));
        Assert.Equal("lists the expenses", TestRun.BrowserTitleOf("tests/expenses.spec.ts", "[chromium] lists the expenses"));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Browser_Title_Of_Ignores_A_Backend_Case_And_An_Empty_Name()
    {
        Assert.Null(TestRun.BrowserTitleOf("Library.Acceptance.Tests.JUnit.JUnitScenarios", "Borrowing_an_available_copy_succeeds"));
        Assert.Null(TestRun.BrowserTitleOf("Library.Acceptance.Tests.Trx.TrxScenarios", "Borrowing_an_available_copy_succeeds"));
        Assert.Null(TestRun.BrowserTitleOf("expenses.spec.ts", ""));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void A_Playwright_Case_Name_Resolves_Its_Title_Outcome()
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
