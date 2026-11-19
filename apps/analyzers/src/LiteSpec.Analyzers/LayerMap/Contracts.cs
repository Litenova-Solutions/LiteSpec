using System;
using System.Linq;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     The public queries and public commands of one compilation: which Contracts file declares
///     a public query, public command or result. A Contracts project publishes data grouped by
///     aggregate: each top folder below the Contracts root is an aggregate folder holding its
///     identifier, one <c>On{Fact}/</c> folder per fact with the event type and the types only it
///     carries, and one folder per public query or public command, and a type in a <c>{UseCase}/</c>
///     folder below one is a public query, public command or result. A type in an <c>On{Fact}/</c>
///     folder is a fact, never a public query, public command or result. A module-level public query
///     or public command sits in its own folder directly below the root, so a type directly in a top
///     folder whose name ends in <c>Query</c> or <c>Command</c> or contains <c>Result</c> is a public
///     query, public command or result too. The Contracts root is the nested <c>Contracts/</c> folder
///     or the sibling <c>{Product}.{Module}.Contracts</c> project folder, whichever the path holds.
/// </summary>
/// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
internal static class Contracts
{
    /// <summary>Returns true when the file declares a public query, public command or result of a Contracts project; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="typeName">The simple name of the type the file declares.</param>
    /// <returns>True when the file sits in a use case folder below an aggregate folder, or directly in a top folder with a query, command or result name; a file in an On{Fact} folder is never one.</returns>
    public static bool IsPublicQueryOrCommand(string? filePath, string typeName)
    {
        if (FoldersBelowRootOf(filePath) is not string[] folders || folders.Length < 1)
        {
            return false;
        }

        if (folders.Any(IsFactFolder))
        {
            return false;
        }

        if (folders.Length > 1)
        {
            return true;
        }

        return typeName.EndsWith("Query", StringComparison.Ordinal)
            || typeName.EndsWith("Command", StringComparison.Ordinal)
            || typeName.Contains("Result");
    }

    /// <summary>Returns true when the folder holds one fact: its name starts with On and an uppercase letter, as OnSessionEnded; otherwise false.</summary>
    /// <param name="folder">The folder name.</param>
    /// <returns>True when the folder is an On{Fact} folder; otherwise false.</returns>
    private static bool IsFactFolder(string folder) =>
        folder.Length > 2
        && folder.StartsWith("On", StringComparison.Ordinal)
        && char.IsUpper(folder[2]);

    /// <summary>Returns the folders below the Contracts root holding the file, or null when no Contracts root does.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The folder names below the Contracts root with the file name dropped, or null when the file sits outside every Contracts root.</returns>
    private static string[]? FoldersBelowRootOf(string? filePath)
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
                var folders = new string[segments.Length - 2 - index];

                for (var at = 0; at < folders.Length; at++)
                {
                    folders[at] = segments[index + 1 + at];
                }

                return folders;
            }
        }

        return null;
    }
}
