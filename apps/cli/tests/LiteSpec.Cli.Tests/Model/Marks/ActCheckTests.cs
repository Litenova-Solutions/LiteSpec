using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>
///     Proves the marked-act findings against the act fixture, where two use cases
///     share one marked method.
/// </summary>
public sealed class ActCheckTests
{
    /// <summary>Handler Calling No Marked Method Reports Use Case Act Uncalled.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
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
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void HandlerCallingTheSharedMethodPasses()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Fan Out Command With No Writes Reports Nothing.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void FanOutCommandWithNoWritesReportsNothing()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.fan-out");
    }

    /// <summary>Entity Command Without A Called Act Still Reports.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void EntityCommandWithoutACalledActStillReports()
    {
        using var temp = WritePassConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Shared Method Raising An Unlisted Event Reports Act Event Undeclared.</summary>
    /// <proves>litespec/core/backend-domain#raise-the-declared-events-from-the-marked-act</proves>
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
    /// <proves>litespec/core/backend-domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void UseCaseSharingTheMethodReportsNoUndeclaredForItsListedEvent()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        Assert.DoesNotContain(undeclared, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Listed Event The Shared Method Never Raises Reports Act Event Unraised.</summary>
    /// <proves>litespec/core/backend-domain#raise-the-declared-events-from-the-marked-act</proves>
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
    /// <proves>litespec/core/backend-domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact]
    public void UseCaseWhoseSharedMethodRaisesItsEventReportsNoUnraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unimplemented");

        Assert.DoesNotContain(unraised, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>Use Case With No Marked Method Reports No Act Event Unraised.</summary>
    /// <proves>litespec/core/backend-domain#raise-the-declared-events-from-the-marked-act</proves>
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

    /// <summary>Handler Loading Through ConfigureAwait And Calling Through A Helper Or Loop Reports Nothing.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void HandlerLoadingThroughConfigureAwaitReportsNothing()
    {
        using var temp = WriteModuleConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.abandon-loan", finding.Subject);
    }

    /// <summary>Writes a consumer in the module layout where three handlers call the marked act through an awaited load, a helper parameter, and a loop, and one calls nothing.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteModuleConsumer()
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
                      "modules": "apps/api/src/Modules/*",
                      "kernel": "apps/api/src/Library.Kernel",
                      "platform": "apps/api/src/Library.Platform"
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow",
                      "renew",
                      "expire",
                      "abandon"
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
            foreach (var (useCase, title) in new[] { ("borrow-copy", "Borrow copy"), ("renew-loan", "Renew loan"), ("expire-loans", "Expire loans"), ("abandon-loan", "Abandon loan") })
            {
                TestRecords.Write(temp.Path, $".litespec/behavior/modules/lending/aggregates/loans/{useCase}.yaml", """
                    {
                      "type": "use-case",
                      "id": "use-case/lending.USECASE",
                      "title": "TITLE",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "kind": "command",
                      "writes": [
                        "aggregate/lending.loan"
                      ],
                      "records": [
                        {
                          "type": "result",
                          "id": "result/USECASE.done",
                          "title": "Done",
                          "owner": "lending-team",
                          "outcome": "success",
                          "meaning": "The copy is handled."
                        }
                      ]
                    }
                    """.Replace("USECASE", useCase).Replace("TITLE", title));
                TestRecords.Explain(temp.Path, $".litespec/behavior/modules/lending/aggregates/loans/{useCase}.md", $"""
                    # {title}

                    A reader handles a copy.
                    """);
            }

            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Loan/Loan.cs", """
                namespace Library.Lending.Domain.Loan;

                /// <implements>aggregate/lending.loan</implements>
                public sealed class Loan : AggregateRoot<LoanId>
                {
                    public string State { get; private set; } = "active";

                    /// <implements>use-case/lending.borrow-copy</implements>
                    public void Borrow()
                    {
                        State = "borrowed";
                    }

                    /// <implements>use-case/lending.renew-loan</implements>
                    public void Renew()
                    {
                        State = "renewed";
                    }

                    /// <implements>use-case/lending.expire-loans</implements>
                    public void Expire()
                    {
                        State = "expired";
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/BorrowCopy/BorrowCopyCommandHandler.cs", """
                namespace Library.Lending.Application.BorrowCopy;

                /// <implements>use-case/lending.borrow-copy</implements>
                public sealed class BorrowCopyCommandHandler
                    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public BorrowCopyCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public async Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                    {
                        var loan = await loanRepository.FindByIdAsync(command.CopyId, cancellationToken).ConfigureAwait(false);
                        loan.Borrow();
                        return new BorrowCopyCommandResult(command.CopyId);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/RenewLoan/RenewLoanCommandHandler.cs", """
                namespace Library.Lending.Application.RenewLoan;

                /// <implements>use-case/lending.renew-loan</implements>
                public sealed class RenewLoanCommandHandler
                    : ICommandHandler<RenewLoanCommand, RenewLoanCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public RenewLoanCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public async Task<RenewLoanCommandResult> HandleAsync(RenewLoanCommand command, CancellationToken cancellationToken)
                    {
                        var loan = await loanRepository.FindByIdAsync(command.CopyId, cancellationToken).ConfigureAwait(false);
                        RenewOne(loan);
                        return new RenewLoanCommandResult(command.CopyId);
                    }

                    private void RenewOne(Loan loan)
                    {
                        loan.Renew();
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/ExpireLoans/ExpireLoansCommandHandler.cs", """
                namespace Library.Lending.Application.ExpireLoans;

                /// <implements>use-case/lending.expire-loans</implements>
                public sealed class ExpireLoansCommandHandler
                    : ICommandHandler<ExpireLoansCommand, ExpireLoansCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public ExpireLoansCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public async Task<ExpireLoansCommandResult> HandleAsync(ExpireLoansCommand command, CancellationToken cancellationToken)
                    {
                        var loans = await loanRepository.FindExpirableAsync(cancellationToken).ConfigureAwait(false);
                        foreach (var loan in loans)
                        {
                            loan.Expire();
                        }

                        return new ExpireLoansCommandResult(loans.Count);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/AbandonLoan/AbandonLoanCommandHandler.cs", """
                namespace Library.Lending.Application.AbandonLoan;

                /// <implements>use-case/lending.abandon-loan</implements>
                public sealed class AbandonLoanCommandHandler
                    : ICommandHandler<AbandonLoanCommand, AbandonLoanCommandResult>
                {
                    public Task<AbandonLoanCommandResult> HandleAsync(AbandonLoanCommand command, CancellationToken cancellationToken)
                    {
                        return Task.FromResult(new AbandonLoanCommandResult(command.CopyId));
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

    /// <summary>Handler Loading Through A Fallback, A List Load, An Aliased Factory, Or A Factory Result Reports Nothing.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void HandlerLoadingThroughFallbackListAliasOrFactoryReportsNothing()
    {
        using var temp = WriteLoadConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        Assert.Empty(uncalled);
    }

    /// <summary>Writes a consumer in the module layout where four handlers call the marked act through a coalesced load, a listed load, an aliased factory, and a factory result.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteLoadConsumer()
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
                      "modules": "apps/api/src/Modules/*",
                      "kernel": "apps/api/src/Library.Kernel",
                      "platform": "apps/api/src/Library.Platform"
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow",
                      "renew",
                      "open",
                      "close"
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
            foreach (var (useCase, title) in new[] { ("borrow-copy", "Borrow copy"), ("renew-loan", "Renew loan"), ("open-loan", "Open loan"), ("close-loan", "Close loan") })
            {
                TestRecords.Write(temp.Path, $".litespec/behavior/modules/lending/aggregates/loans/{useCase}.yaml", """
                    {
                      "type": "use-case",
                      "id": "use-case/lending.USECASE",
                      "title": "TITLE",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "kind": "command",
                      "writes": [
                        "aggregate/lending.loan"
                      ],
                      "records": [
                        {
                          "type": "result",
                          "id": "result/USECASE.done",
                          "title": "Done",
                          "owner": "lending-team",
                          "outcome": "success",
                          "meaning": "The copy is handled."
                        }
                      ]
                    }
                    """.Replace("USECASE", useCase).Replace("TITLE", title));
                TestRecords.Explain(temp.Path, $".litespec/behavior/modules/lending/aggregates/loans/{useCase}.md", $"""
                    # {title}

                    A reader handles a copy.
                    """);
            }

            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Loan/Loan.cs", """
                namespace Library.Lending.Domain.Loan;

                /// <implements>aggregate/lending.loan</implements>
                public sealed class Loan : AggregateRoot<LoanId>
                {
                    public string State { get; private set; } = "active";

                    /// <implements>use-case/lending.open-loan</implements>
                    public static Loan Open(LoanId id)
                    {
                        return new Loan();
                    }

                    private Loan()
                    {
                    }

                    /// <implements>use-case/lending.borrow-copy</implements>
                    public void Borrow()
                    {
                        State = "borrowed";
                    }

                    /// <implements>use-case/lending.renew-loan</implements>
                    public void Renew()
                    {
                        State = "renewed";
                    }

                    /// <implements>use-case/lending.close-loan</implements>
                    public void Close()
                    {
                        State = "closed";
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/BorrowCopy/BorrowCopyCommandHandler.cs", """
                namespace Library.Lending.Application.BorrowCopy;

                /// <implements>use-case/lending.borrow-copy</implements>
                public sealed class BorrowCopyCommandHandler
                    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public BorrowCopyCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public async Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                    {
                        var loan = await loanRepository.FindByIdAsync(command.CopyId, cancellationToken).ConfigureAwait(false)
                            ?? throw new LoanNotFoundException(command.CopyId);
                        loan.Borrow();
                        return new BorrowCopyCommandResult(command.CopyId);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/RenewLoan/RenewLoanCommandHandler.cs", """
                namespace Library.Lending.Application.RenewLoan;

                /// <implements>use-case/lending.renew-loan</implements>
                public sealed class RenewLoanCommandHandler
                    : ICommandHandler<RenewLoanCommand, RenewLoanCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public RenewLoanCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public async Task<RenewLoanCommandResult> HandleAsync(RenewLoanCommand command, CancellationToken cancellationToken)
                    {
                        var loans = await loanRepository.ListActiveAsync(cancellationToken).ConfigureAwait(false);
                        foreach (var loan in loans)
                        {
                            loan.Renew();
                        }

                        return new RenewLoanCommandResult(loans.Count);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/OpenLoan/OpenLoanCommandHandler.cs", """
                namespace Library.Lending.Application.OpenLoan;

                using LoanAggregate = Library.Lending.Domain.Loan.Loan;

                /// <implements>use-case/lending.open-loan</implements>
                public sealed class OpenLoanCommandHandler
                    : ICommandHandler<OpenLoanCommand, OpenLoanCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public OpenLoanCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public Task<OpenLoanCommandResult> HandleAsync(OpenLoanCommand command, CancellationToken cancellationToken)
                    {
                        var loan = LoanAggregate.Open(command.CopyId);
                        loanRepository.Stage(loan);
                        return Task.FromResult(new OpenLoanCommandResult(command.CopyId));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/CloseLoan/CloseLoanCommandHandler.cs", """
                namespace Library.Lending.Application.CloseLoan;

                /// <implements>use-case/lending.close-loan</implements>
                public sealed class CloseLoanCommandHandler
                    : ICommandHandler<CloseLoanCommand, CloseLoanCommandResult>
                {
                    private readonly ILoanRepository loanRepository;

                    public CloseLoanCommandHandler(ILoanRepository loanRepository)
                    {
                        this.loanRepository = loanRepository;
                    }

                    public async Task<CloseLoanCommandResult> HandleAsync(CloseLoanCommand command, CancellationToken cancellationToken)
                    {
                        var loan = await loanRepository.FindByIdAsync(command.CopyId, cancellationToken).ConfigureAwait(false)
                            ?? throw new LoanNotFoundException(command.CopyId);
                        var chain = Loan.OpenChain(loan.Id);
                        chain.Close();
                        loanRepository.Stage(loan);
                        return new CloseLoanCommandResult(command.CopyId);
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
                      "modules": "apps/api/src/Modules/Library.Lending",
                      "webapi": "apps/api/src/Library.WebApi",
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
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/BorrowCopy/BorrowCopyCommand.cs", """
                namespace Library.Lending.Application.BorrowCopy;

                public sealed record BorrowCopyCommand(Guid CopyId);

                public sealed record BorrowCopyCommandResult(Guid CopyId);
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/BorrowCopy/BorrowCopyCommandHandler.cs", """
                namespace Library.Lending.Application.BorrowCopy;

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
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/FanOut/FanOutCommand.cs", """
                namespace Library.Lending.Application.FanOut;

                public sealed record FanOutCommand;

                public sealed record FanOutCommandResult(int Issued);
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/FanOut/FanOutCommandHandler.cs", """
                namespace Library.Lending.Application.FanOut;

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
                      "modules": "apps/api/src/Modules/Library.Lending",
                      "webapi": "apps/api/src/Library.WebApi",
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
                  ]
                }
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/domain-events/loan-borrowed/loan-borrowed.yaml", """
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
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.md", """
                # Loan

                A loan lends one copy to one reader.
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Loans/Loan.cs", """
                namespace Library.Lending.Domain.Loans;

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
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Loans/Events/LoanBorrowedEvent.cs", """
                namespace Library.Lending.Domain.Loans.Events;

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
