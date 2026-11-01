using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1012: an aggregate identity is a readonly record struct wrapping one Guid with no implicit conversion.</summary>
/// <follows>litespec/backend/dotnet/domain#use-strongly-typed-version-7-identifiers</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedIdentifierAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1012);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the typed identifier analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var guid = context.Compilation.GetTypeByMetadataName("System.Guid");
        var reported = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
        context.RegisterSymbolAction(symbol => Analyze(symbol, guid, reported), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="guid">The guid supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="reported">The reported this operation works with.</param>
    private static void Analyze(
        SymbolAnalysisContext context, INamedTypeSymbol? guid, ConcurrentDictionary<ISymbol, byte> reported)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.DeclaringSyntaxReferences.Length == 0)
        {
            return;
        }

        if (type.TypeKind == TypeKind.Struct && type.Name.EndsWith("Id", StringComparison.Ordinal)
            && !IsWellFormedIdentifier(type, guid))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1012, type.Locations[0], type.Name));
        }

        AnalyzeTypeArgument(context, type, guid, reported);
    }

    /// <summary>Analyzes the type argument.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The type under inspection.</param>
    /// <param name="guid">The guid supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="reported">The reported this operation works with.</param>
    private static void AnalyzeTypeArgument(
        SymbolAnalysisContext context, INamedTypeSymbol type, INamedTypeSymbol? guid, ConcurrentDictionary<ISymbol, byte> reported)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        var baseType = type.BaseType;
        if (baseType == null || baseType.TypeArguments.Length != 1)
        {
            return;
        }

        var definition = baseType.OriginalDefinition;
        if (definition.TypeKind != TypeKind.Class
            || definition.Arity != 1
            || (definition.Name != "AggregateRoot" && definition.Name != "Entity"))
        {
            return;
        }

        var identity = baseType.TypeArguments[0];
        if (guid != null && SymbolEqualityComparer.Default.Equals(identity, guid))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1012, BaseTypeLocation(type), "Guid"));
            return;
        }

        if (identity is INamedTypeSymbol identityType
            && identityType.DeclaringSyntaxReferences.Length != 0
            && (identityType.TypeKind != TypeKind.Struct || !identityType.Name.EndsWith("Id", StringComparison.Ordinal))
            && !IsWellFormedIdentifier(identityType, guid)
            && reported.TryAdd(identityType, 0))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1012, identityType.Locations[0], identityType.Name));
        }
    }

    /// <summary>Returns true when the type is well formed identifier; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="guid">The guid supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the type is well formed identifier; otherwise false.</returns>
    private static bool IsWellFormedIdentifier(INamedTypeSymbol type, INamedTypeSymbol? guid)
    {
        if (!type.IsRecord || type.TypeKind != TypeKind.Struct || !type.IsReadOnly)
        {
            return false;
        }

        var fields = 0;
        foreach (var member in type.GetMembers())
        {
            if (member is IFieldSymbol field && !field.IsStatic)
            {
                if (fields != 0 || guid == null || !SymbolEqualityComparer.Default.Equals(field.Type, guid))
                {
                    return false;
                }

                fields++;
            }
        }

        if (fields == 0)
        {
            return false;
        }

        foreach (var member in type.GetMembers("op_Implicit"))
        {
            if (member is IMethodSymbol method && method.MethodKind == MethodKind.Conversion && !method.IsImplicitlyDeclared)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns the type location.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The type location.</returns>
    private static Location BaseTypeLocation(INamedTypeSymbol type)
    {
        if (type.DeclaringSyntaxReferences.Length != 0
            && type.DeclaringSyntaxReferences[0].GetSyntax() is BaseTypeDeclarationSyntax declaration
            && declaration.BaseList != null)
        {
            foreach (var baseType in declaration.BaseList.Types)
            {
                var name = baseType.Type is QualifiedNameSyntax qualified ? qualified.Right : baseType.Type;
                if (name is GenericNameSyntax generic
                    && (generic.Identifier.Text == "AggregateRoot" || generic.Identifier.Text == "Entity"))
                {
                    return baseType.Type.GetLocation();
                }
            }
        }

        return type.Locations[0];
    }
}
