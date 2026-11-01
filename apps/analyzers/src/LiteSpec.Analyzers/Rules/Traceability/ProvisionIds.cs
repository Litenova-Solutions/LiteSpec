using System;
using System.Collections.Generic;
using System.IO;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     The provision identifiers of the LiteSpec release the package embeds at
///     build time, from <c>docs/reference/provisions.md</c>. Read once from the
///     embedded resource, so a <c>follows</c> mark naming a provision needs no
///     network or file outside the package. The analyzer checks existence only;
///     the CLI owns whether the page applies to the project.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class ProvisionIds
{
    /// <summary>Holds the provision identifiers, loaded once from the embedded index on first use.</summary>
    private static readonly Lazy<HashSet<string>> Ids = new Lazy<HashSet<string>>(Load);

    /// <summary>Whether the identifier is a provision of the carried release.</summary>
    /// <param name="id">The identifier from the mark.</param>
    /// <returns>True when the embedded index holds the identifier.</returns>
    public static bool Contains(string id) => Ids.Value.Contains(id);

    /// <summary>Loads the provision identifiers from the embedded provisions index.</summary>
    /// <returns>The provision identifiers of the carried release, or an empty set when the resource is missing.</returns>
    private static HashSet<string> Load()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        using var stream = typeof(ProvisionIds).Assembly.GetManifestResourceStream("LiteSpec.Provisions.md");

        if (stream is null)
        {
            return found;
        }

        using var reader = new StreamReader(stream);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (!line.StartsWith("| litespec/", StringComparison.Ordinal))
            {
                continue;
            }

            var end = line.IndexOf(" |", StringComparison.Ordinal);

            if (end > 2)
            {
                found.Add(line.Substring(2, end - 2));
            }
        }

        return found;
    }
}
