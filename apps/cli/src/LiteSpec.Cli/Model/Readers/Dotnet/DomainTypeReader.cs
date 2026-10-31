using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Reads every top-level Domain type of one file into the model.</summary>
/// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
static class DomainTypeReader
{
    /// <summary>
    ///     Reads every top-level Domain type of one file into the model: its name, namespace,
    ///     file, line, and whether it carries a placing mark. Only files under the Domain root
    ///     are read. Nested types, generated types, enumerations and delegates are left out:
    ///     enumerations and delegates are not type declarations, so the type walk skips them.
    /// </summary>
    /// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="model">The code model a domain type adds to.</param>
    public static void ReadDomainTypes(ConsumerConfig config, string file, SyntaxTree tree, CodeModel model)
    {
        if (!SourceRoots.IsUnder(config.DomainRoot, file))
        {
            return;
        }

        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (declared.Parent is TypeDeclarationSyntax)
            {
                continue;
            }

            if (GeneratedCodeFacts.IsGeneratedFileName(file) || GeneratedCodeFacts.HasGeneratedCode(declared))
            {
                continue;
            }

            var placed = MarkReader.Read(declared).Any(mark => mark.Verb != MarkVerb.Follows);

            model.DomainTypes.Add(new DomainType(
                declared.Identifier.ValueText,
                DotnetNames.NamespaceOf(declared),
                file,
                tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1,
                placed));
        }
    }

    /// <summary>
    ///     Merges the partial parts of one Domain type into one entry: parts count as one type
    ///     and a mark on any part counts. The merged entry keeps the first file in ordinal path
    ///     order. Parts group by namespace and type name.
    /// </summary>
    /// <param name="types">The domain types.</param>
    /// <returns>The domain types with the partial parts of one type merged.</returns>
    public static List<DomainType> MergeDomainPartials(List<DomainType> types)
    {
        var merged = new List<DomainType>();
        var parts = new Dictionary<string, List<DomainType>>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            var key = type.Namespace + "\0" + type.Name;

            if (!parts.TryGetValue(key, out var group))
            {
                group = [];
                parts[key] = group;
            }

            group.Add(type);
        }

        foreach (var group in parts.Values)
        {
            var first = group
                .OrderBy(part => part.File, StringComparer.Ordinal)
                .ThenBy(part => part.Line)
                .First();

            merged.Add(first with { HasMark = group.Any(part => part.HasMark) });
        }

        return merged
            .OrderBy(type => type.File, StringComparer.Ordinal)
            .ThenBy(type => type.Line)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }
}
