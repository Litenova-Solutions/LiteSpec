using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the handler shape checks: the use-case verb against the aggregate method, event tense, names across layers, route shape, reasonless names, collaborators, loop reads, memory filters, dependency counts and inline sends.</summary>
public sealed class HandlerShapeCheckTests
{
    [Fact]
    public void Handler_Calling_Its_Verb_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            AggregateCalls: [new CodeCall("order", "Void", file, 8)]));

        Assert.DoesNotContain("use-case-verb-method", TestModel.Codes(model));
    }

    [Fact]
    public void Handler_Calling_Another_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            AggregateCalls: [new CodeCall("order", "Abandon", file, 8)]));
        model.Code.AggregateMethods.Add(new AggregateMethod("Order", "Abandon", ["OrderAbandonedEvent"], file, 3));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "use-case-verb-method");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Abandon", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_Calling_Only_A_Reading_Method_Is_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            AggregateCalls: [new CodeCall("order", "IsVoidable", file, 8)]));
        model.Code.AggregateMethods.Add(new AggregateMethod("Order", "IsVoidable", [], file, 3));

        Assert.DoesNotContain("use-case-verb-method", TestModel.Codes(model));
    }

    [Fact]
    public void Handler_Calling_No_Aggregate_Method_Is_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file));

        Assert.DoesNotContain("use-case-verb-method", TestModel.Codes(model));
    }

    [Fact]
    public void Method_Raising_Its_Past_Tense_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Order", "Abandon", ["OrderAbandonedEvent"], Path.Combine(temp.Path, "Order.cs"), 5));

        Assert.DoesNotContain("event-tense-mismatch", TestModel.Codes(model));
    }

    [Fact]
    public void Method_Raising_Another_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Order", "Abandon", ["OrderVoidedEvent"], Path.Combine(temp.Path, "Order.cs"), 5));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "event-tense-mismatch");

        Assert.Contains("abandoned", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_Past_Tense_Map_Tenses_A_Domain_Verb()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"vocabulary": {"pastTense": {"write": "written"}}}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Document", "Write", ["DocumentWrittenEvent"], Path.Combine(temp.Path, "Document.cs"), 5));

        Assert.DoesNotContain("event-tense-mismatch", TestModel.Codes(model));
    }

    [Fact]
    public void Domain_Verb_Without_A_Map_Entry_Tenses_Regularly()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Document", "Smite", ["DocumentSmittenEvent"], Path.Combine(temp.Path, "Document.cs"), 5));

        Assert.Contains("event-tense-mismatch", TestModel.Codes(model));
    }

    [Fact]
    public void Common_Verb_Doubling_Its_Consonant_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Lead", "Submit", ["LeadSubmittedEvent"], Path.Combine(temp.Path, "Lead.cs"), 5));

        Assert.DoesNotContain("event-tense-mismatch", TestModel.Codes(model));
    }

    [Fact]
    public void Second_Route_With_Its_Own_Operation_Id_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "void-order");
        model.Code.Endpoints.Add(new EndpointOperation("VoidOrderByNumber", "use-case/lending.void-order", "", "", Path.Combine(temp.Path, "VoidOrderEndpoint.cs")));

        Assert.DoesNotContain("name-drift-across-layers", TestModel.Codes(model));
    }

    [Fact]
    public void Matching_Layers_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "void-order");

        Assert.DoesNotContain("name-drift-across-layers", TestModel.Codes(model));
    }

    [Fact]
    public void Drifted_Message_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "CancelOrderCommand", "VoidOrder", "void-order");

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-drift-across-layers");

        Assert.Contains("CancelOrderCommand", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Drifted_Operation_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "CancelOrder", "void-order");

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-drift-across-layers");

        Assert.Contains("CancelOrder", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Drifted_Audit_Action_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "cancel-order");

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-drift-across-layers");

        Assert.Contains("cancel-order", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shaped_Command_Route_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/orders/{orderId}/void");

        Assert.DoesNotContain("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Command_Post_To_The_Collection_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/orders");

        Assert.DoesNotContain("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Command_Put_On_One_Resource_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "PUT", "/api/orders/{orderId}");

        Assert.DoesNotContain("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Command_Route_Ending_Outside_Its_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/orders/{orderId}/cancel");

        Assert.Contains("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Command_Route_Without_Its_Aggregate_Plural_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/ledger/{orderId}/void");

        Assert.Contains("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Query_Route_Skips_The_Verb_Last_Segment()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "query", "GET", "/api/orders");

        Assert.DoesNotContain("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Query_Route_Without_Its_Aggregate_Plural_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "query", "GET", "/api/ledger");

        Assert.Contains("route-shape", TestModel.Codes(model));
    }

    [Fact]
    public void Reasoned_Names_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Lending", "VoidOrderCommandHandler.cs"),
            "VoidOrderCommandHandler"));

        Assert.DoesNotContain("reasonless-name", TestModel.Codes(model));
    }

    [Fact]
    public void Reasonless_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Shared", "VoidOrderCommandHandler.cs"),
            "VoidOrderCommandHandler"));

        Assert.Contains("reasonless-name", TestModel.Codes(model));
    }

    [Fact]
    public void Reasonless_Type_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Lending", "OrderManager.cs"),
            "OrderManager"));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "reasonless-name");

        Assert.Contains("OrderManager", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Kernel_Folder_Is_Exempt()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"stackConfig": {"dotnet": {"sharedKernel": "src/App/Shared"}}}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command",
            Path.Combine(temp.Path, "src", "App", "Shared", "OrderHelper.cs"),
            "OrderHelper"));

        Assert.DoesNotContain("reasonless-name", TestModel.Codes(model));
    }

    [Fact]
    public void Injected_Collaborators_Pass()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file));

        Assert.DoesNotContain("collaborator-holds-repository", TestModel.Codes(model));
    }

    [Fact]
    public void Collaborator_Holding_A_Repository_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            Collaborators: [new CodeCollaborator("OrderEraser", file, 7)]));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "collaborator-holds-repository");

        Assert.Contains("OrderEraser", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reaction_Collaborator_Holding_A_Repository_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "SweepReaction.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Reactions.Add(new ReactionInfo(
            "SweepReaction", file, 3, "reaction", null, "reaction/lending.sweep",
            Collaborators: [new CodeCollaborator("OrderEraser", file, 9)]));

        Assert.Contains("collaborator-holds-repository", TestModel.Codes(model));
    }

    [Fact]
    public void Reads_Outside_Loops_Pass()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file));

        Assert.DoesNotContain("repository-read-in-loop", TestModel.Codes(model));
    }

    [Fact]
    public void Repository_Read_In_A_Loop_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            LoopReads: [new CodeLoopRead("IOrderRepository", "FindByIdAsync", file, 10)]));

        Assert.Contains("repository-read-in-loop", TestModel.Codes(model));
    }

    [Fact]
    public void Store_Filtered_Query_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ListLoansQueryHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.list-loans", "lending", "list-loans", "ListLoans", "query", file));

        Assert.DoesNotContain("query-filters-in-memory", TestModel.Codes(model));
    }

    [Fact]
    public void Query_Filter_In_Memory_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ListLoansQueryHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.list-loans", "lending", "list-loans", "ListLoans", "query", file,
            MemoryFilters: [new CodeMemoryFilter(file, 9)]));

        Assert.Contains("query-filters-in-memory", TestModel.Codes(model));
    }

    [Fact]
    public void Ten_Dependencies_Pass()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            DependencyCount: 10));

        Assert.DoesNotContain("dependency-count", TestModel.Codes(model));
    }

    [Fact]
    public void Eleven_Dependencies_Report()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            DependencyCount: 11));

        Assert.Contains("dependency-count", TestModel.Codes(model));
    }

    [Fact]
    public void Reaction_With_Eleven_Dependencies_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "SweepReaction.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Reactions.Add(new ReactionInfo(
            "SweepReaction", file, 3, "reaction", null, "reaction/lending.sweep", DependencyCount: 12));

        Assert.Contains("dependency-count", TestModel.Codes(model));
    }

    [Fact]
    public void Sends_After_Commit_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.md"), """{"kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "after-commit"}]}"""));

        Assert.DoesNotContain("send-before-commit", TestModel.Codes(model));
    }

    [Fact]
    public void Inline_Send_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.md"), """{"kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "inline"}]}"""));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "send-before-commit");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Query_Inline_Send_Is_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "list.md"), """{"kind": "query", "effects": [{"sends": "notification/lending.listed", "timing": "inline"}]}"""));

        Assert.DoesNotContain("send-before-commit", TestModel.Codes(model));
    }

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

    static ProjectModel RouteModel(string root, string kind, string method, string route)
    {
        var folder = Path.Combine(root, "docs", "behavior", "modules", "lending", "orders");
        var model = TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(folder, "void-order.md"), $$"""{"kind": "{{kind}}"}"""),
            TestModel.Record("aggregate", "aggregate/lending.order", Path.Combine(folder, "aggregate.md"), """{"states": [{"name": "Open", "meaning": "The order is open."}]}"""));
        model.Code.Endpoints.Add(new EndpointOperation("", "use-case/lending.void-order", method, route, Path.Combine(root, "VoidOrderEndpoint.cs")));
        return model;
    }
}
