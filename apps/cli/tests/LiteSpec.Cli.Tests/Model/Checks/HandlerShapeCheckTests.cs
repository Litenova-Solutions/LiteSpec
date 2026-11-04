using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the handler shape checks: operation ids, reasonless names, collaborators, loop reads, memory filters and inline effects.</summary>
public sealed class HandlerShapeCheckTests
{
    /// <summary>Two Endpoints With One Operation Id Report.</summary>
    /// <proves>litespec/backend/api#declare-stable-operation-metadata-in-the-contract</proves>
    [Fact]
    public void Two_Endpoints_With_One_Operation_Id_Report()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "void-order");
        model.Code.Endpoints.Add(new EndpointOperation("VoidOrder", "use-case/lending.void-order", "POST", "/api/orders/{orderId}/void-again", Path.Combine(temp.Path, "VoidOrderEndpoint.cs")));

        Assert.Contains("operation-id-duplicate", TestModel.Codes(model));
    }

    /// <summary>Reasoned Names Pass.</summary>
    /// <proves>litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason</proves>
    [Fact]
    public void Reasoned_Names_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Lending", "VoidOrderCommandHandler.cs"),
            "VoidOrderCommandHandler"));

        Assert.DoesNotContain("name-reason-missing", TestModel.Codes(model));
    }

    /// <summary>Reasonless Folder Reports.</summary>
    /// <proves>litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason</proves>
    [Fact]
    public void Reasonless_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Shared", "VoidOrderCommandHandler.cs"),
            "VoidOrderCommandHandler"));

        Assert.Contains("name-reason-missing", TestModel.Codes(model));
    }

    /// <summary>Reasonless Type Reports.</summary>
    /// <proves>litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason</proves>
    [Fact]
    public void Reasonless_Type_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Lending", "OrderManager.cs"),
            "OrderManager"));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-reason-missing");

        Assert.Contains("OrderManager", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Shared Kernel Folder Is Exempt.</summary>
    /// <proves>litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason</proves>
    [Fact]
    public void Shared_Kernel_Folder_Is_Exempt()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.json"),
            """{"stackConfig": {"dotnet": {"sharedKernel": "src/App/Shared"}}}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Shared", "OrderHelper.cs"),
            "OrderHelper"));

        Assert.DoesNotContain("name-reason-missing", TestModel.Codes(model));
    }

    /// <summary>Injected Collaborators Pass.</summary>
    /// <proves>litespec/backend/application#build-collaborators-through-the-container</proves>
    [Fact]
    public void Injected_Collaborators_Pass()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file));

        Assert.DoesNotContain("collaborator-repository-invalid", TestModel.Codes(model));
    }

    /// <summary>Collaborator Holding A Repository Reports.</summary>
    /// <proves>litespec/backend/application#build-collaborators-through-the-container</proves>
    [Fact]
    public void Collaborator_Holding_A_Repository_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            Collaborators: [new CodeCollaborator("OrderEraser", file, 7)]));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "collaborator-repository-invalid");

        Assert.Contains("OrderEraser", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Job Collaborator Holding A Repository Reports.</summary>
    /// <proves>litespec/backend/application#build-collaborators-through-the-container</proves>
    [Fact]
    public void Job_Collaborator_Holding_A_Repository_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "SweepJob.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Jobs.Add(new JobInfo(
            "SweepJob", file, 3, null, "job/lending.sweep",
            Collaborators: [new CodeCollaborator("OrderEraser", file, 9)]));

        Assert.Contains("collaborator-repository-invalid", TestModel.Codes(model));
    }

    /// <summary>Reads Outside Loops Pass.</summary>
    /// <proves>litespec/backend/application#read-no-repository-inside-a-loop</proves>
    [Fact]
    public void Reads_Outside_Loops_Pass()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file));

        Assert.DoesNotContain("repository-read-invalid", TestModel.Codes(model));
    }

    /// <summary>Repository Read In A Loop Reports.</summary>
    /// <proves>litespec/backend/application#read-no-repository-inside-a-loop</proves>
    [Fact]
    public void Repository_Read_In_A_Loop_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            LoopReads: [new CodeLoopRead("IOrderRepository", "FindByIdAsync", file, 10)]));

        Assert.Contains("repository-read-invalid", TestModel.Codes(model));
    }

    /// <summary>Store Filtered Query Passes.</summary>
    /// <proves>litespec/backend/persistence#read-collection-queries-in-the-store-with-paging-and-filters</proves>
    [Fact]
    public void Store_Filtered_Query_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ListLoansQueryHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.list-loans", "lending", "list-loans", "ListLoans", "query", file));

        Assert.DoesNotContain("query-filter-invalid", TestModel.Codes(model));
    }

    /// <summary>Query Filter In Memory Reports.</summary>
    /// <proves>litespec/backend/persistence#read-collection-queries-in-the-store-with-paging-and-filters</proves>
    [Fact]
    public void Query_Filter_In_Memory_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ListLoansQueryHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.list-loans", "lending", "list-loans", "ListLoans", "query", file,
            MemoryFilters: [new CodeMemoryFilter(file, 9)]));

        Assert.Contains("query-filter-invalid", TestModel.Codes(model));
    }

    /// <summary>Effects After Commit Pass.</summary>
    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Effects_After_Commit_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.yaml"), """{"kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "after-commit"}, {"calls": "external-system/billing#void", "timing": "after-commit"}]}"""));

        Assert.DoesNotContain("effect-timing-invalid", TestModel.Codes(model));
    }

    /// <summary>Inline Send Reports.</summary>
    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Inline_Send_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.yaml"), """{"kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "inline"}]}"""));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "effect-timing-invalid");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
        Assert.Contains("notification/lending.voided", finding.Message, StringComparison.Ordinal);
        Assert.EndsWith(" inline.", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Approved Exception Holds An Inline Call On Its Subject.</summary>
    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Approved_Exception_Holds_An_Inline_Call_On_Its_Subject()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "effects": [{"calls": "external-system/bank#pay", "timing": "inline"}]}"""),
            TestModel.Record("exception", "exception/reimburse-expense-inline-call", Path.Combine(temp.Path, "inline.yaml"), """{"status": "approved", "kind": "gap", "covers": ["effect-timing-invalid"], "subject": ["use-case/spending.reimburse-expense"], "reason": "The spender sees the bank's answer at once.", "expiresOn": "2999-01-01"}"""));

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "effect-timing-invalid");
        Assert.Contains(known, finding => finding.Code == "effect-timing-invalid");
    }

    /// <summary>Inline Call Reports.</summary>
    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Inline_Call_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.yaml"), """{"kind": "command", "effects": [{"calls": "external-system/billing#void", "timing": "inline"}]}"""));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "effect-timing-invalid");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
        Assert.Contains("external-system/billing#void", finding.Message, StringComparison.Ordinal);
        Assert.EndsWith(" inline.", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Query Inline Send Is Not Reported.</summary>
    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Query_Inline_Send_Is_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "effects": [{"sends": "notification/lending.listed", "timing": "inline"}]}"""));

        Assert.DoesNotContain("effect-timing-invalid", TestModel.Codes(model));
    }

    /// <summary>Model Json Serializes The Handler Shape.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Model_Json_Serializes_The_Handler_Shape()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            "VoidOrderCommandHandler", 3, MessageType: "VoidOrderCommand", DependencyCount: 2,
            AggregateCalls: [new CodeCall("order", "Void", file, 8)],
            Collaborators: [new CodeCollaborator("OrderEraser", file, 7)],
            LoopReads: [new CodeLoopRead("IOrderRepository", "GetAsync", file, 10)],
            MemoryFilters: [new CodeMemoryFilter(file, 9)]));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Order", "Void", ["OrderVoidedEvent"], Path.Combine(temp.Path, "Order.cs"), 5));
        model.Code.AuditActions.Add(new CodeAuditAction(
            "VoidOrder", "void-order", Path.Combine(temp.Path, "VoidOrderCommand.cs"), 3));
        model.Code.Endpoints.Add(new EndpointOperation(
            "VoidOrder", "use-case/lending.void-order", "POST", "/api/orders/{orderId}/void",
            Path.Combine(temp.Path, "VoidOrderEndpoint.cs"), 11));

        var json = model.ToJson();

        foreach (var section in new[]
        {
            "aggregateMethods", "auditActions", "endpoints", "aggregateCalls",
            "collaborators", "loopReads", "memoryFilters", "dependencyCount", "messageType",
        })
        {
            Assert.Contains($"\"{section}\"", json);
        }
    }

    static ProjectModel DriftModel(string root, string message, string operation, string action)
    {
        var handlerFile = Path.Combine(root, "VoidOrderCommandHandler.cs");
        var endpointFile = Path.Combine(root, "VoidOrderEndpoint.cs");
        var commandFile = Path.Combine(root, "VoidOrderCommand.cs");
        var model = TestModel.Model(TestModel.Config(root));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", handlerFile,
            MessageType: message));
        model.Code.Endpoints.Add(new EndpointOperation(operation, "use-case/lending.void-order", "", "", endpointFile));
        model.Code.AuditActions.Add(new CodeAuditAction("VoidOrder", action, commandFile, 3));
        return model;
    }
}
