using System;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     The public queries of one compilation: which Contracts file declares a public query
///     or result. A Contracts project publishes data grouped by aggregate: each top folder
///     below the Contracts root is an aggregate folder holding identifiers and facts, and a
///     type in a <c>{UseCase}/</c> folder below one is a public query or result. A module-level
///     public query sits in its own folder directly below the root, so a type directly in a
///     top folder whose name ends in <c>Query</c> or contains <c>Result</c> is a public query
///     or result too. The Contracts root is the nested <c>Contracts/</c> folder or the sibling
///     <c>{Product}.{Module}.Contracts</c> project folder, whichever the path holds.
/// </summary>
/// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
internal static class Contracts
{
    /// <summary>Returns true when the file declares a public query or result of a Contracts project; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="typeName">The simple name of the type the file declares.</param>
    /// <returns>True when the file sits in a use case folder below an aggregate folder, or directly in a top folder with a query or result name.</returns>
    public static bool IsPublicQuery(string? filePath, string typeName)
    {
        if (DepthBelowRootOf(filePath) is not int depth || depth < 1)
        {
            return false;
        }

        if (depth > 1)
        {
            return true;
        }

        return typeName.EndsWith("Query", StringComparison.Ordinal) || typeName.Contains("Result");
    }

    /// <summary>Returns how many folders below the Contracts root hold the file, or null when no Contracts root does.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The folder depth below the Contracts root, or null when the file sits outside every Contracts root.</returns>
    private static int? DepthBelowRootOf(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var segments = filePath!.Split('/', '\\');
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (segments[index].Equals("Contracts", StringComparison.Ordinal) || CodeMap.IsContractsProjectName(segments[index]))
            {
                return segments.Length - 2 - index;
            }
        }

        return null;
    }
}
