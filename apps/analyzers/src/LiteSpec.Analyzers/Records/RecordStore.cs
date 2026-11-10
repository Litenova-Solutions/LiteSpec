using System;
using System.Collections.Generic;
using System.Threading;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Records;

/// <summary>
///     The consumer's records for one compilation: the project settings and the
///     record index built from the record files. Loads from the project file and
///     the record files found among the additional files, and stays silent
///     (returns null) when no project file is in reach, when it does not read
///     through <see cref="ProjectPaths" />, or when no record page sits under its
///     docs roots; the CLI owns those configuration errors. Results are cached by the files' content checksums,
///     so a keystroke in C# does not reparse record files.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public sealed class RecordStore
{
    /// <summary>Guards the shared store cache against concurrent access.</summary>
    private static readonly object CacheLock = new object();
    /// <summary>Stores the cache used by the containing code.</summary>
    private static readonly Dictionary<string, RecordStore?> Cache =
        new Dictionary<string, RecordStore?>(StringComparer.Ordinal);

    /// <summary>Holds the use case by home collected for this operation, keyed or ordered as the type states.</summary>
    private readonly Dictionary<string, string> useCaseByHome;
    /// <summary>Holds the record roots the project file declares, in declaration order.</summary>
    private readonly IReadOnlyList<string> docsRoots;

    /// <summary>Initializes a new instance of the <see cref="RecordStore"/> class.</summary>
    /// <param name="settings">The project settings parsed from the project file.</param>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="projectFile">The project file path, as the host reported it.</param>
    /// <param name="docsRoots">The docs roots resolved from the project file, in declaration order.</param>
    private RecordStore(ProjectSettings settings, RecordIndex index, string root, string projectFile, IReadOnlyList<string> docsRoots)
    {
        Settings = settings;
        Index = index;
        Root = root;
        ProjectFile = projectFile;
        this.docsRoots = docsRoots;
        useCaseByHome = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var record in index.Records)
        {
            if (record.Type == "use-case"
                && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name.Length > 0
                && !useCaseByHome.ContainsKey(name))
            {
                useCaseByHome.Add(name, record.Id);
            }
        }
    }

    /// <summary>Gets the check settings from the project file: the mark style.</summary>
    public ProjectSettings Settings { get; }

    /// <summary>Gets the records read from the pages under the docs roots.</summary>
    public RecordIndex Index { get; }

    /// <summary>Gets the project directory holding the project file.</summary>
    public string Root { get; }

    /// <summary>Gets the project file path, as the host reported it.</summary>
    public string ProjectFile { get; }

    /// <summary>
    ///     Loads the store from an analyzer's additional files: the project file
    ///     plus every record file. Returns null when the rules must stay silent.
    /// </summary>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>The store, or null when the rules stay silent.</returns>
    internal static RecordStore? Load(AnalyzerOptions options, CancellationToken cancellationToken)
    {
        var files = new List<RecordFile>();

        foreach (var file in options.AdditionalFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = file.GetText(cancellationToken);

            if (text is not null)
            {
                files.Add(new RecordFile(file.Path, text.ToString()));
            }
        }

        return Load(files);
    }

    /// <summary>
    ///     Loads the store from file paths and texts, consulting the checksum
    ///     cache first. Returns null when the rules must stay silent: no project
    ///     file, a project file that does not read, or no record page under the
    ///     docs roots.
    /// </summary>
    /// <param name="files">The additional files: the project file plus every record file.</param>
    /// <returns>The store, or null when the rules stay silent.</returns>
    public static RecordStore? Load(IReadOnlyList<RecordFile> files)
    {
        var key = Checksum(files);

        lock (CacheLock)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var store = LoadUncached(files);

        lock (CacheLock)
        {
            if (Cache.Count > 32)
            {
                Cache.Clear();
            }

            Cache[key] = store;
        }

        return store;
    }

    /// <summary>
    ///     Whether a live exception covers the finding, as
    ///     <see cref="LiteSpec.Model.Suppression.Partition" /> decides for the CLI.
    ///     An exception is live when it is present and unexpired.
    /// </summary>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding is known and stays silent.</returns>
    public bool IsKnown(Finding finding)
    {
        var (_, known) = LiteSpec.Model.Suppression.Partition(
            [finding], Index.Records, Root, DateTime.UtcNow.Date, UseCaseOfHome);

        return known.Count > 0;
    }

    /// <summary>The id of the use case a home names, or null when none holds it.</summary>
    /// <param name="home">The home part of a result or scenario id.</param>
    /// <returns>The holding use case id, or null.</returns>
    public string? UseCaseOfHome(string home) =>
        useCaseByHome.TryGetValue(home, out var id) ? id : null;

    /// <summary>Whether the file sits under the project's implementation decisions folder.</summary>
    /// <param name="file">The record file path, as the host reported it.</param>
    /// <returns>True when the file is a technical decision page.</returns>
    public bool UnderImplementationDecisions(string file)
    {
        var candidate = file.Replace('\\', '/');

        foreach (var docs in docsRoots)
        {
            var prefix = docs.TrimEnd('/') + "/implementation/decisions/";

            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the store read from the project file and the record files, or null when none applies.</summary>
    /// <param name="files">The additional files: the project file plus every record file.</param>
    /// <returns>The store, or null when the project file does not read or no record page sits under its roots.</returns>
    private static RecordStore? LoadUncached(IReadOnlyList<RecordFile> files)
    {
        var project = ProjectFileOf(files);

        if (project is null || !ProjectPaths.TryRead(project.Path, project.Text, out var paths, out _) || paths is null)
        {
            return null;
        }

        var records = new List<IRecordFacts>();

        foreach (var file in files)
        {
            if (IsRecordFile(file.Path) && UnderRoots(file.Path, paths.DocsRoots))
            {
                records.AddRange(RecordFiles.Read(file.Path, file.Text));
            }
        }

        if (records.Count == 0)
        {
            return null;
        }

        return new RecordStore(paths.Settings, new RecordIndex(records), paths.RepositoryRoot, project.Path, paths.DocsRoots);
    }

    /// <summary>Returns the project file among the files, the first by path when several hold one.</summary>
    /// <param name="files">The additional files: the project file plus every record file.</param>
    /// <returns>The project file, or null when none applies.</returns>
    private static RecordFile? ProjectFileOf(IReadOnlyList<RecordFile> files)
    {
        RecordFile? found = null;

        foreach (var file in files)
        {
            if (ProjectPaths.IsProjectFile(file.Path)
                && (found is null || string.Compare(file.Path, found.Path, StringComparison.Ordinal) < 0))
            {
                found = file;
            }
        }

        return found;
    }

    /// <summary>Returns true when the path is a record file; otherwise false.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <returns>True when the path is a record file; otherwise false.</returns>
    private static bool IsRecordFile(string path) =>
        path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true when the under-roots condition holds; otherwise false.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <param name="roots">The docs roots that bound the search, as absolute or project-relative paths.</param>
    /// <returns>True when the under-roots condition holds; otherwise false.</returns>
    private static bool UnderRoots(string path, IReadOnlyList<string> roots)
    {
        var candidate = path.Replace('\\', '/');

        foreach (var root in roots)
        {
            var prefix = root.TrimEnd('/') + "/";

            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     The checksum key of the files: a 64-bit FNV-1a hash over the project
    ///     file and every record file path and text, in path order.
    /// </summary>
    /// <param name="files">The additional files: the project file plus every record file.</param>
    /// <returns>The string as text.</returns>
    private static string Checksum(IReadOnlyList<RecordFile> files)
    {
        var paths = new List<string>();

        foreach (var file in files)
        {
            paths.Add(file.Path);
        }

        paths.Sort(StringComparer.Ordinal);

        var byPath = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            byPath[file.Path] = file.Text;
        }

        unchecked
        {
            const ulong basis = 14695981039346656037;
            const ulong prime = 1099511628211;
            var hash = basis;

            foreach (var path in paths)
            {
                hash = Fold(hash, prime, path);
                hash = Fold(hash, prime, "\0");
                hash = Fold(hash, prime, byPath[path]);
                hash = Fold(hash, prime, "\0");
            }

            return hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Returns the ulong.</summary>
    /// <param name="hash">The running hash value to fold the text into.</param>
    /// <param name="prime">The FNV prime the hash folds with.</param>
    /// <param name="text">The source text the edit applies to.</param>
    /// <returns>The ulong.</returns>
    private static ulong Fold(ulong hash, ulong prime, string text)
    {
        unchecked
        {
            foreach (var character in text)
            {
                hash ^= (byte)(character & 0xFF);
                hash *= prime;
                hash ^= (byte)((character >> 8) & 0xFF);
                hash *= prime;
            }

            return hash;
        }
    }
}
