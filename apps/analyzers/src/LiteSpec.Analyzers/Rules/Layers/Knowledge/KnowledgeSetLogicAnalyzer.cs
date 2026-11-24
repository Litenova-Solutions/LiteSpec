using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Knowledge;

/// <summary>LS2039: a knowledge set holds only rows built through its model's constructors.</summary>
/// <follows>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KnowledgeSetLogicAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2039);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.None;

    /// <summary>
    ///     Registers the analysis actions for the knowledge set logic analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        if (!global::LiteSpec.Analyzers.LayerMap.Knowledge.IsSetCompilation(context.Compilation))
        {
            return;
        }

        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => AnalyzeMethod(c, scope), SymbolKind.Method);
        context.RegisterSymbolAction(c => AnalyzeProperty(c, scope), SymbolKind.Property);
        context.RegisterSymbolAction(c => AnalyzeField(c, scope), SymbolKind.Field);
    }

    /// <summary>Analyzes the method.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeMethod(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var method = (IMethodSymbol)context.Symbol;
        if (method.IsImplicitlyDeclared
            || method.MethodKind is MethodKind.LocalFunction
                or MethodKind.PropertyGet
                or MethodKind.PropertySet
                or MethodKind.EventAdd
                or MethodKind.EventRemove
                or MethodKind.EventRaise)
        {
            return;
        }

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(context.CancellationToken) is not BaseMethodDeclarationSyntax syntax)
            {
                continue;
            }

            if (!global::LiteSpec.Analyzers.LayerMap.Knowledge.IsSetFile(reference.SyntaxTree.FilePath))
            {
                continue;
            }

            if ((syntax.Body is not null && DeclaresLogic(syntax.Body))
                || (syntax.ExpressionBody is not null && DeclaresLogic(syntax.ExpressionBody)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2039, MemberLocation(syntax), method.Name));
            }
        }
    }

    /// <summary>Analyzes the property.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeProperty(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var property = (IPropertySymbol)context.Symbol;
        if (property.IsImplicitlyDeclared)
        {
            return;
        }

        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            if (!global::LiteSpec.Analyzers.LayerMap.Knowledge.IsSetFile(reference.SyntaxTree.FilePath))
            {
                continue;
            }

            var node = reference.GetSyntax(context.CancellationToken);
            Location? location = null;
            var logic = false;
            if (node is PropertyDeclarationSyntax declaration)
            {
                location = declaration.Identifier.GetLocation();
                logic = (declaration.ExpressionBody is not null && DeclaresLogic(declaration.ExpressionBody))
                    || (declaration.Initializer is not null && DeclaresLogic(declaration.Initializer))
                    || AccessorsDeclareLogic(declaration.AccessorList);
            }
            else if (node is IndexerDeclarationSyntax indexer)
            {
                location = indexer.ThisKeyword.GetLocation();
                logic = AccessorsDeclareLogic(indexer.AccessorList);
            }

            if (logic && location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2039, location, property.Name));
            }
        }
    }

    /// <summary>Analyzes the field.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeField(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var field = (IFieldSymbol)context.Symbol;
        if (field.IsImplicitlyDeclared)
        {
            return;
        }

        foreach (var reference in field.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(context.CancellationToken) is not VariableDeclaratorSyntax declarator)
            {
                continue;
            }

            if (!global::LiteSpec.Analyzers.LayerMap.Knowledge.IsSetFile(reference.SyntaxTree.FilePath))
            {
                continue;
            }

            if (declarator.Initializer is not null && DeclaresLogic(declarator.Initializer))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2039, declarator.Identifier.GetLocation(), field.Name));
            }
        }
    }

    /// <summary>Returns true when the member body declares logic: a branch, loop, switch, throw, or a call other than a model value factory; otherwise false.</summary>
    /// <param name="body">The member body, arrow clause, or initializer under inspection.</param>
    /// <returns>True when the body holds logic beyond returning or constructing rows.</returns>
    private static bool DeclaresLogic(SyntaxNode body)
    {
        foreach (var child in body.DescendantNodes())
        {
            switch (child)
            {
                case IfStatementSyntax or SwitchStatementSyntax or SwitchExpressionSyntax
                    or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax
                    or WhileStatementSyntax or DoStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax:
                    return true;
                case InvocationExpressionSyntax invocation:
                    if (!IsValueFactory(invocation))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    /// <summary>
    ///     Returns true when the call builds one value from literals: a static Parse, From, FromCode, Of or Create called on a
    ///     named type, never a keyword type such as decimal, with only literal arguments. A set builds its typed keys this way,
    ///     because LS2040 keeps string-built keys inside sets.
    /// </summary>
    /// <param name="invocation">The call under inspection.</param>
    /// <returns>True when the call is a value factory over literals; otherwise false.</returns>
    private static bool IsValueFactory(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax or QualifiedNameSyntax or GenericNameSyntax } access
        && access.Name.Identifier.ValueText is "Parse" or "From" or "FromCode" or "Of" or "Create"
        && invocation.ArgumentList.Arguments.All(static argument => argument.Expression is LiteralExpressionSyntax);

    /// <summary>Returns true when an accessor body declares logic; otherwise false.</summary>
    /// <param name="accessors">The accessor list, or null when the property has none.</param>
    /// <returns>True when an accessor body or arrow clause declares logic.</returns>
    private static bool AccessorsDeclareLogic(AccessorListSyntax? accessors)
    {
        if (accessors is null)
        {
            return false;
        }

        foreach (var accessor in accessors.Accessors)
        {
            if ((accessor.Body is not null && DeclaresLogic(accessor.Body))
                || (accessor.ExpressionBody is not null && DeclaresLogic(accessor.ExpressionBody)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the location.</summary>
    /// <param name="syntax">The method, constructor, operator, or destructor declaration under inspection.</param>
    /// <returns>The member name location, or the declaration location when it has no name.</returns>
    private static Location MemberLocation(BaseMethodDeclarationSyntax syntax) =>
        syntax switch
        {
            MethodDeclarationSyntax method => method.Identifier.GetLocation(),
            ConstructorDeclarationSyntax constructor => constructor.Identifier.GetLocation(),
            _ => syntax.GetLocation(),
        };
}
