using System.Formats.Tar;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Diff;

/// <summary>
///     One loaded comparison base: its model and its label. Disposing removes the
///     temporary folder of a git base; a folder base needs no cleanup.
/// </summary>
/// <follows>litespec/core/language#keep-wire-codes-stable</follows>
sealed class DiffBase : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DiffBase"/> class.
    ///     The temporary folder is removed on disposal; a folder base passes null for it.
    /// </summary>
    /// <param name="model">The base model built from the folder or revision.</param>
    /// <param name="label">The base name as the caller passed it.</param>
    /// <param name="temp">The temporary folder holding a git base, or null for a folder base.</param>
    DiffBase(ProjectModel model, string label, string? temp)
    {
        Model = model;
        Label = label;
        Temp = temp;
    }

    /// <summary>Gets the base model.</summary>
    public ProjectModel Model { get; }

    /// <summary>Gets the base name, as the caller passed it.</summary>
    public string Label { get; }

    /// <summary>Gets the temporary folder holding a git base, or null for a folder base.</summary>
    string? Temp { get; }

    /// <summary>Removes the temporary folder of a git base; a folder base has none to remove.</summary>
    public void Dispose()
    {
        if (Temp is null)
        {
            return;
        }

        try
        {
            Directory.Delete(Temp, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    ///     Loads the base: a consumer folder holding <c>.litespec/project.yaml</c>, or a
    ///     git revision of the consumer, read through an archive into a temporary folder.
    /// </summary>
    /// <param name="headConfig">The head consumer configuration, whose root anchors relative base paths.</param>
    /// <param name="baseArg">The base argument: a consumer folder or a git revision.</param>
    /// <returns>The loaded base, holding its model and label.</returns>
    public static DiffBase Load(ConsumerConfig headConfig, string baseArg)
    {
        if (FolderOf(headConfig, baseArg) is { } folder)
        {
            var project = ConsumerRoot.ProjectFile(folder);

            if (!File.Exists(project))
            {
                throw new InvalidOperationException($"Base '{baseArg}' holds no .litespec/project.yaml.");
            }

            return new DiffBase(
                ModelBuilder.Build(ConsumerConfig.Load(folder)), baseArg, null);
        }

        return LoadRevision(headConfig, baseArg);
    }

    /// <summary>
    ///     The consumer folder the base argument names: the path itself or the folder beside the head root, or
    ///     null when neither exists.
    /// </summary>
    /// <param name="headConfig">The head consumer configuration, whose root anchors relative base paths.</param>
    /// <param name="baseArg">The base argument: a consumer folder or a git revision.</param>
    /// <returns>The folder path the base argument names, or null when it names no existing folder.</returns>
    static string? FolderOf(ConsumerConfig headConfig, string baseArg)
    {
        var direct = Path.IsPathRooted(baseArg)
            ? baseArg
            : Path.Combine(Directory.GetCurrentDirectory(), baseArg);

        if (Directory.Exists(direct))
        {
            return direct;
        }

        if (!Path.IsPathRooted(baseArg))
        {
            var beside = Path.Combine(headConfig.Root, baseArg);

            if (Directory.Exists(beside))
            {
                return beside;
            }
        }

        return null;
    }

    /// <summary>
    ///     Loads a git revision base by archiving the revision into a temporary folder and building the model
    ///     from that copy.
    /// </summary>
    /// <param name="headConfig">The head consumer configuration, whose root anchors relative base paths.</param>
    /// <param name="baseArg">The base argument: a consumer folder or a git revision.</param>
    /// <returns>The loaded base read from the git revision archive.</returns>
    static DiffBase LoadRevision(ConsumerConfig headConfig, string baseArg)
    {
        string Neither() => $"Base '{baseArg}' is neither a consumer folder nor a git revision.";

        // The path from the root up to the repository top. Git names the top by its resolved path, which
        // differs from the root's own form when the root runs through a symbolic link, such as /var on
        // macOS, so the top is reached from the root and the root's place inside it stays exact.
        var up = ProcessRunner.Git(headConfig.Root, "rev-parse", "--show-cdup");

        if (up.Status != 0)
        {
            throw new InvalidOperationException(Neither());
        }

        var toplevel = Path.GetFullPath(Path.Combine(headConfig.Root, up.Stdout.Trim()));
        var resolved = ProcessRunner.Git(toplevel, "rev-parse", "--verify", "--quiet", $"{baseArg}^{{commit}}");

        if (resolved.Status != 0 || resolved.Stdout.Trim().Length == 0)
        {
            throw new InvalidOperationException(Neither());
        }

        var archived = ProcessRunner.Git(toplevel, "archive", "--format=tar", resolved.Stdout.Trim());

        if (archived.Status != 0)
        {
            throw new InvalidOperationException($"Base '{baseArg}' could not be read: {archived.Error.Trim()}.");
        }

        var temp = Directory.CreateTempSubdirectory("litespec-base-").FullName;

        try
        {
            using var archive = new MemoryStream(archived.Output);

            TarFile.ExtractToDirectory(archive, temp, overwriteFiles: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Directory.Delete(temp, recursive: true);
            throw new InvalidOperationException($"Base '{baseArg}' could not be read: {exception.Message}.");
        }

        var relative = PathText.Relative(toplevel, headConfig.Root);
        var root = relative is "." or "" ? temp : Path.Combine(temp, relative);

        if (!File.Exists(ConsumerRoot.ProjectFile(root)))
        {
            Directory.Delete(temp, recursive: true);
            throw new InvalidOperationException($"Base '{baseArg}' holds no .litespec/project.yaml.");
        }

        return new DiffBase(ModelBuilder.Build(ConsumerConfig.Load(root)), baseArg, temp);
    }
}
