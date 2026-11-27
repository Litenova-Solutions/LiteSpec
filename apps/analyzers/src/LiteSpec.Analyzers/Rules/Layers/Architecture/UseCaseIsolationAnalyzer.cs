using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>
///     LS2019: a use case folder names no type of another use case folder, the message and result of another use case
///     included, except the command a reaction or job sends from a catch for the use case it runs on refusal.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCaseIsolationAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2019);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the use case isolation analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        if (map.UseCaseOf(node.SyntaxTree.FilePath) is not string useCase)
        {
            return;
        }

        if (!References.TryResolve(context, node, type => OtherUseCaseOf(type, useCase, map, context.Compilation) is not null, out var name, out var type)
            || type is null
            || OtherUseCaseOf(type, useCase, map, context.Compilation) is not string other)
        {
            return;
        }

        if (IsRefusalRun(node, type, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2019, node.Identifier.GetLocation(), name, CodeMap.FolderName(other)));
    }

    /// <summary>
    ///     Returns true when a reaction or job names the command message of another use case inside a catch: the run of
    ///     the use case its record names in onRefusal, which follows only a refused run that committed nothing.
    /// </summary>
    /// <param name="node">The name under inspection.</param>
    /// <param name="type">The type the name resolves to.</param>
    /// <param name="model">The semantic model of the file.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>True when the name is the refusal run of a trigger; otherwise false.</returns>
    /// <follows>litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job</follows>
    private static bool IsRefusalRun(SimpleNameSyntax node, INamedTypeSymbol type, SemanticModel model, System.Threading.CancellationToken cancellationToken)
    {
        if (!type.Name.EndsWith("Command", StringComparison.Ordinal))
        {
            return false;
        }

        var inCatch = false;
        TypeDeclarationSyntax? declaration = null;

        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is CatchClauseSyntax)
            {
                inCatch = true;
            }
            else if (ancestor is TypeDeclarationSyntax found)
            {
                declaration = found;
                break;
            }
        }

        return inCatch
            && declaration is not null
            && model.GetDeclaredSymbol(declaration, cancellationToken) is INamedTypeSymbol owner
            && TriggerTypes.IsReactionOrJob(owner);
    }

    /// <summary>Returns the other use case key declaring the type, or null when it declares none.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="useCase">The use case key holding the file under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The other use case key, or null when the type is declared in no other use case.</returns>
    private static string? OtherUseCaseOf(INamedTypeSymbol type, string useCase, CodeMap map, Compilation compilation)
    {
        if (CodeMap.SourceFileOf(type, compilation) is not string file)
        {
            return null;
        }

        if (map.UseCaseOf(file) is string other && !other.Equals(useCase, StringComparison.Ordinal))
        {
            return other;
        }

        return null;
    }

}
