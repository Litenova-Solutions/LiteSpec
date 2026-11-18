using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>
///     LS2021: a type that is not an endpoint, reaction or job holds no command, query or event mediator,
///     so only the three entry points send a use case message. The platform composes the pipeline and is exempt,
///     with the shared kernel of a one-modules-project solution. A provider adapter in
///     <c>Infrastructure/{Provider}/</c>, where the compilation references the provider module's Contracts
///     project, may hold the query and command mediators: it is the one place that sends another module's public
///     queries and public commands.
/// </summary>
/// <follows>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageSentOutsideEntryPointAnalyzer : LayerAnalyzer
{
    /// <summary>The simple names of the mediators a type outside an entry point may not hold.</summary>
    private static readonly ImmutableHashSet<string> MediatorNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ICommandMediator",
        "IQueryMediator",
        "IEventMediator");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2021);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Domain || layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure;

    /// <summary>
    ///     Registers the analysis actions for the message sent outside an entry point analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        if (IsPipelineAssembly(context.Compilation.AssemblyName))
        {
            return;
        }

        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var providers = ProviderModules(context.Compilation);
        context.RegisterSymbolAction(c => AnalyzeSymbol(c, scope, providers), SymbolKind.Parameter, SymbolKind.Field, SymbolKind.Property);
    }

    /// <summary>Analyzes a constructor parameter, field or property holding a mediator.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="providers">The modules whose Contracts project the compilation references.</param>
    private static void AnalyzeSymbol(SymbolAnalysisContext context, FileScope scope, ImmutableHashSet<string> providers)
    {
        var symbol = context.Symbol;
        if (symbol.IsImplicitlyDeclared || !scope.AppliesTo(symbol))
        {
            return;
        }

        ITypeSymbol held;
        INamedTypeSymbol? owner;
        switch (symbol)
        {
            case IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: INamedTypeSymbol containing } } parameter:
                held = parameter.Type;
                owner = containing;
                break;
            case IFieldSymbol field:
                held = field.Type;
                owner = field.ContainingType;
                break;
            case IPropertySymbol property:
                held = property.Type;
                owner = property.ContainingType;
                break;
            default:
                return;
        }

        if (owner is null || !IsMediator(held) || IsEntryPoint(owner) || TypeLocation(symbol) is not Location location)
        {
            return;
        }

        if (held.Name != "IEventMediator" && ProviderAdapterFolder(location.SourceTree?.FilePath) is string provider && providers.Contains(provider))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2021, location, owner.Name, held.Name));
    }

    /// <summary>Returns true when the type is one of the mediators by its simple name; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a command, query or event mediator; otherwise false.</returns>
    private static bool IsMediator(ITypeSymbol type) =>
        type is INamedTypeSymbol named && MediatorNames.Contains(named.Name);

    /// <summary>
    ///     Returns true when the type is an endpoint, a reaction or a job: a type named *Endpoint or *Job, a type
    ///     deriving from BackgroundService, or a type implementing IEventHandler. A WebApi file lies outside the
    ///     rule's layers, so an endpoint in a WebApi folder never reaches this check.
    /// </summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is an entry point; otherwise false.</returns>
    private static bool IsEntryPoint(INamedTypeSymbol type)
    {
        if (type.Name.EndsWith("Endpoint", StringComparison.Ordinal) || type.Name.EndsWith("Job", StringComparison.Ordinal))
        {
            return true;
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "BackgroundService")
            {
                return true;
            }
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IEventHandler")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The module names whose Contracts project the compilation references or compiles: `{Product}.{Module}.Contracts` gives `{Module}`.</summary>
    /// <param name="compilation">The compilation under analysis.</param>
    /// <returns>The referenced provider module names.</returns>
    private static ImmutableHashSet<string> ProviderModules(Compilation compilation)
    {
        var names = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            const string Suffix = ".Contracts";
            if (!reference.Name.EndsWith(Suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var stem = reference.Name.Substring(0, reference.Name.Length - Suffix.Length);
            var dot = stem.LastIndexOf('.');
            names.Add(dot < 0 ? stem : stem.Substring(dot + 1));
        }

        foreach (var tree in compilation.SyntaxTrees)
        {
            foreach (var segment in tree.FilePath.Replace('\\', '/').Split('/'))
            {
                const string Suffix = ".Contracts";
                if (segment.EndsWith(Suffix, StringComparison.Ordinal))
                {
                    var stem = segment.Substring(0, segment.Length - Suffix.Length);
                    var dot = stem.LastIndexOf('.');
                    names.Add(dot < 0 ? stem : stem.Substring(dot + 1));
                }
            }
        }

        return names.ToImmutable();
    }

    /// <summary>The adapter folder a file sits in under `Infrastructure/`, or null when it sits in no adapter folder.</summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The folder name after `Infrastructure/`, or null.</returns>
    private static string? ProviderAdapterFolder(string? filePath)
    {
        if (!CodeMap.IsAdapter(filePath))
        {
            return null;
        }

        var segments = filePath!.Replace('\\', '/').Split('/');
        for (var index = 0; index + 2 < segments.Length; index++)
        {
            if (segments[index].Equals("Infrastructure", StringComparison.Ordinal))
            {
                return segments[index + 1];
            }
        }

        return null;
    }

    /// <summary>Returns true when the assembly holds the pipeline: the platform, or the shared kernel of a one-modules-project solution; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name, or null when it has none.</param>
    /// <returns>True when the assembly name ends with the Platform or SharedKernel segment; otherwise false.</returns>
    private static bool IsPipelineAssembly(string? assemblyName) =>
        assemblyName is not null
        && (assemblyName.Equals("Platform", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Platform", StringComparison.Ordinal)
            || assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
            || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal));

    /// <summary>Returns the location of the declared type of a parameter, field or property, or null when it has none.</summary>
    /// <param name="symbol">The symbol whose declared type the diagnostic sits on.</param>
    /// <returns>The location of the type syntax, or null.</returns>
    private static Location? TypeLocation(ISymbol symbol)
    {
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            TypeSyntax? type = reference.GetSyntax() switch
            {
                ParameterSyntax parameter => parameter.Type,
                VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } => declaration.Type,
                PropertyDeclarationSyntax property => property.Type,
                _ => null,
            };
            if (type is not null)
            {
                return type.GetLocation();
            }
        }

        return null;
    }
}
