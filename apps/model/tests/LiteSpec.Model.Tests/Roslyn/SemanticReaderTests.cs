using System.Collections.Generic;
using System.Linq;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests that the semantic overloads read what the compiler knows where syntax guesses.</summary>
public sealed class SemanticReaderTests
{
    /// <summary>A semantic model resolves an aliased property type where syntax reads the alias.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact(DisplayName = "A semantic model resolves an aliased property type")]
    public void SemanticModelResolvesAliasedPropertyType()
    {
        const string code = """
            using Title = System.String;

            public sealed class BorrowCopyCommand
            {
                public Title CopyId { get; set; } = "";
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var declared = TestTrees.First<TypeDeclarationSyntax>(tree);

        Assert.Equal("Title", Assert.Single(MessageProperties.Of(declared)).Type);
        Assert.Equal("string", Assert.Single(MessageProperties.Of(declared, model)).Type);
    }

    /// <summary>A property whose type names nothing keeps its written type under a semantic model.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact(DisplayName = "An unresolvable property type keeps its written type")]
    public void UnresolvablePropertyTypeKeepsWrittenType()
    {
        const string code = """
            public sealed class BorrowCopyCommand
            {
                public Missing CopyId { get; set; } = null!;
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var declared = TestTrees.First<TypeDeclarationSyntax>(tree);

        Assert.Equal("Missing", Assert.Single(MessageProperties.Of(declared, model)).Type);
    }

    /// <summary>A semantic model resolves a call on a constructed value where syntax reads nothing.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A semantic model resolves a call on a constructed value")]
    public void SemanticModelResolvesCallOnConstructedValue()
    {
        const string code = """
            public sealed class Loan
            {
                public void Borrow()
                {
                }
            }

            public sealed class BorrowCopyCommandHandler
            {
                public Loan MakeLoan() => new Loan();

                public void Handle()
                {
                    MakeLoan().Borrow();
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var declared = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "BorrowCopyCommandHandler");
        var roots = new HashSet<string>();

        Assert.Empty(MethodCalls.OfClass(declared, roots));

        var semantic = MethodCalls.OfClass(declared, roots, model);
        var call = Assert.Single(semantic);

        Assert.Equal("Loan", call.Type);
        Assert.Equal("Borrow", call.Method);
    }

    /// <summary>Syntax and semantic reads agree on a static call.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "Syntax and semantic reads agree on a static call")]
    public void SyntaxAndSemanticAgreeOnStaticCall()
    {
        const string code = """
            public static class Verdict
            {
                public static string Deny(string message) => message;
            }

            public sealed class Gate
            {
                public string Check() => Verdict.Deny("No.");
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var declared = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "Gate");
        var roots = new HashSet<string>();

        var syntax = Assert.Single(MethodCalls.OfClass(declared, roots));
        var semantic = Assert.Single(MethodCalls.OfClass(declared, roots, model));

        Assert.Equal((syntax.Type, syntax.Method), (semantic.Type, semantic.Method));
        Assert.Equal(("Verdict", "Deny"), (semantic.Type, semantic.Method));
    }

    /// <summary>A call onto the class itself reads nothing under a semantic model.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A call onto the class itself reads nothing")]
    public void CallOntoItselfReadsNothing()
    {
        const string code = """
            public sealed class Gate
            {
                public string Check() => this.Decide();

                public string Decide() => "No.";
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var declared = TestTrees.First<TypeDeclarationSyntax>(tree);

        Assert.Empty(MethodCalls.OfClass(declared, new HashSet<string>(), model));
    }

    /// <summary>A semantic model resolves a target-typed creation where syntax reads nothing.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A semantic model resolves a target-typed creation")]
    public void SemanticModelResolvesTargetTypedCreation()
    {
        const string code = """
            public class LoanNotAvailableException
            {
                public LoanNotAvailableException(string code, string message)
                {
                }
            }

            public sealed class Loans
            {
                public void Borrow()
                {
                    LoanNotAvailableException error = new("lending.loan-not-available", "No loan.");
                    throw error;
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(candidate => candidate.Identifier.ValueText == "Borrow");

        Assert.Empty(ThrownRefusals.OfMethod(method, tree));

        var thrown = Assert.Single(ThrownRefusals.OfMethod(method, tree, model));

        Assert.Equal("LoanNotAvailableException", thrown.Name);
        Assert.Equal(12, thrown.Line);
    }

    /// <summary>A constant read through using static resolves under a semantic model with an empty map.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A using static constant resolves with an empty map")]
    public void UsingStaticConstantResolvesWithEmptyMap()
    {
        const string code = """
            using static Library.LendingRefusalCodes;

            namespace Library
            {
                public static class LendingRefusalCodes
                {
                    public const string PriceRefused = "lending.price-refused";
                }

                public sealed class PricingRefusedException
                {
                    public PricingRefusedException(string code, string message)
                    {
                    }
                }

                public sealed class Pricing
                {
                    public static void Quote()
                    {
                        throw new PricingRefusedException(PriceRefused, "No quote.");
                    }
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(candidate => candidate.Identifier.ValueText == "Quote");
        var constants = new Dictionary<string, string>();

        Assert.Empty(ThrownRefusals.CodesOf(method, tree, constants));

        var resolved = Assert.Single(ThrownRefusals.CodesOf(method, tree, constants, model));

        Assert.Equal("lending.price-refused", resolved.Code);
        Assert.Equal(21, resolved.Line);
    }

    /// <summary>A handler shape resolves through an inherited handler base.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A handler shape resolves through an inherited handler base")]
    public void HandlerShapeResolvesThroughInheritedBase()
    {
        const string code = """
            public interface ICommandHandler<TCommand, TResult>;

            public interface IQueryHandler<TQuery, TResult>;

            public sealed class BorrowCopyCommand;

            public sealed class BorrowCopyCommandResult;

            public abstract class CommandHandlerBase : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>;

            public sealed class BorrowCopyCommandHandler : CommandHandlerBase;
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);

        INamedTypeSymbol SymbolOf(string name)
        {
            var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                .First(type => type.Identifier.ValueText == name);
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken) as INamedTypeSymbol;

            Assert.NotNull(symbol);

            return symbol;
        }

        var shape = HandlerInterfaces.Of(SymbolOf("BorrowCopyCommandHandler"));

        Assert.NotNull(shape);
        Assert.Equal("BorrowCopyCommand", shape.Message);
        Assert.Equal("BorrowCopyCommandResult", shape.Result);
        Assert.True(shape.IsCommand);
        Assert.False(shape.IsQuery);
        Assert.True(HandlerInterfaces.Implements(SymbolOf("BorrowCopyCommandHandler"), "ICommandHandler"));
        Assert.False(HandlerInterfaces.Implements(SymbolOf("BorrowCopyCommandHandler"), "IQueryHandler"));
        Assert.Null(HandlerInterfaces.Of(SymbolOf("BorrowCopyCommand")));
    }

    /// <summary>An event subscription resolves through the event-handler interface.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An event subscription resolves through the event-handler interface")]
    public void EventSubscriptionResolvesThroughInterface()
    {
        const string code = """
            public interface IEventHandler<TFact>;

            public sealed class CopyBorrowedEvent;

            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>;

            public sealed class Plain;
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);

        INamedTypeSymbol SymbolOf(string name)
        {
            var node = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>()
                .First(type => type.Identifier.ValueText == name);
            var symbol = model.GetDeclaredSymbol(node, cancellationToken: TestContext.Current.CancellationToken) as INamedTypeSymbol;

            Assert.NotNull(symbol);

            return symbol;
        }

        Assert.Equal("CopyBorrowedEvent", HandlerInterfaces.EventOf(SymbolOf("NotifyReaderOnCopyBorrowedHandler")));
        Assert.Null(HandlerInterfaces.EventOf(SymbolOf("Plain")));
    }
}
