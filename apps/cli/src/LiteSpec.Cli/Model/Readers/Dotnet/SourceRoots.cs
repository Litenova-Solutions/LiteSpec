namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>The backend source roots of a consumer, as the reader walks them.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class SourceRoots
{
    /// <summary>The short project name holding the file, or null when it sits under no backend root.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file path.</param>
    /// <returns>The short project name holding the file, or null when it sits under no backend root.</returns>
    public static string? ProjectOf(ConsumerConfig config, string file)
    {
        var abstractions = config.ApplicationRoot + ".Abstractions";

        if (IsUnder(abstractions, file))
        {
            return FolderName(abstractions);
        }

        if (IsUnder(config.ApplicationRoot, file))
        {
            return FolderName(config.ApplicationRoot);
        }

        if (IsUnder(config.DomainRoot, file))
        {
            return FolderName(config.DomainRoot);
        }

        if (IsUnder(config.WebApiRoot, file))
        {
            return FolderName(config.WebApiRoot);
        }

        if (IsUnder(config.InfrastructureRoot, file))
        {
            return FolderName(config.InfrastructureRoot);
        }

        if (IsUnder(config.WorkerRoot, file))
        {
            return FolderName(config.WorkerRoot);
        }

        return null;
    }

    /// <summary>Whether the file sits under the root.</summary>
    /// <param name="root">The source root.</param>
    /// <param name="file">The file path.</param>
    /// <returns>True when the file sits under the root.</returns>
    public static bool IsUnder(string root, string file)
    {
        if (root.Length == 0)
        {
            return false;
        }

        var relative = Path.GetRelativePath(root, file);

        return !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>The folder name of a source root: the short project name.</summary>
    /// <param name="root">The source root.</param>
    /// <returns>The folder name of a source root.</returns>
    public static string FolderName(string root) =>
        Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
