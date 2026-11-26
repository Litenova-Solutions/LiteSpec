using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1015: a non-root type of an aggregate exposes no public setter, no public constructor and no public mutable collection.</summary>
/// <follows>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AggregateMutationAnalyzer : LayerAnalyzer
{
    /// <summary>The mutable collection types, by original definition name.</summary>
    private static readonly string[] MutableCollections =
    [
        "List", "Dictionary", "HashSet", "Collection", "ObservableCollection",
        "LinkedList", "Queue", "Stack", "SortedList", "SortedDictionary", "SortedSet",
        "IList", "IDictionary", "ICollection", "ISet",
        "ArrayList", "Hashtable",
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1015);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the aggregate mutation analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope, map), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope, CodeMap map)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind is not TypeKind.Class and not TypeKind.Struct)
        {
            return;
        }

        if (FirstSourceFile(type) is not string file || map.AggregateOf(file) is not string aggregate)
        {
            return;
        }

        if (type.Name.EndsWith("Id", StringComparison.Ordinal)
            || Aggregates.IsRoot(type)
            || Aggregates.IsDomainEvent(type)
            || Aggregates.IsException(type))
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            AnalyzeMember(context, type, member, aggregate);
        }
    }

    /// <summary>Analyzes the member.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The type under inspection.</param>
    /// <param name="member">The member under inspection.</param>
    /// <param name="aggregate">The aggregate folder holding the type.</param>
    private static void AnalyzeMember(SymbolAnalysisContext context, INamedTypeSymbol type, ISymbol member, string aggregate)
    {
        if (member.IsStatic || member.IsImplicitlyDeclared)
        {
            return;
        }

        var aggregateName = CodeMap.FolderName(aggregate);

        if (member is IPropertySymbol property)
        {
            AnalyzeProperty(context, type, property, aggregateName);
            return;
        }

        if (member is IFieldSymbol field)
        {
            if (IsPublic(field.DeclaredAccessibility)
                && field.Type is INamedTypeSymbol fieldType
                && IsMutableCollection(fieldType))
            {
                Report(context, type, field, aggregateName, "it must not expose the mutable collection '" + field.Name + "'");
            }

            return;
        }

        if (member is IMethodSymbol method
            && method.MethodKind == MethodKind.Constructor
            && IsPublic(method.DeclaredAccessibility)
            && !type.IsAbstract
            && !type.IsRecord
            && !IsInputType(type, aggregate))
        {
            Report(context, type, method, aggregateName, "it must not declare the public constructor '" + type.Name + "'");
        }
    }

    /// <summary>Analyzes the property.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The type under inspection.</param>
    /// <param name="property">The property under inspection.</param>
    /// <param name="aggregateName">The aggregate name the diagnostic reports.</param>
    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol type, IPropertySymbol property, string aggregateName)
    {
        if (property.SetMethod is not null
            && !property.SetMethod.IsInitOnly
            && IsPublic(property.DeclaredAccessibility)
            && IsPublic(property.SetMethod.DeclaredAccessibility))
        {
            Report(context, type, property.SetMethod, aggregateName, "it must not declare the public setter '" + property.Name + "'");
        }

        if (IsPublic(property.DeclaredAccessibility)
            && property.Type is INamedTypeSymbol propertyType
            && IsMutableCollection(propertyType))
        {
            Report(context, type, property, aggregateName, "it must not expose the mutable collection '" + property.Name + "'");
        }
    }

    /// <summary>Reports the mutation surface on the member's name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The type under inspection.</param>
    /// <param name="member">The member under inspection.</param>
    /// <param name="aggregateName">The aggregate name the diagnostic reports.</param>
    /// <param name="finding">The mutation the message names.</param>
    private static void Report(
        SymbolAnalysisContext context, INamedTypeSymbol type, ISymbol member, string aggregateName, string finding)
    {
        foreach (var reference in member.DeclaringSyntaxReferences)
        {
            if (NameLocationOf(reference.GetSyntax()) is Location location)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1015, location, type.Name, aggregateName, finding));
                return;
            }
        }
    }

    /// <summary>Returns the name location of a member declaration: the keyword of a setter, or the identifier; otherwise null.</summary>
    /// <param name="syntax">The member declaration.</param>
    /// <returns>The name location, or null when the declaration names none.</returns>
    private static Location? NameLocationOf(SyntaxNode syntax) =>
        syntax switch
        {
            AccessorDeclarationSyntax accessor => accessor.Keyword.GetLocation(),
            ConstructorDeclarationSyntax constructor => constructor.Identifier.GetLocation(),
            PropertyDeclarationSyntax property => property.Identifier.GetLocation(),
            VariableDeclaratorSyntax field => field.Identifier.GetLocation(),
            _ => null,
        };

    /// <summary>Returns true when the type is an input of the aggregate root's factories or methods; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="aggregate">The aggregate folder holding the type.</param>
    /// <returns>True when the root takes the type as a parameter.</returns>
    private static bool IsInputType(INamedTypeSymbol type, string aggregate)
    {
        foreach (var candidate in type.ContainingNamespace.GetTypeMembers())
        {
            if (!Aggregates.IsRoot(candidate) || !Aggregates.DeclaredIn(candidate, aggregate))
            {
                continue;
            }

            foreach (var member in candidate.GetMembers())
            {
                if (member is IMethodSymbol method && !method.IsImplicitlyDeclared)
                {
                    foreach (var parameter in method.Parameters)
                    {
                        if (parameter.Type is INamedTypeSymbol input
                            && SymbolEqualityComparer.Default.Equals(input.OriginalDefinition, type.OriginalDefinition))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is a mutable collection; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the original definition names a mutable collection.</returns>
    private static bool IsMutableCollection(INamedTypeSymbol type)
    {
        var name = type.OriginalDefinition.Name;
        foreach (var mutable in MutableCollections)
        {
            if (name.Equals(mutable, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the accessibility is public; otherwise false.</summary>
    /// <param name="accessibility">The declared accessibility.</param>
    /// <returns>True when the accessibility is public.</returns>
    private static bool IsPublic(Accessibility accessibility) => accessibility == Accessibility.Public;

    /// <summary>Returns the first source file declaring the type, or null for metadata.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The source file path, or null when the type comes from metadata.</returns>
    private static string? FirstSourceFile(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (!string.IsNullOrEmpty(reference.SyntaxTree.FilePath))
            {
                return reference.SyntaxTree.FilePath;
            }
        }

        return null;
    }
}
