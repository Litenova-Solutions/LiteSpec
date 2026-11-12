using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>Judges the namespace a code file declares against the placement path its record states, reading each file's namespace once.</summary>
/// <follows>litespec/core/model#match-each-backend-mark-to-its-folder</follows>
internal static class NamespacePlacement
{
    /// <summary>Namespace segments that state no placement: layers, plumbing, and record mirrors.</summary>
    internal static readonly HashSet<string> PlacelessSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Abstractions", "Infrastructure", "WebApi", "Worker",
        "Persistence", "Endpoints", "Contracts", "Events", "Exceptions",
        "Identifiers", "IntegrationEvents", "Commands", "Ports",
        "UseCases", "Composition", "Jobs", "Tests", "Test", "Acceptance", "Integration", "Unit",
    };

    /// <summary>
    ///     Whether the declared namespace segments carry each expected segment in order, with only layer and
    ///     test folder names before or between them. A module or aggregate named like such a folder, such as
    ///     an Events module, still counts as its own segment.
    /// </summary>
    /// <param name="placed">The declared namespace segments after the product.</param>
    /// <param name="expected">The expected segments in placement order, without the product.</param>
    /// <returns>True when the placed segments carry each expected segment in order.</returns>
    internal static bool NamespaceMatches(List<string> placed, List<string> expected)
    {
        var at = 0;

        foreach (var segment in expected)
        {
            while (at < placed.Count && !SameName(placed[at], segment) && PlacelessSegments.Contains(placed[at]))
            {
                at++;
            }

            if (at == placed.Count || !SameName(placed[at], segment))
            {
                return false;
            }

            at++;
        }

        return true;
    }

    /// <summary>The declared namespace segments after the product.</summary>
    /// <param name="product">The product name starting the namespace.</param>
    /// <param name="actual">The declared namespace as written in the file.</param>
    /// <returns>The segments after the product, or null when the namespace is empty or starts outside the product.</returns>
    internal static List<string>? PlacedSegments(string product, string actual)
    {
        var segments = actual.Split('.').Where(segment => segment.Length > 0).ToList();

        if (segments.Count == 0 || !SameName(segments[0], product))
        {
            return null;
        }

        return segments.Skip(1).ToList();
    }

    /// <summary>The declared namespace of the file, or an empty string when the file cannot be read or declares none.</summary>
    /// <param name="file">The full path of the file whose namespace is wanted.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <returns>The declared namespace, or an empty string when none could be read.</returns>
    internal static string NamespaceOf(string file, Dictionary<string, string> namespaces)
    {
        if (namespaces.TryGetValue(file, out var known))
        {
            return known;
        }

        var found = ParseNamespace(file);
        namespaces[file] = found;

        return found;
    }

    /// <summary>Reads the declared namespace of each file in parallel, so the placement checks that follow find every one read.</summary>
    /// <param name="files">The full paths of the files whose namespaces the checks will look up.</param>
    /// <returns>The declared namespace per file, empty where a file cannot be read or declares none.</returns>
    internal static Dictionary<string, string> NamespacesOf(IEnumerable<string> files)
    {
        var distinct = files.Distinct(StringComparer.Ordinal).ToList();
        var found = new string[distinct.Count];

        Parallel.For(0, distinct.Count, index => found[index] = ParseNamespace(distinct[index]));

        var namespaces = new Dictionary<string, string>(distinct.Count, StringComparer.Ordinal);

        for (var index = 0; index < distinct.Count; index++)
        {
            namespaces[distinct[index]] = found[index];
        }

        return namespaces;
    }

    /// <summary>Parses the declared namespace of one file.</summary>
    /// <param name="file">The full path of the file whose namespace is wanted.</param>
    /// <returns>The file-scoped namespace, the nested block namespaces joined by dots, or an empty string when the file cannot be read or declares none.</returns>
    private static string ParseNamespace(string file)
    {
        try
        {
            if (!File.Exists(file))
            {
                return "";
            }

            // A namespace is declared only in the compilation unit or inside another namespace.
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var spaces = tree.GetRoot()
                .DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .OfType<BaseNamespaceDeclarationSyntax>()
                .ToList();

            if (spaces.FirstOrDefault(space => space is FileScopedNamespaceDeclarationSyntax) is { } scoped)
            {
                return scoped.Name.ToString();
            }

            return spaces.Count > 0 ? string.Join(".", spaces.Select(space => space.Name.ToString())) : "";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "";
        }
    }
}
