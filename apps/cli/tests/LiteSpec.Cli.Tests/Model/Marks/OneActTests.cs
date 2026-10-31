using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>
///     Proves each aggregate act takes one use case: a root method with two marks
///     reports mark-count-invalid, and a mark on a reading method, a private method, or a
///     method with no test attribute reports mark-unknown.
/// </summary>
public sealed class OneActTests
{
    /// <summary>Two Marks On A Root Method Report Mark Count.</summary>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact]
    public void Two_Marks_On_A_Root_Method_Report_Mark_Count()
    {
        using var fixture = MarksFixture.Prepare("act");
        var counts = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-count-invalid");

        var finding = Assert.Single(counts, found => found.Message.Contains("Loan.Borrow"));
        Assert.Contains("carries 2 marks", finding.Message);
        Assert.Contains("takes one", finding.Message);
    }

    /// <summary>Single Mark On A Root Method Reports No Mark Count.</summary>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact]
    public void Single_Mark_On_A_Root_Method_Reports_No_Mark_Count()
    {
        using var fixture = MarksFixture.Prepare("clean");
        var counts = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-count-invalid");

        Assert.Empty(counts);
    }

    /// <summary>Marked Reading Method Reports Mark Unresolved.</summary>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact]
    public void Marked_Reading_Method_Reports_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("LoanReader.TotalBorrowed"));
        Assert.Contains("takes no mark", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Unmarked Reading Method Reports No Mark Unresolved.</summary>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact]
    public void Unmarked_Reading_Method_Reports_No_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        Assert.DoesNotContain(unresolved, finding => finding.Message.Contains("OpenCount"));
        Assert.DoesNotContain(unresolved, finding => finding.Message.Contains("Loan.Borrow"));
    }

    /// <summary>Marked Private Method Reports Mark Unresolved.</summary>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact]
    public void Marked_Private_Method_Reports_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("Loan.RecordBorrow"));
        Assert.Contains("takes no mark", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Unmarked Private Helper Reports No Mark Unresolved.</summary>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact]
    public void Unmarked_Private_Helper_Reports_No_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        Assert.DoesNotContain(unresolved, finding => finding.Message.Contains("ApplyBorrow"));
    }

    /// <summary>Proves Without Test Attribute Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Proves_Without_Test_Attribute_Reports_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("BadBorrowCopyScenarios"));
        Assert.Contains("takes no mark", finding.Message);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", finding.Subject);
        Assert.Equal(3, unresolved.Count);
    }

    /// <summary>Proves With Test Attribute Reports No Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Proves_With_Test_Attribute_Reports_No_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        Assert.DoesNotContain(unresolved, finding => finding.Message.Contains("GoodBorrowCopyScenarios"));
    }

    /// <summary>
    ///     Writes a consumer with a marked act, a marked reading method, a marked
    ///     private helper, and a proves mark with and without its test attribute.
    /// </summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteConsumer()
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
                    },
                    {
                      "type": "scenario",
                      "id": "scenario/borrow-copy.borrowing-succeeds",
                      "title": "Borrowing succeeds",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "given": "An available copy.",
                      "when": "The reader borrows the copy.",
                      "then": "The loan is active.",
                      "result": "result/borrow-copy.borrowed"
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

                    /// <implements>use-case/lending.borrow-copy</implements>
                    public void Borrow(Guid readerId, DateOnly dueDate)
                    {
                        RaiseDomainEvent(new LoanBorrowedEvent(Id, readerId, dueDate));
                    }

                    /// <implements>use-case/lending.borrow-copy</implements>
                    private void RecordBorrow(Guid readerId, DateOnly dueDate)
                    {
                        RaiseDomainEvent(new LoanBorrowedEvent(Id, readerId, dueDate));
                    }

                    private void ApplyBorrow(Guid readerId, DateOnly dueDate)
                    {
                        RaiseDomainEvent(new LoanBorrowedEvent(Id, readerId, dueDate));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/LoanReader.cs", """
                namespace Library.Domain.Lending.Loans;

                public sealed class LoanReader
                {
                    /// <implements>use-case/lending.borrow-copy</implements>
                    public int TotalBorrowed() => 0;

                    public int OpenCount() => 0;
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/Events/LoanBorrowedEvent.cs", """
                namespace Library.Domain.Lending.Loans.Events;

                /// <implements>event/loan.loan-borrowed</implements>
                public sealed record LoanBorrowedEvent(Guid LoanId, Guid ReaderId, DateOnly DueDate) : IDomainEvent;
                """);
            Write(temp.Path, "apps/api/tests/Library.Tests/Lending/Loans/BorrowCopy/BadBorrowCopyScenarios.cs", """
                namespace Library.Tests.Lending.Loans.BorrowCopy;

                public sealed class BadBorrowCopyScenarios
                {
                    /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                    public void Borrowing_succeeds()
                    {
                        Assert.True(true);
                    }
                }
                """);
            Write(temp.Path, "apps/api/tests/Library.Tests/Lending/Loans/BorrowCopy/GoodBorrowCopyScenarios.cs", """
                namespace Library.Tests.Lending.Loans.BorrowCopy;

                public sealed class GoodBorrowCopyScenarios
                {
                    /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                    [Fact]
                    public void Borrowing_succeeds()
                    {
                        Assert.True(true);
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

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
