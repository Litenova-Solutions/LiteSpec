using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2015: a command handler never changes records in a loop.</summary>
/// <follows>litespec/backend/application#issue-per-record-work-from-a-job</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerLoopWriteAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2015);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeLoop, OperationKind.Loop);

    private static void AnalyzeLoop(OperationAnalysisContext context)
    {
        var loop = (ILoopOperation)context.Operation;
        if (loop.LoopKind is not (LoopKind.For or LoopKind.ForEach or LoopKind.While))
        {
            return;
        }

        var handler = InnermostType(context.ContainingSymbol);
        if (handler == null || !IsCommandHandler(handler))
        {
            return;
        }

        var writes = new List<IInvocationOperation>();
        CollectWrites(loop.Body, writes);
        foreach (var write in writes)
        {
            if (IsFollowedByLeave(write, loop))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2015, MemberLocation(write), handler.Name, write.TargetMethod.Name));
        }
    }

    // Whether the write runs at most once per invocation: a search loop that acts on
    // the first item with work and then leaves, so no batch rolls back together.
    private static bool IsFollowedByLeave(IInvocationOperation write, ILoopOperation loop)
    {
        var child = (IOperation)write;
        for (var parent = write.Parent; parent != null && parent != loop; child = parent, parent = parent.Parent)
        {
            if (parent is IBlockOperation block)
            {
                var operations = block.Operations;
                for (var i = 0; i < operations.Length - 1; i++)
                {
                    if (operations[i] == child && Leaves(operations[i + 1]))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool Leaves(IOperation operation) =>
        operation switch
        {
            IReturnOperation or IThrowOperation => true,
            IExpressionStatementOperation { Operation: IThrowOperation } => true,
            IBlockOperation block => block.Operations.Length > 0 && Leaves(block.Operations[block.Operations.Length - 1]),
            _ => false,
        };

    private static void CollectWrites(IOperation? operation, List<IInvocationOperation> writes)
    {
        if (operation == null)
        {
            return;
        }

        foreach (var child in operation.ChildOperations)
        {
            if (child is ILoopOperation)
            {
                // A nested loop reports its own writes, so this walk stops at its edge.
                continue;
            }

            if (child is IInvocationOperation invocation && IsRepositoryWrite(invocation))
            {
                writes.Add(invocation);
            }

            CollectWrites(child, writes);
        }
    }

    private static bool IsRepositoryWrite(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name is not ("Store" or "Add" or "Update" or "Delete"))
        {
            return false;
        }

        var receiver = invocation.Instance?.Type;
        return receiver != null && receiver.Name.EndsWith("Repository", StringComparison.Ordinal);
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
