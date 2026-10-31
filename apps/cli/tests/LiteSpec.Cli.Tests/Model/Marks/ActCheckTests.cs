using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>
///     Proves the marked-act findings against the act fixture, where two use cases
///     share one marked method.
/// </summary>
public sealed class ActCheckTests
{
    /// <proves>litespec/backend/domain#call-the-marked-act-from-each-command-handler</proves>
    [Fact]
    public void Handler_Calling_No_Marked_Method_Reports_Use_Case_Act_Uncalled()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("use-case/lending.renew-loan", finding.Message);
    }

    /// <proves>litespec/backend/domain#call-the-marked-act-from-each-command-handler</proves>
    [Fact]
    public void Handler_Calling_The_Shared_Method_Passes()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <proves>litespec/backend/domain#call-the-marked-act-from-each-command-handler</proves>
    [Fact]
    public void Fan_Out_Command_With_No_Changes_Reports_Nothing()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.fan-out");
    }

    /// <proves>litespec/backend/domain#call-the-marked-act-from-each-command-handler</proves>
    [Fact]
    public void Store_Only_Command_Reports_Nothing()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.record-scan");
    }

    /// <proves>litespec/backend/domain#call-the-marked-act-from-each-command-handler</proves>
    [Fact]
    public void Entity_Command_Without_A_Called_Act_Still_Reports()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void Shared_Method_Raising_An_Unlisted_Event_Reports_Act_Event_Undeclared()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        var finding = Assert.Single(undeclared);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("Loan.Borrow", finding.Message);
        Assert.Contains("event/loan.loan-borrowed", finding.Message);
    }

    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void Use_Case_Sharing_The_Method_Reports_No_Undeclared_For_Its_Listed_Event()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        Assert.DoesNotContain(undeclared, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void Listed_Event_The_Shared_Method_Never_Raises_Reports_Act_Event_Unraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unimplemented");

        var finding = Assert.Single(unraised);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("event/loan.loan-renewed", finding.Message);
    }

    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void Use_Case_Whose_Shared_Method_Raises_Its_Event_Reports_No_Unraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unimplemented");

        Assert.DoesNotContain(unraised, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void Use_Case_With_No_Marked_Method_Reports_No_Act_Event_Unraised()
    {
        using var temp = WriteUnmarkedConsumer();
        var unraised = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "act-event-unimplemented");

        Assert.DoesNotContain(unraised, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Unmarked_Raising_Method_Reports_Mark_Missing()
    {
        using var temp = WriteUnmarkedConsumer();
        var missing = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Loan.Borrow", finding.Message);
    }

    /// <summary>Writes a consumer where an entity command, a fan-out with no changes, and a store-only command each have a handler calling nothing.</summary>
    static TempFolder WritePassConsumer()
    {
        var temp = Cli.Temp("marks");

        try
        {
            Write(temp.Path, "litespec.project.json", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "reviewedLiteSpecVersion": "3.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": "docs",
                    "behaviorDocs": "docs/behavior",
                    "glossary": "docs/glossary.json",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "application": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow",
                      "fan",
                      "record"
                    ]
                  }
                }
                """);
            Write(temp.Path, "docs/glossary.json", """
                {
                  "schemaVersion": 2,
                  "terms": [],
                  "mannered": []
                }
                """);
            Write(temp.Path, "docs/behavior/modules/lending/loans/README.md", """
                ---
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "entity",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ]
                }
                ---

                # Loan

                A loan lends one copy to one reader.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/outbox/README.md", """
                ---
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.outbox",
                  "title": "Outbox",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "store"
                }
                ---

                # Outbox

                The lending-owned table of scans awaiting dispatch.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/loans/borrow-copy.md", """
                ---
                {
                  "type": "use-case",
                  "id": "use-case/lending.borrow-copy",
                  "title": "Borrow copy",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "changes": [
                    "aggregate/lending.loan"
                  ],
                  "records": [
                    {
                      "type": "result",
                      "id": "result/borrow-copy.borrowed",
                      "title": "Borrowed",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "The copy is lent."
                    }
                  ]
                }
                ---

                # Borrow copy

                A reader borrows an available copy.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/loans/fan-out.md", """
                ---
                {
                  "type": "use-case",
                  "id": "use-case/lending.fan-out",
                  "title": "Fan out",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "effects": [
                    {
                      "issues": "use-case/lending.borrow-copy",
                      "timing": "after-commit"
                    }
                  ],
                  "records": [
                    {
                      "type": "result",
                      "id": "result/fan-out.fanned-out",
                      "title": "Fanned out",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "One command per record is issued."
                    }
                  ]
                }
                ---

                # Fan out

                A pass issues one borrow command per record.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/outbox/record-scan.md", """
                ---
                {
                  "type": "use-case",
                  "id": "use-case/lending.record-scan",
                  "title": "Record scan",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "changes": [
                    "aggregate/lending.outbox"
                  ],
                  "records": [
                    {
                      "type": "result",
                      "id": "result/record-scan.recorded",
                      "title": "Recorded",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "The scan is recorded."
                    }
                  ]
                }
                ---

                # Record scan

                A kiosk records each scan attempt.
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopy/BorrowCopyCommand.cs", """
                namespace Library.Application.Lending.Loans.BorrowCopy;

                public sealed record BorrowCopyCommand(Guid CopyId);

                public sealed record BorrowCopyCommandResult(Guid CopyId);
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopy/BorrowCopyCommandHandler.cs", """
                namespace Library.Application.Lending.Loans.BorrowCopy;

                /// <implements>use-case/lending.borrow-copy</implements>
                public sealed class BorrowCopyCommandHandler
                    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
                {
                    public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                    {
                        return Task.FromResult(new BorrowCopyCommandResult(command.CopyId));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/FanOut/FanOutCommand.cs", """
                namespace Library.Application.Lending.Loans.FanOut;

                public sealed record FanOutCommand;

                public sealed record FanOutCommandResult(int Issued);
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/FanOut/FanOutCommandHandler.cs", """
                namespace Library.Application.Lending.Loans.FanOut;

                /// <implements>use-case/lending.fan-out</implements>
                public sealed class FanOutCommandHandler
                    : ICommandHandler<FanOutCommand, FanOutCommandResult>
                {
                    public Task<FanOutCommandResult> HandleAsync(FanOutCommand command, CancellationToken cancellationToken)
                    {
                        return Task.FromResult(new FanOutCommandResult(0));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Outbox/RecordScan/RecordScanCommand.cs", """
                namespace Library.Application.Lending.Outbox.RecordScan;

                public sealed record RecordScanCommand(string Barcode);

                public sealed record RecordScanCommandResult(string Barcode);
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Outbox/RecordScan/RecordScanCommandHandler.cs", """
                namespace Library.Application.Lending.Outbox.RecordScan;

                /// <implements>use-case/lending.record-scan</implements>
                public sealed class RecordScanCommandHandler
                    : ICommandHandler<RecordScanCommand, RecordScanCommandResult>
                {
                    public Task<RecordScanCommandResult> HandleAsync(RecordScanCommand command, CancellationToken cancellationToken)
                    {
                        return Task.FromResult(new RecordScanCommandResult(command.Barcode));
                    }
                }
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    /// <summary>Writes a consumer where the raising method carries no mark, so the use case has no marked method.</summary>
    static TempFolder WriteUnmarkedConsumer()
    {
        var temp = Cli.Temp("marks");

        try
        {
            Write(temp.Path, "litespec.project.json", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "reviewedLiteSpecVersion": "3.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": "docs",
                    "behaviorDocs": "docs/behavior",
                    "glossary": "docs/glossary.json",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "application": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow"
                    ]
                  }
                }
                """);
            Write(temp.Path, "docs/glossary.json", """
                {
                  "schemaVersion": 2,
                  "terms": [],
                  "mannered": []
                }
                """);
            Write(temp.Path, "docs/behavior/modules/lending/loans/borrow-copy.md", """
                ---
                {
                  "type": "use-case",
                  "id": "use-case/lending.borrow-copy",
                  "title": "Borrow copy",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "raises": [
                    "event/loan.loan-borrowed"
                  ],
                  "changes": [
                    "aggregate/lending.loan"
                  ],
                  "records": [
                    {
                      "type": "result",
                      "id": "result/borrow-copy.borrowed",
                      "title": "Borrowed",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "The copy is lent."
                    }
                  ]
                }
                ---

                # Borrow copy

                A reader borrows an available copy.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/loans/README.md", """
                ---
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ],
                  "records": [
                    {
                      "type": "event",
                      "id": "event/loan.loan-borrowed",
                      "title": "Loan borrowed",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "meaning": "A copy was lent.",
                      "carries": [
                        "loanId"
                      ]
                    }
                  ]
                }
                ---

                # Loan

                A loan lends one copy to one reader.
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/Loan.cs", """
                namespace Library.Domain.Lending.Loans;

                /// <implements>aggregate/lending.loan</implements>
                public sealed class Loan : AggregateRoot<LoanId>
                {
                    public Guid Id { get; init; }

                    public void Borrow(Guid readerId, DateOnly dueDate)
                    {
                        RaiseDomainEvent(new LoanBorrowedEvent(Id, readerId, dueDate));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/Events/LoanBorrowedEvent.cs", """
                namespace Library.Domain.Lending.Loans.Events;

                /// <implements>event/loan.loan-borrowed</implements>
                public sealed record LoanBorrowedEvent(Guid LoanId, Guid ReaderId, DateOnly DueDate) : IDomainEvent;
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
