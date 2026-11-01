using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the write-undeclared, result-undeclared and effect-undeclared checks against hand-built models.</summary>
public sealed class CodeCheckTests
{
    /// <summary>Handler Taking An Undeclared Repository Reports.</summary>
    /// <proves>litespec/core/use-cases#name-every-aggregate-a-use-case-changes-or-reads</proves>
    [Fact]
    public void Handler_Taking_An_Undeclared_Repository_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Repositories: [new CodeRepository("IHoldRepository", file, 5)]));

        Assert.Contains("write-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Taking A Declared Repository Passes.</summary>
    /// <proves>litespec/core/use-cases#name-every-aggregate-a-use-case-changes-or-reads</proves>
    [Fact]
    public void Handler_Taking_A_Declared_Repository_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "reads": ["aggregate/lending.hold"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.md"), """{"status": "reviewed", "states": [{"name": "Waiting", "meaning": "The hold waits."}]}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Repositories: [new CodeRepository("IHoldRepository", file, 5)]));

        Assert.DoesNotContain("write-undeclared", TestModel.Codes(model));
    }

    /// <summary>Repository Serves The Aggregate Its Implementation Marks.</summary>
    /// <proves>litespec/core/use-cases#name-every-aggregate-a-use-case-changes-or-reads</proves>
    [Fact]
    public void Repository_Serves_The_Aggregate_Its_Implementation_Marks()
    {
        using var temp = Cli.Temp("checks");
        var handlerFile = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var repositoryFile = Path.Combine(temp.Path, "LoanBookRepository.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "changes": ["aggregate/lending.loan"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), """{"status": "reviewed", "states": [{"name": "Open", "meaning": "The loan runs."}]}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", handlerFile,
            Repositories: [new CodeRepository("ILoanBookRepository", handlerFile, 5)]));
        model.Code.RepositoryImplementations.Add(new CodeRepositoryImplementation("ILoanBookRepository", "LoanBookRepository", repositoryFile, 3));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Technical, "LoanBookRepository", null, repositoryFile, 3,
            [new CodeMark("aggregate/lending.loan", MarkStyle.Xml, MarkVerb.Implements, repositoryFile, 2)],
            InTests: false));

        Assert.DoesNotContain("write-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Producing An Undeclared Literal Reports.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Producing_An_Undeclared_Literal_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Refusals: [new CodeRefusal("lending.ghost-refusal", file, 9)]));

        Assert.Contains("result-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Throwing An Undeclared Type Reports.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Throwing_An_Undeclared_Type_Reports()
    {
        using var temp = Cli.Temp("checks");
        var handlerFile = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Literals.Add(new CodeLiteral(
            "lending.ghost-refusal", Path.Combine(temp.Path, "LoanGhostRefusalException.cs"), 8, "LoanGhostRefusalException"));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", handlerFile,
            Thrown: [new CodeThrown("LoanGhostRefusalException", handlerFile, 11)]));

        Assert.Contains("result-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Producing A Declared Code Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Producing_A_Declared_Code_Passes()
    {
        using var temp = Cli.Temp("checks");
        var handlerFile = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""),
            TestModel.Record(
                "result",
                "result/borrow-copy.copy-unavailable",
                Path.Combine(temp.Path, "borrow.md"),
                """{"status": "reviewed", "outcome": "refusal", "code": "lending.copy-not-available", "meaning": "The copy is unavailable."}"""));
        model.Code.Literals.Add(new CodeLiteral(
            "lending.copy-not-available", Path.Combine(temp.Path, "CopyNotAvailableException.cs"), 8, "CopyNotAvailableException"));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", handlerFile,
            Thrown: [new CodeThrown("CopyNotAvailableException", handlerFile, 11)]));

        Assert.DoesNotContain("result-undeclared", TestModel.Codes(model));
    }

    /// <summary>Permission Grant Allows The Forbidden Code.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Permission_Grant_Allows_The_Forbidden_Code()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "access": {"auth": ["session"], "grant": "permission/borrow-copy", "tenant": "own"}}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Refusals: [new CodeRefusal("authorization.forbidden", file, 9)]));

        Assert.DoesNotContain("result-undeclared", TestModel.Codes(model));
    }

    /// <summary>Custom Refusals Codes Are Allowed.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Custom_Refusals_Codes_Are_Allowed()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"refusals": {"forbidden": "library.forbidden", "invalidInput": "library.invalid"}}""");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "access": {"auth": ["session"], "grant": "permission/borrow-copy", "tenant": "own"}}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Refusals: [new CodeRefusal("library.forbidden", file, 9)]));

        Assert.DoesNotContain("result-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Dispatching An Undeclared Command Reports.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Dispatching_An_Undeclared_Command_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Dispatches: [new CodeDispatch("runs", "PurgeLoansCommand", "", file, 13)]));

        Assert.Contains("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Dispatching A Declared Command Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Dispatching_A_Declared_Command_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "effects": [{"runs": "use-case/lending.purge-loans", "timing": "async"}]}"""),
            TestModel.Record("use-case", "use-case/lending.purge-loans", Path.Combine(temp.Path, "purge.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Dispatches: [new CodeDispatch("runs", "PurgeLoansCommand", "", file, 13)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Reaction Dispatching Against Runs Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Reaction_Dispatching_Against_Runs_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "LoanBorrowedReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""),
            TestModel.Record("reaction", "reaction/lending.notify-on-borrow", Path.Combine(temp.Path, "notify.md"), """{"status": "reviewed", "triggeredBy": ["event/loan.loan-borrowed"], "runs": ["use-case/lending.borrow-copy"]}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "LoanBorrowedReaction", file, 3, null, "reaction/lending.notify-on-borrow",
            Dispatches: [new CodeDispatch("runs", "BorrowCopyCommand", "", file, 9)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Reaction Dispatching Against An Effects Entry Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Reaction_Dispatching_Against_An_Effects_Entry_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "LoanBorrowedReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""),
            TestModel.Record("reaction", "reaction/lending.notify-on-borrow", Path.Combine(temp.Path, "notify.md"), """{"status": "reviewed", "triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.borrow-copy"}]}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "LoanBorrowedReaction", file, 3, null, "reaction/lending.notify-on-borrow",
            Dispatches: [new CodeDispatch("runs", "BorrowCopyCommand", "", file, 9)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Untagged Reaction In A Use Case Folder Answers To The Use Case Effects.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Untagged_Reaction_In_A_Use_Case_Folder_Answers_To_The_Use_Case_Effects()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyOnRequestReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""),
            TestModel.Record("use-case", "use-case/lending.purge-loans", Path.Combine(temp.Path, "purge.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "BorrowCopyOnRequestReaction", file, 3, "use-case/lending.borrow-copy",
            Dispatches: [new CodeDispatch("runs", "BorrowCopyCommand", "", file, 9), new CodeDispatch("runs", "PurgeLoansCommand", "", file, 12)]));

        var undeclared = Checker.Check(model).Findings.Where(finding => finding.Code == "effect-undeclared").ToList();

        Assert.Single(undeclared);
        Assert.Contains("PurgeLoansCommand", undeclared[0].Message);
    }

    /// <summary>Reaction Calling A Declared Operation Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Reaction_Calling_A_Declared_Operation_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "PushPassOnLoanReaction.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.push-pass-on-loan", Path.Combine(temp.Path, "push.md"), """{"status": "reviewed", "triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"calls": "external-system/wallet#push"}]}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "PushPassOnLoanReaction", file, 3, null, "reaction/lending.push-pass-on-loan",
            Dispatches: [new CodeDispatch("calls", "PushAsync", "IWalletPasses", file, 9)]));

        Assert.DoesNotContain("effect-undeclared", TestModel.Codes(model));
    }

    /// <summary>Handler Dispatching Through A Helper Names The Helper.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Dispatching_Through_A_Helper_Names_The_Helper()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Dispatches: [new CodeDispatch("runs", "PurgeLoansCommand", "", file, 13, "LoanFanout.RetryAsync")]));

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "effect-undeclared");

        Assert.Contains("via 'LoanFanout.RetryAsync'", finding.Message);
    }

    /// <summary>Handler Dispatching Directly Names No Helper.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_Dispatching_Directly_Names_No_Helper()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "BorrowCopyCommandHandler.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command", file,
            Dispatches: [new CodeDispatch("runs", "PurgeLoansCommand", "", file, 13)]));

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "effect-undeclared");

        Assert.DoesNotContain(" via '", finding.Message);
    }
}
