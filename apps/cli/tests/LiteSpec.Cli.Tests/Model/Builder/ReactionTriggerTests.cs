using System.Text.Json;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves a reaction or job without a mark links no use case and reports mark-missing.</summary>
public sealed class ReactionTriggerTests
{
    /// <summary>An unmarked reaction links nothing, whatever its trigger names.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Reaction_Without_A_Mark_Links_Nothing_And_Reports_Mark_Missing()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/loans/notify-overdue.md", UseCasePage);
        Write(temp.Path, "docs/behavior/modules/lending/reactions/send-notice-on-loan-borrowed.md", ReactionPage);
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/OnLoanBorrowed/NotifyReaderOnLoanBorrowedHandler.cs", ReactionFile);

        using var document = Build(temp.Path);
        var root = document.RootElement;

        Assert.False(
            root.GetProperty("links").GetProperty("implements").EnumerateArray().Any(
                link => link.GetProperty("path").GetString() == "apps/api/src/Library.Application/Lending/Loans/OnLoanBorrowed/NotifyReaderOnLoanBorrowedHandler.cs"));
        Assert.False(root.GetProperty("status").GetProperty("use-case/lending.notify-overdue").GetProperty("implemented").GetBoolean());

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var missing = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("mark-missing") && candidate.Contains("NotifyReaderOnLoanBorrowedHandler.cs"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", missing);
    }

    /// <summary>An unmarked job links nothing, whatever its name names.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Job_Without_A_Mark_Links_Nothing_And_Reports_Mark_Missing()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/ListLoans/ListLoansJob.cs", JobFile);

        using var document = Build(temp.Path);

        Assert.False(
            document.RootElement.GetProperty("links").GetProperty("implements").EnumerateArray().Any(
                link => link.GetProperty("path").GetString() == "apps/api/src/Library.Application/Lending/Loans/ListLoans/ListLoansJob.cs"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var missing = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("mark-missing") && candidate.Contains("ListLoansJob.cs"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", missing);
    }

    static JsonDocument Build(string root)
    {
        var output = Path.Combine(root, ".model", "model.json");
        var result = Cli.Run("model", "build", "--root", root, "--out", output);
        Assert.Equal(0, result.Status);

        return JsonDocument.Parse(File.ReadAllText(output));
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string UseCasePage = """
        ---
        {
          "type": "use-case",
          "id": "use-case/lending.notify-overdue",
          "title": "Notify overdue",
          "owner": "lending-team",
          "status": "reviewed",
          "kind": "command",
          "access": {
            "grant": "system",
            "tenant": "own"
          },
          "reachedThrough": [
            "entry-point/library-app"
          ],
          "fulfills": [
            "requirement/borrowing.loan-period-days"
          ],
          "records": [
            {
              "type": "result",
              "id": "result/notify-overdue.notified",
              "title": "Notified",
              "owner": "lending-team",
              "outcome": "success",
              "meaning": "The reader is told."
            }
          ]
        }
        ---

        # Notify overdue

        A reader learns a loan passed its due date.
        """;

    const string ReactionPage = """
        ---
        {
          "type": "reaction",
          "id": "reaction/lending.send-notice-on-loan-borrowed",
          "title": "Send notice on loan borrowed",
          "owner": "lending-team",
          "status": "reviewed",
          "triggeredBy": [
            "event/loan.loan-borrowed"
          ],
          "effects": [
            {
              "runs": "use-case/lending.notify-overdue",
              "timing": "async"
            }
          ],
          "fulfills": [
            "requirement/borrowing.loan-period-days"
          ]
        }
        ---

        # Send notice on loan borrowed

        Every borrowed loan earns one notice for its reader.
        """;

    const string ReactionFile = """
        namespace Library.Application.Lending.Loans.OnLoanBorrowed;

        /// <summary>Notifies the reader when a loan is borrowed.</summary>
        public sealed class NotifyReaderOnLoanBorrowedHandler : IEventHandler<LoanBorrowedEvent>
        {
            public Task HandleAsync(LoanBorrowedEvent raised) => Task.CompletedTask;
        }
        """;

    const string JobFile = """
        namespace Library.Application.Lending.Loans.ListLoans;

        /// <summary>Warms the loan list on a schedule.</summary>
        public sealed class ListLoansJob : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stopping) => Task.CompletedTask;
        }
        """;
}
