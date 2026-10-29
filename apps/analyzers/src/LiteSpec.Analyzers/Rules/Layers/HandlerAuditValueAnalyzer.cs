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

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

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

    private static bool InSystem(INamespaceSymbol? space)
    {
        var name = space?.ToDisplayString();
        return name != null
            && (name.Equals("System", StringComparison.Ordinal)
                || name.StartsWith("System.", StringComparison.Ordinal));
    }

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
