using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Model.Records;

/// <summary>
///     Where a record file sits in the consumer: the folder it is in, relative to the
///     consumer root, and the module folder it is under.
/// </summary>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
static class RecordPlacement
{
    /// <summary>The folder a record file sits in, relative to the consumer root with forward slashes.</summary>
    /// <param name="file">The absolute record file path, or empty for a derived record.</param>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <returns>The folder path, or an empty string when the record has no file.</returns>
    public static string FolderOf(string file, ConsumerConfig config)
    {
        var folder = file.Length == 0 ? null : Path.GetDirectoryName(file);

        return folder is null
            ? ""
            : Path.GetRelativePath(config.Root, folder).Replace('\\', '/');
    }

    /// <summary>The module folder a record file sits under.</summary>
    /// <param name="file">The absolute record file path, or empty for a derived record.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The name of the first module folder above the file, or null when no module folder holds it.</returns>
    public static string? ModuleOf(string file, ConsumerConfig config)
    {
        if (file.Length == 0)
        {
            return null;
        }

        var segments = RelativeToRoot(file, config).Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "modules", StringComparison.Ordinal))
            {
                return segments[at + 1];
            }
        }

        return null;
    }

    /// <summary>The file path relative to its record root, or the file itself outside every root.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The path relative to the record root holding the file, or the file itself.</returns>
    static string RelativeToRoot(string file, ConsumerConfig config)
    {
        foreach (var root in config.RecordRoots)
        {
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return file[prefix.Length..];
            }
        }

        return file;
    }
}
