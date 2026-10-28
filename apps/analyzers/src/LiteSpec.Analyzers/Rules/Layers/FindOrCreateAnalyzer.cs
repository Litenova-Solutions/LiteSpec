using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2014: a command handler never finds a record and creates it in the same method.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FindOrCreateAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2014);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationBlockAction(AnalyzeBlock);

    private static void AnalyzeBlock(OperationBlockAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
        {
            return;
        }

        if (!IsCommandHandler(method.ContainingType))
        {
            return;
        }

        var found = new List<ITypeSymbol>();
        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.Descendants().OfType<IInvocationOperation>())
            {
                if (IsRepositoryFind(operation, out var foundType))
                {
                    found.Add(foundType);
                }
            }
        }

        if (found.Count == 0)
        {
            return;
        }

        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.Descendants())
            {
                switch (operation)
                {
                    case IObjectCreationOperation creation
                        when creation.Type != null && MatchesFound(creation.Type, found):
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LS2014,
                            CreationLocation(creation),
                            method.ContainingType.Name,
                            creation.Type.Name));
                        break;
                    case IInvocationOperation creation
                        when creation.TargetMethod.IsStatic
                        && MatchesFound(Unwrap(creation.TargetMethod.ReturnType), found):
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LS2014,
                            MemberLocation(creation),
                            method.ContainingType.Name,
                            Unwrap(creation.TargetMethod.ReturnType).Name));
                        break;
                }
            }
        }
    }

    private static bool IsRepositoryFind(IInvocationOperation invocation, out ITypeSymbol found)
    {
        found = invocation.TargetMethod.ReturnType;
        if (!invocation.TargetMethod.Name.StartsWith("Find", StringComparison.Ordinal))
        {
            return false;
        }

        var receiver = invocation.Instance?.Type;
        if (receiver == null || !receiver.Name.EndsWith("Repository", StringComparison.Ordinal))
        {
            return false;
        }

        found = Unwrap(invocation.TargetMethod.ReturnType);
        return !IsVoid(found);
    }

    private static bool MatchesFound(ITypeSymbol created, List<ITypeSymbol> found)
    {
        if (IsVoid(created))
        {
            return false;
        }

        foreach (var type in found)
        {
            if (SymbolEqualityComparer.Default.Equals(type, created))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVoid(ITypeSymbol type) => type.SpecialType == SpecialType.System_Void;

    private static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        while (type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = named.TypeArguments[0];
                continue;
            }

            if ((named.Name is "Task" or "ValueTask") && InSystem(named.ContainingNamespace))
            {
                type = named.TypeArguments[0];
                continue;
            }

            break;
        }

        return type;
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

    private static Location CreationLocation(IObjectCreationOperation creation) =>
        creation.Syntax is ObjectCreationExpressionSyntax syntax && syntax.Type != null
            ? syntax.Type.GetLocation()
            : creation.Syntax.GetLocation();

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
