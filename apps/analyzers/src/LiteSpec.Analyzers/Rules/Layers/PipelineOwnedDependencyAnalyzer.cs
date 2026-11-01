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
    /// <summary>The options key text held for this operation.</summary>
    private const string OptionsKey = "litespec.LS2016.pipeline_owned_types";

    /// <summary>Creates an immutable hash set string default owned types.</summary>
    private static readonly ImmutableHashSet<string> DefaultOwnedTypes = ImmutableHashSet.Create(
        "LiteBus.Messaging.Abstractions.IUnitOfWork",
        "LiteBus.Messaging.Abstractions.IIdempotencyStore",
        "LiteBus.Outbox.Abstractions.ITransactionalOutbox",
        "LiteBus.Outbox.Abstractions.IOutbox");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2016);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the pipeline owned dependency analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);

    /// <summary>Analyzes the parameter.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
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

    /// <summary>Returns the types.</summary>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <param name="tree">The syntax tree under inspection.</param>
    /// <returns>The types.</returns>
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
}
