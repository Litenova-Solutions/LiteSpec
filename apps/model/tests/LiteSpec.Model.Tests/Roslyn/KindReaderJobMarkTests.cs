using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests that a plain class carrying a job mark is a job host, from syntax and from symbols.</summary>
public sealed class KindReaderJobMarkTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked plain class in Application, Infrastructure, or Worker is a job")]
    public void MarkedPlainClassIsAJob()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        Assert.Equal(CodeKind.Job, KindReader.KindOf(type, KindContext.Empty, "Shop.Application"));
        Assert.Equal(CodeKind.Job, KindReader.KindOf(type, KindContext.Empty, "Shop.Infrastructure"));
        Assert.Equal(CodeKind.Job, KindReader.KindOf(type, KindContext.Empty, "Shop.Worker"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked class without a job name is a job")]
    public void MarkedClassWithoutJobNameIsAJob()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDues
            {
                public void Run()
                {
                }
            }
            """);

        Assert.Equal(CodeKind.Job, KindReader.KindOf(type, KindContext.Empty, "Shop.Infrastructure"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "The same class unmarked is technical")]
    public void UnmarkedPlainClassIsTechnical()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        Assert.Equal(CodeKind.Technical, KindReader.KindOf(type, KindContext.Empty, "Shop.Infrastructure"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked class in Domain is not a job")]
    public void MarkedClassInDomainIsNotAJob()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Shop.Domain"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked plain class in Application, Infrastructure, or Worker is a job from its symbol")]
    public void MarkedPlainClassIsAJobFromSymbol()
    {
        const string code = """
            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """;
        var symbol = SymbolOf(code, "ChargeDuesJob");

        Assert.Equal(CodeKind.Job, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Application"));
        Assert.Equal(CodeKind.Job, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Infrastructure"));
        Assert.Equal(CodeKind.Job, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Worker"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "The same class unmarked is technical from its symbol")]
    public void UnmarkedPlainClassIsTechnicalFromSymbol()
    {
        const string code = """
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """;
        var symbol = SymbolOf(code, "ChargeDuesJob");

        Assert.Equal(CodeKind.Technical, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Infrastructure"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked class in Domain is not a job from its symbol")]
    public void MarkedClassInDomainIsNotAJobFromSymbol()
    {
        const string code = """
            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """;
        var symbol = SymbolOf(code, "ChargeDuesJob");

        Assert.Null(KindReader.KindOf(symbol, KindContext.Empty, "Shop.Domain"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Syntax and symbol overloads agree on a marked job host")]
    public void SyntaxAndSymbolAgreeOnMarkedJob()
    {
        const string code = """
            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """;
        var compilation = TestTrees.Compile(code);
        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "ChargeDuesJob");
        var symbol = model.GetDeclaredSymbol(node);
        Assert.NotNull(symbol);

        Assert.Equal(CodeKind.Job, KindReader.KindOf(node, KindContext.Empty, "Shop.Infrastructure"));
        Assert.Equal(CodeKind.Job, KindReader.KindOf(symbol, KindContext.Empty, "Shop.Infrastructure"));
        Assert.Null(KindReader.KindOf(node, KindContext.Empty, "Shop.Domain"));
        Assert.Null(KindReader.KindOf(symbol, KindContext.Empty, "Shop.Domain"));
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
