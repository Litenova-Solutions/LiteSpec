using System.Text.Json;
using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec new entry</c> append path: one requirement, rule,
///     permission, or setting entry goes onto its owning list page's
///     frontmatter records, last, while every other byte of the page stays
///     untouched. List pages carry no record identifier, so the page argument
///     is the page's consumer-relative path.
/// </summary>
/// <follows>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</follows>
static class AppendListEntry
{
    /// <summary>Runs the append path and returns its exit code.</summary>
    /// <param name="parsed">The parsed command line, holding the page path, the entry type, the entry name and the options.</param>
    /// <returns>The process exit code: 0 when the entry is appended, 1 on a read or write failure, and 2 on invalid input.</returns>
    public static int Run(CliArgs parsed)
    {
        if (parsed.Positionals is not [_, var pageArg, var entryType, var name])
        {
            Console.Error.WriteLine("New entry needs a page, a type, and a name.");
            Console.Error.WriteLine(NewCommand.Usage);
            return 2;
        }

        foreach (var option in new[] { "--code", "--outcome", "--result" })
        {
            if (parsed.Value(option) is not null)
            {
                Console.Error.WriteLine($"Option {option} applies only to new result and new scenario.");
                return 2;
            }
        }

        if (OwnerOf(entryType) is null)
        {
            Console.Error.WriteLine($"Unknown entry type '{entryType}'. Use requirement, rule, permission, or setting.");
            return 2;
        }

        string root;

        try
        {
            root = Path.GetFullPath(parsed.Value("--root") ?? Directory.GetCurrentDirectory());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root")}': {exception.Message}");
            Console.Error.WriteLine(NewCommand.Usage);
            return 2;
        }

        ConsumerConfig config;
        IReadOnlyList<Record> records;

        try
        {
            config = ConsumerConfig.Load(root);
            records = RecordReader.Read(config).Records;
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"litespec.project.json is not valid JSON: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            Console.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        var pageFile = Path.IsPathFullyQualified(pageArg) ? pageArg : Path.Combine(root, pageArg);

        if (!File.Exists(pageFile))
        {
            Console.Error.WriteLine($"No page at '{pageArg}'.");
            return 2;
        }

        string page;

        try
        {
            page = File.ReadAllText(pageFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Failed to read '{config.Relative(pageFile)}': {exception.Message}");
            return 1;
        }

        if (!Frontmatter.TryParse(page, out var frontmatter, out var frontmatterError))
        {
            Console.Error.WriteLine(frontmatterError.StartsWith("frontmatter is not JSON:", StringComparison.Ordinal)
                ? $"The page's {frontmatterError}"
                : "The page holds no frontmatter to append to.");
            return 2;
        }

        var pageType = frontmatter.Root.ValueKind == JsonValueKind.Object
            && frontmatter.Root.TryGetProperty("type", out var style)
            && style.ValueKind == JsonValueKind.String
                ? style.GetString()
                : null;

        if (pageType != PageOf(entryType))
        {
            var actual = pageType is null ? "a page without a type" : $"'{pageType}'";
            Console.Error.WriteLine($"Type '{entryType}' belongs on {OwnerOf(entryType)}; '{config.Relative(pageFile)}' is {actual}.");
            return 2;
        }

        var home = entryType == "permission" ? null : HomeOf(frontmatter.Root, entryType, pageFile);

        if (home is null && entryType != "permission")
        {
            Console.Error.WriteLine(entryType == "setting"
                ? $"The page '{config.Relative(pageFile)}' sits outside a modules folder and holds no setting to take the module from."
                : $"The page '{config.Relative(pageFile)}' names no topic to take the home from.");
            return 2;
        }

        var entryId = entryType == "permission" ? $"{entryType}/{name}" : $"{entryType}/{home}.{name}";

        if (RecordPages.GrammarError(entryType, home, name, entryId) is { } grammar)
        {
            Console.Error.WriteLine(grammar);
            return 2;
        }

        var clash = records.FirstOrDefault(record => record.Id == entryId);

        if (clash is not null)
        {
            Console.Error.WriteLine($"Identifier '{entryId}' is already taken by {config.Relative(clash.File)}.");
            return 2;
        }

        var fields = EntryFields(entryType, entryId, AppendEntry.TitleFrom(name));
        var updated = AppendEntry.Insert(page, AppendEntry.Placement.Entry, fields, out var error);

        if (updated is null)
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        try
        {
            File.WriteAllText(pageFile, updated);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Failed to write '{config.Relative(pageFile)}': {exception.Message}");
            return 1;
        }

        Console.WriteLine($"Appended {entryId} to {config.Relative(pageFile)}.");
        return 0;
    }

    /// <summary>The page style owning the entry type, or null for an unknown type.</summary>
    /// <param name="entryType">The entry type name from the command line, such as requirement or permission.</param>
    /// <returns>The page style that owns the entry type, or null when the type is unknown.</returns>
    static string? PageOf(string entryType) => entryType switch
    {
        "requirement" => "topic",
        "rule" => "rule-set",
        "permission" => "permission-list",
        "setting" => "setting-set",
        _ => null,
    };

    /// <summary>Where the entry type belongs, for the refusal message, or null for an unknown type.</summary>
    /// <param name="entryType">The entry type name from the command line, such as requirement or permission.</param>
    /// <returns>The description of where the entry type belongs for the refusal message, or null when the type is unknown.</returns>
    static string? OwnerOf(string entryType) => entryType switch
    {
        "requirement" => "a topic page",
        "rule" => "a rule-set page",
        "permission" => "the permission list",
        "setting" => "a setting set",
        _ => null,
    };

    /// <summary>
    ///     The new entry's home: the uniform home of the page's existing entries
    ///     of the same type, else the topic from the file name for a requirement
    ///     or rule, else the parent modules folder for a setting.
    /// </summary>
    /// <param name="root">The parsed frontmatter object of the list page.</param>
    /// <param name="entryType">The entry type name whose home is sought.</param>
    /// <param name="pageFile">The full path of the list page, used when the entries give no home.</param>
    /// <returns>The home segment for the entry identifier, or null when neither the entries nor the location gives one.</returns>
    static string? HomeOf(JsonElement root, string entryType, string pageFile) =>
        UniformHome(root, entryType) ?? LocationHome(entryType, pageFile);

    /// <summary>
    ///     The home every existing entry of the type shares, or null when the
    ///     page holds none or mixes homes.
    /// </summary>
    /// <param name="root">The parsed frontmatter object of the list page.</param>
    /// <param name="entryType">The entry type name whose existing entries are examined.</param>
    /// <returns>The home shared by every existing entry of the type, or null when the page holds none or mixes homes.</returns>
    static string? UniformHome(JsonElement root, string entryType)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("records", out var entries)
            || entries.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? home = null;

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty("type", out var kind)
                || kind.ValueKind != JsonValueKind.String
                || kind.GetString() != entryType
                || !entry.TryGetProperty("id", out var id)
                || id.ValueKind != JsonValueKind.String
                || id.GetString() is not { } text
                || !Identifiers.TrySplit(text, out _, out var splitHome, out _)
                || splitHome is null)
            {
                continue;
            }

            if (home is null)
            {
                home = splitHome;
            }
            else if (!string.Equals(home, splitHome, StringComparison.Ordinal))
            {
                return null;
            }
        }

        return home;
    }

    /// <summary>The home the page's place gives: the file stem, or the parent modules folder for a setting.</summary>
    /// <param name="entryType">The entry type name whose home is sought.</param>
    /// <param name="pageFile">The full path of the list page.</param>
    /// <returns>For a setting, the module folder name; otherwise the page file stem, or null when the stem is empty.</returns>
    static string? LocationHome(string entryType, string pageFile)
    {
        if (entryType == "setting")
        {
            return ModuleOf(pageFile);
        }

        var stem = Path.GetFileNameWithoutExtension(pageFile);

        return stem.Length > 0 ? stem : null;
    }

    /// <summary>The parent modules folder's child holding the page, or null outside one.</summary>
    /// <param name="pageFile">The full path of the list page.</param>
    /// <returns>The name of the folder directly under a modules folder that holds the page, or null when the page sits outside one.</returns>
    static string? ModuleOf(string pageFile)
    {
        var parts = Path.GetFullPath(pageFile)
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index + 2 < parts.Length; index++)
        {
            if (parts[index].Equals("modules", StringComparison.OrdinalIgnoreCase))
            {
                return parts[index + 1];
            }
        }

        return null;
    }

    /// <summary>The field lines of the new entry, without indentation: every required field with its template placeholder.</summary>
    /// <param name="entryType">The entry type name, which selects the field template.</param>
    /// <param name="id">The full identifier of the entry.</param>
    /// <param name="title">The display title of the entry.</param>
    /// <returns>The field lines of the entry for its type, without indentation, with template placeholders for the text fields.</returns>
    static IReadOnlyList<string> EntryFields(string entryType, string id, string title) => entryType switch
    {
        "requirement" =>
        [
            "\"type\": \"requirement\"",
            $"\"id\": \"{AppendEntry.Escape(id)}\"",
            $"\"title\": \"{AppendEntry.Escape(title)}\"",
            "\"kind\": \"functional\"",
            "\"statement\": \"<The commitment in one sentence>\"",
            "\"rationale\": \"<Why this commitment holds, in one sentence>\"",
        ],
        "rule" =>
        [
            "\"type\": \"rule\"",
            $"\"id\": \"{AppendEntry.Escape(id)}\"",
            $"\"title\": \"{AppendEntry.Escape(title)}\"",
            "\"kind\": \"invariant\"",
            "\"statement\": \"<The rule in one sentence>\"",
        ],
        "permission" =>
        [
            "\"type\": \"permission\"",
            $"\"id\": \"{AppendEntry.Escape(id)}\"",
            $"\"title\": \"{AppendEntry.Escape(title)}\"",
            "\"meaning\": \"<What this permission allows in one sentence>\"",
            "\"scope\": \"<scope, such as platform, tenant or self>\"",
        ],
        _ =>
        [
            "\"type\": \"setting\"",
            $"\"id\": \"{AppendEntry.Escape(id)}\"",
            $"\"title\": \"{AppendEntry.Escape(title)}\"",
            "\"valueType\": \"integer\"",
            "\"setBy\": \"decider\"",
        ],
    };
}
