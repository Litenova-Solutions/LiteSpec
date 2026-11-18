using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using LiteSpec.Cli.Book;
using LiteSpec.Cli.Book.Lexicon;
using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Book.Live;

/// <summary>
///     The live book: the last good build in memory, the rebuild loop that follows edits to
///     <c>.litespec/</c> and, when the project claims Implementation or Proof, to its code roots
///     and test results, and the notifications the open page receives. A failed build keeps the
///     last good book and reports its error; nothing is written to disk.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
sealed class BookLive : IDisposable
{
    /// <summary>The quiet time after the last edit before a rebuild starts, so a save storm builds once.</summary>
    static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(500);

    /// <summary>The consumer root the book follows.</summary>
    readonly string root;

    /// <summary>The book lexicon read from the carried release, which does not change while the book serves.</summary>
    readonly BookLexicon lexicon;

    /// <summary>The source link template, or null for no source links.</summary>
    readonly string? linksTemplate;

    /// <summary>Guards the current build, the subscribers, the watched paths and the last status.</summary>
    readonly object gate = new();

    /// <summary>The open pages' channels, each receiving every notification.</summary>
    readonly List<Channel<BookLiveEvent>> subscribers = [];

    /// <summary>The file system watchers over the watched paths.</summary>
    readonly List<FileSystemWatcher> watchers = [];

    /// <summary>The folders the watchers cover, in order.</summary>
    IReadOnlyList<string> watched = [];

    /// <summary>The last status a new page must hear about: building or failed, or null after a good build.</summary>
    BookLiveEvent? status;

    /// <summary>The last good build, which the server answers from.</summary>
    BookSiteBuild? current;

    /// <summary>The time of the last file change, in UTC ticks, read and written atomically.</summary>
    long lastChangeTicks;

    /// <summary>Wakes the rebuild loop when a change arrives; holds at most one wake, so a burst of changes wakes it once.</summary>
    readonly Channel<bool> wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    /// <summary>Whether the book was disposed.</summary>
    bool disposed;

    /// <summary>Initializes a new instance of the <see cref="BookLive" /> class over one consumer.</summary>
    /// <param name="root">The absolute consumer root.</param>
    /// <param name="lexicon">The book lexicon read from the carried release.</param>
    /// <param name="linksTemplate">The source link template, or null for no source links.</param>
    public BookLive(string root, BookLexicon lexicon, string? linksTemplate)
    {
        this.root = root;
        this.lexicon = lexicon;
        this.linksTemplate = linksTemplate;
    }

    /// <summary>Gets the last good build; the server answers every request from it.</summary>
    public BookSiteBuild Current
    {
        get
        {
            lock (gate)
            {
                return current ?? throw new InvalidOperationException("The live book has no build yet.");
            }
        }
    }

    /// <summary>Sets the first build, and watches the folders its configuration names.</summary>
    /// <param name="first">The first good build.</param>
    /// <param name="config">The configuration the build used.</param>
    public void Start(BookSiteBuild first, ConsumerConfig config)
    {
        lock (gate)
        {
            current = first;
            status = null;
        }

        Watch(WatchedPaths(config));
    }

    /// <summary>
    ///     Runs the rebuild loop until the token is cancelled: each change wakes it, a quiet pause
    ///     settles the edits, then the book rebuilds and the page hears built or failed.
    /// </summary>
    /// <param name="token">The token that stops the loop.</param>
    /// <returns>A task that completes when the token is cancelled.</returns>
    public async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (true)
            {
                await wake.Reader.ReadAsync(token);
                while (true)
                {
                    var idle = DateTime.UtcNow - new DateTime(Interlocked.Read(ref lastChangeTicks), DateTimeKind.Utc);
                    if (idle >= Pause)
                    {
                        break;
                    }

                    await Task.Delay(Pause - idle, token);
                }

                Rebuild();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The server is stopping: the loop ends with it.
        }
        catch (ChannelClosedException)
        {
            // The book was disposed: no more changes can arrive, so the loop ends.
        }
    }

    /// <summary>
    ///     Subscribes a page to the notifications. A page that connects during a build or after a
    ///     failed one hears that status at once, so it never shows a stale state.
    /// </summary>
    /// <returns>The channel the page reads its notifications from.</returns>
    public Channel<BookLiveEvent> Subscribe()
    {
        var channel = Channel.CreateUnbounded<BookLiveEvent>(new UnboundedChannelOptions { SingleReader = true });
        lock (gate)
        {
            if (status is not null)
            {
                channel.Writer.TryWrite(status);
            }

            subscribers.Add(channel);
        }

        return channel;
    }

    /// <summary>Removes a page's channel when the page disconnects.</summary>
    /// <param name="channel">The channel the page subscribed with.</param>
    public void Unsubscribe(Channel<BookLiveEvent> channel)
    {
        lock (gate)
        {
            subscribers.Remove(channel);
        }

        channel.Writer.TryComplete();
    }

    /// <summary>Stops the watchers and closes every page's channel.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            wake.Writer.TryComplete();
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }

            watchers.Clear();
            foreach (var channel in subscribers)
            {
                channel.Writer.TryComplete();
            }

            subscribers.Clear();
        }

    }

    /// <summary>Rebuilds the book from the consumer's files and publishes the outcome.</summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A rebuild that fails for any reason keeps the last good book and tells the page the error, so the live book never stops serving.")]
    void Rebuild()
    {
        Publish(new BookLiveEvent("building", ""));
        try
        {
            var config = ConsumerConfig.Load(root);
            var build = BookBuild.Run(config, lexicon, linksTemplate);
            Watch(WatchedPaths(config));
            lock (gate)
            {
                current = build;
            }

            Publish(new BookLiveEvent("built", build.GeneratedOn));
        }
        catch (Exception exception)
        {
            Publish(new BookLiveEvent("failed", OneLine(exception.Message)));
        }
    }

    /// <summary>Sends one notification to every page and remembers it as the status a new page hears.</summary>
    /// <param name="evt">The notification.</param>
    void Publish(BookLiveEvent evt)
    {
        lock (gate)
        {
            status = evt.Name == "built" ? null : evt;
            foreach (var channel in subscribers)
            {
                channel.Writer.TryWrite(evt);
            }
        }
    }

    /// <summary>Records a change and wakes the loop; a change during a build queues one more rebuild.</summary>
    void NoteChange()
    {
        Interlocked.Exchange(ref lastChangeTicks, DateTime.UtcNow.Ticks);
        // A full channel already holds a wake, and the loop reads the newest change time either way.
        wake.Writer.TryWrite(true);
    }

    /// <summary>The folders a book watches: <c>.litespec/</c>, and the code roots and test results when the project claims code.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The existing folders, absolute and in order.</returns>
    static IReadOnlyList<string> WatchedPaths(ConsumerConfig config)
    {
        var paths = new List<string> { Path.Combine(config.Root, ConsumerRoot.FolderName) };
        if (config.ReadsCode)
        {
            paths.AddRange(config.ModulesRoots);
            paths.AddRange([config.DomainRoot, config.KernelRoot, config.PlatformRoot, config.InfrastructureRoot, config.TestResultsDir]);
            paths.AddRange(config.WebApiRoots);
            paths.AddRange(config.WorkerRoots);
            paths.AddRange(config.TestRoots);
        }

        return [.. paths
            .Where(path => path.Length > 0 && Directory.Exists(path))
            .Select(path => Path.GetFullPath(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Replaces the watchers when the watched folders change; a folder the system cannot watch is skipped.</summary>
    /// <param name="paths">The folders to watch.</param>
    void Watch(IReadOnlyList<string> paths)
    {
        lock (gate)
        {
            if (disposed || watched.SequenceEqual(paths, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }

            watchers.Clear();
            foreach (var path in paths)
            {
                var watcher = CreateWatcher(path);
                if (watcher is not null)
                {
                    watchers.Add(watcher);
                }
            }

            watched = paths;
        }
    }

    /// <summary>Starts a watcher over one folder that reports every change to the rebuild loop.</summary>
    /// <param name="path">The folder to watch, with its subfolders.</param>
    /// <returns>The running watcher, or null when the system will not watch the folder.</returns>
    FileSystemWatcher? CreateWatcher(string path)
    {
        try
        {
            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            };
            watcher.Changed += (_, _) => NoteChange();
            watcher.Created += (_, _) => NoteChange();
            watcher.Deleted += (_, _) => NoteChange();
            watcher.Renamed += (_, _) => NoteChange();
            watcher.Error += (_, _) => NoteChange();
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // A folder the system will not watch still builds; the book rebuilds on the others.
            return null;
        }
    }

    /// <summary>One line of a message, for the single-line notification data.</summary>
    /// <param name="text">The message, possibly with line breaks.</param>
    /// <returns>The message with each line break replaced by a space.</returns>
    static string OneLine(string text) => string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
}
