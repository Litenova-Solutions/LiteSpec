using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the unrecorded findings against the marks fixtures: a use-case folder, an event handler and a job host with no record behind one finding each, and the per-class mark findings they cover.</summary>
public sealed class UnrecordedCodeTests
{
    /// <summary>Unmarked Folder Without A Record Reports Use Case Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void UnmarkedFolderWithoutARecordReportsUseCaseUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("missing");
        WriteRenewLoan(fixture.Path);

        var findings = MarksFixture.Check(fixture.Path);
        var finding = Assert.Single(MarksFixture.WithCode(findings, "use-case-unrecorded"));

        Assert.Contains("Application/Loans/RenewLoan", finding.Message);
        Assert.Contains("RenewLoanCommand", finding.Message);
        Assert.Contains(".litespec/behavior/modules/lending/aggregates/loans/use-cases/renew-loan/renew-loan.yaml", finding.Message);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.EndsWith("RenewLoanCommandHandler.cs", finding.Path);
    }

    /// <summary>Recorded Folder With An Unmarked Handler Keeps Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void RecordedFolderWithAnUnmarkedHandlerKeepsMarkMissing()
    {
        using var fixture = MarksFixture.Prepare("missing");

        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(MarksFixture.WithCode(findings, "use-case-unrecorded"), _ => true);
        Assert.Contains(MarksFixture.WithCode(findings, "mark-missing"), found => found.Message.Contains("BorrowCopyCommandHandler"));
    }

    /// <summary>Marked Folder With A Record Reports No Use Case Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void MarkedFolderWithARecordReportsNoUseCaseUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("clean");

        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(MarksFixture.WithCode(findings, "use-case-unrecorded"), _ => true);
        Assert.DoesNotContain(MarksFixture.WithCode(findings, "reaction-unrecorded"), _ => true);
        Assert.DoesNotContain(MarksFixture.WithCode(findings, "job-unrecorded"), _ => true);
    }

    /// <summary>Unrecorded Folder Covers The Mark Findings Of Its Classes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void UnrecordedFolderCoversTheMarkFindingsOfItsClasses()
    {
        using var fixture = MarksFixture.Prepare("missing");
        WriteRenewLoan(fixture.Path);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.Single(MarksFixture.WithCode(findings, "use-case-unrecorded"));
        Assert.DoesNotContain(findings, found =>
            found.Code is "mark-missing" or "mark-unknown" && found.Path.Contains("RenewLoan"));
        Assert.Contains(MarksFixture.WithCode(findings, "mark-missing"), found => found.Message.Contains("BorrowCopyCommandHandler"));
    }

    /// <summary>Unknown Marks In An Unrecorded Flat Folder Collapse To One Finding.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void UnknownMarksInAnUnrecordedFlatFolderCollapseToOneFinding()
    {
        using var fixture = MarksFixture.Prepare("missing");
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "PurgeHolds"), "PurgeHoldsCommand.cs", """
            namespace Library.Lending.Application.PurgeHolds;

            public sealed record PurgeHoldsCommand(Guid LoanId);

            public sealed record PurgeHoldsCommandResult(int Purged);
            """);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "PurgeHolds"), "PurgeHoldsCommandHandler.cs", """
            namespace Library.Lending.Application.PurgeHolds;

            /// <implements>use-case/lending.purge-holds</implements>
            public sealed class PurgeHoldsCommandHandler
                : ICommandHandler<PurgeHoldsCommand, PurgeHoldsCommandResult>
            {
                public Task<PurgeHoldsCommandResult> HandleAsync(PurgeHoldsCommand command, CancellationToken cancellationToken)
                {
                    return Task.FromResult(new PurgeHoldsCommandResult(0));
                }
            }
            """);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "WebApi", "PurgeHolds"), "PurgeHoldsEndpoint.cs", """
            namespace Library.Lending.WebApi.PurgeHolds;

            /// <implements>use-case/lending.purge-holds-report</implements>
            public static class PurgeHoldsEndpoint
            {
                public static void Map(WebApplication app)
                {
                    app.MapPost("/api/lending/purge-holds", Handle).WithName("PurgeHolds");
                }

                private static void Handle()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);
        var finding = Assert.Single(MarksFixture.WithCode(findings, "use-case-unrecorded"));

        Assert.Contains("Application/PurgeHolds", finding.Message);
        Assert.Contains("PurgeHoldsCommand", finding.Message);
        Assert.Contains(".litespec/behavior/modules/lending/use-cases/purge-holds/purge-holds.yaml", finding.Message);
        Assert.Equal("use-case/lending.purge-holds", finding.Subject);
        Assert.DoesNotContain(findings, found =>
            found.Code is "mark-missing" or "mark-unknown" && found.Path.Contains("PurgeHolds"));
    }

    /// <summary>Event Handler Without A Record Reports Reaction Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void EventHandlerWithoutARecordReportsReactionUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "Loans", "BorrowCopy"), "FlagLoanOnLoanOverdueHandler.cs", """
            namespace Library.Lending.Application.Loans.BorrowCopy;

            public sealed class FlagLoanOnLoanOverdueHandler : IEventHandler<LoanOverdueEvent>
            {
                public Task HandleAsync(LoanOverdueEvent fact, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);
        var finding = Assert.Single(MarksFixture.WithCode(findings, "reaction-unrecorded"));

        Assert.Contains("FlagLoanOnLoanOverdueHandler", finding.Message);
        Assert.Contains(".litespec/behavior/modules/lending/reactions/flag-loan-on-loan-overdue/flag-loan-on-loan-overdue.yaml", finding.Message);
        Assert.Equal("reaction/lending.flag-loan-on-loan-overdue", finding.Subject);
        Assert.DoesNotContain(findings, found =>
            found.Code is "mark-missing" or "mark-unknown" && found.Path.Contains("FlagLoanOnLoanOverdueHandler"));
    }

    /// <summary>Event Handler With A Record Reports No Reaction Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void EventHandlerWithARecordReportsNoReactionUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "Loans", "BorrowCopy"), "SendDueNoticeOnLoanBorrowedHandler.cs", """
            namespace Library.Lending.Application.Loans.BorrowCopy;

            /// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
            public sealed class SendDueNoticeOnLoanBorrowedHandler : IEventHandler<LoanBorrowedEvent>
            {
                public Task HandleAsync(LoanBorrowedEvent fact, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(MarksFixture.WithCode(findings, "reaction-unrecorded"), _ => true);
    }

    /// <summary>Job Host Without A Record Reports Job Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void JobHostWithoutARecordReportsJobUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "Notifications", "LoanDue"), "ExpireLapsedHoldsJob.cs", """
            namespace Library.Lending.Application.Notifications.LoanDue;

            public sealed class ExpireLapsedHoldsJob : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);
        var finding = Assert.Single(MarksFixture.WithCode(findings, "job-unrecorded"));

        Assert.Contains("ExpireLapsedHoldsJob", finding.Message);
        Assert.Contains(".litespec/behavior/modules/lending/jobs/expire-lapsed-holds/expire-lapsed-holds.yaml", finding.Message);
        Assert.Equal("job/lending.expire-lapsed-holds", finding.Subject);
        Assert.DoesNotContain(findings, found =>
            found.Code is "mark-missing" or "mark-unknown" && found.Path.Contains("ExpireLapsedHoldsJob"));
    }

    /// <summary>Job Host With A Record Reports No Job Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void JobHostWithARecordReportsNoJobUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "Notifications", "LoanDue"), "NightlyDueScanJob.cs", """
            namespace Library.Lending.Application.Notifications.LoanDue;

            /// <implements>job/lending.nightly-due-scan</implements>
            public sealed class NightlyDueScanJob : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(MarksFixture.WithCode(findings, "job-unrecorded"), _ => true);
    }

    /// <summary>Read Model Projection Keeps No Reaction Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ReadModelProjectionKeepsNoReactionUnrecorded()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteReadModel(fixture.Path);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "ReadModels", "ReimbursedExpenses"), "ReimbursedExpenseOnExpenseReimbursedProjection.cs", """
            namespace Library.Lending.Application.ReadModels.ReimbursedExpenses;

            /// <implements>read-model/lending.reimbursed-expense</implements>
            internal sealed class ReimbursedExpenseOnExpenseReimbursedProjection : IEventHandler<ExpenseReimbursedEvent>
            {
                public Task HandleAsync(ExpenseReimbursedEvent fact, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(MarksFixture.WithCode(findings, "reaction-unrecorded"), _ => true);
        Assert.DoesNotContain(findings, found =>
            found.Code is "mark-missing" or "mark-unknown" && found.Path.Contains("ReimbursedExpenseOnExpenseReimbursedProjection"));
    }

    /// <summary>Unmarked Read Model Projection Keeps Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void UnmarkedReadModelProjectionKeepsMarkMissing()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteReadModel(fixture.Path);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "ReadModels", "ReimbursedExpenses"), "ReimbursedExpenseOnExpenseReimbursedProjection.cs", """
            namespace Library.Lending.Application.ReadModels.ReimbursedExpenses;

            internal sealed class ReimbursedExpenseOnExpenseReimbursedProjection : IEventHandler<ExpenseReimbursedEvent>
            {
                public Task HandleAsync(ExpenseReimbursedEvent fact, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(MarksFixture.WithCode(findings, "reaction-unrecorded"), _ => true);
        Assert.Contains(MarksFixture.WithCode(findings, "mark-missing"), found => found.Path.Contains("ReimbursedExpenseOnExpenseReimbursedProjection"));
    }

    /// <summary>Writes the reimbursed-expense read model the projection tests mark.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteReadModel(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/lending/read-models/reimbursed-expense/reimbursed-expense.yaml", """
            {
              "type": "read-model",
              "id": "read-model/lending.reimbursed-expense",
              "title": "Reimbursed expense",
              "summary": "What lending keeps about each reimbursed expense.",
              "keyedBy": "aggregate/billing.invoice",
              "fields": [
                {
                  "name": "employeeId",
                  "meaning": "The employee who was paid.",
                  "type": "id",
                  "from": "integration-event/expense-reimbursed#employeeId",
                  "classification": "internal"
                }
              ]
            }
            """);
        TestRecords.Explain(root, ".litespec/behavior/modules/lending/read-models/reimbursed-expense/reimbursed-expense.md", """
            # Reimbursed expense

            What lending keeps about each reimbursed expense.
            """);
    }

    /// <summary>Writes an unmarked message, handler, validator and endpoint for the renew-loan folder, which names no record.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteRenewLoan(string root)
    {
        WriteSource(root, Path.Combine("Modules", "Library.Lending", "Application", "Loans", "RenewLoan"), "RenewLoanCommand.cs", """
            namespace Library.Lending.Application.Loans.RenewLoan;

            public sealed record RenewLoanCommand(Guid LoanId);

            public sealed record RenewLoanCommandResult(Guid LoanId);
            """);
        WriteSource(root, Path.Combine("Modules", "Library.Lending", "Application", "Loans", "RenewLoan"), "RenewLoanCommandHandler.cs", """
            namespace Library.Lending.Application.Loans.RenewLoan;

            public sealed class RenewLoanCommandHandler
                : ICommandHandler<RenewLoanCommand, RenewLoanCommandResult>
            {
                public Task<RenewLoanCommandResult> HandleAsync(RenewLoanCommand command, CancellationToken cancellationToken)
                {
                    return Task.FromResult(new RenewLoanCommandResult(command.LoanId));
                }
            }
            """);
        WriteSource(root, Path.Combine("Modules", "Library.Lending", "Application", "Loans", "RenewLoan"), "RenewLoanCommandValidator.cs", """
            namespace Library.Lending.Application.Loans.RenewLoan;

            internal sealed class RenewLoanCommandValidator
            {
                public Task ValidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);
        WriteSource(root, Path.Combine("Modules", "Library.Lending", "WebApi", "Loans", "RenewLoan"), "RenewLoanEndpoint.cs", """
            namespace Library.Lending.WebApi.Loans.RenewLoan;

            public static class RenewLoanEndpoint
            {
                public static void Map(WebApplication app)
                {
                    app.MapPost("/api/lending/renew-loan", Handle).WithName("RenewLoan");
                }

                private static void Handle()
                {
                }
            }
            """);
    }

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "apps", "api", "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
