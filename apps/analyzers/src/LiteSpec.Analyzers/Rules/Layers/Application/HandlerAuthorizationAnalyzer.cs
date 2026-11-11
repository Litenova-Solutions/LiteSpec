using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2012: a command or query handler never authorizes itself by throwing an access refusal or taking the authorization guard.</summary>
/// <follows>litespec/backend/application#enforce-target-authorization-in-the-pipeline</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerAuthorizationAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2012);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the handler authorization analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => AnalyzeThrow(c, scope), OperationKind.Throw);
        context.RegisterSymbolAction(c => AnalyzeParameter(c, scope), SymbolKind.Parameter);
    }

    /// <summary>Analyzes the throw.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeThrow(OperationAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Operation.Syntax?.SyntaxTree))
        {
            return;
        }

        var throwOperation = (IThrowOperation)context.Operation;
        var handler = InnermostType(context.ContainingSymbol);
        if (handler == null || !IsCommandOrQueryHandler(handler))
        {
            return;
        }

        var thrown = throwOperation.Exception;
        while (thrown is IConversionOperation { IsImplicit: true } conversion)
        {
            thrown = conversion.Operand;
        }

        if (thrown?.Type == null || !IsAccessException(thrown.Type))
        {
            return;
        }

        var location = thrown.Syntax is ObjectCreationExpressionSyntax creation
            ? creation.Type.GetLocation()
            : thrown.Syntax.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2012, location, handler.Name, thrown.Type.Name));
    }

    /// <summary>Analyzes the parameter.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeParameter(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.IsImplicitlyDeclared)
        {
            return;
        }

        if (!(parameter.ContainingSymbol is IMethodSymbol method) || method.MethodKind != MethodKind.Constructor)
        {
            return;
        }

        if (!IsCommandOrQueryHandler(method.ContainingType))
        {
            return;
        }

        var type = Unwrap(parameter.Type);
        if (!IsAuthorizationGuard(type))
        {
            return;
        }

        foreach (var declaration in parameter.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is ParameterSyntax syntax && syntax.Type != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2012, syntax.Type.GetLocation(), method.ContainingType.Name, type.Name));
                return;
            }
        }
    }

    /// <summary>Returns true when the type is command or query handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is command or query handler; otherwise false.</returns>
    private static bool IsCommandOrQueryHandler(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal)
            || type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name is "ICommandHandler" or "IQueryHandler")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is access exception; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is access exception; otherwise false.</returns>
    private static bool IsAccessException(ITypeSymbol type) =>
        type.Name.EndsWith("ForbiddenException", StringComparison.Ordinal)
        || type.Name.EndsWith("UnauthorizedException", StringComparison.Ordinal)
        || type.Name.EndsWith("AccessDeniedException", StringComparison.Ordinal);

    /// <summary>Returns true when the type is authorization guard; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is authorization guard; otherwise false.</returns>
    private static bool IsAuthorizationGuard(ITypeSymbol type)
    {
        if (IsAuthorizationGuardName(type.Name))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (IsAuthorizationGuardName(contract.Name))
            {
                return true;
            }
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (IsAuthorizationGuardName(current.Name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the name is authorization guard name; otherwise false.</summary>
    /// <param name="name">The name under inspection.</param>
    /// <returns>True when the name is authorization guard name; otherwise false.</returns>
    private static bool IsAuthorizationGuardName(string name) =>
        name.EndsWith("AuthorizationGuard", StringComparison.Ordinal);

    /// <summary>Returns the i type symbol.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The i type symbol.</returns>
    private static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }

    /// <summary>Returns the type.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The type, or null when none applies.</returns>
    private static INamedTypeSymbol? InnermostType(ISymbol symbol)
    {
        for (var current = symbol; current != null; current = current.ContainingSymbol)
        {
            if (current is INamedTypeSymbol type)
            {
                return type;
            }
        }

        return null;
    }
}
