using System.Text.Json;
using LiteSpec.Model;
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
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/notify-overdue.yaml", UseCaseJson);
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/reactions/send-notice-on-loan-borrowed.yaml", ReactionJson);
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/OnLoanBorrowed/NotifyReaderOnLoanBorrowedHandler.cs", ReactionFile);

        using var document = Build(temp.Path);
        var root = document.RootElement;

        Assert.DoesNotContain(root.GetProperty("links").GetProperty("implements").EnumerateArray(), link => link.GetProperty("path").GetString() == "apps/api/src/Library.Application/Lending/Loans/OnLoanBorrowed/NotifyReaderOnLoanBorrowedHandler.cs");
        var completion = CompletionOf(root, "use-case/lending.notify-overdue");

        Assert.Equal("unfulfilled", completion.GetProperty("stage").GetString());
        Assert.Equal(0, completion.GetProperty("counts").GetProperty("code found").GetProperty("done").GetInt32());

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

        Assert.DoesNotContain(document.RootElement.GetProperty("links").GetProperty("implements").EnumerateArray(), link => link.GetProperty("path").GetString() == "apps/api/src/Library.Application/Lending/Loans/ListLoans/ListLoansJob.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var missing = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("mark-missing") && candidate.Contains("ListLoansJob.cs"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", missing);
    }

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static JsonElement CompletionOf(JsonElement root, string id) =>
        root.GetProperty("records").EnumerateArray()
            .Single(record => record.GetProperty("id").GetString() == id)
            .GetProperty("completion");

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string UseCaseJson = """
        {
          "type": "use-case",
          "id": "use-case/lending.notify-overdue",
          "title": "Notify overdue",
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
              "outcome": "success",
              "meaning": "The reader is told."
            }
          ]
        }
        """;

    const string ReactionJson = """
        {
          "type": "reaction",
          "id": "reaction/lending.send-notice-on-loan-borrowed",
          "title": "Send notice on loan borrowed",
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
