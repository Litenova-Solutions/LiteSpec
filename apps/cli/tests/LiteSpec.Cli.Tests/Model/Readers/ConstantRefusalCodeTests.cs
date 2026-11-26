using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves the backend reader resolves refusal codes held in generated constants the same as typed literals.</summary>
public sealed class ConstantRefusalCodeTests
{
    /// <summary>Code Property Holding A Generated Constant Is Collected.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void CodePropertyConstantIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                public ExpenseReceiptMissingException()
                    : base("The expense is already reimbursed.")
                {
                }

                public override string Code => SpendingRefusalCodes.ReceiptMissing;
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));
        var literal = Assert.Single(code.Literals, literal => literal.Declares);

        Assert.Equal("spending.receipt-missing", literal.Value);
        Assert.Equal("ExpenseReceiptMissingException", literal.Class);
        Assert.Equal("spending.receipt-missing", code.Elements.Single(element => element.Class == "ExpenseReceiptMissingException").RuntimeValue);
    }

    /// <summary>RefusalCode Constant Holding A Generated Constant Is Collected.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void RefusalCodeConstWithConstantIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                public const string RefusalCode = SpendingRefusalCodes.ReceiptMissing;

                public ExpenseReceiptMissingException()
                    : base(RefusalCode, "The expense is already reimbursed.")
                {
                }
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));
        var declaring = code.Literals.Where(literal => literal.Declares).ToList();

        Assert.Equal(2, declaring.Count);
        Assert.All(declaring, literal => Assert.Equal("spending.receipt-missing", literal.Value));
        Assert.All(declaring, literal => Assert.Equal("ExpenseReceiptMissingException", literal.Class));
        Assert.Contains(declaring, literal => literal.Line == 6);
        Assert.Contains(declaring, literal => literal.Line == 9);
        Assert.Equal("spending.receipt-missing", code.Elements.Single(element => element.Class == "ExpenseReceiptMissingException").RuntimeValue);
    }

    /// <summary>Base Call Passing A Generated Constant Is Collected.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void BaseInitializerConstantIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                public ExpenseReceiptMissingException()
                    : base(SpendingRefusalCodes.ReceiptMissing, "The expense is already reimbursed.")
                {
                }
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));
        var literal = Assert.Single(code.Literals, literal => literal.Declares);

        Assert.Equal("spending.receipt-missing", literal.Value);
        Assert.Equal("ExpenseReceiptMissingException", literal.Class);
        Assert.Equal("spending.receipt-missing", code.Elements.Single(element => element.Class == "ExpenseReceiptMissingException").RuntimeValue);
    }

    /// <summary>RefusalCode Constant Holding A Typed Literal Is Collected.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void RefusalCodeLiteralIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                public const string RefusalCode = "spending.receipt-missing";

                public ExpenseReceiptMissingException()
                    : base(RefusalCode, "The expense is already reimbursed.")
                {
                }
            }
            """);

        var declaring = Literals(temp.Path);

        Assert.Equal(2, declaring.Count);
        Assert.All(declaring, literal => Assert.Equal("spending.receipt-missing", literal.Value));
        Assert.All(declaring, literal => Assert.Equal("ExpenseReceiptMissingException", literal.Class));
        Assert.Contains(declaring, literal => literal.Line == 6);
        Assert.Contains(declaring, literal => literal.Line == 9);
    }

    /// <summary>Bare Identifier Holding A Same-Type Constant Is Collected.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void BareIdentifierInsideTypeResolves()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                private const string Held = "spending.receipt-missing";

                public ExpenseReceiptMissingException()
                    : base("The expense is already reimbursed.")
                {
                }

                public override string Code => Held;
            }
            """);

        var literal = Assert.Single(Literals(temp.Path));

        Assert.Equal("spending.receipt-missing", literal.Value);
        Assert.Equal("ExpenseReceiptMissingException", literal.Class);
    }

    /// <summary>Constant Outside A Declaring Position Declares Nothing.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ConstantOutsideDeclaringPositionIsIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Application/BorrowCopy/BorrowCopyCommandHandler.cs", """
            namespace Library.Modules.Spending.Application.BorrowCopy;

            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
                public Task Handle(BorrowCopyCommand command)
                {
                    Console.WriteLine(SpendingRefusalCodes.ReceiptMissing);
                    return Task.CompletedTask;
                }
            }
            """);

        Assert.Empty(Literals(temp.Path));
    }

    /// <summary>Constant Holding No Refusal Code Declares Nothing.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void NonCodeConstantInCodePositionIsIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseNoteException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            public static class ExpenseNotes
            {
                public const string Sentence = "The expense is already reimbursed.";
            }

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseNoteException : DomainException
            {
                public ExpenseNoteException()
                    : base("The expense is already reimbursed.")
                {
                }

                public override string Code => ExpenseNotes.Sentence;
            }
            """);

        Assert.Empty(Literals(temp.Path));
    }

    /// <summary>Handler Throwing A Generated Constant Produces Its Code.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerThrowConstantIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Application/ReturnCopy/ReturnCopyCommandHandler.cs", """
            namespace Library.Modules.Spending.Application.ReturnCopy;

            /// <implements>use-case/lending.return-copy</implements>
            public sealed class ReturnCopyCommandHandler : ICommandHandler<ReturnCopyCommand>
            {
                public object Handle(ReturnCopyCommand command)
                {
                    if (command.Late)
                        throw new UseCaseUnprocessableException(SpendingRefusalCodes.ReceiptMissing, "The copy is late.");
                    return new object();
                }
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));
        var handler = Assert.Single(code.Handlers);
        var refusal = Assert.Single(handler.Refusals!);

        Assert.Equal("spending.receipt-missing", refusal.Code);
        Assert.Contains(code.Literals, literal => literal.Declares && literal.Value == "spending.receipt-missing" && literal.Class == "ReturnCopyCommandHandler");
    }

    /// <summary>Two Exceptions Marking One Rule With Different Constants Report No Duplicate.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void TwoExceptionsOneRuleDifferentConstantsReportNoDuplicate()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                public ExpenseReceiptMissingException()
                    : base("The expense is already reimbursed.")
                {
                }

                public override string Code => SpendingRefusalCodes.ReceiptMissing;
            }
            """);
        WriteSource(temp.Path, "Spending/Domain/Receipts/Exceptions/ReceiptExpenseDecidedException.cs", """
            namespace Library.Modules.Spending.Domain.Receipts.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ReceiptExpenseDecidedException : DomainException
            {
                public ReceiptExpenseDecidedException()
                    : base("The receipt is decided.")
                {
                }

                public override string Code => SpendingRefusalCodes.ExpenseDecided;
            }
            """);

        var model = OwnershipModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(model.Config);
        ModelLinks.ComputeImplements(model);

        var declaring = model.Code.Literals.Where(literal => literal.Declares).Select(literal => literal.Value).ToList();

        Assert.Contains("spending.receipt-missing", declaring);
        Assert.Contains("spending.expense-decided", declaring);
        Assert.Equal(2, model.Code.Elements.Count(element => element.Kind == CodeKind.RuleException));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "refusal-code-duplicate");
    }

    /// <summary>Two Exceptions Holding One Constant Report Duplicate.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void RealDuplicateThroughConstantsStillReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        WriteGeneratedCodes(temp.Path);
        WriteSource(temp.Path, "Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Expenses.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ExpenseReceiptMissingException : DomainException
            {
                public ExpenseReceiptMissingException()
                    : base("The expense is already reimbursed.")
                {
                }

                public override string Code => SpendingRefusalCodes.ReceiptMissing;
            }
            """);
        WriteSource(temp.Path, "Spending/Domain/Receipts/Exceptions/ReceiptMissingException.cs", """
            namespace Library.Modules.Spending.Domain.Receipts.Exceptions;

            /// <implements>rule/expense.receipt-required</implements>
            public sealed class ReceiptMissingException : DomainException
            {
                public ReceiptMissingException()
                    : base("The receipt is missing.")
                {
                }

                public override string Code => SpendingRefusalCodes.ReceiptMissing;
            }
            """);

        var model = OwnershipModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(model.Config);
        ModelLinks.ComputeImplements(model);

        var message = Assert.Single(Checker.Check(model).Findings, finding => finding.Code == "refusal-code-duplicate").Message;

        Assert.Contains("ExpenseReceiptMissingException", message, StringComparison.Ordinal);
        Assert.Contains("ReceiptMissingException", message, StringComparison.Ordinal);
    }

    static List<CodeLiteral> Literals(string root) =>
        new DotnetBackendReader().Read(ConsumerConfig.Load(root))
            .Literals.Where(literal => literal.Declares).ToList();

    static void WriteProject(string root)
    {
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "apps/api/src/Library.Modules"}}}
            """);
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Modules", "Spending"));
    }

    static void WriteGeneratedCodes(string root) =>
        WriteSource(root, "Spending/SpendingRefusalCodes.generated.cs", """
            namespace Library.Modules.Spending;

            public static class SpendingRefusalCodes
            {
                public const string ReceiptMissing = "spending.receipt-missing";
                public const string ExpenseDecided = "spending.expense-decided";
            }
            """);

    static void WriteSource(string root, string relative, string text)
    {
        var path = Path.Combine(root, "apps", "api", "src", "Library.Modules", relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    /// <summary>A model whose spending set declares both fixture codes, with the rule both fixture exceptions mark.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The project model holding the refusal set and the rule.</returns>
    static ProjectModel OwnershipModel(string root)
    {
        var model = TestModel.Model(TestModel.Config(root));
        model.RefusalSets.Add(new RefusalEntry(
            "spending",
            "spending.receipt-missing",
            "The expense is above the receipt threshold and carries no receipt.",
            Path.Combine(root, "refusals.yaml"),
            null));
        model.RefusalSets.Add(new RefusalEntry(
            "spending",
            "spending.expense-decided",
            "The expense is already decided.",
            Path.Combine(root, "refusals.yaml"),
            null));
        model.Records.Add(TestModel.Record(
            "rule",
            "rule/expense.receipt-required",
            Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "receipt-required", "receipt-required.yaml"),
            4,
            """{"title": "Receipt Required", "kind": "invariant", "statement": "An expense above the threshold carries a receipt.", "refusal": "refusal/spending.receipt-missing"}""",
            isEntry: true));

        return model;
    }
}
