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

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the handler loop write analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeLoop, OperationKind.Loop);

    /// <summary>Analyzes the loop.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
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
    /// <summary>Returns true when the write is followed by leave; otherwise false.</summary>
    /// <param name="write">The write operation under inspection.</param>
    /// <param name="loop">The loop statement under inspection.</param>
    /// <returns>True when the write is followed by leave; otherwise false.</returns>
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

    /// <summary>Returns true when the leaves condition holds; otherwise false.</summary>
    /// <param name="operation">The operation under inspection.</param>
    /// <returns>True when the leaves condition holds; otherwise false.</returns>
    private static bool Leaves(IOperation operation) =>
        operation switch
        {
            IReturnOperation or IThrowOperation => true,
            IExpressionStatementOperation { Operation: IThrowOperation } => true,
            IBlockOperation block => block.Operations.Length > 0 && Leaves(block.Operations[block.Operations.Length - 1]),
            _ => false,
        };

    /// <summary>Collects the writes.</summary>
    /// <param name="operation">The operation supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="writes">The write operations collected so far.</param>
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

    /// <summary>Returns true when the invocation is repository write; otherwise false.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>True when the invocation is repository write; otherwise false.</returns>
    private static bool IsRepositoryWrite(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name is not ("Store" or "Add" or "Update" or "Delete"))
        {
            return false;
        }

        var receiver = invocation.Instance?.Type;
        return receiver != null && receiver.Name.EndsWith("Repository", StringComparison.Ordinal);
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
