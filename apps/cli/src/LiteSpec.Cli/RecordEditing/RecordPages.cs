using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.RecordEditing;

/// <summary>
///     Where a record file lives: the home folder of each page-owning record
///     type, the entry types that live inside another file, and the shared
///     identifier checks of <c>litespec new</c> and <c>litespec rename</c>.
/// </summary>
/// <follows>litespec/core/model#scaffold-pages-and-reserve-identifiers</follows>
/// <follows>litespec/core/model#rewrite-every-link-on-a-rename</follows>
static class RecordPages
{
    /// <summary>Record types that own a file <c>litespec new</c> can scaffold.</summary>
    public static readonly HashSet<string> PageTypes = new(StringComparer.Ordinal)
    {
        "product", "goal", "vision", "problem", "feature", "requirement", "constraint", "research",
        "decision", "journey", "actor", "module", "aggregate", "use-case",
        "reaction", "job", "notification", "role", "external-system",
        "runbook", "departure", "screen",
    };

    /// <summary>Document styles <c>litespec new</c> scaffolds as a Markdown document.</summary>
    public static readonly HashSet<string> DocumentStyles = new(StringComparer.Ordinal)
    {
        "index", "tutorial", "how-to", "reference", "command",
        "configuration", "glossary", "example-cast",
    };

    /// <summary>Where an entry type lives, for the refusal message.</summary>
    public static readonly Dictionary<string, string> EntryHomes = new(StringComparer.Ordinal)
    {
        ["rule"] = "its aggregate file, or a rule file under .litespec/behavior/rules/ for a cross-aggregate rule",
        ["result"] = "its use-case file",
        ["scenario"] = "its use-case, reaction or job file",
        ["event"] = "its aggregate file",
        ["integration-event"] = "one file per event under .litespec/behavior/modules/<m>/integration-events/",
        ["setting"] = "a setting-set file under .litespec/behavior/modules/<m>/settings/",
        ["permission"] = ".litespec/behavior/modules/<m>/permissions.yaml",
        ["entry-point"] = ".litespec/behavior/entry-points/<entry-point>/entry-point.yaml",
        ["term"] = "the glossary",
    };

    /// <summary>The file kinds that cite record identifiers: files, data, and the code that anchors to records.</summary>
    private static readonly HashSet<string> CitingExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".yaml", ".json", ".cs", ".ts", ".tsx", ".js", ".mjs", ".svelte", ".razor",
    };

    /// <summary>The documentation root new files are written under.</summary>
    /// <param name="config">The consumer configuration that holds the record roots.</param>
    /// <returns>The first configured record root, or the .litespec folder under the consumer root when none is configured.</returns>
    public static string DocsRoot(ConsumerConfig config) =>
        config.RecordRoots.Count > 0 ? config.RecordRoots[0] : Path.Combine(config.Root, ".litespec");

    /// <summary>
    ///     The absolute path of the file a record owns, from the folder map. A use-case file starts
    ///     under <c>use-cases/</c> beside its module; the author moves the file beside its aggregate
    ///     once the use case names one.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the file.</param>
    /// <param name="home">The identifier home segment, such as the module name, or null for a type without a home.</param>
    /// <param name="name">The identifier name segment, which becomes the file name for most types.</param>
    /// <returns>The absolute path where a file of the type starts.</returns>
    public static string PagePath(ConsumerConfig config, string type, string? home, string name)
    {
        var segments = RecordFolders.ScaffoldPath(type, home, name).Split('/');

        return segments[0] == "behavior"
            ? Path.Combine([config.BehaviorDocs, .. segments[1..]])
            : Path.Combine([DocsRoot(config), .. segments]);
    }

    /// <summary>
    ///     The absolute path of the document a style owns. An index is the
    ///     README.md of its section folder; every other document takes the name
    ///     as its file stem under its home folder.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="style">The document style that owns the document.</param>
    /// <param name="name">The kebab-case name, which becomes the file stem or the section folder.</param>
    /// <returns>The absolute path where a document of the style starts.</returns>
    public static string DocumentPath(ConsumerConfig config, string style, string name)
    {
        var docs = DocsRoot(config);
        var behavior = config.BehaviorDocs;

        return style switch
        {
            "index" => Path.Combine(docs, name, "README.md"),
            "tutorial" => Path.Combine(docs, "guide", $"{name}.md"),
            "how-to" => Path.Combine(docs, "guide", $"{name}.md"),
            "reference" => Path.Combine(docs, "reference", $"{name}.md"),
            "command" => Path.Combine(docs, "tools", $"{name}.md"),
            "configuration" => Path.Combine(docs, "tools", $"{name}.md"),
            "glossary" => Path.Combine(behavior, $"{name}.md"),
            "example-cast" => Path.Combine(docs, $"{name}.md"),

            _ => throw new ArgumentOutOfRangeException(nameof(style), $"Style '{style}' owns no document."),
        };
    }

    /// <summary>
    ///     The stem a record's file takes at its identifier: the name the scaffold pattern gives the file, such as
    ///     <c>library-app.loan-list</c> for a screen, or the bare name for a type whose file takes the name.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type.</param>
    /// <param name="home">The identifier home, or null for a type without a home.</param>
    /// <param name="name">The identifier name.</param>
    /// <returns>The file stem the record takes, which also names its own folder.</returns>
    public static string StemOf(ConsumerConfig config, string type, string? home, string name)
    {
        if (!RecordFolders.Scaffolds(type) || (RecordFolders.NeedsHome(type) && home is null))
        {
            return name;
        }

        return Path.GetFileNameWithoutExtension(PagePath(config, type, home, name));
    }

    /// <summary>
    ///     The folder a record's own folder sits in once a rename moves the record to the home its new identifier
    ///     names. A plain rename does not call this: it keeps the folder the record already sits in.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the file.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <param name="oldFile">The current absolute path of the file.</param>
    /// <returns>The absolute path of the folder that holds the record's own folder at its new home.</returns>
    public static string? OwnerContainer(ConsumerConfig config, string type, string? newHome, string newName, string oldFile)
    {
        // A technical decision lives under Implementation and stays there when renamed.
        if (type == "decision" && IsTechnicalDecision(config, oldFile))
        {
            return Path.Combine(DocsRoot(config), "implementation", "decisions");
        }

        // A type whose identifier carries no home, such as an integration event, keeps the folder it sits in.
        if (newHome is null && RecordFolders.NeedsHome(type))
        {
            return ContainerOf(oldFile);
        }

        // The placed path is <container>/<name>/<file>, so the container is two folders up.
        var placed = PagePath(config, type, newHome, newName);

        return Path.GetDirectoryName(Path.GetDirectoryName(placed));
    }

    /// <summary>The path of a named record's file: its own folder under the container, holding the file that carries the name.</summary>
    /// <param name="container">The folder the record's own folder sits in.</param>
    /// <param name="name">The record name, which names the folder and the file.</param>
    /// <param name="extension">The file extension, including the dot.</param>
    /// <returns>The path <c>&lt;container&gt;/&lt;name&gt;/&lt;name&gt;&lt;extension&gt;</c>.</returns>
    public static string PlaceIn(string container, string name, string extension) =>
        Path.Combine(container, name, name + extension);

    /// <summary>
    ///     The folder of the feature a requirement belongs to: the folder of the feature record
    ///     <c>feature/&lt;feature&gt;</c>. A requirement's own folder sits inside it.
    /// </summary>
    /// <param name="records">Every record the model read.</param>
    /// <param name="feature">The feature name the requirement identifier names.</param>
    /// <returns>The requirements folder of that feature, or null when no feature record holds that name.</returns>
    public static string? FeatureFolder(IReadOnlyList<Record> records, string feature) =>
        records.FirstOrDefault(record => !record.Derived && record.Id == $"feature/{feature}") is { } found
            ? Path.Combine(Path.GetDirectoryName(found.File)!, "requirements")
            : null;

    /// <summary>
    ///     The folder that holds a record's own folder: the container a named record's folder sits in, or the
    ///     folder of a flat record file. A file whose folder carries its stem is a named record's file.
    /// </summary>
    /// <param name="file">The absolute record file path.</param>
    /// <returns>The container folder, or null when the file has no folder.</returns>
    public static string? ContainerOf(string file)
    {
        var folder = Path.GetDirectoryName(file);

        if (string.IsNullOrEmpty(folder))
        {
            return null;
        }

        var stem = Path.GetFileNameWithoutExtension(file);

        return string.Equals(Path.GetFileName(folder), stem, StringComparison.Ordinal)
            ? Path.GetDirectoryName(folder)
            : folder;
    }

    /// <summary>Whether a decision file sits under the implementation decisions folder.</summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="file">The absolute path of the decision file.</param>
    /// <returns>True when the file sits under the implementation decisions folder; otherwise false.</returns>
    static bool IsTechnicalDecision(ConsumerConfig config, string file) =>
        file.StartsWith(Path.Combine(DocsRoot(config), "implementation", "decisions") + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    /// <summary>
    ///     The grammar error in an identifier's home and name, or null when both
    ///     follow the record identifier grammar.
    /// </summary>
    /// <param name="type">The record type segment of the identifier.</param>
    /// <param name="home">The identifier home segment, or null when the identifier carries no home.</param>
    /// <param name="name">The identifier name segment.</param>
    /// <param name="full">The full identifier text, used in the error message.</param>
    /// <returns>The grammar error message, or null when the home and name follow the identifier grammar.</returns>
    public static string? GrammarError(string type, string? home, string name, string full)
    {
        if (RecordTypes.WithHome.Contains(type) && home is null)
        {
            return $"Identifier '{full}' needs a home: use {type}/<home>.<name>.";
        }

        if (!RecordTypes.WithHome.Contains(type) && home is not null)
        {
            return $"Identifier '{full}' carries no home: use {type}/<name>.";
        }

        if (home is not null && (!Identifiers.ValidName(home, false) || Identifiers.AllDigitsName(home)))
        {
            return $"Identifier '{full}' is not a valid {type} identifier.";
        }

        if (!Identifiers.ValidName(name, type == "decision") || Identifiers.AllDigitsName(name))
        {
            return type == "decision"
                ? $"Identifier '{full}' is not a valid decision identifier: use decision/<number>-<name>."
                : $"Identifier '{full}' is not a valid {type} identifier.";
        }

        return null;
    }

    /// <summary>
    ///     Rewrites every citation of <paramref name="oldId"/> as
    ///     <paramref name="newId"/>. A match inside a longer identifier is left
    ///     alone; a journey step keeps its fragment.
    /// </summary>
    /// <param name="text">The file text to rewrite.</param>
    /// <param name="oldId">The identifier to replace.</param>
    /// <param name="newId">The identifier that replaces each standalone citation.</param>
    /// <param name="count">The number of citations rewritten.</param>
    /// <returns>The text with every standalone citation of the old identifier rewritten as the new identifier.</returns>
    public static string ReplaceCitations(string text, string oldId, string newId, out int count)
    {
        count = 0;

        if (text.IndexOf(oldId, StringComparison.Ordinal) < 0)
        {
            return text;
        }

        var built = new System.Text.StringBuilder(text.Length);
        var index = 0;

        while (true)
        {
            var found = text.IndexOf(oldId, index, StringComparison.Ordinal);

            if (found < 0)
            {
                built.Append(text, index, text.Length - index);
                break;
            }

            var before = found > 0 ? text[found - 1] : '\0';
            var afterIndex = found + oldId.Length;
            var after = afterIndex < text.Length ? text[afterIndex] : '\0';

            if (IsIdChar(before) || before == '/' || IsIdChar(after) || after == '/')
            {
                built.Append(text, index, afterIndex - index);
                index = afterIndex;
                continue;
            }

            built.Append(text, index, found - index);
            built.Append(newId);
            count += 1;
            index = afterIndex;
        }

        return built.ToString();
    }

    /// <summary>Whether the character continues an identifier.</summary>
    /// <param name="value">The character beside a candidate citation.</param>
    /// <returns>True when the character continues an identifier, covering letters, digits, underscore, hyphen and dot; otherwise false.</returns>
    static bool IsIdChar(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '_' or '-' or '.';

    /// <summary>
    ///     Every file a record identifier can be cited in under the root, in a stable order: record files,
    ///     explanation files, documents, data, and the source and test files whose anchor and proof tags cite
    ///     records. Build output, installed packages, and hidden folders are left out.
    /// </summary>
    /// <param name="root">The absolute path of the folder to walk, typically the consumer root.</param>
    /// <returns>Every citing file under the root in ordinal path order, or an empty list when the root does not exist.</returns>
    public static IReadOnlyList<string> ProjectFiles(string root)
    {
        var found = new List<string>();

        if (Directory.Exists(root))
        {
            Walk(root, found);
        }

        found.Sort(StringComparer.Ordinal);

        return found;
    }

    /// <summary>Collects the citing files in one folder and recurses into its non-skipped subfolders.</summary>
    /// <param name="directory">The absolute path of the folder to walk.</param>
    /// <param name="found">The list that receives each citing file path found.</param>
    static void Walk(string directory, List<string> found)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (CitingExtensions.Contains(Path.GetExtension(file)))
            {
                found.Add(file);
            }
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(child) ?? "";

            // The LiteSpec folder is a dot folder the walk must enter: it holds the
            // records. Its generated subfolders hold no citing text.
            if (string.Equals(Path.GetFileName(directory), ".litespec", StringComparison.Ordinal)
                && (name is "model" or "book"))
            {
                continue;
            }

            if ((name.StartsWith('.') && !string.Equals(name, ".litespec", StringComparison.Ordinal))
                || SourceTree.SkippedFolders.Contains(name))
            {
                continue;
            }

            Walk(child, found);
        }
    }
}
