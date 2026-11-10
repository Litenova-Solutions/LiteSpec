using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     The paths a consumer declares in <c>.litespec/project.yaml</c>, read once and
///     resolved against the repository root: the record roots, the behavior docs root
///     and the glossary, with the mark style. The CLI and the analyzers both read the
///     project file through this type, so a default or a path rule is written once.
/// </summary>
/// <follows>litespec/core/scope#keep-every-litespec-file-under-one-folder</follows>
public sealed class ProjectPaths
{
    /// <summary>The record root when <c>paths.docs</c> names none.</summary>
    public const string DefaultDocs = ".litespec";

    /// <summary>The behavior docs root when <c>paths.behaviorDocs</c> names none.</summary>
    public const string DefaultBehaviorDocs = ".litespec/behavior";

    /// <summary>The glossary when <c>paths.glossary</c> names none.</summary>
    public const string DefaultGlossary = ".litespec/glossary.yaml";

    /// <summary>Initializes a new instance of the <see cref="ProjectPaths"/> class.</summary>
    /// <param name="projectFile">The project file path, as the host reported it.</param>
    /// <param name="repositoryRoot">The repository root, with forward slashes.</param>
    /// <param name="settings">The settings the project file declares.</param>
    /// <param name="docsRoots">The record roots, resolved, in declaration order.</param>
    /// <param name="behaviorDocs">The behavior docs root, resolved.</param>
    /// <param name="glossary">The glossary path, resolved.</param>
    private ProjectPaths(
        string projectFile,
        string repositoryRoot,
        ProjectSettings settings,
        IReadOnlyList<string> docsRoots,
        string behaviorDocs,
        string glossary)
    {
        ProjectFile = projectFile;
        RepositoryRoot = repositoryRoot;
        Settings = settings;
        DocsRoots = docsRoots;
        BehaviorDocs = behaviorDocs;
        GlossaryPath = glossary;
    }

    /// <summary>Gets the project file path, as the host reported it.</summary>
    public string ProjectFile { get; }

    /// <summary>Gets the repository root the declared paths resolve against, with forward slashes.</summary>
    public string RepositoryRoot { get; }

    /// <summary>Gets the settings the project file declares: the mark style.</summary>
    public ProjectSettings Settings { get; }

    /// <summary>Gets the record roots from <c>paths.docs</c>, resolved, with forward slashes.</summary>
    public IReadOnlyList<string> DocsRoots { get; }

    /// <summary>Gets the behavior docs root from <c>paths.behaviorDocs</c>, resolved, with forward slashes.</summary>
    public string BehaviorDocs { get; }

    /// <summary>Gets the glossary path from <c>paths.glossary</c>, resolved, with forward slashes.</summary>
    public string GlossaryPath { get; }

    /// <summary>Whether a path names a project file: <c>.litespec/project.yaml</c> at the end.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <returns>True when the path ends in the project file's location.</returns>
    public static bool IsProjectFile(string path) =>
        path.Replace('\\', '/').EndsWith("/.litespec/project.yaml", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Reads the project file text. A missing or unknown value reads as its default:
    ///     <c>paths.docs</c> as <c>.litespec</c>, <c>paths.behaviorDocs</c> as
    ///     <c>.litespec/behavior</c>, and <c>paths.glossary</c> as <c>.litespec/glossary.yaml</c>.
    ///     A value of the wrong shape reads as its default too; the project schema reports it.
    /// </summary>
    /// <param name="projectFile">The project file path, as the host reports it.</param>
    /// <param name="text">The project file text.</param>
    /// <param name="paths">The parsed paths, or null when the text is not a valid project file.</param>
    /// <param name="error">The error the host prints, or null when the text reads.</param>
    /// <returns>Whether the project file reads.</returns>
    public static bool TryRead(string projectFile, string text, out ProjectPaths? paths, out string? error)
    {
        paths = null;

        if (!ProjectSettings.TryParse(text, out var settings, out error))
        {
            return false;
        }

        var root = YamlText.Parse(text);
        var repository = RepositoryRootOf(projectFile);
        JsonText? declared = null;

        if (root.TryGetProperty("paths", out var found) && found.Kind == JsonKind.Object)
        {
            declared = found;
        }

        var docs = Folders(declared, "docs");

        if (docs.Count == 0)
        {
            docs.Add(DefaultDocs);
        }

        paths = new ProjectPaths(
            projectFile,
            repository,
            settings,
            [.. docs.Select(folder => ResolveRoot(repository, folder)).Distinct(StringComparer.Ordinal)],
            ResolveRoot(repository, Single(declared, "behaviorDocs") ?? DefaultBehaviorDocs),
            ResolveRoot(repository, Single(declared, "glossary") ?? DefaultGlossary));
        return true;
    }

    /// <summary>The repository root holding the project file: the folder above <c>.litespec/</c>.</summary>
    /// <param name="projectFile">The project file path, as the host reports it.</param>
    /// <returns>The repository root, with forward slashes.</returns>
    private static string RepositoryRootOf(string projectFile)
    {
        var cleaned = projectFile.Replace('\\', '/');
        var folder = cleaned.Substring(0, Math.Max(cleaned.LastIndexOf('/'), 0));

        if (!folder.EndsWith("/.litespec", StringComparison.OrdinalIgnoreCase))
        {
            return folder;
        }

        var parent = folder.Substring(0, folder.Length - "/.litespec".Length);
        return parent.Length > 0 ? parent : cleaned.StartsWith("/", StringComparison.Ordinal) ? "/" : "";
    }

    /// <summary>
    ///     The non-empty folders of a field that holds one folder or an array of folders.
    /// </summary>
    /// <param name="paths">The paths object, or null when the file names none.</param>
    /// <param name="field">The field name to read.</param>
    /// <returns>The folders, in declaration order; empty when the field names none.</returns>
    private static List<string> Folders(JsonText? paths, string field)
    {
        var folders = new List<string>();

        if (paths is null || !paths.TryGetProperty(field, out var value))
        {
            return folders;
        }

        if (value.Kind == JsonKind.String && value.GetString() is { Length: > 0 } single)
        {
            folders.Add(single);
        }
        else if (value.Kind == JsonKind.Array)
        {
            foreach (var entry in value.Items)
            {
                if (entry.Kind == JsonKind.String && entry.GetString() is { Length: > 0 } folder)
                {
                    folders.Add(folder);
                }
            }
        }

        return folders;
    }

    /// <summary>The one non-empty string of a field, or null when the field names none.</summary>
    /// <param name="paths">The paths object, or null when the file names none.</param>
    /// <param name="field">The field name to read.</param>
    /// <returns>The string, or null.</returns>
    private static string? Single(JsonText? paths, string field) =>
        paths is not null
        && paths.TryGetProperty(field, out var value)
        && value.Kind == JsonKind.String
        && value.GetString() is { Length: > 0 } text
            ? text
            : null;

    /// <summary>
    ///     Resolves a declared path against the repository root: a rooted path stands as
    ///     written, and <c>.</c> and <c>..</c> segments collapse.
    /// </summary>
    /// <param name="root">The repository root, with forward slashes.</param>
    /// <param name="declared">The path as the project file states it.</param>
    /// <returns>The resolved path, with forward slashes.</returns>
    private static string ResolveRoot(string root, string declared)
    {
        var cleaned = declared.Replace('\\', '/');
        var rooted = cleaned.StartsWith("/", StringComparison.Ordinal) || (cleaned.Length > 1 && cleaned[1] == ':');
        var joined = rooted || root.Length == 0 ? cleaned : root.TrimEnd('/') + "/" + cleaned;
        var kept = new List<string>();

        foreach (var part in joined.Split(['/'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (kept.Count > 0)
                {
                    kept.RemoveAt(kept.Count - 1);
                }

                continue;
            }

            kept.Add(part);
        }

        var resolved = string.Join("/", kept);
        return joined.StartsWith("/", StringComparison.Ordinal) ? "/" + resolved : resolved;
    }
}
