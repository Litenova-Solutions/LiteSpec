using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>
///     Proves the marked-act findings against the act fixture, where two use cases
///     share one marked method.
/// </summary>
public sealed class ActCheckTests
{
    /// <summary>Handler Calling No Marked Method Reports Use Case Act Uncalled.</summary>
    /// <proves>litespec/backend/domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void HandlerCallingNoMarkedMethodReportsUseCaseActUncalled()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("use-case/lending.renew-loan", finding.Message);
    }

    /// <summary>Handler Calling The Shared Method Passes.</summary>
    /// <proves>litespec/backend/domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void HandlerCallingTheSharedMethodPasses()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Fan Out Command With No Writes Reports Nothing.</summary>
    /// <proves>litespec/backend/domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void FanOutCommandWithNoWritesReportsNothing()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.fan-out");
    }

    /// <summary>Entity Command Without A Called Act Still Reports.</summary>
    /// <proves>litespec/backend/domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void EntityCommandWithoutACalledActStillReports()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Shared Method Raising An Unlisted Event Reports Act Event Undeclared.</summary>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void SharedMethodRaisingAnUnlistedEventReportsActEventUndeclared()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        var finding = Assert.Single(undeclared);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("Loan.Borrow", finding.Message);
        Assert.Contains("domain-event/loan.loan-borrowed", finding.Message);
    }

    /// <summary>Use Case Sharing The Method Reports No Undeclared For Its Listed Event.</summary>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void UseCaseSharingTheMethodReportsNoUndeclaredForItsListedEvent()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        Assert.DoesNotContain(undeclared, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Listed Event The Shared Method Never Raises Reports Act Event Unraised.</summary>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void ListedEventTheSharedMethodNeverRaisesReportsActEventUnraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unimplemented");

        var finding = Assert.Single(unraised);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("domain-event/loan.loan-renewed", finding.Message);
    }

    /// <summary>Use Case Whose Shared Method Raises Its Event Reports No Unraised.</summary>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void UseCaseWhoseSharedMethodRaisesItsEventReportsNoUnraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unimplemented");

        Assert.DoesNotContain(unraised, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Use Case With No Marked Method Reports No Act Event Unraised.</summary>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void UseCaseWithNoMarkedMethodReportsNoActEventUnraised()
    {
        using var temp = WriteUnmarkedConsumer();
        var unraised = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "act-event-unimplemented");

        Assert.DoesNotContain(unraised, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Unmarked Raising Method Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void UnmarkedRaisingMethodReportsMarkMissing()
    {
        using var temp = WriteUnmarkedConsumer();
        var missing = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Loan.Borrow", finding.Message);
    }

    /// <summary>Writes a consumer where an entity command and a fan-out with no changes each have a handler calling nothing.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WritePassConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "7.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow",
                      "fan"
                    ]
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.md", """
                # Loan

                A loan lends one copy to one reader.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/lending.borrow-copy",
                  "title": "Borrow copy",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "writes": [
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
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy.md", """
                # Borrow copy

                A reader borrows an available copy.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/fan-out.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/lending.fan-out",
                  "title": "Fan out",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
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
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/fan-out.md", """
                # Fan out

                A pass issues one borrow command per record.
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
            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    /// <summary>Writes a consumer where the raising method carries no mark, so the use case has no marked method.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteUnmarkedConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "7.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Library.Application",
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
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/lending.borrow-copy",
                  "title": "Borrow copy",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "raises": [
                    "domain-event/loan.loan-borrowed"
                  ],
                  "writes": [
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
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy.md", """
                # Borrow copy

                A reader borrows an available copy.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ],
                  "records": [
                    {
                      "type": "domain-event",
                      "id": "domain-event/loan.loan-borrowed",
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
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.md", """
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

                /// <implements>domain-event/loan.loan-borrowed</implements>
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
