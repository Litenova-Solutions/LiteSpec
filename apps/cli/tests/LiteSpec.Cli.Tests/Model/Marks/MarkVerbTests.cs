using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>
///     Proves a mark written with the wrong verb is unresolved: proves on a
///     handler, implements on a test; the matching verb passes.
/// </summary>
public sealed class MarkVerbTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Proves_On_A_Handler_Reports_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("BadBorrowCopyCommandHandler"));
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("proves verb", finding.Message);
        Assert.Contains("takes implements", finding.Message);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Implements_On_A_Test_Reports_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("BadBorrowCopyScenarios"));
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", finding.Subject);
        Assert.Contains("implements verb", finding.Message);
        Assert.Contains("takes proves", finding.Message);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Matching_Verbs_Report_No_Mark_Unresolved()
    {
        using var temp = WriteConsumer();
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved");

        Assert.Equal(2, unresolved.Count);
        Assert.DoesNotContain(unresolved, finding => finding.Message.Contains("GoodBorrowCopyCommandHandler"));
        Assert.DoesNotContain(unresolved, finding => finding.Message.Contains("GoodBorrowCopyScenarios"));
    }

    /// <summary>Writes a consumer where one handler proves and one test implements, beside matching marks.</summary>
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
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopy/BorrowCopyCommand.cs", """
                namespace Library.Application.Lending.Loans.BorrowCopy;

                public sealed record BorrowCopyCommand(Guid CopyId, Guid ReaderId);

                public sealed record BorrowCopyCommandResult(Guid LoanId);
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopy/BadBorrowCopyCommandHandler.cs", """
                namespace Library.Application.Lending.Loans.BorrowCopy;

                /// <proves>use-case/lending.borrow-copy</proves>
                public sealed class BadBorrowCopyCommandHandler
                    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
                {
                    public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                    {
                        return Task.FromResult(new BorrowCopyCommandResult(Guid.Empty));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopy/GoodBorrowCopyCommandHandler.cs", """
                namespace Library.Application.Lending.Loans.BorrowCopy;

                /// <implements>use-case/lending.borrow-copy</implements>
                public sealed class GoodBorrowCopyCommandHandler
                    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
                {
                    public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                    {
                        return Task.FromResult(new BorrowCopyCommandResult(Guid.Empty));
                    }
                }
                """);
            Write(temp.Path, "apps/api/tests/Library.Tests/Lending/Loans/BorrowCopy/BadBorrowCopyScenarios.cs", """
                namespace Library.Tests.Lending.Loans.BorrowCopy;

                public sealed class BadBorrowCopyScenarios
                {
                    /// <implements>scenario/borrow-copy.borrowing-succeeds</implements>
                    [Fact]
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
