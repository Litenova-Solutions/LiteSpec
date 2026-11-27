using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Knowledge;

/// <summary>LS2040: outside a knowledge set, code reads knowledge by role and date, never by building a key from a string literal.</summary>
/// <follows>litespec/core/knowledge#read-knowledge-by-role-and-date</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KnowledgeKeyLiteralAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2040);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the knowledge key literal analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => AnalyzeCreation(c, scope), OperationKind.ObjectCreation);
        context.RegisterOperationAction(c => AnalyzeInvocation(c, scope), OperationKind.Invocation);
    }

    /// <summary>Analyzes the object creation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeCreation(OperationAnalysisContext context, FileScope scope)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var creation = (IObjectCreationOperation)context.Operation;
        if (creation.Type is not INamedTypeSymbol key
            || !IsKnowledgeKey(key, context.Compilation)
            || !HasStringLiteralArgument(creation.Arguments))
        {
            return;
        }

        var location = creation.Syntax is ObjectCreationExpressionSyntax syntax
            ? syntax.Type.GetLocation()
            : creation.Syntax.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2040, location, key.Name));
    }

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeInvocation(OperationAnalysisContext context, FileScope scope)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!method.Name.Equals("Parse", StringComparison.Ordinal) && !method.Name.Equals("From", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsKnowledgeKey(method.ContainingType, context.Compilation) || !HasStringLiteralArgument(invocation.Arguments))
        {
            return;
        }

        var location = invocation.Syntax is InvocationExpressionSyntax syntax && syntax.Expression is MemberAccessExpressionSyntax access
            ? access.Name.GetLocation()
            : invocation.Syntax.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2040, location, method.ContainingType.Name));
    }

    /// <summary>Returns true when the type is a knowledge key; otherwise false.</summary>
    /// <param name="type">The type under inspection, or null when the operation has none.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the type implements a knowledge key marker, or is named *Key in a knowledge model project.</returns>
    private static bool IsKnowledgeKey(ITypeSymbol? type, Compilation compilation)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        foreach (var contract in named.AllInterfaces)
        {
            if (contract.Name.EndsWith("KnowledgeKey", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return named.Name.EndsWith("Key", StringComparison.Ordinal)
            && CodeMap.SourceFileOf(named, compilation) is string file
            && global::LiteSpec.Analyzers.LayerMap.Knowledge.IsModelFile(file);
    }

    /// <summary>Returns true when an argument is a string literal; otherwise false.</summary>
    /// <param name="arguments">The creation or invocation arguments.</param>
    /// <returns>True when an argument passes a string literal.</returns>
    private static bool HasStringLiteralArgument(ImmutableArray<IArgumentOperation> arguments)
    {
        foreach (var argument in arguments)
        {
            if (Unwrap(argument.Value) is ILiteralOperation { ConstantValue: { HasValue: true, Value: string } })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the operation with its conversions removed.</summary>
    /// <param name="operation">The operation under inspection.</param>
    /// <returns>The innermost operand.</returns>
    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }
}
