namespace LiteSpec.Model;

/// <summary>
///     Where a consumer's LiteSpec files live: every LiteSpec file in a consumer
///     repository sits under one folder, <c>.litespec/</c>, at the repository root.
///     The project file is <c>.litespec/project.json</c>; a command finds the
///     consumer by walking up from its starting folder to the first folder that
///     holds that file.
/// </summary>
/// <follows>litespec/core/scope#keep-every-litespec-file-under-one-folder</follows>
static class ConsumerRoot
{
    /// <summary>The folder under the repository root that holds every LiteSpec file.</summary>
    public const string FolderName = ".litespec";

    /// <summary>The project file name inside the LiteSpec folder.</summary>
    public const string ProjectFileName = "project.json";

    /// <summary>The project file path as written in messages, relative to the repository root.</summary>
    public const string ProjectFileDisplay = ".litespec/project.json";

    /// <summary>The absolute path of the project file for a consumer root.</summary>
    /// <param name="root">The consumer repository root.</param>
    /// <returns>The project file path under the root's LiteSpec folder.</returns>
    public static string ProjectFile(string root) => Path.Combine(root, FolderName, ProjectFileName);

    /// <summary>
    ///     The consumer root found by walking up from a starting folder to the first
    ///     folder holding <c>.litespec/project.json</c>, or the starting folder itself
    ///     when no folder above it holds one, so the caller reports the place it looked.
    /// </summary>
    /// <param name="start">The folder the command starts from.</param>
    /// <returns>The consumer root, or the full starting folder when none is found above it.</returns>
    public static string Find(string start)
    {
        var folder = new DirectoryInfo(Path.GetFullPath(start));

        for (var current = folder; current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, FolderName, ProjectFileName)))
            {
                return current.FullName;
            }
        }

        return folder.FullName;
    }

    /// <summary>
    ///     The consumer root a command works on: an explicit root names the place to
    ///     start the upward search, and no root starts from the working directory.
    /// </summary>
    /// <param name="explicitRoot">The --root value, or null when it was not passed.</param>
    /// <returns>The consumer root found from that start.</returns>
    public static string Resolve(string? explicitRoot) =>
        Find(string.IsNullOrEmpty(explicitRoot) ? Directory.GetCurrentDirectory() : explicitRoot);
}
