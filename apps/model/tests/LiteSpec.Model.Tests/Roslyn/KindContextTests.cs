using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests the cross-file name sets, including the transitive handler, job and event contracts.</summary>
public sealed class KindContextTests
{
    [Fact(DisplayName = "The empty context holds no names")]
    public void EmptyHoldsNoNames()
    {
        Assert.Empty(KindContext.Empty.AggregateRoots);
        Assert.Empty(KindContext.Empty.DomainExceptions);
        Assert.Empty(KindContext.Empty.MarkedPorts);
        Assert.Empty(KindContext.Empty.CommandHandlers);
        Assert.Empty(KindContext.Empty.QueryHandlers);
        Assert.Empty(KindContext.Empty.EventHandlers);
        Assert.Empty(KindContext.Empty.JobHosts);
        Assert.Empty(KindContext.Empty.DomainEvents);
        Assert.Empty(KindContext.Empty.TechnicalClasses);
    }

    [Fact(DisplayName = "The three-argument constructor leaves the contract sets empty")]
    public void ThreeArgumentsLeaveContractSetsEmpty()
    {
        var context = new KindContext(new[] { "Expense" }, new string[0], new string[0]);

        Assert.Contains("Expense", context.AggregateRoots);
        Assert.Empty(context.CommandHandlers);
        Assert.Empty(context.QueryHandlers);
        Assert.Empty(context.EventHandlers);
        Assert.Empty(context.JobHosts);
        Assert.Empty(context.DomainEvents);
        Assert.Empty(context.TechnicalClasses);
    }

    [Fact(DisplayName = "The constructor stores every set")]
    public void ConstructorStoresEverySet()
    {
        var context = new KindContext(
            new[] { "Expense" },
            new[] { "ReceiptMissingException" },
            new[] { "IPayoutPort" },
            new[] { "BorrowCopyHandlerBase" },
            new[] { "ListLoansHandlerBase" },
            new[] { "LoanBorrowedReactionBase" },
            new[] { "ScanServiceBase" },
            new[] { "ILoanEvent" },
            new[] { "CommitStep" });

        Assert.Contains("Expense", context.AggregateRoots);
        Assert.Contains("ReceiptMissingException", context.DomainExceptions);
        Assert.Contains("IPayoutPort", context.MarkedPorts);
        Assert.Contains("BorrowCopyHandlerBase", context.CommandHandlers);
        Assert.Contains("ListLoansHandlerBase", context.QueryHandlers);
        Assert.Contains("LoanBorrowedReactionBase", context.EventHandlers);
        Assert.Contains("ScanServiceBase", context.JobHosts);
        Assert.Contains("ILoanEvent", context.DomainEvents);
        Assert.Contains("CommitStep", context.TechnicalClasses);
    }

    [Fact(DisplayName = "A null required collection throws")]
    public void NullRequiredCollectionThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new KindContext(null!, new string[0], new string[0]));
        Assert.Throws<ArgumentNullException>(() => new KindContext(new string[0], null!, new string[0]));
        Assert.Throws<ArgumentNullException>(() => new KindContext(new string[0], new string[0], null!));
    }

    [Fact(DisplayName = "A null contract collection means none are known")]
    public void NullContractCollectionMeansNone()
    {
        var context = new KindContext(
            new string[0],
            new string[0],
            new string[0],
            null,
            null,
            null,
            null,
            null,
            null);

        Assert.Empty(context.CommandHandlers);
        Assert.Empty(context.QueryHandlers);
        Assert.Empty(context.EventHandlers);
        Assert.Empty(context.JobHosts);
        Assert.Empty(context.DomainEvents);
        Assert.Empty(context.TechnicalClasses);
    }

    [Fact(DisplayName = "Names compare with ordinal case sensitivity")]
    public void NamesCompareOrdinal()
    {
        var context = new KindContext(
            new string[0],
            new string[0],
            new string[0],
            commandHandlers: new[] { "BorrowCopyHandlerBase" });

        Assert.Contains("BorrowCopyHandlerBase", context.CommandHandlers);
        Assert.DoesNotContain("borrowcopyhandlerbase", context.CommandHandlers);
    }

    [Fact(DisplayName = "The symbol overload recognizes a handler through its base class")]
    public void SymbolsRecognizeHandlerThroughBaseClass()
    {
        const string code = """
            public interface ICommandHandler<TCommand, TResult>;

            public sealed class BorrowCopyCommand;

            public sealed class BorrowCopyCommandResult;

            public abstract class BorrowCopyHandlerBase : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>;

            public sealed class BorrowCopyCommandHandler : BorrowCopyHandlerBase;
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "BorrowCopyCommandHandler");
        var symbol = model.GetDeclaredSymbol(node);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.Handler, KindReader.KindOf(symbol, KindContext.Empty));
    }

    [Fact(DisplayName = "The symbol overload recognizes an event through an intermediate interface")]
    public void SymbolsRecognizeEventThroughIntermediateInterface()
    {
        const string code = """
            public interface IDomainEvent;

            public interface ILoanEvent : IDomainEvent;

            public sealed class LoanBorrowedEvent : ILoanEvent;
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "LoanBorrowedEvent");
        var symbol = model.GetDeclaredSymbol(node);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.DomainEvent, KindReader.KindOf(symbol, KindContext.Empty));

        var contract = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "ILoanEvent");
        var contractSymbol = model.GetDeclaredSymbol(contract);
        Assert.NotNull(contractSymbol);

        Assert.Null(KindReader.KindOf(contractSymbol, KindContext.Empty));
    }
}
