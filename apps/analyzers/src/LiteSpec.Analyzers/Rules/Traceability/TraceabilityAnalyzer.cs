using System.Collections.Generic;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Analyzers.Records;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     The base of every traceability rule: loads the consumer's records once per
///     compilation, stays silent without them, and hands the rule its context.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public abstract class TraceabilityAnalyzer : LayerAnalyzer
{
    /// <summary>Loads the records for the compilation and hands the rule its traceability context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected sealed override void Register(CompilationStartAnalysisContext context)
    {
        if (TraceabilityContext.TryGet(context.Compilation, out var cached) && cached is not null)
        {
            RegisterTraceability(context, cached);
            return;
        }

        var store = RecordStore.Load(context.Options, context.CancellationToken);

        if (store is null)
        {
            return;
        }

        var layer = LayerResolver.Resolve(context.Compilation, context.Options);
        var state = TraceabilityContext.GetOrCreate(context.Compilation, layer, store);

        RegisterTraceability(context, state);
    }

    /// <summary>Registers the rule's actions for one compilation with records.</summary>
    /// <param name="context">The compilation start context.</param>
    /// <param name="state">The compilation's traceability context.</param>
    private protected abstract void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state);

    /// <summary>
    ///     Every member declaration of the symbol in this compilation, for reading
    ///     its marks across partial parts.
    /// </summary>
    /// <param name="symbol">The type or method symbol.</param>
    /// <returns>The member declarations.</returns>
    protected static IReadOnlyList<MemberDeclarationSyntax> Declarations(ISymbol symbol)
    {
        var declarations = new List<MemberDeclarationSyntax>();

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MemberDeclarationSyntax member)
            {
                declarations.Add(member);
            }
        }

        return declarations;
    }

    /// <summary>Every mark on the symbol, across its partial parts, in source order.</summary>
    /// <param name="symbol">The type or method symbol.</param>
    /// <returns>The marks.</returns>
    protected static IReadOnlyList<Mark> MarksOf(ISymbol symbol)
    {
        var marks = new List<Mark>();

        foreach (var member in Declarations(symbol))
        {
            marks.AddRange(MarkReader.Read(member));
        }

        return marks;
    }

    /// <summary>The location of one mark's identifier inside its declaration tree.</summary>
    /// <param name="member">The declaration carrying the mark.</param>
    /// <param name="mark">The mark.</param>
    /// <returns>The mark location.</returns>
    protected static Location MarkLocation(MemberDeclarationSyntax member, Mark mark) =>
        Location.Create(member.SyntaxTree, mark.Span);
}
