using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the stack readers keep the build identical and read JUnit results.</summary>
public sealed class StackReaderTests
{
    /// <summary><c>model build</c> on the fixture prints the counts its contents hold.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Theory]
    [InlineData("small", 72, 7, 0, 0)]
    [InlineData("large", 93, 7, 1, 9)]
    public void Build_Prints_Fixture_Counts(string fixture, int records, int handlers, int screens, int scenarios)
    {
        using var temp = ModelFixture.Prepare(fixture);
        var result = Cli.Run("model", "build", "--root", temp.Path);
        Assert.Equal(0, result.Status);
        Assert.Contains(
            $"{records} records, {handlers} handlers, {screens} screens, {scenarios} scenarios.",
            result.Stdout);
    }

    /// <summary>A project naming neither dotnet nor blazor reads no .NET backend or scenarios.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Build_Without_Dotnet_Or_Blazor_Reads_No_Backend()
    {
        using var temp = ModelFixture.Prepare("small");

        var projectFile = Path.Combine(temp.Path, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(projectFile))!.AsObject();
        project["stacks"] = new JsonArray(JsonValue.Create("react"));
        File.WriteAllText(projectFile, project.ToJsonString());

        var result = Cli.Run("model", "build", "--root", temp.Path);
        Assert.Equal(0, result.Status);
        Assert.Contains("0 handlers", result.Stdout);
        Assert.Contains("0 scenarios", result.Stdout);
    }

    /// <summary>Clean fixtures write no test run: neither holds a result file.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Theory]
    [InlineData("small")]
    [InlineData("large")]
    public void Build_Writes_Null_Test_Run_Without_Result_Files(string fixture)
    {
        using var temp = ModelFixture.Prepare(fixture);
        var output = Path.Combine(temp.Path, ".model", "model.json");
        var result = Cli.Run("model", "build", "--root", temp.Path, "--out", output);
        Assert.Equal(0, result.Status);

        using var document = JsonDocument.Parse(File.ReadAllText(output));

        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("testRun").ValueKind);
    }

    /// <summary>A JUnit file verifies the passing scenario's use case and leaves the failing one unverified.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void JUnit_Results_Verify_The_Passing_Scenario_Only()
    {
        using var temp = ModelFixture.Prepare("small");

        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy-junit-scenario.md"),
            BorrowScenarioPage);
        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "return-copy-junit-scenario.md"),
            ReturnScenarioPage);

        var scenariosFolder = Path.Combine(
            temp.Path, "apps", "api", "tests", "Library.Acceptance.Tests", "JUnit");
        Directory.CreateDirectory(scenariosFolder);
        File.WriteAllText(Path.Combine(scenariosFolder, "JUnitScenarios.cs"), ScenariosFile);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(Cli.Fixture("Model/junit/results.xml"), Path.Combine(resultsFolder, "results.xml"));

        var output = Path.Combine(temp.Path, ".model", "model.json");
        var built = Cli.Run("model", "build", "--root", temp.Path, "--out", output);
        Assert.Equal(0, built.Status);

        using var document = JsonDocument.Parse(File.ReadAllText(output));
        var root = document.RootElement;
        var run = root.GetProperty("testRun");

        Assert.Equal(2, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(1, run.GetProperty("failed").GetInt32());

        var status = root.GetProperty("status");

        Assert.True(status.GetProperty("use-case/lending.borrow-copy").GetProperty("verified").GetBoolean());
        Assert.False(status.GetProperty("use-case/lending.return-copy").GetProperty("verified").GetBoolean());
    }

    /// <summary>A TRX file verifies the passing scenario's use case and leaves the failing one unverified.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Trx_Results_Verify_The_Passing_Scenario_Only()
    {
        using var temp = ModelFixture.Prepare("small");

        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy-trx-scenario.md"),
            BorrowScenarioPage);
        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "return-copy-trx-scenario.md"),
            ReturnScenarioPage);

        var scenariosFolder = Path.Combine(
            temp.Path, "apps", "api", "tests", "Library.Acceptance.Tests", "Trx");
        Directory.CreateDirectory(scenariosFolder);
        File.WriteAllText(Path.Combine(scenariosFolder, "TrxScenarios.cs"), TrxScenariosFile);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(Cli.Fixture("Model/trx/results.trx"), Path.Combine(resultsFolder, "results.trx"));

        var output = Path.Combine(temp.Path, ".model", "model.json");
        var built = Cli.Run("model", "build", "--root", temp.Path, "--out", output);
        Assert.Equal(0, built.Status);

        using var document = JsonDocument.Parse(File.ReadAllText(output));
        var root = document.RootElement;
        var run = root.GetProperty("testRun");

        Assert.Equal(2, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(1, run.GetProperty("failed").GetInt32());

        var status = root.GetProperty("status");

        Assert.True(status.GetProperty("use-case/lending.borrow-copy").GetProperty("verified").GetBoolean());
        Assert.False(status.GetProperty("use-case/lending.return-copy").GetProperty("verified").GetBoolean());
    }

    /// <summary>A Playwright result verifies the browser-only use case and exercises its journey step.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Playwright_Results_Verify_The_Browser_Only_Use_Case()
    {
        using var temp = ModelFixture.Prepare("large");

        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "review-loans.md"),
            ReviewLoansPage);
        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "review-loans-browser-scenario.md"),
            ReviewLoansScenarioPage);
        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "screens", "library-app.loan-review.md"),
            LoanReviewScreenPage);

        var specsFolder = Path.Combine(temp.Path, "apps", "catalog", "tests");
        Directory.CreateDirectory(specsFolder);
        File.WriteAllText(Path.Combine(specsFolder, "loans.spec.ts"), LoansSpec);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(Cli.Fixture("Model/junit/browser.xml"), Path.Combine(resultsFolder, "browser.xml"));

        var output = Path.Combine(temp.Path, ".model", "model.json");
        var built = Cli.Run("model", "build", "--root", temp.Path, "--out", output);
        Assert.Equal(0, built.Status);

        using var document = JsonDocument.Parse(File.ReadAllText(output));
        var root = document.RootElement;
        var run = root.GetProperty("testRun");

        Assert.Equal(1, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(0, run.GetProperty("failed").GetInt32());

        var status = root.GetProperty("status");

        Assert.True(status.GetProperty("use-case/lending.review-loans").GetProperty("verified").GetBoolean());
        Assert.Contains(
            "journey/borrow-and-return#s4",
            root.GetProperty("exercisedSteps").EnumerateArray().Select(step => step.GetString()));
    }

    /// <summary>A browser success proof asserting another string confirms nothing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Playwright_Results_Asserting_Another_String_Do_Not_Verify()
    {
        using var temp = ModelFixture.Prepare("large");

        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "review-loans.md"),
            ReviewLoansPage);
        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "review-loans-browser-scenario.md"),
            ReviewLoansScenarioPage);
        File.WriteAllText(
            Path.Combine(temp.Path, "docs", "behavior", "screens", "library-app.loan-review.md"),
            LoanReviewScreenPage);

        var specsFolder = Path.Combine(temp.Path, "apps", "catalog", "tests");
        Directory.CreateDirectory(specsFolder);
        File.WriteAllText(Path.Combine(specsFolder, "loans.spec.ts"), LoansSpecAssertingAnotherString);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(Cli.Fixture("Model/junit/browser.xml"), Path.Combine(resultsFolder, "browser.xml"));

        var output = Path.Combine(temp.Path, ".model", "model.json");
        var built = Cli.Run("model", "build", "--root", temp.Path, "--out", output);
        Assert.Equal(0, built.Status);

        using var document = JsonDocument.Parse(File.ReadAllText(output));
        var root = document.RootElement;

        Assert.False(root.GetProperty("status").GetProperty("use-case/lending.review-loans").GetProperty("verified").GetBoolean());
        Assert.False(ProvesLink(root, "scenario/review-loans.reviewing-loans-succeeds").GetProperty("confirmed").GetBoolean());
    }

    static JsonElement ProvesLink(JsonElement root, string claim)
    {
        foreach (var link in root.GetProperty("links").GetProperty("proves").EnumerateArray())
        {
            if (link.GetProperty("claim").GetString() == claim)
            {
                return link;
            }
        }

        throw new InvalidOperationException($"No proves link claims '{claim}'.");
    }

    const string BorrowScenarioPage = """
        ---
        {
          "type": "scenario",
          "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
          "title": "Borrowing an available copy succeeds",
          "owner": "lending-team",
          "status": "reviewed",
          "given": "A copy sits available on its shelf.",
          "when": "The reader borrows the copy.",
          "result": "result/borrow-copy.borrowed",
          "then": "The loan is active."
        }
        ---

        # Borrowing an available copy succeeds

        A reader borrows an available copy.
        """;

    const string ReturnScenarioPage = """
        ---
        {
          "type": "scenario",
          "id": "scenario/return-copy.returning-a-borrowed-copy-succeeds",
          "title": "Returning a borrowed copy succeeds",
          "owner": "lending-team",
          "status": "reviewed",
          "given": "A loan stands active on a copy.",
          "when": "The reader returns the copy.",
          "result": "result/return-copy.returned",
          "then": "The loan is returned."
        }
        ---

        # Returning a borrowed copy succeeds

        A reader returns a borrowed copy.
        """;

    const string TrxScenariosFile = """
        namespace Library.Acceptance.Tests.Trx;

        public sealed class TrxScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            public Task Borrowing_an_available_copy_succeeds() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                then => new BorrowCopySteps().TheLoanIsActive());

            /// <proves>scenario/return-copy.returning-a-borrowed-copy-succeeds</proves>
            public Task Returning_a_borrowed_copy_succeeds() => Runner.RunScenarioAsync(
                given => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                when => new ReturnCopySteps().ReturnACopy("loan-1"),
                then => new ReturnCopySteps().TheLoanIsReturned());
        }
        """;

    const string ScenariosFile = """
        namespace Library.Acceptance.Tests.JUnit;

        public sealed class JUnitScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            public Task Borrowing_an_available_copy_succeeds() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                then => new BorrowCopySteps().TheLoanIsActive());

            /// <proves>scenario/return-copy.returning-a-borrowed-copy-succeeds</proves>
            public Task Returning_a_borrowed_copy_succeeds() => Runner.RunScenarioAsync(
                given => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                when => new ReturnCopySteps().ReturnACopy("loan-1"),
                then => new ReturnCopySteps().TheLoanIsReturned());
        }
        """;

    const string ReviewLoansPage = """
        ---
        {
          "type": "use-case",
          "id": "use-case/lending.review-loans",
          "title": "Review loans",
          "owner": "lending-team",
          "status": "reviewed",
          "kind": "query",
          "access": {
            "auth": [
              "session"
            ],
            "grant": "permission/read-loans",
            "tenant": "own"
          },
          "reachedThrough": [
            "entry-point/library-app"
          ],
          "serves": [
            "journey/borrow-and-return#s4"
          ],
          "records": [
            {
              "type": "result",
              "id": "result/review-loans.reviewed",
              "title": "Reviewed",
              "owner": "lending-team",
              "outcome": "success",
              "meaning": "The loans are listed."
            }
          ]
        }
        ---

        # Review loans

        A reader reviews their own loans.
        """;

    const string ReviewLoansScenarioPage = """
        ---
        {
          "type": "scenario",
          "id": "scenario/review-loans.reviewing-loans-succeeds",
          "title": "Reviewing loans succeeds",
          "owner": "lending-team",
          "status": "reviewed",
          "given": "Loans stand active on the reader.",
          "when": "The reader reviews their loans.",
          "result": "result/review-loans.reviewed",
          "then": "The loans are listed."
        }
        ---

        # Reviewing loans succeeds

        A reader reviews their loans.
        """;

    const string LoanReviewScreenPage = """
        ---
        {
          "type": "screen",
          "id": "screen/library-app.loan-review",
          "title": "Loan review",
          "owner": "lending-team",
          "status": "reviewed",
          "entryPoint": "entry-point/library-app",
          "address": "/loans",
          "offers": ["use-case/lending.review-loans"]
        }
        ---

        # Loan review

        ## Summary

        A reader reviews their loans.
        """;

    const string LoansSpec = """
        import { test, expect } from '@playwright/test';

        test.describe('loans list', () => {
          test('lists the loans', proves("scenario/review-loans.reviewing-loans-succeeds"), async ({ page }) => {
            await page.goto('/loans');
            await expect(page.getByText('The loans are listed.')).toBeVisible();
          });
        });
        """;

    const string LoansSpecAssertingAnotherString = """
        import { test, expect } from '@playwright/test';

        test.describe('loans list', () => {
          test('lists the loans', proves("scenario/review-loans.reviewing-loans-succeeds"), async ({ page }) => {
            await page.goto('/loans');
            await expect(page.getByText('Loans')).toBeVisible();
          });
        });
        """;
}
