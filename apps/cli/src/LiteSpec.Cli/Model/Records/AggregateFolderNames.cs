using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Records;

/// <summary>
///     The aggregate folder records and code take: everything of one aggregate sits in a folder named
///     for the aggregate in the plural, in the record tree and in every layer. The plural comes from
///     the name part of the aggregate id, so the expected folder stays right even when the record
///     tree drifts.
/// </summary>
/// <follows>litespec/core/model#write-every-record-in-its-home</follows>
static class AggregateFolderNames
{
    /// <summary>The folder an aggregate record owns: the aggregates folder its file sits in, or null outside one.</summary>
    /// <param name="file">The absolute record file path.</param>
    /// <returns>The absolute aggregates folder path, or null when no aggregates folder holds the file.</returns>
    public static string? OwnerDirectory(string file)
    {
        var folder = Path.GetDirectoryName(file);

        while (folder is not null)
        {
            var parent = Path.GetDirectoryName(folder);

            if (parent is null)
            {
                return null;
            }

            if (string.Equals(Path.GetFileName(parent), "aggregates", StringComparison.Ordinal))
            {
                return folder;
            }

            folder = parent;
        }

        return null;
    }

    /// <summary>
    ///     Resolves the aggregate folder for records under one aggregates folder: whether an aggregate
    ///     record owns the folder, and the expected folder name. The expected name is the plural of the
    ///     owning aggregate, else the plural of the declared aggregate the folder names, else the folder
    ///     as written, so a folder no aggregate declares is left to the checks that own it.
    /// </summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="folder">The aggregate folder name as written.</param>
    /// <param name="ownerDirectory">The absolute aggregates folder path the records sit under.</param>
    /// <returns>Whether an aggregate record owns the folder, and the expected folder name.</returns>
    public static (bool Owned, string Expected) Resolve(ProjectModel model, string folder, string ownerDirectory)
    {
        if (model.AggregatesByFolder.TryGetValue(ownerDirectory, out var owner))
        {
            if (Identifiers.TrySplit(owner.Id, out _, out _, out var ownerName) && ownerName.Length > 0)
            {
                return (true, Naming.PluralKebab(ownerName));
            }

            return (true, folder);
        }

        string? named = null;

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate || record.Derived)
            {
                continue;
            }

            if (!UnderSameModule(record.File, ownerDirectory))
            {
                continue;
            }

            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            if (string.Equals(Naming.PluralKebab(name), folder, StringComparison.Ordinal))
            {
                return (false, folder);
            }

            named ??= string.Equals(name, folder, StringComparison.Ordinal) ? Naming.PluralKebab(name) : null;
        }

        return (false, named ?? folder);
    }

    /// <summary>Whether the aggregate file sits under the aggregates folder of the module holding the owner directory.</summary>
    /// <param name="file">The absolute aggregate file path.</param>
    /// <param name="ownerDirectory">The absolute aggregates folder path the records sit under.</param>
    /// <returns>True when the file sits under the same module's aggregates folder; otherwise false.</returns>
    private static bool UnderSameModule(string file, string ownerDirectory)
    {
        var moduleDirectory = Path.GetDirectoryName(Path.GetDirectoryName(ownerDirectory));

        if (moduleDirectory is null)
        {
            return false;
        }

        var prefix = Path.Combine(moduleDirectory, "aggregates") + Path.DirectorySeparatorChar;

        return file.StartsWith(prefix, StringComparison.Ordinal);
    }
}
