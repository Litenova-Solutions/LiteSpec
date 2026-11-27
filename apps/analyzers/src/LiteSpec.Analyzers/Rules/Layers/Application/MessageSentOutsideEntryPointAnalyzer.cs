using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>
///     LS2021: a command or query handler holds the query mediator to send another module's public query, a
///     reaction or a job holds the command and query mediators, and no module type holds the event mediator.
///     The platform composes the pipeline and is exempt, with the Kernel.
/// </summary>
/// <follows>litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageSentOutsideEntryPointAnalyzer : LayerAnalyzer
{
    /// <summary>The simple names of the mediators a type may hold only in its role.</summary>
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
        context.RegisterSymbolAction(c => AnalyzeSymbol(c, scope), SymbolKind.Parameter, SymbolKind.Field, SymbolKind.Property);
    }

    /// <summary>Analyzes a constructor parameter, field or property holding a mediator.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeSymbol(SymbolAnalysisContext context, FileScope scope)
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

        if (owner is null || !IsMediator(held) || TypeLocation(symbol) is not Location location)
        {
            return;
        }

        if (MayHold(owner, held.Name))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2021, location, owner.Name, held.Name, ReasonFor(held.Name)));
    }

    /// <summary>Returns true when the type is one of the mediators by its simple name; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a command, query or event mediator; otherwise false.</returns>
    private static bool IsMediator(ITypeSymbol type) =>
        type is INamedTypeSymbol named && MediatorNames.Contains(named.Name);

    /// <summary>Returns true when the type may hold the mediator: a handler, reaction or job holds the query mediator, and a reaction or a job holds the command mediator; otherwise false.</summary>
    /// <param name="owner">The type holding the mediator.</param>
    /// <param name="mediator">The simple name of the mediator held.</param>
    /// <returns>True when the holder's role may hold the mediator; otherwise false.</returns>
    private static bool MayHold(INamedTypeSymbol owner, string mediator)
    {
        if (mediator.Equals("IQueryMediator", StringComparison.Ordinal))
        {
            return IsHandler(owner) || TriggerTypes.IsReactionOrJob(owner);
        }

        if (mediator.Equals("ICommandMediator", StringComparison.Ordinal))
        {
            return TriggerTypes.IsReactionOrJob(owner);
        }

        return false;
    }

    /// <summary>Returns why the holder may not hold the mediator.</summary>
    /// <param name="mediator">The simple name of the mediator held.</param>
    /// <returns>The reason clause naming the roles that may hold the mediator.</returns>
    private static string ReasonFor(string mediator)
    {
        if (mediator.Equals("IQueryMediator", StringComparison.Ordinal))
        {
            return "which only a handler, a reaction, or a job may hold";
        }

        if (mediator.Equals("ICommandMediator", StringComparison.Ordinal))
        {
            return "which only a reaction or a job may hold";
        }

        return "which only the platform may hold";
    }

    /// <summary>Returns true when the type is a command or query handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a command or query handler; otherwise false.</returns>
    private static bool IsHandler(INamedTypeSymbol type)
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

    /// <summary>Returns true when the assembly holds the pipeline: the platform, or the Kernel; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name, or null when it has none.</param>
    /// <returns>True when the assembly name ends with the Platform or Kernel segment; otherwise false.</returns>
    private static bool IsPipelineAssembly(string? assemblyName) =>
        assemblyName is not null
        && (assemblyName.Equals("Platform", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Platform", StringComparison.Ordinal)
            || assemblyName.Equals("Kernel", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Kernel", StringComparison.Ordinal));

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
