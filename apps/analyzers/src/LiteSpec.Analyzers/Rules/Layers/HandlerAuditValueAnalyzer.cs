using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2013: a command handler adds no audit value that the message already carries.</summary>
/// <follows>litespec/backend/application#declare-audit-values-beside-the-message</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerAuditValueAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2013);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the handler audit value analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is not ("WithTarget" or "WithReason"))
        {
            return;
        }

        if (ReceiverType(invocation)?.Name != "IAuditScope")
        {
            return;
        }

        var handler = InnermostType(context.ContainingSymbol);
        if (handler == null || !IsCommandHandler(handler))
        {
            return;
        }

        var message = MessageParameter(InnermostMethod(context.ContainingSymbol));
        if (message == null || invocation.Arguments.Length == 0)
        {
            return;
        }

        // A static call through an unreduced extension method carries the scope as its first argument.
        var first = invocation.Instance == null
            && invocation.TargetMethod.IsExtensionMethod
            && invocation.TargetMethod.ReducedFrom == null
            ? 1
            : 0;

        var sawMessage = false;
        for (var index = first; index < invocation.Arguments.Length; index++)
        {
            if (!ReadsOnlyMessage(invocation.Arguments[index].Value, message, ref sawMessage))
            {
                return;
            }
        }

        if (!sawMessage)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2013, MemberLocation(invocation), handler.Name, invocation.TargetMethod.Name));
    }

    /// <summary>Returns the type.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>The type, or null when none applies.</returns>
    private static ITypeSymbol? ReceiverType(IInvocationOperation invocation)
    {
        if (invocation.Instance != null)
        {
            return invocation.Instance.Type;
        }

        if (invocation.TargetMethod.IsExtensionMethod
            && invocation.TargetMethod.ReducedFrom == null
            && invocation.Arguments.Length > 0)
        {
            return invocation.Arguments[0].Value.Type;
        }

        return invocation.TargetMethod.ContainingType;
    }

    /// <summary>Returns the parameter.</summary>
    /// <param name="method">The method under inspection.</param>
    /// <returns>The parameter, or null when none applies.</returns>
    private static IParameterSymbol? MessageParameter(IMethodSymbol? method)
    {
        if (method == null || method.Parameters.Length == 0)
        {
            return null;
        }

        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.Name.EndsWith("Command", StringComparison.Ordinal))
            {
                return parameter;
            }
        }

        return method.Parameters[0];
    }

    /// <summary>Returns true when the reads-only-message condition holds; otherwise false.</summary>
    /// <param name="operation">The operation under inspection.</param>
    /// <param name="message">The message type under inspection.</param>
    /// <param name="sawMessage">Whether a message handler has been seen in the type.</param>
    /// <returns>True when the reads-only-message condition holds; otherwise false.</returns>
    private static bool ReadsOnlyMessage(IOperation operation, IParameterSymbol message, ref bool sawMessage)
    {
        switch (operation)
        {
            case IParameterReferenceOperation parameter:
                if (!SymbolEqualityComparer.Default.Equals(parameter.Parameter, message))
                {
                    return false;
                }

                sawMessage = true;
                return true;
            case ILocalReferenceOperation:
            case IAnonymousFunctionOperation:
                return false;
            case IFieldReferenceOperation field:
                return field.Field.IsConst
                    || (field.Instance != null && ReadsOnlyMessage(field.Instance, message, ref sawMessage));
            case IPropertyReferenceOperation property:
                return property.Instance != null
                    ? ReadsOnlyMessage(property.Instance, message, ref sawMessage)
                        && ArgumentsClean(property.Arguments, message, ref sawMessage)
                    : InSystem(property.Property.ContainingNamespace);
            case IInvocationOperation invocation:
                if (invocation.Instance != null
                    && !ReadsOnlyMessage(invocation.Instance, message, ref sawMessage))
                {
                    return false;
                }

                if (invocation.Instance == null
                    && !InSystem(invocation.TargetMethod.ContainingNamespace))
                {
                    return false;
                }

                return ArgumentsClean(invocation.Arguments, message, ref sawMessage);
            case IObjectCreationOperation creation:
                return creation.Type != null
                    && InSystem(creation.Type.ContainingNamespace)
                    && ArgumentsClean(creation.Arguments, message, ref sawMessage)
                    && (creation.Initializer == null
                        || ReadsOnlyMessage(creation.Initializer, message, ref sawMessage));
            default:
                foreach (var child in operation.ChildOperations)
                {
                    if (!ReadsOnlyMessage(child, message, ref sawMessage))
                    {
                        return false;
                    }
                }

                return true;
        }
    }

    /// <summary>Returns true when the arguments-clean condition holds; otherwise false.</summary>
    /// <param name="arguments">The argument list under inspection.</param>
    /// <param name="message">The message type under inspection.</param>
    /// <param name="sawMessage">Whether a message handler has been seen in the type.</param>
    /// <returns>True when the arguments-clean condition holds; otherwise false.</returns>
    private static bool ArgumentsClean(
        ImmutableArray<IArgumentOperation> arguments, IParameterSymbol message, ref bool sawMessage)
    {
        foreach (var argument in arguments)
        {
            if (!ReadsOnlyMessage(argument.Value, message, ref sawMessage))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns true when the in-system condition holds; otherwise false.</summary>
    /// <param name="space">The space supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the in-system condition holds; otherwise false.</returns>
    private static bool InSystem(INamespaceSymbol? space)
    {
        var name = space?.ToDisplayString();
        return name != null
            && (name.Equals("System", StringComparison.Ordinal)
                || name.StartsWith("System.", StringComparison.Ordinal));
    }

    /// <summary>Returns true when the type is command handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is command handler; otherwise false.</returns>
    private static bool IsCommandHandler(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "ICommandHandler")
            {
                return true;
            }
        }

        return false;
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

    /// <summary>Returns the method.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The method, or null when none applies.</returns>
    private static IMethodSymbol? InnermostMethod(ISymbol symbol)
    {
        for (var current = symbol; current != null; current = current.ContainingSymbol)
        {
            if (current is IMethodSymbol method)
            {
                return method;
            }
        }

        return null;
    }

    /// <summary>Returns the location.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>The location.</returns>
    private static Location MemberLocation(IInvocationOperation invocation)
    {
        if (invocation.Syntax is InvocationExpressionSyntax syntax)
        {
            if (syntax.Expression is MemberAccessExpressionSyntax access)
            {
                return access.Name.GetLocation();
            }

            if (syntax.Expression is MemberBindingExpressionSyntax binding)
            {
                return binding.Name.GetLocation();
            }

            return syntax.Expression.GetLocation();
        }

        return invocation.Syntax.GetLocation();
    }
}
