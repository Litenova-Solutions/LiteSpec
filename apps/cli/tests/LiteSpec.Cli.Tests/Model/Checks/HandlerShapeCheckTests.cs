using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the handler shape checks: the use-case verb against the aggregate method, event tense, names across layers, route shape, reasonless names, collaborators, loop reads, memory filters, dependency counts and inline effects.</summary>
public sealed class HandlerShapeCheckTests
{
    /// <proves>litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs</proves>
    [Fact]
    public void Handler_Calling_Its_Verb_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            AggregateCalls: [new CodeCall("order", "Void", file, 8)]));

        Assert.DoesNotContain("use-case-method-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs</proves>
    [Fact]
    public void Handler_Recording_A_Request_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ReimburseExpenseCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/spending.reimburse-expense", "spending", "reimburse-expense", "ReimburseExpense", "command", file,
            AggregateCalls: [new CodeCall("expense", "RequestPayout", file, 8)]));
        model.Code.AggregateMethods.Add(new AggregateMethod("Expense", "RequestPayout", ["ExpensePayoutRequestedEvent"], file, 3));

        Assert.DoesNotContain("use-case-method-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs</proves>
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

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "use-case-method-drift");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Abandon", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs</proves>
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

        Assert.DoesNotContain("use-case-method-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs</proves>
    [Fact]
    public void Handler_Calling_No_Aggregate_Method_Is_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file));

        Assert.DoesNotContain("use-case-method-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Method_Raising_Its_Past_Tense_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Order", "Abandon", ["OrderAbandonedEvent"], Path.Combine(temp.Path, "Order.cs"), 5));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Method_Raising_Another_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Order", "Abandon", ["OrderVoidedEvent"], Path.Combine(temp.Path, "Order.cs"), 5));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "event-tense-invalid");

        Assert.Contains("abandoned", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
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

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Domain_Verb_Without_A_Map_Entry_Tenses_Regularly()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Document", "Smite", ["DocumentSmittenEvent"], Path.Combine(temp.Path, "Document.cs"), 5));

        Assert.Contains("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Common_Verb_Doubling_Its_Consonant_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Lead", "Submit", ["LeadSubmittedEvent"], Path.Combine(temp.Path, "Lead.cs"), 5));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Irregular_Past_Tense_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "Override", ["ExpenseOverriddenEvent"], Path.Combine(temp.Path, "Expense.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "Withhold", ["ExpenseWithheldEvent"], Path.Combine(temp.Path, "Expense.cs"), 9));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Doubled_Final_Consonant_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Copy", "Shred", ["CopyShreddedEvent"], Path.Combine(temp.Path, "Copy.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Copy", "Untag", ["CopyUntaggedEvent"], Path.Combine(temp.Path, "Copy.cs"), 9));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Prefixed_Verb_Tensing_Its_Base_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "Reset", ["ExpenseResetEvent"], Path.Combine(temp.Path, "Expense.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "Resend", ["ExpenseResentEvent"], Path.Combine(temp.Path, "Expense.cs"), 9));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Event_With_Prepositional_Complement_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "AssignToApprover", ["ExpenseAssignedToApproverEvent"], Path.Combine(temp.Path, "Expense.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Copy", "List", ["CopyListedForResaleEvent"], Path.Combine(temp.Path, "Copy.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "CorrectAfterSales", ["ExpenseCorrectedAfterSalesEvent"], Path.Combine(temp.Path, "Expense.cs"), 9));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Event_With_Particle_Complement_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Loan", "SignOff", ["LoanSignedOffEvent"], Path.Combine(temp.Path, "Loan.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Member", "UnlinkLoginByStaff", ["MemberLoginUnlinkedByStaffEvent"], Path.Combine(temp.Path, "Member.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "SettleOutsideProcessor", ["ExpenseSettledOutsideProcessorEvent"], Path.Combine(temp.Path, "Expense.cs"), 5));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Mark_Method_Naming_Its_State_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Loan", "MarkOverdue", ["LoanOverdueEvent"], Path.Combine(temp.Path, "Loan.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "MarkSent", ["ExpenseSentEvent"], Path.Combine(temp.Path, "Expense.cs"), 5));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Copy", "MarkProvisioned", ["CopyProvisionedEvent"], Path.Combine(temp.Path, "Copy.cs"), 5));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Method_Raising_Its_Own_Fact_And_A_Consequence_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Loan", "ChangeDueDate", ["LoanDueDateChangedEvent", "LoanRemindersClearedEvent"], Path.Combine(temp.Path, "Loan.cs"), 5));

        Assert.DoesNotContain("event-tense-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Method_Raising_Only_Other_Facts_Reports_Each()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Loan", "ChangeDueDate", ["LoanExtendedEvent", "LoanRemindersClearedEvent"], Path.Combine(temp.Path, "Loan.cs"), 5));

        Assert.Equal(2, Checker.Check(model).Findings.Count(finding => finding.Code == "event-tense-invalid"));
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Mark_Method_Naming_Another_State_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Loan", "MarkOverdue", ["LoanLostEvent"], Path.Combine(temp.Path, "Loan.cs"), 5));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "event-tense-invalid");

        Assert.Contains("overdue", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Define_Raising_Another_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Policy", "Define", ["PolicySetEvent"], Path.Combine(temp.Path, "Policy.cs"), 5));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "event-tense-invalid");

        Assert.Contains("defined", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Request_Raising_Another_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Loan", "Request", ["LoanGrantedEvent"], Path.Combine(temp.Path, "Loan.cs"), 5));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "event-tense-invalid");

        Assert.Contains("requested", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Fact]
    public void Cancel_With_American_Spelling_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.AggregateMethods.Add(new AggregateMethod(
            "Expense", "Cancel", ["ExpenseCanceledEvent"], Path.Combine(temp.Path, "Expense.cs"), 5));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "event-tense-invalid");

        Assert.Contains("cancelled", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/api#keep-one-name-for-a-use-case-across-layers</proves>
    [Fact]
    public void Second_Route_With_Its_Own_Operation_Id_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "void-order");
        model.Code.Endpoints.Add(new EndpointOperation("VoidOrderByNumber", "use-case/lending.void-order", "", "", Path.Combine(temp.Path, "VoidOrderEndpoint.cs")));

        Assert.DoesNotContain("name-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#keep-one-name-for-a-use-case-across-layers</proves>
    [Fact]
    public void Routes_Each_Opening_With_The_Use_Case_Name_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrderByCard", "void-order");
        model.Code.Endpoints.Add(new EndpointOperation("VoidOrderByNumber", "use-case/lending.void-order", "", "", Path.Combine(temp.Path, "VoidOrderEndpoint.cs")));

        Assert.DoesNotContain("name-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#declare-stable-operation-metadata-in-the-contract</proves>
    [Fact]
    public void Two_Endpoints_With_One_Operation_Id_Report()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "void-order");
        model.Code.Endpoints.Add(new EndpointOperation("VoidOrder", "use-case/lending.void-order", "POST", "/api/orders/{orderId}/void-again", Path.Combine(temp.Path, "VoidOrderEndpoint.cs")));

        Assert.Contains("operation-id-duplicate", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#keep-one-name-for-a-use-case-across-layers</proves>
    [Fact]
    public void Matching_Layers_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "void-order");

        Assert.DoesNotContain("name-drift", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#keep-one-name-for-a-use-case-across-layers</proves>
    [Fact]
    public void Drifted_Message_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "CancelOrderCommand", "VoidOrder", "void-order");

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-drift");

        Assert.Contains("CancelOrderCommand", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/api#keep-one-name-for-a-use-case-across-layers</proves>
    [Fact]
    public void Drifted_Operation_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "CancelOrder", "void-order");

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-drift");

        Assert.Contains("CancelOrder", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/api#keep-one-name-for-a-use-case-across-layers</proves>
    [Fact]
    public void Drifted_Audit_Action_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = DriftModel(temp.Path, "VoidOrderCommand", "VoidOrder", "cancel-order");

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "name-drift");

        Assert.Contains("cancel-order", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Shaped_Command_Route_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/orders/{orderId}/void");

        Assert.DoesNotContain("route-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Command_Post_To_The_Collection_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/orders");

        Assert.DoesNotContain("route-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Command_Put_On_One_Resource_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "PUT", "/api/orders/{orderId}");

        Assert.DoesNotContain("route-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Command_Route_Ending_Outside_Its_Verb_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/orders/{orderId}/cancel");

        Assert.Contains("route-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Command_Route_Without_Its_Aggregate_Plural_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "command", "POST", "/api/ledger/{orderId}/void");

        Assert.Contains("route-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Query_Route_Skips_The_Verb_Last_Segment()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "query", "GET", "/api/orders");

        Assert.DoesNotContain("route-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/api#shape-command-routes-from-the-aggregate-and-the-verb</proves>
    [Fact]
    public void Query_Route_Without_Its_Aggregate_Plural_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = RouteModel(temp.Path, "query", "GET", "/api/ledger");

        Assert.Contains("route-invalid", TestModel.Codes(model));
    }

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

    /// <proves>litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason</proves>
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

        Assert.DoesNotContain("name-reason-missing", TestModel.Codes(model));
    }

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

    /// <proves>litespec/backend/application#build-collaborators-through-the-container</proves>
    [Fact]
    public void Automation_Collaborator_Holding_A_Repository_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "SweepAutomation.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Automations.Add(new AutomationInfo(
            "SweepAutomation", file, 3, null, "automation/lending.sweep",
            Collaborators: [new CodeCollaborator("OrderEraser", file, 9)]));

        Assert.Contains("collaborator-repository-invalid", TestModel.Codes(model));
    }

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

    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact]
    public void Ten_Dependencies_Pass()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            DependencyCount: 10));

        Assert.DoesNotContain("dependency-count-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact]
    public void Eleven_Dependencies_Report()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "VoidOrderCommandHandler.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", file,
            DependencyCount: 11));

        Assert.Contains("dependency-count-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact]
    public void Automation_With_Eleven_Dependencies_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "SweepAutomation.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Automations.Add(new AutomationInfo(
            "SweepAutomation", file, 3, null, "automation/lending.sweep", DependencyCount: 12));

        Assert.Contains("dependency-count-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Effects_After_Commit_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.md"), """{"kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "after-commit"}, {"calls": "external-system/billing#void", "timing": "after-commit"}]}"""));

        Assert.DoesNotContain("effect-timing-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Inline_Send_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.md"), """{"kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "inline"}]}"""));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "effect-timing-invalid");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
        Assert.Contains("notification/lending.voided", finding.Message, StringComparison.Ordinal);
        Assert.EndsWith(" inline.", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Approved_Deviation_Holds_An_Inline_Call_On_Its_Subject()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.md"), """{"kind": "command", "effects": [{"calls": "external-system/bank#pay", "timing": "inline"}]}"""),
            TestModel.Record("deviation", "deviation/reimburse-expense-inline-call", Path.Combine(temp.Path, "inline.md"), """{"status": "approved", "provision": "litespec/backend/application#call-external-systems-through-their-ports", "reason": "The spender sees the bank's answer at once.", "reviewBy": "2999-01-01", "removeWhen": "The bank answers by message.", "subject": ["use-case/spending.reimburse-expense"]}"""));

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "effect-timing-invalid");
        Assert.Contains(known, finding => finding.Code == "effect-timing-invalid");
    }

    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Draft_Deviation_Holds_Nothing()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.md"), """{"kind": "command", "effects": [{"calls": "external-system/bank#pay", "timing": "inline"}]}"""),
            TestModel.Record("deviation", "deviation/reimburse-expense-inline-call", Path.Combine(temp.Path, "inline.md"), """{"status": "draft", "provision": "litespec/backend/application#call-external-systems-through-their-ports", "reason": "The spender sees the bank's answer at once.", "reviewBy": "2999-01-01", "removeWhen": "The bank answers by message.", "subject": ["use-case/spending.reimburse-expense"]}"""));

        Assert.Contains("effect-timing-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Inline_Call_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.void-order", Path.Combine(temp.Path, "void.md"), """{"kind": "command", "effects": [{"calls": "external-system/billing#void", "timing": "inline"}]}"""));

        var finding = Checker.Check(model).Findings.Single(finding => finding.Code == "effect-timing-invalid");

        Assert.Contains("void-order", finding.Message, StringComparison.Ordinal);
        Assert.Contains("external-system/billing#void", finding.Message, StringComparison.Ordinal);
        Assert.EndsWith(" inline.", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/backend/application#send-messages-after-commit</proves>
    [Fact]
    public void Query_Inline_Send_Is_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "list.md"), """{"kind": "query", "effects": [{"sends": "notification/lending.listed", "timing": "inline"}]}"""));

        Assert.DoesNotContain("effect-timing-invalid", TestModel.Codes(model));
    }

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
