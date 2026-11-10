using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     Where a record file lives: the home folder of each page-owning record
///     type, the entry types that live inside another file, and the shared
///     identifier checks of <c>litespec new</c> and <c>litespec rename</c>.
/// </summary>
/// <follows>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</follows>
/// <follows>litespec/core/authoring#rewrite-every-link-on-a-rename</follows>
static class RecordPages
{
    /// <summary>Record types that own a file <c>litespec new</c> can scaffold.</summary>
    public static readonly HashSet<string> PageTypes = new(StringComparer.Ordinal)
    {
        "product", "goal", "problem", "feature", "constraint", "research",
        "decision", "journey", "actor", "module", "aggregate", "use-case",
        "reaction", "job", "notification", "role", "external-system",
        "runbook", "departure",
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
        ["requirement"] = "a feature file under .litespec/intent/modules/<m>/features/",
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

    /// <summary>Folder names the citation walk skips, covering build output, installed packages and tool state.</summary>
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "TestResults", "node_modules", ".git",
        ".svelte-kit", "build", "dist", "coverage",
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
            "example-cast" => Path.Combine(behavior, $"{name}.md"),

            _ => throw new ArgumentOutOfRangeException(nameof(style), $"Style '{style}' owns no document."),
        };
    }

    /// <summary>
    ///     The file a renamed record moves to, or null when the file stays where
    ///     it is. A file that already sits inside its home's module area keeps its
    ///     folder and takes the new file name, so a use case beside its aggregate
    ///     is not dragged back under <c>reads/</c> by its own rename.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the file.</param>
    /// <param name="oldHome">The identifier home before the rename, or null for a type without a home.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <param name="oldFile">The current absolute path of the file.</param>
    /// <returns>The absolute path the file moves to, or null when the file stays at its current path.</returns>
    public static string? RenameTarget(
        ConsumerConfig config, string type, string? oldHome, string? newHome, string newName, string oldFile)
    {
        // A type whose identifier carries no home, such as an integration event, which lives under a
        // module folder its id does not name, stays in the folder it already sits in.
        if (newHome is null && RecordFolders.NeedsHome(type))
        {
            var folder = Path.GetDirectoryName(oldFile);

            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            var renamed = Path.Combine(folder, newName + Path.GetExtension(oldFile));

            return string.Equals(renamed, oldFile, StringComparison.Ordinal) ? null : renamed;
        }

        var placed = PagePath(config, type, newHome, newName);

        // A technical decision lives under Implementation and stays there when renamed.
        if (type == "decision" && IsTechnicalDecision(config, oldFile))
        {
            placed = Path.Combine(DocsRoot(config), "implementation", "decisions", Path.GetFileName(placed));
        }

        if (string.Equals(placed, oldFile, StringComparison.Ordinal))
        {
            return null;
        }

        var oldFolder = Path.GetDirectoryName(oldFile);
        var placedFolder = Path.GetDirectoryName(placed);

        if (oldFolder is null
            || string.Equals(oldFolder, placedFolder, StringComparison.Ordinal)
            || !StaysInFolder(config, type, oldHome, newHome, oldFolder))
        {
            return placed;
        }

        var stayed = Path.Combine(oldFolder, Path.GetFileName(placed));

        return string.Equals(stayed, oldFile, StringComparison.Ordinal) ? null : stayed;
    }

    /// <summary>Whether a decision file sits under the implementation decisions folder.</summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="file">The absolute path of the decision file.</param>
    /// <returns>True when the file sits under the implementation decisions folder; otherwise false.</returns>
    static bool IsTechnicalDecision(ConsumerConfig config, string file) =>
        file.StartsWith(Path.Combine(DocsRoot(config), "implementation", "decisions") + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    /// <summary>
    ///     The folder a renamed module or aggregate file moves to. The folder moves
    ///     whole, so the use cases beside a renamed aggregate move with it.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the folder, either module or aggregate.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <returns>The absolute path of the folder the file moves to after the rename.</returns>
    public static string RenameFolderTarget(ConsumerConfig config, string type, string? newHome, string newName) =>
        Path.GetDirectoryName(PagePath(config, type, newHome, newName))!;

    /// <summary>
    ///     Whether the renamed file keeps its folder: the home is unchanged and the
    ///     folder sits inside that home's module area.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the file.</param>
    /// <param name="oldHome">The identifier home before the rename, or null for a type without a home.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="folder">The folder that currently holds the file.</param>
    /// <returns>True when the home is unchanged and the folder sits inside that home module area; otherwise false.</returns>
    static bool StaysInFolder(ConsumerConfig config, string type, string? oldHome, string? newHome, string folder)
    {
        if (type is "module" or "aggregate"
            || oldHome is null || newHome is null
            || !string.Equals(oldHome, newHome, StringComparison.Ordinal))
        {
            return false;
        }

        var area = Path.Combine(config.BehaviorDocs, "modules", newHome) + Path.DirectorySeparatorChar;

        return (folder + Path.DirectorySeparatorChar).StartsWith(area, StringComparison.Ordinal);
    }

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
                || SkippedFolders.Contains(name))
            {
                continue;
            }

            Walk(child, found);
        }
    }
}
