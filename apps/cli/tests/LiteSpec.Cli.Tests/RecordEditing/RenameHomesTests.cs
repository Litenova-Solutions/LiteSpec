using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec rename</c> keeping each record at its level and owner, and moving whole folders only with <c>--move</c>.</summary>
public sealed class RenameHomesTests
{
    const string Expenses = ".litespec/behavior/modules/spending/aggregates/expenses";

    /// <summary>A plain rename keeps a use case in its aggregate's use-cases folder and takes the new name.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename keeps a use case in its aggregate's use-cases folder")]
    public void RenameKeepsAUseCaseUnderItsAggregate()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "use-case/spending.submit-expense", "use-case/spending.file-expense", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, Expenses, "use-cases", "file-expense", "file-expense.yaml");
        Assert.True(File.Exists(moved), result.Output);
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, Expenses, "use-cases", "submit-expense")));
        Assert.Contains("id: use-case/spending.file-expense", File.ReadAllText(moved));
    }

    /// <summary>A plain rename keeps an aggregate in its module's aggregates folder, and the folder moves whole with its use cases.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename keeps an aggregate in its module and moves its folder whole")]
    public void RenameKeepsAnAggregateInItsModule()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "aggregate/spending.expenses", "aggregate/spending.claims", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var aggregates = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/aggregates");
        Assert.True(File.Exists(Path.Combine(aggregates, "claims", "aggregate.yaml")), result.Output);
        Assert.True(File.Exists(Path.Combine(aggregates, "claims", "use-cases", "submit-expense", "submit-expense.yaml")));
        Assert.False(Directory.Exists(Path.Combine(aggregates, "expenses")));
    }

    /// <summary>A plain rename keeps a journey in its folder beside the other journeys, with its explanation beside it.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename keeps a journey in its folder and moves its explanation with it")]
    public void RenameKeepsAJourneyAndItsExplanation()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "journey/close-the-year", "journey/year-end", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var journeys = Path.Combine(consumer.Path, ".litespec/intent/journeys");
        Assert.True(File.Exists(Path.Combine(journeys, "year-end", "year-end.yaml")), result.Output);
        Assert.True(File.Exists(Path.Combine(journeys, "year-end", "year-end.md")));
        Assert.False(Directory.Exists(Path.Combine(journeys, "close-the-year")));
    }

    /// <summary>A rename that names another owner refuses without --move and changes nothing.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename refuses another owner without --move and changes nothing")]
    public void RenameRefusesAnotherOwnerWithoutMove()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "use-case/spending.submit-expense", "use-case/billing.submit-expense", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("without --move", result.Stderr);
        Assert.True(File.Exists(Path.Combine(consumer.Path, Expenses, "use-cases", "submit-expense", "submit-expense.yaml")));
    }

    /// <summary>With --move a use case takes the owner its new identifier names, in that module's use-cases folder.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename with --move places a use case under its new owner")]
    public void RenameWithMoveChangesTheOwner()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "use-case/spending.submit-expense", "use-case/billing.submit-expense", "--move", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, ".litespec/behavior/modules/billing/use-cases/submit-expense/submit-expense.yaml");
        Assert.True(File.Exists(moved), result.Output);
        Assert.Contains("id: use-case/billing.submit-expense", File.ReadAllText(moved));
    }

    /// <summary>A record folder moves whole, so a supporting file it holds moves with the record.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename moves a record folder whole with its supporting files")]
    public void RenameMovesTheWholeRecordFolder()
    {
        using var consumer = Consumer();
        var folder = Path.Combine(consumer.Path, ".litespec/intent/journeys/close-the-year");
        File.WriteAllText(Path.Combine(folder, "fixtures.csv"), "reader,fare\n");

        var result = TestCli.Run("rename", "journey/close-the-year", "journey/year-end", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".litespec/intent/journeys/year-end/fixtures.csv")), result.Output);
    }

    /// <summary>A destination folder that already exists refuses the move before anything changes.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename refuses a taken folder before it moves anything")]
    public void RenameRefusesATakenFolderBeforeItMoves()
    {
        using var consumer = Consumer();
        Directory.CreateDirectory(Path.Combine(consumer.Path, ".litespec/intent/journeys/year-end"));

        var result = TestCli.Run("rename", "journey/close-the-year", "journey/year-end", "--root", consumer.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("already exists", result.Stderr);
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".litespec/intent/journeys/close-the-year/close-the-year.yaml")));
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".litespec/intent/journeys/close-the-year/close-the-year.md")));
    }

    /// <summary>A consumer holding one use case in an aggregate, one aggregate, and one journey with its explanation.</summary>
    /// <returns>The temporary consumer folder.</returns>
    static TempFolder Consumer()
    {
        var temp = TestCli.Temp("rename-homes");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, $"{Expenses}/aggregate.yaml", """
            {
              "type": "aggregate",
              "id": "aggregate/spending.expenses",
              "title": "Expenses"
            }
            """);
        TestRecords.Write(temp.Path, $"{Expenses}/use-cases/submit-expense/submit-expense.yaml", """
            {
              "type": "use-case",
              "id": "use-case/spending.submit-expense",
              "title": "Submit Expense"
            }
            """);
        TestRecords.Write(temp.Path, ".litespec/intent/journeys/close-the-year/close-the-year.yaml", """
            {
              "type": "journey",
              "id": "journey/close-the-year",
              "title": "Close the Year"
            }
            """);
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/intent/journeys/close-the-year/close-the-year.md"), "# Close the Year\n\nA reader closes the year.\n");
        return temp;
    }
}
