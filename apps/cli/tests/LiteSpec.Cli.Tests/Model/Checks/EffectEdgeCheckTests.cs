using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a send of another module's public query or command answers to a declared queries or commands edge, and a declared edge is sent.</summary>
public sealed class EffectEdgeCheckTests
{
    /// <summary>Handler Querying A Declared Public Query Passes.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void HandlerQueryingADeclaredPublicQueryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "RecordRepaymentCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/reporting.record-repayment", Path.Combine(temp.Path, "record.yaml"), """{"kind": "command", "effects": [{"queries": "use-case/spending.read-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/reporting.record-repayment", "reporting", "record-repayment", "RecordRepayment", "command", file,
            Dispatches: [new CodeDispatch("queries", "ReadExpenseQuery", "", file, 13)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
        Assert.DoesNotContain("effect-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Handler Querying Without A Declared Edge Reports.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void HandlerQueryingWithoutADeclaredEdgeReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "RecordRepaymentCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/reporting.record-repayment", Path.Combine(temp.Path, "record.yaml"), """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/reporting.record-repayment", "reporting", "record-repayment", "RecordRepayment", "command", file,
            Dispatches: [new CodeDispatch("queries", "ReadExpenseQuery", "", file, 13)]));

        Assert.Contains("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Declared Queries Edge No Code Sends Reports.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void DeclaredQueriesEdgeNoCodeSendsReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "RecordRepaymentCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/reporting.record-repayment", Path.Combine(temp.Path, "record.yaml"), """{"kind": "command", "effects": [{"queries": "use-case/spending.read-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/reporting.record-repayment", "reporting", "record-repayment", "RecordRepayment", "command", file));

        Assert.Contains("effect-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reaction Commanding A Declared Public Command Passes.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void ReactionCommandingADeclaredPublicCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "ReimburseOnPayoutReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""),
            TestModel.Record("reaction", "reaction/reporting.reimburse-on-payout", Path.Combine(temp.Path, "reimburse.yaml"), """{"triggeredBy": ["domain-event/payout.payout-confirmed"], "effects": [{"commands": "use-case/spending.reimburse-expense"}]}"""));
        model.Code.Reactions.Add(new CodeReaction(
            "ReimburseOnPayoutReaction", file, 3, null, "reaction/reporting.reimburse-on-payout",
            Dispatches: [new CodeDispatch("runs", "ReimburseExpenseCommand", "", file, 9)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
        Assert.DoesNotContain("effect-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reaction Commanding Without A Declared Edge Reports.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void ReactionCommandingWithoutADeclaredEdgeReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "ReimburseOnPayoutReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""),
            TestModel.Record("reaction", "reaction/reporting.reimburse-on-payout", Path.Combine(temp.Path, "reimburse.yaml"), """{"triggeredBy": ["domain-event/payout.payout-confirmed"], "effects": [{"sends": "notification/reporting.payout-done"}]}"""));
        model.Code.Reactions.Add(new CodeReaction(
            "ReimburseOnPayoutReaction", file, 3, null, "reaction/reporting.reimburse-on-payout",
            Dispatches: [new CodeDispatch("runs", "ReimburseExpenseCommand", "", file, 9)]));

        Assert.Contains("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Declared Commands Edge No Code Sends Reports.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void DeclaredCommandsEdgeNoCodeSendsReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "ReimburseOnPayoutReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""),
            TestModel.Record("reaction", "reaction/reporting.reimburse-on-payout", Path.Combine(temp.Path, "reimburse.yaml"), """{"triggeredBy": ["domain-event/payout.payout-confirmed"], "effects": [{"commands": "use-case/spending.reimburse-expense"}]}"""));
        model.Code.Reactions.Add(new CodeReaction(
            "ReimburseOnPayoutReaction", file, 3, null, "reaction/reporting.reimburse-on-payout"));

        Assert.Contains("effect-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reaction Calling The Delivering Port Takes Its Sends Edge.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void ReactionCallingTheDeliveringPortTakesItsSendsEdge()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "SendDueReminderReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "loan-due.yaml"), """{"deliveredThrough": "external-system/mail-relay"}"""),
            TestModel.Record("reaction", "reaction/lending.send-due-reminder", Path.Combine(temp.Path, "send-due-reminder.yaml"), """{"triggeredBy": ["domain-event/loan.loan-due"], "effects": [{"sends": "notification/lending.loan-due"}]}"""));
        var port = Path.Combine(temp.Path, "IMailRelay.cs");
        File.WriteAllText(port, "namespace Library.Lending.Ports.MailRelay;\n\npublic interface IMailRelay\n{\n}\n");
        model.Code.Elements.Add(Port("IMailRelay", port, "external-system/mail-relay"));
        model.Code.Reactions.Add(new CodeReaction(
            "SendDueReminderReaction", file, 3, null, "reaction/lending.send-due-reminder",
            Dispatches: [new CodeDispatch("calls", "SendAsync", "IMailRelay", file, 9)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
        Assert.DoesNotContain("effect-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reaction Calling Another Port Leaves Its Sends Edge Untaken.</summary>
    /// <proves>litespec/backend/application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void ReactionCallingAnotherPortLeavesItsSendsEdgeUntaken()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "SendDueReminderReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "loan-due.yaml"), """{"deliveredThrough": "external-system/mail-relay"}"""),
            TestModel.Record("reaction", "reaction/lending.send-due-reminder", Path.Combine(temp.Path, "send-due-reminder.yaml"), """{"triggeredBy": ["domain-event/loan.loan-due"], "effects": [{"sends": "notification/lending.loan-due"}]}"""));
        var port = Path.Combine(temp.Path, "IEventBus.cs");
        File.WriteAllText(port, "namespace Library.Lending.Ports.EventBus;\n\npublic interface IEventBus\n{\n}\n");
        model.Code.Elements.Add(Port("IEventBus", port, "external-system/event-bus"));
        model.Code.Reactions.Add(new CodeReaction(
            "SendDueReminderReaction", file, 3, null, "reaction/lending.send-due-reminder",
            Dispatches: [new CodeDispatch("calls", "PublishAsync", "IEventBus", file, 9)]));

        Assert.Contains("effect-undeclared", TestModel.Codes(model));
        Assert.Contains("effect-unimplemented", TestModel.Codes(model));
    }

    /// <summary>One port interface carrying one implements mark for its external system.</summary>
    /// <param name="class">The interface name carrying the mark.</param>
    /// <param name="file">The file the interface sits in.</param>
    /// <param name="recordId">The external-system id the mark names.</param>
    /// <returns>The marked port element.</returns>
    static MarkedElement Port(string @class, string file, string recordId) =>
        new(CodeKind.Port, @class, null, file, 3, [new CodeMark(recordId, MarkStyle.Xml, MarkVerb.Implements, file, 1)], InTests: false);
}
