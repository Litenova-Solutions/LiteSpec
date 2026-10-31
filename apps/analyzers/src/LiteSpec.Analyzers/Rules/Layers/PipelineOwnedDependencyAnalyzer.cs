using System;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2016: a command or query handler takes no pipeline-owned collaborator.</summary>
/// <follows>litespec/backend/persistence#keep-uniqueness-out-of-handlers</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PipelineOwnedDependencyAnalyzer : LayerAnalyzer
{
    private const string OptionsKey = "litespec.LS2016.pipeline_owned_types";

    private static readonly ImmutableHashSet<string> DefaultOwnedTypes = ImmutableHashSet.Create(
        "LiteBus.Messaging.Abstractions.IUnitOfWork",
        "LiteBus.Messaging.Abstractions.IIdempotencyStore",
        "LiteBus.Outbox.Abstractions.ITransactionalOutbox",
        "LiteBus.Outbox.Abstractions.IOutbox");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2016);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);

    private static void AnalyzeParameter(SymbolAnalysisContext context)
    {
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
        var tree = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;
        if (tree == null || !OwnedTypes(context.Options, tree).Contains(type.ToDisplayString()))
        {
            return;
        }

        foreach (var declaration in parameter.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is ParameterSyntax syntax && syntax.Type != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2016, syntax.Type.GetLocation(), method.ContainingType.Name, type.Name));
                return;
            }
        }
    }

    private static ImmutableHashSet<string> OwnedTypes(AnalyzerOptions options, SyntaxTree tree)
    {
        if (options.AnalyzerConfigOptionsProvider.GetOptions(tree).TryGetValue(OptionsKey, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return ImmutableHashSet.CreateRange(
                value.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0));
        }

        return DefaultOwnedTypes;
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
}
