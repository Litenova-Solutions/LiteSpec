using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves the reader places every record from its folder and queries either identifier.</summary>
public sealed class PlacementTests
{
    /// <summary>Every record carries the qualified identifier its folder and identifier compute.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Reader_Sets_Qualified_Ids_From_Placement()
    {
        using var temp = Cli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/intent/product.yaml", """
            type: product
            id: product/claimwick
            title: Claimwick
            """);
        Write(temp.Path, ".litespec/intent/goals/faster.yaml", """
            type: goal
            id: goal/faster-reimbursement
            title: Faster reimbursement
            """);
        Write(temp.Path, ".litespec/intent/modules/finance/module.yaml", """
            type: module
            id: module/finance
            title: Finance
            """);
        Write(temp.Path, ".litespec/intent/modules/finance/features/payout.yaml", """
            type: feature
            id: feature/payout
            title: Payout
            records:
              - type: requirement
                id: requirement/payout.request-payout
                title: Request payout
            """);
        Write(temp.Path, ".litespec/behavior/modules/finance/payouts/aggregate.yaml", """
            type: aggregate
            id: aggregate/finance.payout
            title: Payout
            """);
        Write(temp.Path, ".litespec/behavior/modules/finance/payouts/request-payout.yaml", """
            type: use-case
            id: use-case/finance.request-payout
            title: Request payout
            records:
              - type: result
                id: result/request-payout.requested
                title: Requested
                outcome: success
                meaning: The payout is requested.
              - type: scenario
                id: scenario/request-payout.requesting-succeeds
                title: Requesting succeeds
                result: result/request-payout.requested
            """);
        Write(temp.Path, ".litespec/behavior/modules/finance/permissions.yaml", """
            type: permission-list
            title: Permissions
            records:
              - type: permission
                id: permission/approve-expense
                title: Approve expense
            """);
        Write(temp.Path, ".litespec/behavior/entry-points/claimwick-app/entry-point.yaml", """
            type: entry-point
            id: entry-point/claimwick-app
            title: Claimwick app
            """);
        Write(temp.Path, ".litespec/behavior/entry-points/claimwick-app/screens/expense-list.yaml", """
            type: screen
            id: screen/claimwick-app.expense-list
            title: Expense list
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Equal("product/claimwick", ById(model, "product/claimwick").QualifiedId);
        Assert.Equal("goal/faster-reimbursement", ById(model, "goal/faster-reimbursement").QualifiedId);
        Assert.Equal("finance", ById(model, "module/finance").QualifiedId);
        Assert.Equal("finance/feature/payout", ById(model, "feature/payout").QualifiedId);
        Assert.Equal(
            "finance/feature/payout/requirement/request-payout",
            ById(model, "requirement/payout.request-payout").QualifiedId);
        Assert.Equal("finance/payouts", ById(model, "aggregate/finance.payout").QualifiedId);
        Assert.Equal(
            "finance/payouts/use-case/request-payout",
            ById(model, "use-case/finance.request-payout").QualifiedId);
        Assert.Equal(
            "finance/payouts/use-case/request-payout/result/requested",
            ById(model, "result/request-payout.requested").QualifiedId);
        Assert.Equal(
            "finance/payouts/use-case/request-payout/scenario/requesting-succeeds",
            ById(model, "scenario/request-payout.requesting-succeeds").QualifiedId);
        Assert.Equal(
            "finance/permission/approve-expense",
            ById(model, "permission/approve-expense").QualifiedId);
        Assert.Equal("claimwick-app", ById(model, "entry-point/claimwick-app").QualifiedId);
        Assert.Equal(
            "claimwick-app/screen/expense-list",
            ById(model, "screen/claimwick-app.expense-list").QualifiedId);
    }

    /// <summary>A screen takes its entry point from its folder under behavior entry points.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Screen_Entry_Point_Comes_From_Its_Folder()
    {
        using var temp = Cli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/entry-points/library-app/screens/loans.yaml", """
            type: screen
            id: screen/other-app.loans
            title: Loans
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var screen = ById(model, "screen/other-app.loans");

        Assert.Equal("library-app/screen/loans", screen.QualifiedId);
    }

    /// <summary>Derived refusal results carry the qualified identifier of their use case file.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void Derived_Results_Carry_Qualified_Ids()
    {
        using var temp = Cli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/modules/finance/payouts/request-payout.yaml", """
            type: use-case
            id: use-case/finance.request-payout
            title: Request payout
            access:
              grant: permission/approve-expense
            input:
              - name: amount
                rules: required
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Equal(
            "finance/payouts/use-case/request-payout/result/forbidden",
            ById(model, "result/request-payout.forbidden").QualifiedId);
        Assert.Equal(
            "finance/payouts/use-case/request-payout/result/invalid-input",
            ById(model, "result/request-payout.invalid-input").QualifiedId);
    }

    /// <summary>Model query resolves either identifier and shows both.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void Model_Query_Accepts_Either_Identifier()
    {
        using var temp = Cli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/modules/spending/expenses/submit-expense.yaml", """
            type: use-case
            id: use-case/spending.submit-expense
            title: Submit expense
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var byId = Querier.Query(model, "use-case/spending.submit-expense");
        var byQualified = Querier.Query(model, "spending/expenses/use-case/submit-expense");

        Assert.NotNull(byId);
        Assert.NotNull(byQualified);

        using var left = JsonDocument.Parse(byId);
        using var right = JsonDocument.Parse(byQualified);

        Assert.Equal("use-case/spending.submit-expense", left.RootElement.GetProperty("id").GetString());
        Assert.Equal(
            "spending/expenses/use-case/submit-expense",
            left.RootElement.GetProperty("qualifiedId").GetString());
        Assert.Equal(
            left.RootElement.GetProperty("id").GetString(),
            right.RootElement.GetProperty("id").GetString());
        Assert.Equal(
            left.RootElement.GetProperty("qualifiedId").GetString(),
            right.RootElement.GetProperty("qualifiedId").GetString());
        Assert.Null(Querier.Query(model, "spending/expenses/use-case/no-such-case"));
    }

    /// <summary>Model JSON shows both the identifier and the qualified identifier.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Model_Json_Shows_Both_Identifiers()
    {
        using var temp = Cli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/modules/spending/expenses/aggregate.yaml", """
            type: aggregate
            id: aggregate/spending.expense
            title: Expense
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());
        var record = document.RootElement.GetProperty("records").EnumerateArray()
            .Single(entry => entry.GetProperty("id").GetString() == "aggregate/spending.expense");

        Assert.Equal("spending/expenses", record.GetProperty("qualifiedId").GetString());
    }

    static LiteSpec.Model.Record ById(ProjectModel model, string id) =>
        model.ById.TryGetValue(id, out var record)
            ? record
            : throw new InvalidOperationException($"No record holds '{id}'.");

    static void WriteProject(string root) =>
        Write(root, ".litespec/project.json", """{"shape": "client-only", "stacks": []}""");

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.Replace("\r\n", "\n"));
    }
}
