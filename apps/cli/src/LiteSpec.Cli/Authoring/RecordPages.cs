using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     Where a record page lives: the home folder of each page-owning record
///     type, the entry types that live inside another page, and the shared
///     identifier checks of <c>litespec new</c> and <c>litespec rename</c>.
/// </summary>
/// <follows>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</follows>
/// <follows>litespec/core/authoring#rewrite-every-link-on-a-rename</follows>
static class RecordPages
{
    /// <summary>Record types that own a page <c>litespec new</c> can scaffold.</summary>
    public static readonly HashSet<string> PageTypes = new(StringComparer.Ordinal)
    {
        "product", "goal", "problem", "feature", "topic", "constraint", "research",
        "decision", "journey", "actor", "module", "aggregate", "use-case",
        "reaction", "job", "notification", "role", "external-system",
        "runbook", "exception",
    };

    /// <summary>Where an entry type lives, for the refusal message.</summary>
    public static readonly Dictionary<string, string> EntryHomes = new(StringComparer.Ordinal)
    {
        ["requirement"] = "a topic page under docs/intent/",
        ["rule"] = "its aggregate page, or a rule page under docs/behavior/rules/ for a cross-aggregate rule",
        ["result"] = "its use-case page",
        ["scenario"] = "its use-case, reaction or job page",
        ["event"] = "its aggregate page",
        ["integration-event"] = "one page per event under docs/behavior/integration-events/",
        ["setting"] = "its module page, or a setting-set page",
        ["permission"] = "docs/behavior/access/permissions.md",
        ["entry-point"] = "docs/behavior/access/entry-points.md",
        ["term"] = "the glossary",
    };

    /// <summary>Folder names the citation walk skips, covering build output, installed packages and tool state.</summary>
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "TestResults", "node_modules", ".git", ".model",
        ".svelte-kit", "build", "dist", "coverage",
    };

    /// <summary>The file kinds that cite record identifiers: pages, data, and the code that anchors to records.</summary>
    private static readonly HashSet<string> CitingExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".json", ".cs", ".ts", ".tsx", ".js", ".mjs", ".svelte", ".razor",
    };

    /// <summary>The documentation root new pages are written under.</summary>
    /// <param name="config">The consumer configuration that holds the record roots.</param>
    /// <returns>The first configured record root, or the docs folder under the consumer root when none is configured.</returns>
    public static string DocsRoot(ConsumerConfig config) =>
        config.RecordRoots.Count > 0 ? config.RecordRoots[0] : Path.Combine(config.Root, "docs");

    /// <summary>
    ///     The absolute path of the page a record owns. A use-case page starts
    ///     under <c>reads/</c> beside its module; the author moves the file beside
    ///     its aggregate once the use case names one. A topic page starts
    ///     directly under <c>intent/</c>; the author moves it into its area folder.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the page.</param>
    /// <param name="home">The identifier home segment, such as the module name, or null for a type without a home.</param>
    /// <param name="name">The identifier name segment, which becomes the file name for most types.</param>
    /// <returns>The absolute path where a page of the type starts.</returns>
    public static string PagePath(ConsumerConfig config, string type, string? home, string name)
    {
        var docs = DocsRoot(config);
        var behavior = config.BehaviorDocs;

        return type switch
        {
            "product" => Path.Combine(docs, "intent", "brief.md"),
            "goal" => Path.Combine(docs, "intent", "goals", $"{name}.md"),
            "problem" => Path.Combine(docs, "intent", "problems", $"{name}.md"),
            "feature" => Path.Combine(docs, "intent", "features", $"{name}.md"),
            "topic" => Path.Combine(docs, "intent", $"{name}.md"),
            "constraint" => Path.Combine(docs, "intent", "constraints", $"{name}.md"),
            "research" => Path.Combine(docs, "intent", "research", $"{name}.md"),
            "decision" => Path.Combine(docs, "intent", "decisions", $"{name}.md"),
            "journey" => Path.Combine(docs, "intent", "journeys", $"{name}.md"),
            "actor" => Path.Combine(docs, "intent", "actors", $"{name}.md"),
            "module" => Path.Combine(behavior, "modules", name, "README.md"),
            "aggregate" => Path.Combine(behavior, "modules", home!, name, "README.md"),
            "use-case" => Path.Combine(behavior, "modules", home!, "reads", $"{name}.md"),
            "reaction" => Path.Combine(behavior, "modules", home!, "reactions", $"{name}.md"),
            "job" => Path.Combine(behavior, "modules", home!, "jobs", $"{name}.md"),
            "notification" => Path.Combine(behavior, "modules", home!, "notifications", $"{name}.md"),
            "role" => Path.Combine(behavior, "access", "roles", $"{name}.md"),
            "integration-event" => Path.Combine(behavior, "integration-events", $"{name}.md"),
            "screen" => Path.Combine(behavior, "screens", $"{home}.{name}.md"),
            "external-system" => Path.Combine(behavior, "external-systems", $"{name}.md"),
            "runbook" => Path.Combine(behavior, "runbooks", $"{name}.md"),
            "exception" => Path.Combine(docs, "governance", "exceptions", $"{name}.md"),

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type '{type}' owns no page."),
        };
    }

    /// <summary>
    ///     The file a renamed page moves to, or null when the page stays where it
    ///     is. A page that already sits inside its home's module area keeps its
    ///     folder and takes the new file name, so a use case beside its aggregate
    ///     is not dragged back under <c>reads/</c> by its own rename.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the page.</param>
    /// <param name="oldHome">The identifier home before the rename, or null for a type without a home.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <param name="oldFile">The current absolute path of the page.</param>
    /// <returns>The absolute path the page moves to, or null when the page stays at its current path.</returns>
    public static string? RenameTarget(
        ConsumerConfig config, string type, string? oldHome, string? newHome, string newName, string oldFile)
    {
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

    /// <summary>Whether a decision page sits under the implementation decisions folder.</summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="file">The absolute path of the decision page.</param>
    /// <returns>True when the page sits under the implementation decisions folder; otherwise false.</returns>
    static bool IsTechnicalDecision(ConsumerConfig config, string file) =>
        file.StartsWith(Path.Combine(DocsRoot(config), "implementation", "decisions") + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    /// <summary>
    ///     The folder a renamed module or aggregate page moves to. The folder moves
    ///     whole, so the use cases beside a renamed aggregate move with it.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the folder, either module or aggregate.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <returns>The absolute path of the folder the page moves to after the rename.</returns>
    public static string RenameFolderTarget(ConsumerConfig config, string type, string? newHome, string newName) =>
        Path.GetDirectoryName(PagePath(config, type, newHome, newName))!;

    /// <summary>
    ///     Whether the renamed page keeps its folder: the home is unchanged and the
    ///     folder sits inside that home's module area.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the page.</param>
    /// <param name="oldHome">The identifier home before the rename, or null for a type without a home.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="folder">The folder that currently holds the page.</param>
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
    ///     Every file a record identifier can be cited in under the root, in a stable order: Markdown,
    ///     JSON, and the source and test files whose anchor and proof tags cite records. Build output,
    ///     installed packages, and hidden folders are left out.
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

            if (name.StartsWith('.') || SkippedFolders.Contains(name))
            {
                continue;
            }

            Walk(child, found);
        }
    }
}
