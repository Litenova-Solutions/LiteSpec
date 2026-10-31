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
            root = Path.GetFullPath(parsed.Value("--root") ?? parsed.Value("--home") ?? Directory.GetCurrentDirectory());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root") ?? parsed.Value("--home")}': {exception.Message}");
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
    static string? PageOf(string entryType) => entryType switch
    {
        "requirement" => "topic",
        "rule" => "rule-set",
        "permission" => "permission-list",
        "setting" => "setting-set",
        _ => null,
    };

    /// <summary>Where the entry type belongs, for the refusal message, or null for an unknown type.</summary>
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
    static string? HomeOf(JsonElement root, string entryType, string pageFile) =>
        UniformHome(root, entryType) ?? LocationHome(entryType, pageFile);

    /// <summary>
    ///     The home every existing entry of the type shares, or null when the
    ///     page holds none or mixes homes.
    /// </summary>
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
