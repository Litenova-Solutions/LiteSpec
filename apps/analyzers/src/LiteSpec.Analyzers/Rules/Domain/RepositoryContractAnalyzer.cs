using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1010: a Domain repository exposes only aggregate and Domain types.</summary>
/// <follows>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepositoryContractAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1010);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the repository contract analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var queryable = context.Compilation.GetTypeByMetadataName("System.Linq.IQueryable");
        var genericQueryable = context.Compilation.GetTypeByMetadataName("System.Linq.IQueryable`1");
        var expression = context.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
        context.RegisterSyntaxNodeAction(
            nodeContext => Analyze(nodeContext, queryable, genericQueryable, expression),
            SyntaxKind.InterfaceDeclaration);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="queryable">The queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="genericQueryable">The generic queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="expression">The expression supplied for the check, or null when the compilation does not provide it.</param>
    private static void Analyze(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? queryable,
        INamedTypeSymbol? genericQueryable,
        INamedTypeSymbol? expression)
    {
        var declaration = (InterfaceDeclarationSyntax)context.Node;
        var type = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
        if (type is null || !type.Name.EndsWith("Repository", StringComparison.Ordinal))
        {
            return;
        }

        // A partial interface is judged once as a whole, at its first declaration.
        var first = type.DeclaringSyntaxReferences[0];
        if (first.SyntaxTree == declaration.SyntaxTree && first.Span == declaration.Span && IsGenericRepository(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1010, type.Locations[0], type.Name, "generic repository"));
        }

        foreach (var member in type.GetMembers())
        {
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, type))
            {
                continue;
            }

            AnalyzeMember(member, declaration, type.Name, context, queryable, genericQueryable, expression);
        }
    }

    /// <summary>Returns true when the type is generic repository; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is generic repository; otherwise false.</returns>
    private static bool IsGenericRepository(INamedTypeSymbol type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        foreach (var parameter in type.TypeParameters)
        {
            foreach (var member in type.GetMembers())
            {
                if (MemberUses(member, parameter))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Returns true when the member-uses condition holds; otherwise false.</summary>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="parameter">The parameter under inspection.</param>
    /// <returns>True when the member-uses condition holds; otherwise false.</returns>
    private static bool MemberUses(ISymbol member, ITypeParameterSymbol parameter)
    {
        switch (member)
        {
            case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary && method.AssociatedSymbol is null:
                if (Uses(method.ReturnType, parameter))
                {
                    return true;
                }

                foreach (var value in method.Parameters)
                {
                    if (Uses(value.Type, parameter))
                    {
                        return true;
                    }
                }

                return false;
            case IPropertySymbol property:
                return Uses(property.Type, parameter);
            default:
                return false;
        }
    }

    /// <summary>Returns true when the uses condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="parameter">The parameter under inspection.</param>
    /// <returns>True when the uses condition holds; otherwise false.</returns>
    private static bool Uses(ITypeSymbol type, ITypeParameterSymbol parameter)
    {
        if (SymbolEqualityComparer.Default.Equals(type, parameter))
        {
            return true;
        }

        switch (type)
        {
            case INamedTypeSymbol named:
                foreach (var argument in named.TypeArguments)
                {
                    if (Uses(argument, parameter))
                    {
                        return true;
                    }
                }

                return false;
            case IArrayTypeSymbol array:
                return Uses(array.ElementType, parameter);
            default:
                return false;
        }
    }

    /// <summary>Analyzes the member.</summary>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="declaration">The declaration under inspection.</param>
    /// <param name="repository">The repository interface name the check compares against.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="queryable">The queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="genericQueryable">The generic queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="expression">The expression supplied for the check, or null when the compilation does not provide it.</param>
    private static void AnalyzeMember(
        ISymbol member,
        InterfaceDeclarationSyntax declaration,
        string repository,
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? queryable,
        INamedTypeSymbol? genericQueryable,
        INamedTypeSymbol? expression)
    {
        SyntaxNode? syntax = null;
        foreach (var reference in member.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree == declaration.SyntaxTree && declaration.Span.Contains(reference.Span))
            {
                syntax = reference.GetSyntax(context.CancellationToken);
                break;
            }
        }

        if (syntax is null)
        {
            return;
        }

        var model = context.SemanticModel;

        switch (member)
        {
            case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary && method.AssociatedSymbol is null:
                if (syntax is MethodDeclarationSyntax methodDeclaration)
                {
                    CheckType(methodDeclaration.ReturnType, isParameter: false, repository, model, queryable, genericQueryable, expression, context);
                    foreach (var parameter in methodDeclaration.ParameterList.Parameters)
                    {
                        if (parameter.Type is not null)
                        {
                            CheckType(parameter.Type, isParameter: true, repository, model, queryable, genericQueryable, expression, context);
                        }
                    }
                }

                break;
            case IPropertySymbol:
                if (syntax is PropertyDeclarationSyntax propertyDeclaration)
                {
                    CheckType(propertyDeclaration.Type, isParameter: false, repository, model, queryable, genericQueryable, expression, context);
                }
                else if (syntax is IndexerDeclarationSyntax indexerDeclaration)
                {
                    CheckType(indexerDeclaration.Type, isParameter: false, repository, model, queryable, genericQueryable, expression, context);
                    foreach (var parameter in indexerDeclaration.ParameterList.Parameters)
                    {
                        if (parameter.Type is not null)
                        {
                            CheckType(parameter.Type, isParameter: true, repository, model, queryable, genericQueryable, expression, context);
                        }
                    }
                }

                break;
            case IEventSymbol:
                if (syntax is EventDeclarationSyntax eventDeclaration)
                {
                    CheckType(eventDeclaration.Type, isParameter: false, repository, model, queryable, genericQueryable, expression, context);
                }
                else if (syntax is EventFieldDeclarationSyntax eventFieldDeclaration)
                {
                    CheckType(eventFieldDeclaration.Declaration.Type, isParameter: false, repository, model, queryable, genericQueryable, expression, context);
                }

                break;
        }
    }

    /// <summary>Checks the type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="isParameter">Whether the type appears in a parameter position.</param>
    /// <param name="repository">The repository interface name the check compares against.</param>
    /// <param name="model">The semantic model that resolves symbols for the tree.</param>
    /// <param name="queryable">The queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="genericQueryable">The generic queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="expression">The expression supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void CheckType(
        TypeSyntax type,
        bool isParameter,
        string repository,
        SemanticModel model,
        INamedTypeSymbol? queryable,
        INamedTypeSymbol? genericQueryable,
        INamedTypeSymbol? expression,
        SyntaxNodeAnalysisContext context)
    {
        if (type.IsMissing)
        {
            return;
        }

        if (Breaches(model.GetTypeInfo(type).Type, isParameter, queryable, genericQueryable, expression))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1010, type.GetLocation(), repository, type.ToString()));
            return;
        }

        foreach (var child in type.ChildNodes())
        {
            CheckNode(child, isParameter, repository, model, queryable, genericQueryable, expression, context);
        }
    }

    /// <summary>Checks the node.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <param name="isParameter">Whether the type appears in a parameter position.</param>
    /// <param name="repository">The repository interface name the check compares against.</param>
    /// <param name="model">The semantic model that resolves symbols for the tree.</param>
    /// <param name="queryable">The queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="genericQueryable">The generic queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="expression">The expression supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void CheckNode(
        SyntaxNode node,
        bool isParameter,
        string repository,
        SemanticModel model,
        INamedTypeSymbol? queryable,
        INamedTypeSymbol? genericQueryable,
        INamedTypeSymbol? expression,
        SyntaxNodeAnalysisContext context)
    {
        if (node is TypeSyntax type)
        {
            CheckType(type, isParameter, repository, model, queryable, genericQueryable, expression, context);
            return;
        }

        foreach (var child in node.ChildNodes())
        {
            CheckNode(child, isParameter, repository, model, queryable, genericQueryable, expression, context);
        }
    }

    /// <summary>Returns true when the breaches condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="isParameter">Whether the type appears in a parameter position.</param>
    /// <param name="queryable">The queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="genericQueryable">The generic queryable supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="expression">The expression supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the breaches condition holds; otherwise false.</returns>
    private static bool Breaches(
        ITypeSymbol? type,
        bool isParameter,
        INamedTypeSymbol? queryable,
        INamedTypeSymbol? genericQueryable,
        INamedTypeSymbol? expression)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (queryable is not null && SymbolEqualityComparer.Default.Equals(named, queryable))
        {
            return true;
        }

        if (genericQueryable is not null && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, genericQueryable))
        {
            return true;
        }

        foreach (var implemented in named.AllInterfaces)
        {
            if ((queryable is not null && SymbolEqualityComparer.Default.Equals(implemented, queryable))
                || (genericQueryable is not null && SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, genericQueryable)))
            {
                return true;
            }
        }

        if ((named.Name.EndsWith("Session", StringComparison.Ordinal)
            || named.Name.EndsWith("DbContext", StringComparison.Ordinal)
            || named.Name.EndsWith("DbSet", StringComparison.Ordinal))
            && !IsAggregate(named))
        {
            return true;
        }

        return isParameter
            && expression is not null
            && named.TypeArguments.Length == 1
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, expression)
            && named.TypeArguments[0] is INamedTypeSymbol target
            && target.TypeKind == TypeKind.Delegate
            && target.Name == "Func"
            && target.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true };
    }

    /// <summary>Returns true when the type is aggregate; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is aggregate; otherwise false.</returns>
    private static bool IsAggregate(INamedTypeSymbol type)
    {
        if (type.DeclaringSyntaxReferences.Length == 0 || type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (definition.TypeKind == TypeKind.Class && definition.Arity == 1 && definition.Name == "AggregateRoot")
            {
                return true;
            }
        }

        return false;
    }
}
