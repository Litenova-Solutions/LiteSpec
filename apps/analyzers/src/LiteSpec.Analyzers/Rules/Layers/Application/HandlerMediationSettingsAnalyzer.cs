using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2034: an event handler runs with no caller, so each command or query it dispatches names explicit mediation settings.</summary>
/// <follows>litespec/packs/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerMediationSettingsAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2034);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the handler mediation settings analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var cancellationToken = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => Analyze(c, scope, cancellationToken), OperationKind.Invocation);
    }

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="cancellationToken">The cancellation token type supplied for the check, or null when the compilation does not provide it.</param>
    private static void Analyze(OperationAnalysisContext context, FileScope scope, INamedTypeSymbol? cancellationToken)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!IsMediatorCall(method))
        {
            return;
        }

        if (EnclosingType(context.ContainingSymbol) is not INamedTypeSymbol enclosing || !IsEventHandler(enclosing))
        {
            return;
        }

        if (HasExplicitSettings(invocation, cancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2034, InvocationLocation(invocation), enclosing.Name, method.Name));
    }

    /// <summary>Returns true when the call dispatches through a command or query mediator; otherwise false.</summary>
    /// <param name="method">The invoked method.</param>
    /// <returns>True for SendAsync on ICommandMediator or QueryAsync on IQueryMediator; otherwise false.</returns>
    private static bool IsMediatorCall(IMethodSymbol method)
    {
        var containing = method.ContainingType;
        if (containing is null)
        {
            return false;
        }

        return (method.Name.Equals("SendAsync", StringComparison.Ordinal)
                && containing.Name.Equals("ICommandMediator", StringComparison.Ordinal))
            || (method.Name.Equals("QueryAsync", StringComparison.Ordinal)
                && containing.Name.Equals("IQueryMediator", StringComparison.Ordinal));
    }

    /// <summary>Returns true when the type is an event handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type implements IEventHandler or IDomainEventReaction; otherwise false.</returns>
    private static bool IsEventHandler(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name.Equals("IEventHandler", StringComparison.Ordinal)
                || contract.Name.Equals("IDomainEventReaction", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the call passes explicit mediation settings; otherwise false.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <param name="cancellationToken">The cancellation token type supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when an argument past the message is no cancellation token; otherwise false.</returns>
    private static bool HasExplicitSettings(IInvocationOperation invocation, INamedTypeSymbol? cancellationToken)
    {
        for (var index = 1; index < invocation.Arguments.Length; index++)
        {
            if (!IsCancellationToken(invocation.Arguments[index].Value.Type, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is the cancellation token; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="cancellationToken">The cancellation token type supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the type is the cancellation token; otherwise false.</returns>
    private static bool IsCancellationToken(ITypeSymbol? type, INamedTypeSymbol? cancellationToken)
    {
        if (type is null)
        {
            return false;
        }

        if (cancellationToken is not null)
        {
            return SymbolEqualityComparer.Default.Equals(type, cancellationToken);
        }

        return type.Name.Equals("CancellationToken", StringComparison.Ordinal);
    }

    /// <summary>Returns the named type enclosing the symbol, or null when there is none.</summary>
    /// <param name="symbol">The symbol under inspection.</param>
    /// <returns>The enclosing named type, or null.</returns>
    private static INamedTypeSymbol? EnclosingType(ISymbol? symbol) => symbol switch
    {
        IMethodSymbol method => method.ContainingType,
        IFieldSymbol field => field.ContainingType,
        IPropertySymbol property => property.ContainingType,
        INamedTypeSymbol type => type,
        _ => null,
    };

    /// <summary>Returns the location.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>The location.</returns>
    private static Location InvocationLocation(IInvocationOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax syntax && syntax.Expression is MemberAccessExpressionSyntax access
            ? access.Name.Identifier.GetLocation()
            : invocation.Syntax.GetLocation();
}
