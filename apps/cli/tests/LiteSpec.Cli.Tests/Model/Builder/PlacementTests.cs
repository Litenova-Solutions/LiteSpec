using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves the reader places every record from its folder and queries it by identifier.</summary>
public sealed class PlacementTests
{
    /// <summary>Every record carries the folder its file sits in and the module folder above it.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ReaderSetsFolderAndModuleFromPlacement()
    {
        using var temp = TestCli.Temp("placement");
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

        AssertPlaced(model, "product/claimwick", ".litespec/intent", null);
        AssertPlaced(model, "goal/faster-reimbursement", ".litespec/intent/goals", null);
        AssertPlaced(model, "module/finance", ".litespec/intent/modules/finance", "finance");
        AssertPlaced(model, "feature/payout", ".litespec/intent/modules/finance/features", "finance");
        AssertPlaced(model, "requirement/payout.request-payout", ".litespec/intent/modules/finance/features", "finance");
        AssertPlaced(model, "aggregate/finance.payout", ".litespec/behavior/modules/finance/payouts", "finance");
        AssertPlaced(model, "use-case/finance.request-payout", ".litespec/behavior/modules/finance/payouts", "finance");
        AssertPlaced(model, "result/request-payout.requested", ".litespec/behavior/modules/finance/payouts", "finance");
        AssertPlaced(model, "scenario/request-payout.requesting-succeeds", ".litespec/behavior/modules/finance/payouts", "finance");
        AssertPlaced(model, "permission/approve-expense", ".litespec/behavior/modules/finance", "finance");
        AssertPlaced(model, "entry-point/claimwick-app", ".litespec/behavior/entry-points/claimwick-app", null);
        AssertPlaced(model, "screen/claimwick-app.expense-list", ".litespec/behavior/entry-points/claimwick-app/screens", null);
    }

    /// <summary>A screen takes its folder from the entry point folder it sits under.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ScreenFolderComesFromItsEntryPointFolder()
    {
        using var temp = TestCli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/entry-points/library-app/screens/loans.yaml", """
            type: screen
            id: screen/other-app.loans
            title: Loans
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        AssertPlaced(model, "screen/other-app.loans", ".litespec/behavior/entry-points/library-app/screens", null);
    }

    /// <summary>Derived refusal results carry the folder and module of their use case file.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void DerivedResultsCarryTheirUseCasePlacement()
    {
        using var temp = TestCli.Temp("placement");
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

        AssertPlaced(model, "result/request-payout.forbidden", ".litespec/behavior/modules/finance/payouts", "finance");
        AssertPlaced(model, "result/request-payout.invalid-input", ".litespec/behavior/modules/finance/payouts", "finance");
    }

    /// <summary>Model query resolves a record id and shows its folder and module.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void ModelQueryShowsFolderAndModule()
    {
        using var temp = TestCli.Temp("placement");
        WriteProject(temp.Path);
        Write(temp.Path, ".litespec/behavior/modules/spending/expenses/submit-expense.yaml", """
            type: use-case
            id: use-case/spending.submit-expense
            title: Submit expense
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var answer = RecordQuery.Query(model, "use-case/spending.submit-expense");

        Assert.NotNull(answer);

        using var document = JsonDocument.Parse(answer);

        Assert.Equal("use-case/spending.submit-expense", document.RootElement.GetProperty("id").GetString());
        Assert.Equal(".litespec/behavior/modules/spending/expenses", document.RootElement.GetProperty("folder").GetString());
        Assert.Equal("spending", document.RootElement.GetProperty("module").GetString());
        Assert.Null(RecordQuery.Query(model, "spending/expenses/use-case/submit-expense"));
        Assert.Null(RecordQuery.Query(model, "use-case/spending.no-such-case"));
    }

    /// <summary>Model JSON shows the folder and the module of every record.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ModelJsonShowsFolderAndModule()
    {
        using var temp = TestCli.Temp("placement");
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

        Assert.Equal(".litespec/behavior/modules/spending/expenses", record.GetProperty("folder").GetString());
        Assert.Equal("spending", record.GetProperty("module").GetString());
    }

    static void AssertPlaced(ProjectModel model, string id, string folder, string? module)
    {
        var record = ById(model, id);

        Assert.Equal(folder, record.Folder);
        Assert.Equal(module, record.Module);
    }

    static LiteSpec.Cli.Model.Records.Record ById(ProjectModel model, string id) =>
        model.ById.TryGetValue(id, out var record)
            ? record
            : throw new InvalidOperationException($"No record holds '{id}'.");

    static void WriteProject(string root) =>
        Write(root, ".litespec/project.yaml", """{"shape": "client-only", "stacks": []}""");

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.Replace("\r\n", "\n"));
    }
}
