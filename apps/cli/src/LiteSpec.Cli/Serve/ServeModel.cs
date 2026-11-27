using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;

namespace LiteSpec.Cli.Serve;

/// <summary>
///     The warm project model one serve session answers from: the built model with its
///     open findings, rebuilt after a watched change under the record or code roots
///     settles for <see cref="SettleMs"/> milliseconds.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class ServeModel : IDisposable
{
    /// <summary>The quiet time after the last watched change before the model rebuilds, in milliseconds.</summary>
    public const int SettleMs = 300;

    /// <summary>The watchers on the record and code roots.</summary>
    readonly List<FileSystemWatcher> watchers = [];

    /// <summary>Whether a watched change arrived since the last rebuild.</summary>
    volatile bool dirty;

    /// <summary>The ticks of the last watched change, in coordinated universal time.</summary>
    long lastChangeTicks;

    /// <summary>Whether the watchers were started.</summary>
    bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServeModel"/> class: builds the model
    ///     once and starts the watchers.
    /// </summary>
    /// <param name="config">The consumer configuration the model is built from.</param>
    public ServeModel(ConsumerConfig config)
    {
        Config = config;
        Rebuild();
        StartWatchers();
    }

    /// <summary>Gets the consumer configuration the model is built from.</summary>
    public ConsumerConfig Config { get; }

    /// <summary>Gets the built project model the tools answer from.</summary>
    public ProjectModel Model { get; private set; } = null!;

    /// <summary>Gets the open findings the check tool answers from.</summary>
    public IReadOnlyList<Finding> Findings { get; private set; } = [];

    /// <summary>Gets the findings waivers cover, kept beside the open findings.</summary>
    public IReadOnlyList<Finding> Covered { get; private set; } = [];

    /// <summary>Gets the last reload failure, or empty when the last reload held.</summary>
    public string ReloadError { get; private set; } = "";

    /// <summary>
    ///     Rebuilds the model when a watched change arrived and settled. A failed reload
    ///     keeps the last good model, so the session keeps answering from cache.
    /// </summary>
    public void EnsureFresh()
    {
        if (!dirty)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - new DateTime(Interlocked.Read(ref lastChangeTicks), DateTimeKind.Utc);

        if (elapsed.TotalMilliseconds < SettleMs)
        {
            return;
        }

        dirty = false;

        try
        {
            Rebuild();
            ReloadError = "";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ReloadError = exception.Message;
            dirty = true;
            lastChangeTicks = DateTime.UtcNow.Ticks;
        }
    }

    /// <summary>Stops the watchers.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var watcher in watchers)
        {
            watcher.Dispose();
        }

        watchers.Clear();
    }

    /// <summary>Builds the model and reads its open findings.</summary>
    void Rebuild()
    {
        var model = ModelBuilder.Build(Config);
        var (findings, covered) = ModelCommand.Collect(model, Config.Root);
        Model = model;
        Findings = findings;
        Covered = covered;
    }

    /// <summary>Marks the model dirty: the next settled request rebuilds it.</summary>
    void MarkDirty()
    {
        lastChangeTicks = DateTime.UtcNow.Ticks;
        dirty = true;
    }

    /// <summary>Starts one watcher per record and code root that exists.</summary>
    void StartWatchers()
    {
        foreach (var root in WatchedRoots())
        {
            FileSystemWatcher watcher;

            try
            {
                watcher = new FileSystemWatcher(root)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true,
                };
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                continue;
            }

            watcher.Changed += (_, _) => MarkDirty();
            watcher.Created += (_, _) => MarkDirty();
            watcher.Deleted += (_, _) => MarkDirty();
            watcher.Renamed += (_, _) => MarkDirty();
            watchers.Add(watcher);
        }

        lastChangeTicks = DateTime.UtcNow.Ticks;
    }

    /// <summary>The roots a change rebuilds the model from: the record roots and the code roots.</summary>
    /// <returns>The existing folders to watch, without duplicates.</returns>
    List<string> WatchedRoots()
    {
        var roots = new List<string> { Path.Combine(Config.Root, ConsumerRoot.FolderName) };
        roots.AddRange(Config.RecordRoots);
        roots.AddRange(Config.ModulesRoots);
        roots.AddRange(Config.WebApiRoots);
        roots.AddRange(Config.WorkerRoots);
        roots.AddRange(Config.TestRoots);

        if (Config.KernelRoot.Length > 0)
        {
            roots.Add(Config.KernelRoot);
        }

        if (Config.PlatformRoot.Length > 0)
        {
            roots.Add(Config.PlatformRoot);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>();

        foreach (var root in roots)
        {
            string full;

            try
            {
                full = Path.GetFullPath(root);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (Directory.Exists(full) && seen.Add(full))
            {
                kept.Add(full);
            }
        }

        return kept;
    }
}
