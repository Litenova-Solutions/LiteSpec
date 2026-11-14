using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Proves <c>litespec new</c> scaffolds every type it lists: the home, the identifier, one Title Case title, and the template's sections.</summary>
public sealed class NewEveryTypeTests
{
    /// <summary>Every type <c>new</c> lists writes its record at its home, with the identifier and the one title, and its explanation with the template's sections.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="argument">The identifier argument, the home and name without the type.</param>
    /// <param name="file">The record file, relative to the .litespec folder.</param>
    /// <param name="title">The Title Case title the record and its explanation share.</param>
    /// <param name="id">The full identifier the record carries.</param>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "new writes every listed type at its home with one title")]
    [MemberData(nameof(Cases))]
    public void NewWritesEveryListedTypeAtItsHome(string type, string argument, string file, string title, string id)
    {
        using var consumer = Consumer();

        // A requirement is written in its feature's folder, so the feature exists first.
        if (type == "requirement")
        {
            Assert.Equal(0, TestCli.Run("new", "feature", "waiting.approval", "--root", consumer.Path).Status);
        }

        var result = TestCli.Run("new", type, argument, "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var record = Path.Combine(consumer.Path, ".litespec", file);
        Assert.True(File.Exists(record), $"Expected the {type} record at '{record}': {result.Output}");

        var text = File.ReadAllText(record);
        Assert.Contains($"id: {id}", text);
        Assert.Contains($"title: {title}", text);

        var explanation = Path.ChangeExtension(record, ".md");

        // A requirement takes no explanation file, and a departure sits on the governance layer, which explanations.layers does not name.
        if (type is "requirement" or "departure")
        {
            Assert.False(File.Exists(explanation), $"A {type} takes no explanation file.");
            return;
        }

        Assert.True(File.Exists(explanation), $"Expected the explanation beside '{record}'.");

        var lines = File.ReadAllText(explanation).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        Assert.Equal($"# {title}", lines[0]);
        Assert.Equal(TemplateSections(type), lines.Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray());
    }

    /// <summary>A query is scaffolded from the query template, which its kind names, and the record opens with the scaffold marker.</summary>
    /// <proves>litespec/core/authoring#copy-each-template-from-the-example</proves>
    [Fact(DisplayName = "new writes a query from the query template")]
    public void NewWritesAQueryFromTheQueryTemplate()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("new", "use-case", "spending.list-expenses", "--kind", "query", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var record = Path.Combine(consumer.Path, ".litespec", "behavior/modules/spending/use-cases/list-expenses/list-expenses.yaml");
        var text = File.ReadAllText(record);
        Assert.StartsWith("# Scaffolded by litespec new from templates/consumer/use-case-query.yaml;", text, StringComparison.Ordinal);
        Assert.Contains("kind: query", text);
    }

    /// <summary>The scaffold cases: each listed type with its identifier argument, file, title and full identifier.</summary>
    /// <returns>One row per listed type.</returns>
    public static IEnumerable<object[]> Cases()
    {
        yield return ["product", "claimwick", "intent/product.yaml", "Claimwick", "product/claimwick"];
        yield return ["goal", "faster-reimbursement", "intent/goals/faster-reimbursement/faster-reimbursement.yaml", "Faster Reimbursement", "goal/faster-reimbursement"];
        yield return ["vision", "one-desk", "intent/visions/one-desk/one-desk.yaml", "One Desk", "vision/one-desk"];
        yield return ["problem", "waiting-for-money", "intent/problems/waiting-for-money/waiting-for-money.yaml", "Waiting for Money", "problem/waiting-for-money"];
        yield return ["feature", "waiting.approval", "intent/problems/waiting/features/approval/approval.yaml", "Approval", "feature/approval"];
        yield return ["requirement", "approval.decided-in-five", "intent/problems/waiting/features/approval/requirements/decided-in-five/decided-in-five.yaml", "Decided in Five", "requirement/approval.decided-in-five"];
        yield return ["constraint", "receipt-retention", "intent/constraints/receipt-retention/receipt-retention.yaml", "Receipt Retention", "constraint/receipt-retention"];
        yield return ["decision", "receipt-threshold", "intent/decisions/0001-receipt-threshold/0001-receipt-threshold.yaml", "Receipt Threshold", "decision/0001-receipt-threshold"];
        yield return ["research", "benchmark-approval-times", "intent/research/benchmark-approval-times/benchmark-approval-times.yaml", "Benchmark Approval Times", "research/benchmark-approval-times"];
        yield return ["journey", "close-the-year", "intent/journeys/close-the-year/close-the-year.yaml", "Close the Year", "journey/close-the-year"];
        yield return ["actor", "reader", "intent/actors/reader/reader.yaml", "Reader", "actor/reader"];
        yield return ["role", "approver", "intent/roles/approver/approver.yaml", "Approver", "role/approver"];
        yield return ["module", "spending", "behavior/modules/spending/module.yaml", "Spending", "module/spending"];
        yield return ["aggregate", "spending.expense", "behavior/modules/spending/aggregates/expense/aggregate.yaml", "Expense", "aggregate/spending.expense"];
        yield return ["use-case", "spending.submit-expense", "behavior/modules/spending/use-cases/submit-expense/submit-expense.yaml", "Submit Expense", "use-case/spending.submit-expense"];
        yield return ["reaction", "spending.send-reminder", "behavior/modules/spending/reactions/send-reminder/send-reminder.yaml", "Send Reminder", "reaction/spending.send-reminder"];
        yield return ["job", "spending.erase-data", "behavior/modules/spending/jobs/erase-data/erase-data.yaml", "Erase Data", "job/spending.erase-data"];
        yield return ["notification", "spending.approver-reminder", "behavior/modules/spending/notifications/approver-reminder/approver-reminder.yaml", "Approver Reminder", "notification/spending.approver-reminder"];
        yield return ["external-system", "bank", "behavior/external-systems/bank/bank.yaml", "Bank", "external-system/bank"];
        yield return ["screen", "library-app.loans", "behavior/entry-points/library-app/screens/library-app.loans/library-app.loans.yaml", "Loans", "screen/library-app.loans"];
        yield return ["runbook", "month-end", "implementation/runbooks/month-end/month-end.yaml", "Month End", "runbook/month-end"];
        yield return ["departure", "legacy-export", "governance/departures/legacy-export/legacy-export.yaml", "Legacy Export", "departure/legacy-export"];
    }

    /// <summary>A consumer that lists every layer, so each type's explanation is written.</summary>
    /// <returns>The temporary consumer folder holding the project file.</returns>
    static TempFolder Consumer()
    {
        var consumer = TestCli.Temp("new-every-type");
        File.WriteAllText(
            Path.Combine(consumer.Path, ".litespec/project.yaml"),
            "{\"explanations\":{\"layers\":[\"intent\",\"behavior\",\"implementation\",\"governance\"]}}");
        return consumer;
    }

    /// <summary>The H2 headings of the template a type's explanation is written from.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The headings in template order.</returns>
    static string[] TemplateSections(string type) =>
        File.ReadAllText(Path.Combine(TestCli.Repository, "templates", "consumer", $"{type}.md"))
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .ToArray();
}
