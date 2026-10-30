using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves <c>litespec model backfill</c> drafts missing fields from code and nothing else.</summary>
public sealed class ModelBackfillTests
{
    static string Page(string root, params string[] parts) => Path.Combine([root, .. parts]);

    /// <summary>Removes a Svelte page's module script, which holds its screen mark.</summary>
    static void Unmark(string page)
    {
        var text = File.ReadAllText(page);
        var end = text.IndexOf("</script>", StringComparison.Ordinal) + "</script>".Length;
        File.WriteAllText(page, text[end..].TrimStart());
    }

    static string BorrowCopy(string root) => Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string BorrowCopyHandler(string root) => Page(
        root, "apps", "api", "src", "Library.Application", "Lending", "Loans",
        "BorrowCopy", "BorrowCopyCommandHandler.cs");

    /// <summary>Backfill drafts the missing changes and marks the record transcribed.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Adds_Missing_Changes_And_Marks_Transcribed()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");
        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("+changes aggregate/lending.hold", result.Output);
        Assert.Contains("lending", result.Output);
        Assert.Contains("Changes", result.Output);
        Assert.Contains("Reads", result.Output);
        Assert.Contains("Effects", result.Output);
        Assert.Contains("Results", result.Output);

        var text = File.ReadAllText(BorrowCopy(fixture.Path));

        Assert.Contains("aggregate/lending.hold", text);
        Assert.Contains("\"source\": \"transcribed\"", text);
        Assert.Contains("result/borrow-copy.copy-unavailable", text);
    }

    /// <summary>A dry run prints the planned writes and leaves every file alone.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Dry_Run_Writes_Nothing()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");
        var before = File.ReadAllText(BorrowCopy(fixture.Path));
        var result = Cli.Run("model", "backfill", "--root", fixture.Path, "--dry-run");

        Assert.Equal(0, result.Status);
        Assert.Contains("(dry run)", result.Output);
        Assert.Contains("+changes aggregate/lending.hold", result.Output);
        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
    }

    /// <summary>Backfill drafts a refusal result for a code no result declares.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Adds_Missing_Refusal_Results()
    {
        using var fixture = ModelFixture.Prepare("large", "result-undeclared");
        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("+results lending.ghost-refusal", result.Output);

        var text = File.ReadAllText(BorrowCopy(fixture.Path));

        Assert.Contains("result/borrow-copy.ghost-refusal", text);
        Assert.Contains("lending.ghost-refusal", text);
        Assert.Contains("result/borrow-copy.copy-unavailable", text);
        Assert.Contains("\"source\": \"transcribed\"", text);
    }

    /// <summary>Human entries keep their place and are never duplicated or rewritten.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Preserves_Human_Entries()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        var path = BorrowCopy(fixture.Path);
        var drafted = File.ReadAllText(path).Replace(
            "\"kind\": \"command\",",
            "\"kind\": \"command\",\n  \"changes\": [\n    \"aggregate/lending.loan\"\n  ],",
            StringComparison.Ordinal);

        File.WriteAllText(path, drafted);

        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var text = File.ReadAllText(path);

        Assert.True(text.IndexOf("aggregate/lending.loan", StringComparison.Ordinal)
            < text.IndexOf("aggregate/lending.hold", StringComparison.Ordinal));
        Assert.Equal(2, text.Split("aggregate/lending").Length - 1);
    }

    /// <summary>Backfill drafts an effects entry for a dispatch that resolves to a use case.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Adds_Effects_From_Dispatch()
    {
        using var fixture = ModelFixture.Prepare("large");

        File.WriteAllText(BorrowCopyHandler(fixture.Path), """
            using Library.Domain.Lending.Loans.Exceptions;

            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler(ICommandMediator mediator) : ICommandHandler<BorrowCopyCommand>
            {
                public async Task Handle(BorrowCopyCommand command)
                {
                    if (command.CopyId == Guid.Empty)
                        throw new CopyNotAvailableException();
                    await mediator.SendAsync(new ExtendLoanCommand());
                }
            }
            """);

        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("+effects issues:use-case/lending.extend-loan", result.Output);

        var text = File.ReadAllText(BorrowCopy(fixture.Path));

        Assert.Contains("use-case/lending.extend-loan", text);
        Assert.Contains("\"timing\": \"after-commit\"", text);
    }

    /// <summary>A dispatch with no matching record is skipped rather than invented.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Skips_Unresolvable_Effect_Targets()
    {
        using var fixture = ModelFixture.Prepare("large", "effect-undeclared");
        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("+effects", result.Output);
        Assert.DoesNotContain("\"effects\"", File.ReadAllText(BorrowCopy(fixture.Path)));
    }

    /// <summary>Backfill drafts a reaction's reads from its constructor repositories.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Backfills_Reaction_Reads()
    {
        using var fixture = ModelFixture.Prepare("large");

        var factFolder = Path.Combine(
            fixture.Path, "apps", "api", "src", "Library.Application",
            "Lending", "Loans", "OnLoanBorrowed");
        Directory.CreateDirectory(factFolder);

        File.WriteAllText(
            Path.Combine(factFolder, "SendDueNoticeOnLoanBorrowedHandler.cs"),
            """
            namespace Library.Application.Lending.Loans.OnLoanBorrowed;

            /// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
            public sealed class SendDueNoticeOnLoanBorrowedHandler(IHoldRepository holds) : IEventHandler<LoanBorrowedEvent>
            {
                public Task Handle() => Task.CompletedTask;
            }
            """);

        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("+reads aggregate/lending.hold", result.Output);

        var text = File.ReadAllText(Page(
            fixture.Path, "docs", "behavior", "modules", "lending",
            "reactions", "send-due-notice-on-loan-borrowed.md"));

        Assert.Contains("aggregate/lending.hold", text);
        Assert.Contains("\"source\": \"transcribed\"", text);
    }

    /// <summary>Backfill drafts a job's issues from its dispatched commands.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Backfills_Job_Issues()
    {
        using var fixture = ModelFixture.Prepare("large");

        File.WriteAllText(
            Path.Combine(
                fixture.Path, "apps", "api", "src", "Library.Application",
                "Lending", "Loans", "NightlyDueScanJob.cs"),
            """
            namespace Library.Application.Lending.Loans;

            /// <implements>job/lending.nightly-due-scan</implements>
            public sealed class NightlyDueScanJob(ICommandMediator mediator) : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken stoppingToken)
                {
                    await mediator.SendAsync(new ExtendLoanCommand());
                }
            }
            """);

        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("+issues use-case/lending.extend-loan", result.Output);

        var text = File.ReadAllText(Page(
            fixture.Path, "docs", "behavior", "modules", "lending",
            "jobs", "nightly-due-scan.md"));

        Assert.Contains("use-case/lending.extend-loan", text);
        Assert.Contains("\"source\": \"transcribed\"", text);
    }

    /// <summary>Backfill on a clean fixture drafts only the unmarked route's screen and still prints the table.</summary>
    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Backfill_On_Clean_Fixture_Drafts_The_Unmarked_Route_Screen()
    {
        using var fixture = ModelFixture.Prepare("large");
        Unmark(Page(fixture.Path, "apps", "catalog", "src", "routes", "loans", "+page.svelte"));
        File.Delete(Page(fixture.Path, "docs", "behavior", "screens", "library-app.loans.md"));

        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("lending", result.Output);
        Assert.Contains("screen/library-app.loans", result.Output);
        Assert.True(File.Exists(Page(
            fixture.Path, "docs", "behavior", "screens", "library-app.loans.md")));
    }

    /// <summary>Two unmarked routes ending in one segment draft two screens on two pages, named from their whole address.</summary>
    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Backfill_Names_Each_Screen_From_Its_Whole_Address()
    {
        using var fixture = ModelFixture.Prepare("large");
        Unmark(Page(fixture.Path, "apps", "catalog", "src", "routes", "loans", "+page.svelte"));
        File.Delete(Page(fixture.Path, "docs", "behavior", "screens", "library-app.loans.md"));
        var archive = Page(fixture.Path, "apps", "catalog", "src", "routes", "archive", "loans", "+page.svelte");
        Directory.CreateDirectory(Path.GetDirectoryName(archive)!);
        File.WriteAllText(archive, "<h1>Archived loans</h1>\n");

        var result = Cli.Run("model", "backfill", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("screen/library-app.archive-loans", result.Output);
        Assert.True(File.Exists(Page(fixture.Path, "docs", "behavior", "screens", "library-app.loans.md")));
        Assert.True(File.Exists(Page(fixture.Path, "docs", "behavior", "screens", "library-app.archive-loans.md")));
    }

    /// <summary>Backfill limits itself to the named module.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Filters_By_Module()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");
        var result = Cli.Run("model", "backfill", "--root", fixture.Path, "lending");

        Assert.Equal(0, result.Status);
        Assert.Contains("aggregate/lending.hold", File.ReadAllText(BorrowCopy(fixture.Path)));
    }

    /// <summary>Backfill names the module when it holds no backfillable records.</summary>
    /// <proves>litespec/core/agent#mark-transcribed-records-during-adoption</proves>
    [Fact]
    public void Backfill_Unknown_Module_Exits_1()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");
        var result = Cli.Run("model", "backfill", "--root", fixture.Path, "billing");

        Assert.Equal(1, result.Status);
        Assert.Contains("billing", result.Output);
    }
}
