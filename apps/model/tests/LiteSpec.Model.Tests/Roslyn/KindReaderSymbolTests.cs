using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests that the symbol overloads agree with the syntax overloads on the same source.</summary>
public sealed class KindReaderSymbolTests
{
    private const string Code = """
        using System;

        public abstract class AggregateRoot<TState>
        {
            protected void RaiseDomainEvent(object fact)
            {
            }
        }

        public sealed class ExpenseState;

        public class DomainException : Exception;

        public interface IDomainEvent;

        public interface ICommandHandler<TCommand>;

        public interface IEventHandler<TFact>;

        public abstract class BackgroundService;

        public interface IEndpoint;

        public sealed class FactAttribute : Attribute;

        public sealed class ExpenseSubmittedEvent : IDomainEvent;

        public sealed class ApproverIsSubmitterException : DomainException;

        public sealed class Expense : AggregateRoot<ExpenseState>
        {
            public void Submit()
            {
                RaiseDomainEvent(new ExpenseSubmittedEvent());
            }

            public int Total() => 0;
        }

        public sealed class SubmitExpenseCommand;

        public sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand>;

        public sealed class NotifyOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedEvent>;

        public sealed class ClearStuckReimbursementsJob : BackgroundService;

        public sealed class SubmitExpenseEndpoint : IEndpoint;

        public sealed class SubmitExpenseTests
        {
            [Fact]
            public void Submits()
            {
            }

            public void Helper()
            {
            }
        }

        public interface IPayoutPort;

        public sealed class PayoutBankAdapter : IPayoutPort;
        """;

    /// <summary>Syntax and symbol overloads agree on every type.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Syntax and symbol overloads agree on every type")]
    public void SyntaxAndSymbolsAgreeOnTypes()
    {
        var compilation = TestTrees.Compile(Code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var context = new KindContext([], [], ["IPayoutPort"]);

        foreach (var node in tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(symbol);

            var fromSyntax = KindReader.KindOf(node, context);
            var fromSymbol = KindReader.KindOf(symbol, context);

            Assert.True(fromSyntax == fromSymbol, $"Kind differs for {node.Identifier.ValueText}.");
        }
    }

    /// <summary>Syntax and symbol overloads agree on every method.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Syntax and symbol overloads agree on every method")]
    public void SyntaxAndSymbolsAgreeOnMethods()
    {
        var compilation = TestTrees.Compile(Code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var context = KindContext.Empty;

        foreach (var node in tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(symbol);

            var fromSyntax = KindReader.KindOf(node, context);
            var fromSymbol = KindReader.KindOf((IMethodSymbol)symbol, context);

            Assert.True(fromSyntax == fromSymbol, $"Kind differs for {node.Identifier.ValueText}.");
        }
    }

    /// <summary>Both overloads recognize the expected kinds.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Both overloads recognize the expected kinds")]
    public void BothOverloadsRecognizeExpectedKinds()
    {
        var compilation = TestTrees.Compile(Code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var context = new KindContext([], [], ["IPayoutPort"]);
        var expected = new (string Name, CodeKind? Kind)[]
        {
            ("Expense", CodeKind.AggregateRoot),
            ("ExpenseSubmittedEvent", CodeKind.DomainEvent),
            ("ApproverIsSubmitterException", CodeKind.RuleException),
            ("SubmitExpenseCommandHandler", CodeKind.Handler),
            ("NotifyOnExpenseSubmittedHandler", CodeKind.EventHandler),
            ("ClearStuckReimbursementsJob", CodeKind.Job),
            ("SubmitExpenseEndpoint", CodeKind.Endpoint),
            ("PayoutBankAdapter", CodeKind.Adapter),
            ("SubmitExpenseCommand", null),
            ("IPayoutPort", null),
        };

        foreach (var (name, kind) in expected)
        {
            var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                .First(type => type.Identifier.ValueText == name);
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(symbol);

            Assert.True(KindReader.KindOf(node, context) == kind, $"Syntax kind differs for {name}.");
            Assert.True(KindReader.KindOf(symbol, context) == kind, $"Symbol kind differs for {name}.");
        }

        var submit = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(method => method.Identifier.ValueText == "Submit");
        var submitSymbol = model.GetDeclaredSymbol(submit, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(submitSymbol);
        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(submit, context));
        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf((IMethodSymbol)submitSymbol, context));

        var proves = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(method => method.Identifier.ValueText == "Submits");
        var provesSymbol = model.GetDeclaredSymbol(proves, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(provesSymbol);
        Assert.Equal(CodeKind.Test, KindReader.KindOf(proves, context));
        Assert.Equal(CodeKind.Test, KindReader.KindOf((IMethodSymbol)provesSymbol, context));
    }

    /// <summary>A marked method raising nothing is recognized from its symbol.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked method raising nothing is recognized from its symbol")]
    public void MarkedMethodRaisingNothingIsRecognizedFromSymbol()
    {
        const string code = """
            public abstract class AggregateRoot<TState>
            {
                protected void RaiseDomainEvent(object fact)
                {
                }
            }

            public sealed class ExpenseState;

            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                /// <implements>use-case/spending.erase-personal-data</implements>
                public void Erase()
                {
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(method => method.Identifier.ValueText == "Erase");
        var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(node, KindContext.Empty));
        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf((IMethodSymbol)symbol, KindContext.Empty));
    }

    /// <summary>An unmarked symbol method assigning state is recognized.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An unmarked symbol method assigning state is recognized")]
    public void SymbolMethodAssigningStateIsRecognized()
    {
        const string code = """
            public abstract class AggregateRoot<TState>;

            public sealed class ExpenseState;

            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private bool erased;

                public void Erase()
                {
                    erased = true;
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(method => method.Identifier.ValueText == "Erase");
        var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(node, KindContext.Empty));
        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf((IMethodSymbol)symbol, KindContext.Empty));
    }

    /// <summary>A symbol mark outside the generated namespace is not a mark.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A symbol mark outside the generated namespace is not a mark")]
    public void SymbolMarkOutsideGeneratedNamespaceIsIgnored()
    {
        const string code = """
            using System;

            namespace Spending
            {
                public sealed class ImplementsAttribute : Attribute
                {
                    public ImplementsAttribute(string recordId)
                    {
                    }
                }
            }

            namespace LiteSpec
            {
                public sealed class ImplementsAttribute : Attribute
                {
                    public ImplementsAttribute(string recordId)
                    {
                    }
                }
            }

            namespace Shop
            {
                [Spending.Implements("external-system/payout-bank")]
                public interface IDomainPort;

                [LiteSpec.Implements("external-system/payout-bank")]
                public interface IGeneratedPort;
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);

        foreach (var name in new[] { "IDomainPort", "IGeneratedPort" })
        {
            var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                .First(type => type.Identifier.ValueText == name);
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(symbol);

            if (name == "IDomainPort")
            {
                Assert.Null(KindReader.KindOf(node, KindContext.Empty, "Shop.Application"));
                Assert.Null(KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
            }
            else
            {
                Assert.Equal(CodeKind.Port, KindReader.KindOf(node, KindContext.Empty, "Shop.Application"));
                Assert.Equal(CodeKind.Port, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
            }
        }
    }

    /// <summary>Symbols resolve a transitive aggregate root that syntax needs context for.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Symbols resolve a transitive aggregate root that syntax needs context for")]
    public void SymbolsResolveTransitiveAggregateRoot()
    {
        const string code = """
            public abstract class AggregateRoot<TState>;

            public sealed class ExpenseState;

            public abstract class SpendingRoot<TState> : AggregateRoot<TState>;

            public sealed class Expense : SpendingRoot<ExpenseState>;
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "Expense");
        var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.AggregateRoot, KindReader.KindOf(symbol, KindContext.Empty));
        Assert.Null(KindReader.KindOf(node, KindContext.Empty));

        var context = new KindContext(["SpendingRoot"], [], []);
        Assert.Equal(CodeKind.AggregateRoot, KindReader.KindOf(node, context));
    }

    /// <summary>A nested symbol sharing a root's or an exception's name takes no kind.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A nested symbol sharing a root's or an exception's name takes no kind")]
    public void NestedSymbolSharingANameTakesNoKind()
    {
        const string code = """
            using System;

            public abstract class AggregateRoot<TState>;

            public class DomainException : Exception;

            public sealed class OrderState;

            public sealed class Order : AggregateRoot<OrderState>;

            public sealed class OrderRefusedException : DomainException;

            public abstract record FieldSubject
            {
                public sealed record Order : FieldSubject;

                public sealed record OrderRefusedException : FieldSubject;
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var nested = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<RecordDeclarationSyntax>()
            .Where(type => type.Parent is TypeDeclarationSyntax)
            .ToList();
        var context = new KindContext(["Order"], ["OrderRefusedException"], []);

        Assert.Equal(2, nested.Count);
        foreach (var node in nested)
        {
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(symbol);
            Assert.Null(KindReader.KindOf(symbol, context));
        }

        var root = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "Order");
        Assert.Equal(CodeKind.AggregateRoot, KindReader.KindOf(model.GetDeclaredSymbol(root, cancellationToken: TestContext.Current.CancellationToken)!, context));
    }

    /// <summary>A symbol port reads its mark from source.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A symbol port reads its mark from source")]
    public void SymbolPortReadsItsMark()
    {
        const string code = """
            /// <implements>external-system/payout-bank</implements>
            public interface IPayoutPort;
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = TestTrees.First<InterfaceDeclarationSyntax>(tree);
        var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.Port, KindReader.KindOf(node, KindContext.Empty, "Shop.Application"));
        Assert.Equal(CodeKind.Port, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
        Assert.Null(KindReader.KindOf(symbol, KindContext.Empty, "Shop.WebApi"));
    }

    /// <summary>A symbol class with a method in an outer layer is technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A symbol class with a method in an outer layer is technical")]
    public void SymbolClassWithMethodIsTechnical()
    {
        const string code = """
            public sealed class CommitStep
            {
                public void Commit()
                {
                }
            }
            """;
        var symbol = SymbolOf(code, "CommitStep");

        Assert.Equal(CodeKind.Technical, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
        Assert.Equal(CodeKind.Technical, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Infrastructure"));
        Assert.Equal(CodeKind.Technical, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Worker"));
        Assert.Null(KindReader.KindOf(symbol, KindContext.Empty, "Shop.Domain"));
        Assert.Null(KindReader.KindOf(symbol, KindContext.Empty));
    }

    /// <summary>A symbol record, nested class, or constructor-only class is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A symbol record, nested class, or constructor-only class is not technical")]
    public void SymbolNonClassShapesAreNotTechnical()
    {
        const string code = """
            public sealed record ExpenseSummary
            {
                public int Open() => 0;
            }

            public sealed class CommitPipeline
            {
                public sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
            }

            public sealed class ExpenseSettings
            {
                public ExpenseSettings(int threshold)
                {
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);

        foreach (var name in new[] { "ExpenseSummary", "CommitStep", "ExpenseSettings" })
        {
            var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                .First(type => type.Identifier.ValueText == name);
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(symbol);

            Assert.Null(KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
        }
    }

    /// <summary>A symbol migration or generated class is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A symbol migration or generated class is not technical")]
    public void SymbolMigrationAndGeneratedAreNotTechnical()
    {
        const string code = """
            using System;

            public class Migration;

            public class ShopMigration : Migration;

            public sealed class GeneratedCodeAttribute : Attribute
            {
                public GeneratedCodeAttribute(string tool, string version)
                {
                }
            }

            public sealed class AddExpenses : ShopMigration
            {
                public void Up()
                {
                }
            }

            [GeneratedCode("tool", "1.0")]
            public sealed class CommitStep
            {
                public void Commit()
                {
                }
            }
            """;

        Assert.Null(KindReader.KindOf(SymbolOf(code, "AddExpenses"), KindContext.Empty, "Shop.Infrastructure"));
        Assert.Null(KindReader.KindOf(SymbolOf(code, "CommitStep"), KindContext.Empty, "Shop.Application"));
    }

    /// <summary>A symbol partial class merges methods across parts.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A symbol partial class merges methods across parts")]
    public void SymbolPartialMergesMethodsAcrossParts()
    {
        var compilation = TestTrees.Compile(
            """
            public sealed partial class CommitStep
            {
            }
            """,
            """
            public sealed partial class CommitStep
            {
                public void Commit()
                {
                }
            }
            """);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "CommitStep");
        var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.Technical, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
    }

    private static INamedTypeSymbol SymbolOf(string code, string name)
    {
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == name);
        var symbol = model.GetDeclaredSymbol(node);
        Assert.NotNull(symbol);

        return symbol;
    }
}
