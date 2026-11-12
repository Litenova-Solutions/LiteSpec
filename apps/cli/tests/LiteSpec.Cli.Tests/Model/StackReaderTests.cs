using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the stack readers keep the build identical and read JUnit results.</summary>
public sealed class StackReaderTests
{
    /// <summary>The builder reads the counts the fixture contents hold.</summary>
    /// <param name="fixture">The fixture size name the case builds, small or large.</param>
    /// <param name="records">The record count the case expects the builder to read.</param>
    /// <param name="handlers">The handler count the case expects the builder to read.</param>
    /// <param name="screens">The screen count the case expects the builder to read.</param>
    /// <param name="scenarios">The scenario count the case expects the builder to read.</param>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Theory]
    [InlineData("small", 73, 7, 0, 0)]
    [InlineData("large", 94, 7, 1, 9)]
    public void BuildReadsFixtureCounts(string fixture, int records, int handlers, int screens, int scenarios)
    {
        using var temp = ModelFixture.Prepare(fixture);
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Equal(records, model.Records.Count);
        Assert.Equal(handlers, model.Code.Handlers.Count);
        Assert.Equal(screens, model.Screens.Count);
        Assert.Equal(scenarios, model.Tests.Count);
    }

    /// <summary>A project naming neither dotnet nor blazor reads no .NET backend or scenarios.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void BuildWithoutDotnetOrBlazorReadsNoBackend()
    {
        using var temp = ModelFixture.Prepare("small");

        var projectFile = Path.Combine(temp.Path, ".litespec/project.yaml");
        var project = RecordText.Read(projectFile)!.AsObject();
        project["stacks"] = new JsonArray(JsonValue.Create("react"));
        File.WriteAllText(projectFile, project.ToJsonString());

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Empty(model.Code.Handlers);
        Assert.Empty(model.Tests);
    }

    /// <summary>Clean fixtures hold no test run: neither holds a result file.</summary>
    /// <param name="fixture">The fixture size name the case builds without a result file.</param>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Theory]
    [InlineData("small")]
    [InlineData("large")]
    public void BuildHoldsNullTestRunWithoutResultFiles(string fixture)
    {
        using var temp = ModelFixture.Prepare(fixture);
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Null(model.TestRun);
    }

    /// <summary>A JUnit file verifies the passing scenario's use case and leaves the failing one unverified.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void JUnitResultsVerifyThePassingScenarioOnly()
    {
        using var temp = ModelFixture.Prepare("small");

        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/borrow-copy-junit-scenario.yaml",
            BorrowScenarioJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/borrow-copy-junit-scenario.md",
            BorrowScenarioExplanation);
        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/return-copy-junit-scenario.yaml",
            ReturnScenarioJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/return-copy-junit-scenario.md",
            ReturnScenarioExplanation);

        var scenariosFolder = Path.Combine(
            temp.Path, "apps", "api", "tests", "Library.Acceptance.Tests", "JUnit");
        Directory.CreateDirectory(scenariosFolder);
        File.WriteAllText(Path.Combine(scenariosFolder, "JUnitScenarios.cs"), ScenariosFile);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(TestCli.Fixture("Model/junit/results.xml"), Path.Combine(resultsFolder, "results.xml"));

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());
        var root = document.RootElement;
        var run = root.GetProperty("testRun");

        Assert.Equal(2, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(1, run.GetProperty("failed").GetInt32());

        Assert.Equal("verified", CompletionOf(root, "use-case/lending.borrow-copy").GetProperty("stage").GetString());
        Assert.Equal("coded", CompletionOf(root, "use-case/lending.return-copy").GetProperty("stage").GetString());
    }

    /// <summary>A TRX file verifies the passing scenario's use case and leaves the failing one unverified.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void TrxResultsVerifyThePassingScenarioOnly()
    {
        using var temp = ModelFixture.Prepare("small");

        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/borrow-copy-trx-scenario.yaml",
            BorrowScenarioJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/borrow-copy-trx-scenario.md",
            BorrowScenarioExplanation);
        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/return-copy-trx-scenario.yaml",
            ReturnScenarioJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/return-copy-trx-scenario.md",
            ReturnScenarioExplanation);

        var scenariosFolder = Path.Combine(
            temp.Path, "apps", "api", "tests", "Library.Acceptance.Tests", "Trx");
        Directory.CreateDirectory(scenariosFolder);
        File.WriteAllText(Path.Combine(scenariosFolder, "TrxScenarios.cs"), TrxScenariosFile);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(TestCli.Fixture("Model/trx/results.trx"), Path.Combine(resultsFolder, "results.trx"));

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());
        var root = document.RootElement;
        var run = root.GetProperty("testRun");

        Assert.Equal(2, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(1, run.GetProperty("failed").GetInt32());

        Assert.Equal("verified", CompletionOf(root, "use-case/lending.borrow-copy").GetProperty("stage").GetString());
        Assert.Equal("coded", CompletionOf(root, "use-case/lending.return-copy").GetProperty("stage").GetString());
    }

    /// <summary>A Playwright result verifies the browser-only use case and exercises its journey step.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PlaywrightResultsVerifyTheBrowserOnlyUseCase()
    {
        using var temp = ModelFixture.Prepare("large");

        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans.yaml",
            ReviewLoansJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans.md",
            ReviewLoansExplanation);
        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans-browser-scenario.yaml",
            ReviewLoansScenarioJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans-browser-scenario.md",
            ReviewLoansScenarioExplanation);
        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/screens/library-app.loan-review.yaml",
            LoanReviewScreenJson);

        var specsFolder = Path.Combine(temp.Path, "apps", "catalog", "tests");
        Directory.CreateDirectory(specsFolder);
        File.WriteAllText(Path.Combine(specsFolder, "loans.spec.ts"), LoansSpec);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(TestCli.Fixture("Model/junit/browser.xml"), Path.Combine(resultsFolder, "browser.xml"));

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());
        var root = document.RootElement;
        var run = root.GetProperty("testRun");

        Assert.Equal(1, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(0, run.GetProperty("failed").GetInt32());

        Assert.Equal("verified", CompletionOf(root, "use-case/lending.review-loans").GetProperty("stage").GetString());
        Assert.Contains(
            "journey/borrow-and-return#s4",
            root.GetProperty("exercisedSteps").EnumerateArray().Select(step => step.GetString()));
    }

    /// <summary>A browser success proof asserting another string confirms nothing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PlaywrightResultsAssertingAnotherStringDoNotVerify()
    {
        using var temp = ModelFixture.Prepare("large");

        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans.yaml",
            ReviewLoansJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans.md",
            ReviewLoansExplanation);
        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans-browser-scenario.yaml",
            ReviewLoansScenarioJson);
        TestRecords.Explain(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/review-loans-browser-scenario.md",
            ReviewLoansScenarioExplanation);
        TestRecords.Write(
            temp.Path,
            ".litespec/behavior/screens/library-app.loan-review.yaml",
            LoanReviewScreenJson);

        var specsFolder = Path.Combine(temp.Path, "apps", "catalog", "tests");
        Directory.CreateDirectory(specsFolder);
        File.WriteAllText(Path.Combine(specsFolder, "loans.spec.ts"), LoansSpecAssertingAnotherString);

        var resultsFolder = Path.Combine(temp.Path, "TestResults");
        Directory.CreateDirectory(resultsFolder);
        File.Copy(TestCli.Fixture("Model/junit/browser.xml"), Path.Combine(resultsFolder, "browser.xml"));

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());
        var root = document.RootElement;

        Assert.Equal("specified", CompletionOf(root, "use-case/lending.review-loans").GetProperty("stage").GetString());
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

    static JsonElement CompletionOf(JsonElement root, string id)
    {
        foreach (var record in root.GetProperty("records").EnumerateArray())
        {
            if (record.GetProperty("id").GetString() == id)
            {
                return record.GetProperty("completion");
            }
        }

        throw new InvalidOperationException($"No record holds '{id}'.");
    }

    const string BorrowScenarioJson = """
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
        """;

    const string BorrowScenarioExplanation = """
        # Borrowing an available copy succeeds

        A reader borrows an available copy.
        """;

    const string ReturnScenarioJson = """
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
        """;

    const string ReturnScenarioExplanation = """
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

    const string ReviewLoansJson = """
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
        """;

    const string ReviewLoansExplanation = """
        # Review loans

        A reader reviews their own loans.
        """;

    const string ReviewLoansScenarioJson = """
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
        """;

    const string ReviewLoansScenarioExplanation = """
        # Reviewing loans succeeds

        A reader reviews their loans.
        """;

    const string LoanReviewScreenJson = """
        {
          "type": "screen",
          "id": "screen/library-app.loan-review",
          "title": "Loan review",
          "owner": "lending-team",
          "status": "reviewed",
          "entryPoint": "entry-point/library-app",
          "address": "/loans",
          "offers": ["use-case/lending.review-loans"],
          "summary": "A reader reviews their loans."
        }
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
