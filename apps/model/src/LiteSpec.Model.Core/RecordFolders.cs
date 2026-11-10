using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     The one map from record type to the folder and file a record of the type lives in.
///     The folder checks read it to place a record, the scaffolder reads it to write one,
///     and the qualifier reads the fixed folders of a behavior module.
/// </summary>
/// <follows>litespec/core/model#write-every-record-in-its-home</follows>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
public static class RecordFolders
{
    /// <summary>The folders a behavior module holds besides its aggregates: any other subfolder of a module is an aggregate.</summary>
    public static readonly HashSet<string> BehaviorModuleFolders = new(StringComparer.Ordinal)
    {
        "use-cases", "rules", "reactions", "jobs", "notifications", "settings",
        "integration-events", "external-systems",
    };

    /// <summary>Record types whose English plural is the type itself, or that take a singular verb.</summary>
    private static readonly HashSet<string> Uncountable = new(StringComparer.Ordinal) { "product", "research" };

    /// <summary>
    ///     Every home of every record type. A type with several homes lists them in the order the
    ///     messages show them; a flat home applies only to a module that keeps its records flat.
    /// </summary>
    private static readonly IReadOnlyList<Home> Table =
    [
        Scaffold("product", "intent", "product.yaml"),
        Scaffold("goal", "intent/goals", "{name}.yaml"),
        At("goal", "intent/modules/{module}/goals", "{name}.yaml"),
        Scaffold("problem", "intent/problems", "{name}.yaml"),
        At("problem", "intent/modules/{module}/problems", "{name}.yaml"),
        Scaffold("feature", "intent/modules/{module}/features", "{name}.yaml"),
        At("requirement", "intent/modules/{module}/features", "*"),
        Scaffold("constraint", "intent/constraints", "{name}.yaml"),
        At("constraint", "intent/modules/{module}/constraints", "{name}.yaml"),
        Scaffold("decision", "intent/decisions", "{name}.yaml"),
        At("decision", "intent/modules/{module}/decisions", "{name}.yaml"),
        At("decision", "implementation/decisions", "{name}.yaml"),
        At("decision", "implementation/modules/{module}/decisions", "{name}.yaml"),
        Scaffold("research", "intent/research", "{name}.yaml"),
        At("research", "intent/modules/{module}/research", "{name}.yaml"),
        Scaffold("journey", "intent/journeys", "{name}.yaml"),
        Scaffold("actor", "intent/actors", "{name}.yaml"),
        Scaffold("role", "intent/roles", "{name}.yaml"),
        Scaffold("module", "intent/modules/{name}", "module.yaml"),
        At("term", "", "glossary.yaml"),
        At("aggregate", "behavior/modules/{module}/{aggregate}", "aggregate.yaml"),
        Flat("aggregate", "behavior/modules/{module}", "aggregate.yaml"),
        At("use-case", "behavior/modules/{module}/{aggregate}", "*", "aggregate.yaml"),
        Scaffold("use-case", "behavior/modules/{module}/use-cases", "{name}.yaml"),
        Flat("use-case", "behavior/modules/{module}", "*", "aggregate.yaml", "permissions.yaml"),
        At("result", "behavior/modules/{module}/{aggregate}", "*", "aggregate.yaml"),
        At("result", "behavior/modules/{module}/use-cases", "*"),
        Flat("result", "behavior/modules/{module}", "*", "aggregate.yaml", "permissions.yaml"),
        At("scenario", "behavior/modules/{module}/{aggregate}", "*", "aggregate.yaml"),
        At("scenario", "behavior/modules/{module}/use-cases", "*"),
        Flat("scenario", "behavior/modules/{module}", "*", "aggregate.yaml", "permissions.yaml"),
        At("event", "behavior/modules/{module}/{aggregate}", "aggregate.yaml"),
        At("event", "behavior/modules/{module}/{aggregate}/events", "*"),
        Flat("event", "behavior/modules/{module}", "aggregate.yaml"),
        Flat("event", "behavior/modules/{module}/events", "*"),
        At("rule", "behavior/rules", "*"),
        At("rule", "behavior/modules/{module}/rules", "*"),
        At("rule", "behavior/modules/{module}/{aggregate}/rules", "*"),
        Scaffold("reaction", "behavior/modules/{module}/reactions", "{name}.yaml"),
        Scaffold("job", "behavior/modules/{module}/jobs", "{name}.yaml"),
        Scaffold("notification", "behavior/modules/{module}/notifications", "{name}.yaml"),
        At("setting", "behavior/modules/{module}/settings", "*"),
        Scaffold("integration-event", "behavior/modules/{module}/integration-events", "{name}.yaml"),
        At("permission", "behavior/modules/{module}", "permissions.yaml"),
        At("entry-point", "behavior/entry-points/{entry-point}", "entry-point.yaml"),
        Scaffold("screen", "behavior/entry-points/{entry-point}/screens", "{home}.{name}.yaml"),
        Scaffold("external-system", "behavior/external-systems", "{name}.yaml"),
        At("external-system", "behavior/modules/{module}/external-systems", "{name}.yaml"),
        Scaffold("runbook", "implementation/runbooks", "{name}.yaml"),
        At("runbook", "implementation/modules/{module}/runbooks", "{name}.yaml"),
        Scaffold("departure", "governance/departures", "{name}.yaml"),
        At("departure", "governance/modules/{module}/departures", "{name}.yaml"),
    ];

    /// <summary>Whether a record file of the type sits in the folder, given the modules that keep their records flat.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folders">The folders the record file sits in, from its record layer down.</param>
    /// <param name="fileName">The record file name.</param>
    /// <param name="flat">The modules that keep their records flat.</param>
    /// <returns>True when one home of the type holds the file; otherwise false.</returns>
    public static bool Places(string type, IReadOnlyList<string> folders, string fileName, HashSet<string> flat) =>
        Table.Any(home => home.Type == type && Holds(home, folders, flat) && TakesFile(home, fileName));

    /// <summary>Whether a type is placed under a home, a module or an entry point, that its file path needs.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>True when the type owns a file whose folder or name holds a home; otherwise false.</returns>
    public static bool NeedsHome(string type) =>
        Table.FirstOrDefault(entry => entry.Type == type && entry.Scaffold && !entry.Flat) is { } scaffold && NeedsHome(scaffold);

    /// <summary>The path a new record file of the type starts at, from the record layer down.</summary>
    /// <param name="type">The record type, kebab-case; it must own a scaffolded home.</param>
    /// <param name="home">The identifier home, such as the module, for a type that lives under one.</param>
    /// <param name="name">The identifier name, which names the file or the folder.</param>
    /// <returns>The path with forward slashes, starting at the record layer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The type owns no scaffolded home.</exception>
    /// <exception cref="ArgumentException">The type lives under a home, and none is given.</exception>
    public static string ScaffoldPath(string type, string? home, string name)
    {
        var scaffold = Table.FirstOrDefault(entry => entry.Type == type && entry.Scaffold && !entry.Flat)
            ?? throw new ArgumentOutOfRangeException(nameof(type), $"Type '{type}' owns no file.");

        if (NeedsHome(scaffold) && home is null)
        {
            throw new ArgumentException($"Type '{type}' needs a home.", nameof(home));
        }

        var folders = Segments(scaffold.Folder)
            .Select(segment => segment switch
            {
                "{module}" or "{entry-point}" => home!,
                "{aggregate}" or "{name}" => name,
                _ => segment,
            });

        var file = scaffold.File.Replace("{home}", home ?? string.Empty)
            .Replace("{name}", name);

        return string.Join("/", folders.Append(file));
    }

    /// <summary>The phrase a message uses for where a type lives, such as <c>goals live in 'intent/goals/'</c>.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <returns>The phrase, with each home shown in quotes and the homes joined by commas and <c>or</c>.</returns>
    public static string Phrase(string type)
    {
        var shown = Table
            .Where(home => home.Type == type && !home.Flat)
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

    /// <summary>Whether the folders match the home's folder pattern, and the module is flat when the home needs it.</summary>
    /// <param name="home">The home under test.</param>
    /// <param name="folders">The folders the record file sits in, from its record layer down.</param>
    /// <param name="flat">The modules that keep their records flat.</param>
    /// <returns>True when the folders match the pattern; otherwise false.</returns>
    private static bool Holds(Home home, IReadOnlyList<string> folders, HashSet<string> flat)
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
                    if (module is null || !IsAggregateName(segment, module, flat))
                    {
                        return false;
                    }

                    break;
                case "{name}":
                case "{entry-point}":
                    break;
                default:
                    if (segment != pattern[at])
                    {
                        return false;
                    }

                    break;
            }
        }

        return !home.Flat || (module is not null && flat.Contains(module));
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

    /// <summary>Whether a folder name is an aggregate in a module: not a fixed behavior folder, and not the events folder of a flat module.</summary>
    /// <param name="name">The folder name under test.</param>
    /// <param name="module">The module the folder sits in.</param>
    /// <param name="flat">The modules that keep their records flat.</param>
    /// <returns>True when the name names an aggregate; otherwise false.</returns>
    private static bool IsAggregateName(string name, string module, HashSet<string> flat) =>
        !BehaviorModuleFolders.Contains(name) && (name != "events" || !flat.Contains(module));

    /// <summary>One home as the message shows it: the folder, then the file when the file is named.</summary>
    /// <param name="home">The home to show.</param>
    /// <returns>The home in quotes, such as <c>'intent/goals/'</c> or <c>'behavior/modules/&lt;m&gt;/permissions.yaml'</c>.</returns>
    private static string Show(Home home)
    {
        var folder = string.Join("/", Segments(home.Folder).Select(Shown));
        var named = home.File != "*" && home.File.IndexOf('{') < 0;

        if (named)
        {
            return folder.Length == 0 ? $"'{home.File}'" : $"'{folder}/{home.File}'";
        }

        return $"'{folder}/'";
    }

    /// <summary>One folder segment as the message shows it, with each token in angle brackets.</summary>
    /// <param name="segment">The folder segment.</param>
    /// <returns>The segment, with a token shown as <c>&lt;m&gt;</c> for a module or <c>&lt;name&gt;</c> for any other token.</returns>
    private static string Shown(string segment) => segment switch
    {
        "{module}" => "<m>",
        _ when segment.StartsWith("{", StringComparison.Ordinal) => $"<{segment.Substring(1, segment.Length - 2)}>",
        _ => segment,
    };

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
        new(type, folder, file, false, except, false);

    /// <summary>Builds one home that holds records in every module, which litespec new writes.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folder">The folder pattern.</param>
    /// <param name="file">The file name or template.</param>
    /// <returns>The home.</returns>
    private static Home Scaffold(string type, string folder, string file) =>
        new(type, folder, file, false, [], true);

    /// <summary>Builds one home that holds records only in a module that keeps its records flat.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="folder">The folder pattern.</param>
    /// <param name="file">The file name or template.</param>
    /// <param name="except">The file names the home does not take.</param>
    /// <returns>The home.</returns>
    private static Home Flat(string type, string folder, string file, params string[] except) =>
        new(type, folder, file, true, except, false);

    /// <summary>Whether a home entry places its file under a module or entry-point folder, or names it after its home.</summary>
    /// <param name="home">The home entry.</param>
    /// <returns>True when the path needs the home.</returns>
    private static bool NeedsHome(Home home) =>
        home.Folder.Contains("{module}") || home.Folder.Contains("{entry-point}") || home.File.Contains("{home}");

    /// <summary>
    ///     One home of a record type: the folder pattern below the record layer and the file rule.
    /// </summary>
    /// <param name="Type">The record type, kebab-case.</param>
    /// <param name="Folder">The folder from the record layer down, with <c>{module}</c>, <c>{aggregate}</c>, <c>{entry-point}</c> and <c>{name}</c> segments; empty for the record root.</param>
    /// <param name="File">A literal file name, a template such as <c>{name}.yaml</c>, or <c>*</c> for any file.</param>
    /// <param name="Flat">Whether the home applies only to a module that keeps its records flat.</param>
    /// <param name="Except">The file names the home does not take.</param>
    /// <param name="Scaffold">Whether <c>litespec new</c> writes a record of the type into this home.</param>
    private sealed record Home(string Type, string Folder, string File, bool Flat, IReadOnlyList<string> Except, bool Scaffold);
}
