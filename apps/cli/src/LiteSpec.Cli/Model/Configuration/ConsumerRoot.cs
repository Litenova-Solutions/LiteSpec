using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     Where a consumer's LiteSpec files live: every LiteSpec file in a consumer
///     repository sits under one folder, <c>.litespec/</c>, at the repository root.
///     The project file is <c>.litespec/project.yaml</c>. Every command finds its
///     consumer through this type: <c>--root</c> (or the working directory) is the
///     place to start, and a command that reads a consumer walks up from that place
///     to the first folder holding the project file. <c>init</c> alone writes into
///     the start folder itself.
/// </summary>
/// <follows>litespec/core/scope#keep-every-litespec-file-under-one-folder</follows>
static class ConsumerRoot
{
    /// <summary>The folder under the repository root that holds every LiteSpec file.</summary>
    public const string FolderName = ".litespec";

    /// <summary>The project file name inside the LiteSpec folder.</summary>
    public const string ProjectFileName = "project.yaml";

    /// <summary>The project file path as written in messages, relative to the repository root.</summary>
    public const string ProjectFileDisplay = ".litespec/project.yaml";

    /// <summary>The absolute path of the project file for a consumer root.</summary>
    /// <param name="root">The consumer repository root.</param>
    /// <returns>The project file path under the root's LiteSpec folder.</returns>
    public static string ProjectFile(string root) => Path.Combine(root, FolderName, ProjectFileName);

    /// <summary>
    ///     The consumer root a command works on: the first folder, from the start
    ///     folder upward, that holds <c>.litespec/project.yaml</c>. The start folder
    ///     is <c>--root</c>, or the working directory when no root was passed.
    ///     Callers use <see cref="Locate" /> or <see cref="Open" />, which report
    ///     a missing project file; this step only finds the folder.
    /// </summary>
    /// <param name="explicitRoot">The --root value, or null when it was not passed.</param>
    /// <returns>The consumer root, or the full start folder when no folder above it holds a project file.</returns>
    static string Resolve(string? explicitRoot)
    {
        var start = new DirectoryInfo(Path.GetFullPath(string.IsNullOrEmpty(explicitRoot) ? CliHost.WorkingDirectory : explicitRoot));

        for (var current = start; current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, FolderName, ProjectFileName)))
            {
                return current.FullName;
            }
        }

        return start.FullName;
    }

    /// <summary>
    ///     Finds the folder a command works on and checks it. With <paramref name="requireProject" />,
    ///     the folder is the consumer found by walking up from the start folder, and it must exist and
    ///     hold the project file. Without it, the folder is the start folder itself, literally, for a
    ///     command that creates the project. A failure is written through <see cref="CommandFailure.Fail" />
    ///     as one message, with exit code 2.
    /// </summary>
    /// <param name="rootOption">The --root value, or null when it was not passed.</param>
    /// <param name="requireProject">Whether the command reads an existing consumer.</param>
    /// <param name="json">True when the caller requested JSON output, so a failure is written as the JSON object.</param>
    /// <param name="exitCode">The exit code for the caller to return when the folder is refused; 0 otherwise.</param>
    /// <returns>The consumer folder, or null when the folder is refused.</returns>
    public static string? Locate(string? rootOption, bool requireProject, bool json, out int exitCode)
    {
        exitCode = 0;
        string root;

        try
        {
            root = requireProject
                ? Resolve(rootOption)
                : Path.GetFullPath(string.IsNullOrEmpty(rootOption) ? CliHost.WorkingDirectory : rootOption);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            exitCode = CommandFailure.Fail($"Invalid root '{rootOption}': {exception.Message}", json, 2);
            return null;
        }

        if (!requireProject)
        {
            return root;
        }

        if (!Directory.Exists(root))
        {
            exitCode = CommandFailure.Fail($"Root '{root}' does not exist.", json, 2);
            return null;
        }

        // Without a project file every path falls back to a default, so a command
        // would read or write a folder that is not a consumer at all.
        if (!File.Exists(ProjectFile(root)))
        {
            exitCode = CommandFailure.Fail(NoProjectMessage(root), json, 2);
            return null;
        }

        return root;
    }

    /// <summary>
    ///     Finds the consumer a command reads, and loads its declaration. Every command
    ///     that reads a consumer calls this, so the folder rule and the project-file
    ///     messages are the same everywhere. A failure is written through
    ///     <see cref="CommandFailure.Fail" /> as one message, with exit code 2.
    /// </summary>
    /// <param name="rootOption">The --root value, or null when it was not passed.</param>
    /// <param name="json">True when the caller requested JSON output, so a failure is written as the JSON object.</param>
    /// <param name="exitCode">The exit code for the caller to return when the consumer is refused; 0 otherwise.</param>
    /// <returns>The loaded declaration, or null when the consumer is refused.</returns>
    public static ConsumerConfig? Open(string? rootOption, bool json, out int exitCode)
    {
        var root = Locate(rootOption, requireProject: true, json, out exitCode);

        if (root is null)
        {
            return null;
        }

        try
        {
            return ConsumerConfig.Load(root);
        }
        catch (JsonTextException exception)
        {
            exitCode = CommandFailure.Fail(InvalidProjectMessage(exception.Message), json, 2);
        }
        catch (InvalidOperationException exception)
        {
            exitCode = CommandFailure.Fail(exception.Message, json, 2);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            exitCode = CommandFailure.Fail($"Cannot read {ProjectFileDisplay}: {exception.Message}", json, 2);
        }

        return null;
    }

    /// <summary>The message for a folder that holds no project file.</summary>
    /// <param name="root">The folder that was checked.</param>
    /// <returns>The message naming the folder and the missing project file.</returns>
    public static string NoProjectMessage(string root) => $"Root '{root}' holds no {ProjectFileDisplay}.";

    /// <summary>The message for a project file that is not valid YAML or JSON.</summary>
    /// <param name="detail">The parser's own message.</param>
    /// <returns>The message naming the project file and the parser's detail.</returns>
    public static string InvalidProjectMessage(string detail) => $"{ProjectFileDisplay} is not valid YAML or JSON: {detail}";
}
