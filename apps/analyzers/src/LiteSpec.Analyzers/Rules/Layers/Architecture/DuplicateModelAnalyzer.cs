using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2026: two use case folders of one module declare no record with the same name and members.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateModelAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2026);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the duplicate model analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterCompilationEndAction(c => Analyze(c, scope, map));
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void Analyze(CompilationAnalysisContext context, FileScope scope, CodeMap map)
    {
        var records = new List<INamedTypeSymbol>();
        CollectTypes(context.Compilation.Assembly.GlobalNamespace, records);

        var groups = new Dictionary<string, List<UseCaseRecord>>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (!IsUseCaseRecord(record, scope, map))
            {
                continue;
            }

            var key = KeyOf(record, map);
            if (!groups.TryGetValue(key, out var group))
            {
                group = new List<UseCaseRecord>();
                groups[key] = group;
            }

            group.Add(new UseCaseRecord(record, FirstSourceFile(record)!, map.UseCaseOf(FirstSourceFile(record)!)!));
        }

        foreach (var group in groups.Values)
        {
            if (group.Count < 2)
            {
                continue;
            }

            group.Sort(static (left, right) =>
            {
                var files = string.Compare(left.File, right.File, StringComparison.Ordinal);
                return files != 0
                    ? files
                    : left.Record.Locations[0].SourceSpan.Start.CompareTo(right.Record.Locations[0].SourceSpan.Start);
            });

            var keeper = group[0];
            var fix = FixOf(keeper.Record, map, context.Compilation);
            for (var index = 1; index < group.Count; index++)
            {
                var duplicate = group[index];
                if (duplicate.UseCase.Equals(keeper.UseCase, StringComparison.Ordinal))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2026,
                    duplicate.Record.Locations[0],
                    duplicate.Record.Name,
                    CodeMap.FolderName(duplicate.UseCase),
                    CodeMap.FolderName(keeper.UseCase),
                    fix));
            }
        }
    }

    /// <summary>Returns the fix clause: the shared value when one matches, or the aggregate folder.</summary>
    /// <param name="keeper">The first declaration of the duplicated model.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The fix clause of the diagnostic message.</returns>
    private static string FixOf(INamedTypeSymbol keeper, CodeMap map, Compilation compilation)
    {
        if (SharedValueOf(keeper, map, compilation) is string shared)
        {
            return "; reference the shared value '" + shared + "' instead";
        }

        return "; declare it once beside the aggregate instead";
    }

    /// <summary>Returns the shared value matching the model, or null when none does.</summary>
    /// <param name="keeper">The first declaration of the duplicated model.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The shared value display name, or null when none matches.</returns>
    private static string? SharedValueOf(INamedTypeSymbol keeper, CodeMap map, Compilation compilation)
    {
        var signature = SignatureOf(keeper);
        var candidates = new List<INamedTypeSymbol>();
        CollectTypes(compilation.Assembly.GlobalNamespace, candidates);
        foreach (var candidate in candidates)
        {
            if (candidate.Name.Equals(keeper.Name, StringComparison.Ordinal)
                && !SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, keeper.OriginalDefinition)
                && SignatureOf(candidate).Equals(signature, StringComparison.Ordinal)
                && FirstSourceFile(candidate) is string file
                && map.ModuleOf(file) is null
                && map.LayerOf(file) == ArchitectureLayer.Domain)
            {
                return candidate.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly
                || !IsKernelAssembly(assembly.Name))
            {
                continue;
            }

            var matches = new List<INamedTypeSymbol>();
            CollectByName(assembly.GlobalNamespace, keeper.Name, matches);
            foreach (var match in matches)
            {
                if (SignatureOf(match).Equals(signature, StringComparison.Ordinal))
                {
                    return match.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                }
            }
        }

        return null;
    }

    /// <summary>Returns true when the type is a record declared in a use case folder; otherwise false.</summary>
    /// <param name="record">The type under inspection.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <returns>True when the record sits in a use case of a production layer.</returns>
    private static bool IsUseCaseRecord(INamedTypeSymbol record, FileScope scope, CodeMap map)
    {
        if (!record.IsRecord || record.IsImplicitlyDeclared || record.DeclaringSyntaxReferences.Length == 0)
        {
            return false;
        }

        return scope.AppliesTo(record)
            && FirstSourceFile(record) is string file
            && map.UseCaseOf(file) is not null;
    }

    /// <summary>Returns the group key: the module, the simple name and the member signature.</summary>
    /// <param name="record">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <returns>The group key.</returns>
    private static string KeyOf(INamedTypeSymbol record, CodeMap map)
    {
        var file = FirstSourceFile(record);
        var module = file is null ? string.Empty : map.ModuleOf(file) ?? string.Empty;
        return module + "|" + record.Name + "|" + SignatureOf(record);
    }

    /// <summary>Returns the member signature: the sorted instance properties and fields with their types.</summary>
    /// <param name="record">The type under inspection.</param>
    /// <returns>The member signature.</returns>
    private static string SignatureOf(INamedTypeSymbol record)
    {
        var members = new List<string>();
        foreach (var member in record.GetMembers())
        {
            if (member.IsStatic)
            {
                continue;
            }

            if (member is IPropertySymbol property && !property.IsIndexer)
            {
                // A positional property is implicitly declared, and it is the member
                // that matters; EqualityContract is identical on every record class.
                if (!property.IsImplicitlyDeclared || !property.Name.Equals("EqualityContract", StringComparison.Ordinal))
                {
                    members.Add("P:" + property.Name + ":" + property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
            else if (member is IFieldSymbol field && !field.IsImplicitlyDeclared)
            {
                members.Add("F:" + field.Name + ":" + field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        members.Sort(StringComparer.Ordinal);
        return string.Join(";", members);
    }

    /// <summary>Collects the types.</summary>
    /// <param name="space">The namespace that contains the types under inspection.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    private static void CollectTypes(INamespaceSymbol space, List<INamedTypeSymbol> types)
    {
        foreach (var member in space.GetTypeMembers())
        {
            CollectType(member, types);
        }

        foreach (var child in space.GetNamespaceMembers())
        {
            CollectTypes(child, types);
        }
    }

    /// <summary>Collects the type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    private static void CollectType(INamedTypeSymbol type, List<INamedTypeSymbol> types)
    {
        types.Add(type);
        foreach (var nested in type.GetTypeMembers())
        {
            CollectType(nested, types);
        }
    }

    /// <summary>Collects the types with the simple name.</summary>
    /// <param name="space">The namespace that contains the types under inspection.</param>
    /// <param name="name">The simple name to match.</param>
    /// <param name="matches">The matches collected so far, in discovery order.</param>
    private static void CollectByName(INamespaceSymbol space, string name, List<INamedTypeSymbol> matches)
    {
        foreach (var member in space.GetTypeMembers(name))
        {
            matches.Add(member);
        }

        foreach (var child in space.GetNamespaceMembers())
        {
            CollectByName(child, name, matches);
        }
    }

    /// <summary>Returns true when the assembly is the business kernel; otherwise false.</summary>
    /// <param name="assemblyName">The assembly name.</param>
    /// <returns>True when the assembly is the kernel.</returns>
    private static bool IsKernelAssembly(string assemblyName) =>
        assemblyName.Equals("Kernel", StringComparison.Ordinal)
        || assemblyName.EndsWith(".Kernel", StringComparison.Ordinal)
        || assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
        || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal);

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

    /// <summary>One record declared in a use case folder: its symbol, file and use case key.</summary>
    private sealed class UseCaseRecord
    {
        /// <summary>Initializes a new instance of the <see cref="UseCaseRecord"/> class.</summary>
        /// <param name="record">The record symbol.</param>
        /// <param name="file">The source file declaring the record.</param>
        /// <param name="useCase">The use case key holding the file.</param>
        public UseCaseRecord(INamedTypeSymbol record, string file, string useCase)
        {
            Record = record;
            File = file;
            UseCase = useCase;
        }

        /// <summary>Gets the record symbol.</summary>
        public INamedTypeSymbol Record { get; }

        /// <summary>Gets the source file declaring the record.</summary>
        public string File { get; }

        /// <summary>Gets the use case key holding the file.</summary>
        public string UseCase { get; }
    }
}
