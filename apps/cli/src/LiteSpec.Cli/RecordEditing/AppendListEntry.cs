using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.RecordEditing;

/// <summary>
///     The <c>litespec new entry</c> append path: one permission entry goes onto its
///     permission file's records, last, and the file is written back in canonical
///     form. Permission files carry no record identifier, so the file argument is
///     the file's consumer-relative path.
/// </summary>
/// <follows>litespec/core/model#scaffold-pages-and-reserve-identifiers</follows>
static class AppendListEntry
{
    /// <summary>Runs the append path and returns its exit code.</summary>
    /// <param name="parsed">The parsed command line, holding the file path, the entry type, the entry name and the options.</param>
    /// <returns>The process exit code: 0 when the entry is appended, 1 on a read or write failure, and 2 on invalid input.</returns>
    public static int Run(CliArgs parsed)
    {
        if (parsed.Positionals is not [_, var pageArg, var entryType, var name])
        {
            return CommandFailure.UsageError("New entry needs a page, a type, and a name.", NewCommand.Usage, json: false);
        }

        foreach (var option in new[] { "--code", "--outcome", "--result" })
        {
            if (parsed.Value(option) is not null)
            {
                CliHost.Error.WriteLine($"Option {option} applies only to new result and new scenario.");
                return 2;
            }
        }

        if (entryType == RecordTypes.Requirement)
        {
            CliHost.Error.WriteLine($"A requirement is a page of its own, not an entry: use litespec new requirement <feature>.{name}.");
            return 2;
        }

        if (entryType is "rule" or "setting" or "domain-event")
        {
            CliHost.Error.WriteLine($"A {entryType} is a page of its own, not an entry: use litespec new {entryType} <home>.{name}.");
            return 2;
        }

        if (entryType != RecordTypes.Permission)
        {
            CliHost.Error.WriteLine($"Unknown entry type '{entryType}'. Use permission.");
            return 2;
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var root = config.Root;
        IReadOnlyList<Record> records;

        try
        {
            records = RecordReader.Read(config).Records;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            CliHost.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        var pageFile = Path.IsPathFullyQualified(pageArg) ? pageArg : Path.Combine(root, pageArg);

        if (!File.Exists(pageFile))
        {
            CliHost.Error.WriteLine($"No page at '{pageArg}'.");
            return 2;
        }

        string text;

        try
        {
            text = File.ReadAllText(pageFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to read '{PathText.Relative(config.Root, pageFile)}': {exception.Message}");
            return 1;
        }

        if (!RecordFile.TryParse(text, out var parsedRoot, out _, out var parseError))
        {
            CliHost.Error.WriteLine($"The file holds no record to append to: {parseError}");
            return 2;
        }

        var pageType = parsedRoot.ValueKind == JsonValueKind.Object
            && parsedRoot.TryGetProperty("type", out var style)
            && style.ValueKind == JsonValueKind.String
                ? style.GetString()
                : null;

        if (pageType != RecordTypes.PermissionSet)
        {
            var actual = pageType is null ? "a file without a type" : $"'{pageType}'";
            CliHost.Error.WriteLine($"Type '{entryType}' belongs on the permission list; '{PathText.Relative(config.Root, pageFile)}' is {actual}.");
            return 2;
        }

        var entryId = $"{entryType}/{name}";

        if (RecordPages.GrammarError(entryType, null, name, entryId) is { } grammar)
        {
            CliHost.Error.WriteLine(grammar);
            return 2;
        }

        var clash = records.FirstOrDefault(record => record.Id == entryId);

        if (clash is not null)
        {
            CliHost.Error.WriteLine($"Identifier '{entryId}' is already taken by {PathText.Relative(config.Root, clash.File)}.");
            return 2;
        }

        var entry = new JsonObject
        {
            ["type"] = "permission",
            ["id"] = entryId,
            ["title"] = AppendEntry.TitleFrom(name),
            ["meaning"] = "This permission allows the action.",
            // Every link points up: the permission names the roles it is granted to. The
            // placeholder role draws reference-unknown until the author names a real one.
            ["grantedTo"] = new JsonArray("role/owner"),
        };
        var updated = AppendEntry.Insert(text, AppendEntry.Placement.Entry, entry, out var error);

        if (updated is null)
        {
            CliHost.Error.WriteLine(error);
            return 2;
        }

        try
        {
            File.WriteAllText(pageFile, updated);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to write '{PathText.Relative(config.Root, pageFile)}': {exception.Message}");
            return 1;
        }

        CliHost.Out.WriteLine($"Appended {entryId} to {PathText.Relative(config.Root, pageFile)}.");
        return 0;
    }
}
