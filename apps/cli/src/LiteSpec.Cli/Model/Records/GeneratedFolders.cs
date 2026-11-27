using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Model.Records;

/// <summary>
///     The folders under a record root that tools write and no person authors: the model and the book the builder
///     writes, and the report folders of the verify, audit and explore commands. No record, page or placement check
///     reads them.
/// </summary>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
internal static class GeneratedFolders
{
    /// <summary>Gets the generated folder names, each directly under a record root.</summary>
    internal static IReadOnlySet<string> Names { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "model", "book", "verify", "audit", "explore",
    };

    /// <summary>Whether the file sits in a generated folder under one of the record roots.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="file">The file to test.</param>
    /// <returns>True when the file is tool output under a generated folder.</returns>
    internal static bool Holds(ConsumerConfig config, string file)
    {
        foreach (var root in config.RecordRoots)
        {
            foreach (var folder in Names)
            {
                var prefix = Path.GetFullPath(Path.Combine(root, folder)) + Path.DirectorySeparatorChar;

                if (file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
