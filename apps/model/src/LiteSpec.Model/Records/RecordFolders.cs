using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     The one map from record type to the folder and file a record of the type lives in.
///     A named record owns a folder named for it, so its record file and explanation sit together
///     as <c>&lt;folder&gt;/&lt;name&gt;/&lt;name&gt;.yaml</c>. The folder checks read it to place a record,
///     the scaffolder reads it to write one, and the qualifier reads the fixed folders of a behavior
///     module.
/// </summary>
/// <follows>litespec/core/model#write-every-record-in-its-home</follows>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
public static class RecordFolders
{
    /// <summary>The typed folders a behavior module holds: aggregates, its module-level use cases, rules and automation, and its edges. Any other subfolder of a module is a placement finding.</summary>
    public static readonly HashSet<string> BehaviorModuleFolders = new(StringComparer.Ordinal)
    {
        "aggregates", "use-cases", "rules", "reactions", "jobs", "notifications", "settings",
        "integration-events", "external-systems",
    };

    /// <summary>The typed folders an aggregate holds beside its aggregate record: its use cases, its events and its rules.</summary>
    public static readonly HashSet<string> AggregateFolders = new(StringComparer.Ordinal)
    {
        "use-cases", "events", "rules",
    };

    /// <summary>Record types whose English plural is the type itself, or that take a singular verb.</summary>
    private static readonly HashSet<string> Uncountable = new(StringComparer.Ordinal) { "product", "research" };

    /// <summary>
    ///     Every home of every record type. A type with several homes lists them in the order the
    ///     messages show them.
    /// </summary>
    private static readonly IReadOnlyList<Home> Table =
    [
        Scaffold("product", "intent", "product.yaml"),
        Scaffold("goal", "intent/goals/{name}", "{name}.yaml"),
        Scaffold("vision", "intent/visions/{name}", "{name}.yaml"),
        Scaffold("problem", "intent/problems/{name}", "{name}.yaml"),
        // A feature sits in the folder of the one problem it answers; its identifier carries no problem.
        Scaffold("feature", "intent/problems/{problem}/features/{name}", "{name}.yaml"),
        // A requirement sits in its own folder inside its feature's folder. litespec new and rename place it
        // from the feature record, since the identifier names the feature but not the problem.
        At("requirement", "intent/problems/{problem}/features/{feature}/requirements/{name}", "{name}.yaml"),
        Scaffold("constraint", "intent/constraints/{name}", "{name}.yaml"),
        Scaffold("decision", "intent/decisions/{name}", "{name}.yaml"),
        At("decision", "implementation/decisions/{name}", "{name}.yaml"),
        At("decision", "implementation/modules/{module}/decisions/{name}", "{name}.yaml"),
        Scaffold("research", "intent/research/{name}", "{name}.yaml"),
        Scaffold("journey", "intent/journeys/{name}", "{name}.yaml"),
        Scaffold("actor", "intent/actors/{name}", "{name}.yaml"),
        Scaffold("role", "intent/roles/{name}", "{name}.yaml"),
        Scaffold("module", "behavior/modules/{name}", "module.yaml"),
        At("term", "", "glossary.yaml"),
        At("aggregate", "behavior/modules/{module}/aggregates/{aggregate}", "aggregate.yaml"),
        At("use-case", "behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}", "{name}.yaml"),
        Scaffold("use-case", "behavior/modules/{module}/use-cases/{name}", "{name}.yaml"),
        At("result", "behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}", "{name}.yaml"),
        At("result", "behavior/modules/{module}/use-cases/{name}", "{name}.yaml"),
        At("scenario", "behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}", "{name}.yaml"),
        At("scenario", "behavior/modules/{module}/use-cases/{name}", "{name}.yaml"),
        At("event", "behavior/modules/{module}/aggregates/{aggregate}", "aggregate.yaml"),
        At("event", "behavior/modules/{module}/aggregates/{aggregate}/events", "*"),
        At("rule", "behavior/rules", "*"),
        At("rule", "behavior/modules/{module}/rules", "*"),
        At("rule", "behavior/modules/{module}/aggregates/{aggregate}/rules", "*"),
        Scaffold("reaction", "behavior/modules/{module}/reactions/{name}", "{name}.yaml"),
        Scaffold("job", "behavior/modules/{module}/jobs/{name}", "{name}.yaml"),
        Scaffold("notification", "behavior/modules/{module}/notifications/{name}", "{name}.yaml"),
        At("setting", "behavior/modules/{module}/settings", "*"),
        Scaffold("integration-event", "behavior/modules/{module}/integration-events/{name}", "{name}.yaml"),
        At("permission", "behavior/modules/{module}", "permissions.yaml"),
        At("entry-point", "behavior/entry-points/{entry-point}", "entry-point.yaml"),
        Scaffold("screen", "behavior/entry-points/{entry-point}/screens/{home}.{name}", "{home}.{name}.yaml"),
        Scaffold("external-system", "behavior/external-systems/{name}", "{name}.yaml"),
        At("external-system", "behavior/modules/{module}/external-systems/{name}", "{name}.yaml"),
        Scaffold("runbook", "implementation/runbooks/{name}", "{name}.yaml"),
        At("runbook", "implementation/modules/{module}/runbooks/{name}", "{name}.yaml"),
        Scaffold("departure", "governance/departures/{name}", "{name}.yaml"),
        At("departure", "governance/modules/{module}/departures/{name}", "{name}.yaml"),
    ];

    /// <summary>
    ///     Every home of every record type in table order, as the folder reference page lists them.
    ///     A type with several homes has one row for each.
    /// </summary>
    /// <returns>The type, folder and file of each home.</returns>
    public static IReadOnlyList<(string Type, string Folder, string File)> Homes() =>
        Table.Select(home => (home.Type, home.Folder, home.File)).ToList();

    /// <summary>Whether a record file sits in a home of its type.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folders">The folders the record file sits in, from its record layer down.</param>
    /// <param name="fileName">The record file name.</param>
    /// <returns>True when one home of the type holds the file; otherwise false.</returns>
    public static bool Places(string type, IReadOnlyList<string> folders, string fileName) =>
        Table.Any(home => home.Type == type && Holds(home, folders) && TakesFile(home, fileName));

    /// <summary>
    ///     Whether a named record file sits in a folder with another name. A home that names its folder
    ///     after the record holds the file, so the folder must carry the file stem.
    /// </summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folders">The folders the record file sits in, from its record layer down.</param>
    /// <param name="fileName">The record file name.</param>
    /// <returns>True when a named home holds the file and its folder name differs from the file stem; otherwise false.</returns>
    public static bool FolderMismatch(string type, IReadOnlyList<string> folders, string fileName)
    {
        if (folders.Count == 0)
        {
            return false;
        }

        var named = Table.Any(home => home.Type == type
            && NamesFolder(home)
            && home.File.IndexOf('{') >= 0
            && Holds(home, folders)
            && TakesFile(home, fileName));

        return named && !string.Equals(folders[folders.Count - 1], StemOf(fileName), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Whether a folder is one a named record owns: a home names it after the record. A use-case folder
    ///     inside an aggregate folder is one of these, and so is a feature folder inside a module.
    /// </summary>
    /// <param name="folders">The folder segments from the record layer down.</param>
    /// <returns>True when a named home holds the folder; otherwise false.</returns>
    public static bool IsRecordFolder(IReadOnlyList<string> folders) =>
        Table.Any(home => NamesFolder(home) && Holds(home, folders));

    /// <summary>Whether a record type may be scaffolded: it owns a home that <c>litespec new</c> writes.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>True when the type owns a file whose folder or name holds a home; otherwise false.</returns>
    public static bool NeedsHome(string type) =>
        Table.FirstOrDefault(entry => entry.Type == type && entry.Scaffold) is { } scaffold && NeedsHome(scaffold);

    /// <summary>The path a new record file of the type starts at, from the record layer down.</summary>
    /// <param name="type">The record type, kebab-case; it must own a scaffolded home.</param>
    /// <param name="home">The identifier home, such as the module, for a type that lives under one.</param>
    /// <param name="name">The identifier name, which names the record folder and the file.</param>
    /// <returns>The path with forward slashes, starting at the record layer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The type owns no scaffolded home.</exception>
    /// <exception cref="ArgumentException">The type lives under a home, and none is given.</exception>
    public static string ScaffoldPath(string type, string? home, string name)
    {
        var scaffold = Table.FirstOrDefault(entry => entry.Type == type && entry.Scaffold)
            ?? throw new ArgumentOutOfRangeException(nameof(type), $"Type '{type}' owns no file.");

        if (NeedsHome(scaffold) && home is null)
        {
            throw new ArgumentException($"Type '{type}' needs a home.", nameof(home));
        }

        var folders = Segments(scaffold.Folder).Select(segment => Fill(segment, home, name));
        var file = Fill(scaffold.File, home, name);

        return string.Join("/", folders.Append(file));
    }

    /// <summary>The phrase a message uses for where a type lives, such as <c>goals live in 'intent/goals/&lt;name&gt;/&lt;name&gt;.yaml'</c>.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <returns>The phrase, with each home shown in quotes and the homes joined by commas and <c>or</c>.</returns>
    public static string Phrase(string type)
    {
        var shown = Table
            .Where(home => home.Type == type)
            .Select(Show)
            .ToList();

        var list = shown.Count == 1
            ? shown[0]
            : $"{string.Join(", ", shown.Take(shown.Count - 1))} or {shown[shown.Count - 1]}";

        var subject = type switch
        {
            "product" => "the product",
            _ when Uncountable.Contains(type) => type,
            _ => type.Replace('-', ' ') + "s",
        };

        return $"{subject} {(Uncountable.Contains(type) ? "lives" : "live")} in {list}";
    }

    /// <summary>Whether the folders match the home's folder pattern.</summary>
    /// <param name="home">The home under test.</param>
    /// <param name="folders">The folders the record file sits in, from its record layer down.</param>
    /// <returns>True when the folders match the pattern; otherwise false.</returns>
    private static bool Holds(Home home, IReadOnlyList<string> folders)
    {
        var pattern = Segments(home.Folder);

        if (pattern.Length != folders.Count)
        {
            return false;
        }

        string? module = null;

        for (var at = 0; at < pattern.Length; at++)
        {
            var segment = folders[at];

            switch (pattern[at])
            {
                case "{module}":
                    module = segment;
                    break;
                case "{aggregate}":
                    if (module is null)
                    {
                        return false;
                    }

                    break;
                default:
                    // A token such as {name}, {entry-point} or {home}.{name} takes any segment.
                    if (pattern[at].IndexOf('{') < 0 && segment != pattern[at])
                    {
                        return false;
                    }

                    break;
            }
        }

        return true;
    }

    /// <summary>Whether a home takes a file of the name: a template or any file takes every name the home does not exclude.</summary>
    /// <param name="home">The home under test.</param>
    /// <param name="fileName">The file name under test.</param>
    /// <returns>True when the home takes the file; otherwise false.</returns>
    private static bool TakesFile(Home home, string fileName)
    {
        if (home.Except.Contains(fileName))
        {
            return false;
        }

        return home.File == "*" || home.File.IndexOf('{') >= 0 || home.File == fileName;
    }

    /// <summary>Whether a home names its folder after the record: its last folder segment is the name token.</summary>
    /// <param name="home">The home under test.</param>
    /// <returns>True when the last folder segment holds the name token.</returns>
    private static bool NamesFolder(Home home)
    {
        var segments = Segments(home.Folder);

        return segments.Length > 0 && segments[segments.Length - 1].IndexOf("{name}", StringComparison.Ordinal) >= 0;
    }

    /// <summary>The file name without its extension, which is the name a record folder carries.</summary>
    /// <param name="fileName">The record file name.</param>
    /// <returns>The stem, such as <c>return-in-minutes</c> for <c>return-in-minutes.yaml</c>.</returns>
    public static string StemOf(string fileName)
    {
        var dot = fileName.LastIndexOf('.');

        return dot > 0 ? fileName.Substring(0, dot) : fileName;
    }

    /// <summary>Fills the tokens of one path segment or file name for a record.</summary>
    /// <param name="text">The segment or file name with tokens.</param>
    /// <param name="home">The identifier home, or null.</param>
    /// <param name="name">The identifier name.</param>
    /// <returns>The text with each token replaced.</returns>
    private static string Fill(string text, string? home, string name) =>
        text.Replace("{module}", home ?? string.Empty)
            .Replace("{problem}", home ?? string.Empty)
            .Replace("{entry-point}", home ?? string.Empty)
            .Replace("{home}", home ?? string.Empty)
            .Replace("{aggregate}", name)
            .Replace("{name}", name);

    /// <summary>One home as the message shows it: the folder, then the file when the file is named.</summary>
    /// <param name="home">The home to show.</param>
    /// <returns>The home in quotes, such as <c>'intent/goals/&lt;name&gt;/&lt;name&gt;.yaml'</c> or <c>'behavior/modules/&lt;m&gt;/permissions.yaml'</c>.</returns>
    private static string Show(Home home)
    {
        var folder = string.Join("/", Segments(home.Folder).Select(Shown));

        if (home.File == "*")
        {
            return $"'{folder}/'";
        }

        var file = Shown(home.File);

        return folder.Length == 0 ? $"'{file}'" : $"'{folder}/{file}'";
    }

    /// <summary>One folder segment or file name as the message shows it, with each token in angle brackets.</summary>
    /// <param name="segment">The folder segment or file name.</param>
    /// <returns>The text, with a module token shown as <c>&lt;m&gt;</c> and any other token as its name in angle brackets.</returns>
    private static string Shown(string segment) =>
        Regex.Replace(
            segment,
            @"\{([a-z-]+)\}",
            match => match.Groups[1].Value == "module" ? "<m>" : $"<{match.Groups[1].Value}>");

    /// <summary>The segments of a folder pattern; the empty pattern is the record root.</summary>
    /// <param name="folder">The folder pattern.</param>
    /// <returns>The segments in order.</returns>
    private static string[] Segments(string folder) =>
        folder.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Builds one home that holds records in every module.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folder">The folder pattern.</param>
    /// <param name="file">The file name or template.</param>
    /// <param name="except">The file names the home does not take.</param>
    /// <returns>The home.</returns>
    private static Home At(string type, string folder, string file, params string[] except) =>
        new(type, folder, file, except, false);

    /// <summary>Builds one home that holds records in every module, which litespec new writes.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folder">The folder pattern.</param>
    /// <param name="file">The file name or template.</param>
    /// <returns>The home.</returns>
    private static Home Scaffold(string type, string folder, string file) =>
        new(type, folder, file, [], true);

    /// <summary>Whether a home entry places its file under a module or entry-point folder, or names it after its home.</summary>
    /// <param name="home">The home entry.</param>
    /// <returns>True when the path needs the home.</returns>
    private static bool NeedsHome(Home home) =>
        home.Folder.Contains("{module}") || home.Folder.Contains("{problem}") || home.Folder.Contains("{entry-point}") || home.File.Contains("{home}");

    /// <summary>
    ///     One home of a record type: the folder pattern below the record layer and the file rule.
    /// </summary>
    /// <param name="Type">The record type, kebab-case.</param>
    /// <param name="Folder">The folder from the record layer down, with <c>{module}</c>, <c>{aggregate}</c>, <c>{entry-point}</c>, <c>{home}</c> and <c>{name}</c> segments; empty for the record root.</param>
    /// <param name="File">A literal file name, a template such as <c>{name}.yaml</c>, or <c>*</c> for any file.</param>
    /// <param name="Except">The file names the home does not take.</param>
    /// <param name="Scaffold">Whether <c>litespec new</c> writes a record of the type into this home.</param>
    private sealed record Home(string Type, string Folder, string File, IReadOnlyList<string> Except, bool Scaffold);
}
