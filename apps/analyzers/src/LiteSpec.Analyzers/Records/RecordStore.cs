using System;
using System.Collections.Generic;
using System.Threading;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Records;

/// <summary>
///     One Markdown file handed to the record index: its path and its text.
/// </summary>
public sealed class RecordFile
{
    /// <summary>Creates a record file.</summary>
    /// <param name="path">The file path, as the host reports it.</param>
    /// <param name="text">The file text.</param>
    public RecordFile(string path, string text)
    {
        Path = path;
        Text = text;
    }

    /// <summary>The file path, as the host reports it.</summary>
    public string Path { get; }

    /// <summary>The file text.</summary>
    public string Text { get; }
}

/// <summary>
///     The consumer's records for one compilation: the project settings and the
///     record index built from the record pages. Loads from the project file and
///     the Markdown pages found among the additional files, and stays silent
///     (returns null) when no project file is in reach, when its settings do not
///     parse, or when the docs root cannot be resolved; the CLI owns those
///     configuration errors. Results are cached by the files' content checksums,
///     so a keystroke in C# does not reparse Markdown.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public sealed class RecordStore
{
    private static readonly object CacheLock = new object();
    private static readonly Dictionary<string, RecordStore?> Cache =
        new Dictionary<string, RecordStore?>(StringComparer.Ordinal);

    private readonly Dictionary<string, string> useCaseByHome;
    private readonly List<string> docsRoots;

    private RecordStore(ProjectSettings settings, RecordIndex index, string root, string projectFile, List<string> docsRoots)
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

    /// <summary>The check settings from the project file: the mark style and the phase.</summary>
    public ProjectSettings Settings { get; }

    /// <summary>The records read from the pages under the docs roots.</summary>
    public RecordIndex Index { get; }

    /// <summary>The project directory holding the project file.</summary>
    public string Root { get; }

    /// <summary>The project file path, as the host reported it.</summary>
    public string ProjectFile { get; }

    /// <summary>
    ///     Loads the store from an analyzer's additional files: the project file
    ///     plus every Markdown page. Returns null when the rules must stay silent.
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
    ///     file, unparsable settings, an unresolvable docs root, or no record
    ///     page under the roots.
    /// </summary>
    /// <param name="files">The additional files: the project file plus every Markdown page.</param>
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

    /// <summary>The effective status of the subject, or null when it names no record.</summary>
    /// <param name="subject">The record id the finding is about, or null.</param>
    /// <returns>The effective status, or null.</returns>
    public string? EffectiveStatus(string? subject)
    {
        if (subject is null)
        {
            return null;
        }

        return Index.EffectiveStatus.TryGetValue(subject, out var status) ? status : null;
    }

    /// <summary>
    ///     Whether an open known gap or an in-date approved deviation covers the
    ///     finding, as <see cref="LiteSpec.Model.Suppression.Partition" /> decides for the CLI.
    /// </summary>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding is known and stays silent.</returns>
    public bool IsKnown(Finding finding)
    {
        var (_, known) = LiteSpec.Model.Suppression.Partition(
            new Finding[] { finding }, Index.Records, Root, DateTime.UtcNow.Date, UseCaseOfHome);

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

    private static RecordStore? LoadUncached(IReadOnlyList<RecordFile> files)
    {
        var project = ProjectFileOf(files);

        if (project is null)
        {
            return null;
        }

        if (!ProjectSettings.TryParse(project.Text, out var settings, out _))
        {
            return null;
        }

        JsonText root;

        try
        {
            root = JsonText.Parse(project.Text);
        }
        catch (JsonTextException)
        {
            return null;
        }

        var directory = DirectoryOf(project.Path);
        var roots = DocsRoots(root, directory, out var docsRoots);

        if (roots is null || roots.Count == 0)
        {
            return null;
        }

        var records = new List<IRecordFacts>();

        foreach (var file in files)
        {
            if (IsMarkdown(file.Path) && UnderRoots(file.Path, roots))
            {
                records.AddRange(RecordFiles.Read(file.Path, file.Text));
            }
        }

        if (records.Count == 0)
        {
            return null;
        }

        return new RecordStore(settings, new RecordIndex(records), directory, project.Path, docsRoots);
    }

    private static RecordFile? ProjectFileOf(IReadOnlyList<RecordFile> files)
    {
        RecordFile? found = null;

        foreach (var file in files)
        {
            if (string.Equals(FileName(file.Path), "litespec.project.json", StringComparison.OrdinalIgnoreCase)
                && (found is null || string.Compare(file.Path, found.Path, StringComparison.Ordinal) < 0))
            {
                found = file;
            }
        }

        return found;
    }

    private static string FileName(string path)
    {
        var slash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

        return slash < 0 ? path : path.Substring(slash + 1);
    }

    private static string DirectoryOf(string path)
    {
        var slash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

        return slash < 0 ? string.Empty : path.Substring(0, slash);
    }

    /// <summary>
    ///     The docs roots from the project file: <c>paths.docs</c> (one folder or
    ///     an array, defaulting to <c>docs</c>) plus <c>paths.behaviorDocs</c>
    ///     (defaulting to <c>docs/behavior</c>), resolved against the project
    ///     directory. Returns null when the paths do not resolve.
    /// </summary>
    private static List<string>? DocsRoots(JsonText root, string directory, out List<string> docsOnly)
    {
        var roots = new List<string>();
        docsOnly = new List<string>();
        JsonText? paths = null;

        if (root.TryGetProperty("paths", out var found))
        {
            if (found.Kind == JsonKind.Null)
            {
                paths = null;
            }
            else if (found.Kind == JsonKind.Object)
            {
                paths = found;
            }
            else
            {
                return null;
            }
        }

        if (paths is null || !paths.TryGetProperty("docs", out var docs) || docs.Kind == JsonKind.Null)
        {
            AddDocsRoot(roots, docsOnly, Combine(directory, "docs"));
        }
        else if (docs.Kind == JsonKind.String && docs.GetString() is string single)
        {
            if (single.Length == 0)
            {
                AddDocsRoot(roots, docsOnly, Combine(directory, "docs"));
            }
            else
            {
                AddDocsRoot(roots, docsOnly, Combine(directory, single));
            }
        }
        else if (docs.Kind == JsonKind.Array)
        {
            foreach (var entry in docs.Items)
            {
                if (entry.Kind == JsonKind.String && entry.GetString() is string folder && folder.Length > 0)
                {
                    AddDocsRoot(roots, docsOnly, Combine(directory, folder));
                }
            }
        }
        else
        {
            return null;
        }

        if (paths is null
            || !paths.TryGetProperty("behaviorDocs", out var behavior)
            || behavior.Kind == JsonKind.Null
            || (behavior.Kind == JsonKind.String && behavior.GetString() is string blank && blank.Length == 0))
        {
            roots.Add(Combine(directory, "docs/behavior"));
        }
        else if (behavior.Kind == JsonKind.String && behavior.GetString() is string folder && folder.Length > 0)
        {
            roots.Add(Combine(directory, folder));
        }
        else if (behavior.Kind != JsonKind.String)
        {
            return null;
        }

        return roots;
    }

    /// <summary>Adds a <c>paths.docs</c> folder to both the index roots and the docs-only roots.</summary>
    private static void AddDocsRoot(List<string> roots, List<string> docsOnly, string folder)
    {
        roots.Add(folder);
        docsOnly.Add(folder);
    }

    private static string Combine(string directory, string folder)
    {
        var combined = folder.Replace('\\', '/');

        if (combined.StartsWith("/", StringComparison.Ordinal)
            || (combined.Length >= 2 && combined[1] == ':'))
        {
            return combined;
        }

        if (directory.Length == 0)
        {
            return combined;
        }

        return directory.Replace('\\', '/').TrimEnd('/') + "/" + combined.TrimStart('/');
    }

    private static bool IsMarkdown(string path) =>
        path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    private static bool UnderRoots(string path, List<string> roots)
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
    ///     file and every Markdown path and text, in path order.
    /// </summary>
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
