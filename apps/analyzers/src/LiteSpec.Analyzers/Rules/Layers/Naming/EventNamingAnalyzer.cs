using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Naming;

/// <summary>LS2031: an event type ends in the suffix of its kind.</summary>
/// <follows>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventNamingAnalyzer : LayerAnalyzer
{
    /// <summary>The record identifier prefix of a module event mark.</summary>
    private const string ModuleEventPrefix = "module-event/";

    /// <summary>The record identifier prefix of an integration event mark.</summary>
    private const string IntegrationEventPrefix = "integration-event/";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2031);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Domain || layer == ArchitectureLayer.Application;

    /// <summary>
    ///     Registers the analysis actions for the event naming analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
        context.RegisterSymbolAction(c => Analyze(c, scope, assemblyName), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    private static void Analyze(SymbolAnalysisContext context, FileScope scope, string assemblyName)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind == TypeKind.Interface)
        {
            return;
        }

        if (ExpectedSuffixOf(type, context.Compilation, scope, assemblyName) is not (string kind, string suffix))
        {
            return;
        }

        if (!type.Name.EndsWith(suffix, StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2031, type.Locations[0], type.Name, kind, suffix));
        }
    }

    /// <summary>Returns the kind name and suffix the type must carry, or null when its kind is unknown.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>The kind name and suffix, or null when no kind applies.</returns>
    private static (string Kind, string Suffix)? ExpectedSuffixOf(
        INamedTypeSymbol type, Compilation compilation, FileScope scope, string assemblyName)
    {
        var file = CodeMap.SourceFileOf(type, compilation);
        if (CodeMap.IsContracts(file) || CodeMap.IsContractsAssembly(assemblyName))
        {
            return SuffixOfContractsMark(type);
        }

        if (scope.LayerOf(type) == ArchitectureLayer.Domain && ImplementsDomainEvent(type))
        {
            return ("domain event", "DomainEvent");
        }

        return null;
    }

    /// <summary>Returns the kind name and suffix of the first event mark on the type, or null when it carries none.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The kind name and suffix, or null when no event mark applies.</returns>
    private static (string Kind, string Suffix)? SuffixOfContractsMark(INamedTypeSymbol type)
    {
        foreach (var mark in MarksOf(type))
        {
            if (!mark.HasRecordId)
            {
                continue;
            }

            if (mark.RecordId.StartsWith(ModuleEventPrefix, StringComparison.Ordinal))
            {
                return ("module event", "ModuleEvent");
            }

            if (mark.RecordId.StartsWith(IntegrationEventPrefix, StringComparison.Ordinal))
            {
                return ("integration event", "IntegrationEvent");
            }
        }

        return null;
    }

    /// <summary>Every mark on the symbol, across its partial parts, in source order.</summary>
    /// <param name="symbol">The type symbol.</param>
    /// <returns>The marks.</returns>
    private static List<Mark> MarksOf(ISymbol symbol)
    {
        var marks = new List<Mark>();

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MemberDeclarationSyntax member)
            {
                marks.AddRange(MarkReader.Read(member));
            }
        }

        return marks;
    }

    /// <summary>Returns true when the type implements the domain event contract; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type implements the domain event contract; otherwise false.</returns>
    private static bool ImplementsDomainEvent(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IDomainEvent")
            {
                return true;
            }
        }

        return false;
    }
}
