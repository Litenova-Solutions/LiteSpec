using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a send of another module's public query or command answers to a declared queries or commands edge, and a declared edge is sent.</summary>
public sealed class EffectEdgeCheckTests
{
    /// <summary>Handler Querying A Declared Public Query Passes.</summary>
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
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

    /// <summary>Handler Calling A Declared Port Operation Takes Its Calls Edge.</summary>
    /// <proves>litespec/core/backend-application#dispatch-only-declared-effect-edges</proves>
    [Fact]
    public void HandlerCallingADeclaredPortOperationTakesItsCallsEdge()
    {
        using var temp = WriteCallsConsumer();
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Contains(model.Records, record => record.Id == "use-case/books.post-entry");
        var handler = Assert.Single(model.Code.Handlers, found => found.UseCaseId == "use-case/books.post-entry");
        var dispatch = Assert.Single(handler.Dispatches!);

        Assert.Equal("calls", dispatch.Verb);
        Assert.Equal("ReadReferenceRateAsync", dispatch.Target);
        Assert.Equal("IExchangeRateSource", dispatch.Detail);

        var codes = Checker.Check(model).Findings.Select(finding => finding.Code).ToList();

        Assert.DoesNotContain("effect-unimplemented", codes);
        Assert.DoesNotContain("effect-undeclared", codes);
    }

    /// <summary>Writes a consumer where a handler holds a marked port in a field and calls its operation for a declared calls edge.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteCallsConsumer()
    {
        var temp = TestCli.Temp("checks");

        try
        {
            WriteFile(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Effect Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "7.0.0",
                  "owners": [
                    "books-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Modules/*",
                      "kernel": "apps/api/src/Library.Kernel",
                      "platform": "apps/api/src/Library.Platform"
                    }
                  }
                }
                """);
            WriteFile(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/books/aggregates/entries/use-cases/post-entry/post-entry.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/books.post-entry",
                  "title": "Post entry",
                  "exposure": "A signed-in bookkeeper reaches this command through the books app under the post-entry permission.",
                  "kind": "command",
                  "access": {"grant": "permission/post-entry", "tenant": "own"},
                  "steps": [{"statement": "The bookkeeper posts the entry in the day's currency."}],
                  "effects": [{"calls": "external-system/exchange-rate-source#read-reference-rate", "timing": "after-commit"}]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/books/aggregates/entries/use-cases/post-entry/post-entry.md", """
                # Post entry

                The bookkeeper posts the entry.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/books/external-systems/exchange-rate-source/exchange-rate-source.yaml", """
                {
                  "type": "external-system",
                  "id": "external-system/exchange-rate-source",
                  "title": "Exchange rate source",
                  "summary": "The reference rates the books convert foreign entries with.",
                  "vendor": "RatesFeed",
                  "represents": "actor/rates-feed"
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/books/external-systems/exchange-rate-source/exchange-rate-source.md", """
                # Exchange rate source

                The reference rates the books convert with.
                """);
            WriteFile(temp.Path, "apps/api/src/Modules/Library.Books/Ports/ExchangeRateSource/IExchangeRateSource.cs", """
                namespace Library.Books.Ports.ExchangeRateSource;

                /// <implements>external-system/exchange-rate-source</implements>
                public interface IExchangeRateSource
                {
                    Task<decimal> ReadReferenceRateAsync(string currency, CancellationToken cancellationToken);
                }
                """);
            WriteFile(temp.Path, "apps/api/src/Modules/Library.Books/Application/Entries/PostEntry/PostEntryCommandHandler.cs", """
                namespace Library.Books.Application.Entries.PostEntry;

                /// <implements>use-case/books.post-entry</implements>
                public sealed class PostEntryCommandHandler(
                    IEntryRepository entries,
                    IExchangeRateSource exchangeRates) : ICommandHandler<PostEntryCommand>
                {
                    private readonly IExchangeRateSource _exchangeRates = exchangeRates;

                    public async Task HandleAsync(PostEntryCommand command, CancellationToken cancellationToken)
                    {
                        var rate = await _exchangeRates.ReadReferenceRateAsync(command.Currency, cancellationToken);
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

    static void WriteFile(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }

    /// <summary>One port interface carrying one implements mark for its external system.</summary>
    /// <param name="class">The interface name carrying the mark.</param>
    /// <param name="file">The file the interface sits in.</param>
    /// <param name="recordId">The external-system id the mark names.</param>
    /// <returns>The marked port element.</returns>
    static MarkedElement Port(string @class, string file, string recordId) =>
        new(CodeKind.Port, @class, null, file, 3, [new CodeMark(recordId, MarkStyle.Xml, MarkVerb.Implements, file, 1)], InTests: false);
}
