using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2012: a command or query handler never authorizes itself by throwing an access refusal.</summary>
/// <follows>litespec/backend/application#enforce-target-authorization-in-the-pipeline</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerAuthorizationAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2012);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeThrow, OperationKind.Throw);

    private static void AnalyzeThrow(OperationAnalysisContext context)
    {
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

    private static bool IsAccessException(ITypeSymbol type) =>
        type.Name.EndsWith("ForbiddenException", StringComparison.Ordinal)
        || type.Name.EndsWith("UnauthorizedException", StringComparison.Ordinal)
        || type.Name.EndsWith("AccessDeniedException", StringComparison.Ordinal);

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
