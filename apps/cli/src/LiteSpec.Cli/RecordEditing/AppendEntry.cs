using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.RecordEditing;

/// <summary>
///     The <c>litespec new result</c> and <c>litespec new scenario</c> append paths:
///     one entry goes onto a use-case file's records, in house order, and the
///     file is written back in canonical form.
/// </summary>
/// <follows>litespec/core/model#scaffold-pages-and-reserve-identifiers</follows>
static partial class AppendEntry
{
    /// <summary>Where the new entry lands among the file's entries.</summary>
    internal enum Placement
    {
        /// <summary>A successful result entry, placed with the success results before any refusal.</summary>
        Success,
        /// <summary>A refusal result entry, placed after the success results and before scenarios.</summary>
        Refusal,
        /// <summary>A scenario entry, placed after all result entries.</summary>
        Scenario,
        /// <summary>A list entry appended by the list path, placed after all result entries.</summary>
        Entry,
    }

    /// <summary>Runs the append path and returns its exit code.</summary>
    /// <param name="parsed">The parsed command line, holding the entry kind, the use case, the entry name and the options.</param>
    /// <returns>The process exit code: 0 when the entry is appended, 1 on a read or write failure, and 2 on invalid input.</returns>
    public static int Run(CliArgs parsed)
    {
        var kind = parsed.Positionals[0];

        if (parsed.Positionals is not [_, var useCaseArg, var name])
        {
            return CommandFailure.UsageError(kind == "result" ? "New result needs a use case and a name." : "New scenario needs a use case and a name.", NewCommand.Usage, json: false);
        }

        var outcome = parsed.Value("--outcome");
        var code = parsed.Value("--code");
        var resultRef = parsed.Value("--result");

        if (kind == "result")
        {
            if (resultRef is not null)
            {
                CliHost.Error.WriteLine("Option --result applies only to new scenario.");
                return 2;
            }

            if (outcome is not "success" and not "refusal")
            {
                CliHost.Error.WriteLine(outcome is null
                    ? "New result needs --outcome success or --outcome refusal."
                    : $"Unknown outcome '{outcome}'. Use success or refusal.");
                return 2;
            }

            if (outcome == "refusal" && code is null)
            {
                CliHost.Error.WriteLine("New result needs --code <module>.<reason> for a refusal.");
                return 2;
            }
        }
        else
        {
            if (code is not null)
            {
                CliHost.Error.WriteLine("Option --code applies only to new result.");
                return 2;
            }

            if (outcome is not null)
            {
                CliHost.Error.WriteLine("Option --outcome applies only to new result.");
                return 2;
            }

            if (resultRef is null)
            {
                CliHost.Error.WriteLine("New scenario needs --result <result-id>.");
                return 2;
            }
        }

        if (code is not null && !RefusalCodePattern().IsMatch(code))
        {
            CliHost.Error.WriteLine($"Code '{code}' is not a refusal code. Use <module>.<reason>.");
            return 2;
        }

        if (resultRef is not null && !ValidResultRef(resultRef))
        {
            CliHost.Error.WriteLine($"Result '{resultRef}' is not a result identifier.");
            return 2;
        }

        var useCaseId = useCaseArg.Contains('/') ? useCaseArg : $"use-case/{useCaseArg}";

        if (!Identifiers.TrySplit(useCaseId, out var usePrefix, out var useHome, out var useName)
            || usePrefix != "use-case")
        {
            CliHost.Error.WriteLine($"'{useCaseArg}' is not a use-case identifier. Use use-case/<module>.<name>.");
            return 2;
        }

        if (RecordPages.GrammarError("use-case", useHome, useName, useCaseId) is { } useGrammar)
        {
            CliHost.Error.WriteLine(useGrammar);
            return 2;
        }

        var entryId = $"{kind}/{useName}.{name}";

        if (RecordPages.GrammarError(kind, useName, name, entryId) is { } entryGrammar)
        {
            CliHost.Error.WriteLine(entryGrammar);
            return 2;
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

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

        var useCase = records.FirstOrDefault(record => record.Type == "use-case" && record.Id == useCaseId);

        if (useCase is null)
        {
            CliHost.Error.WriteLine($"No use case holds '{useCaseId}'.");
            return 2;
        }

        var clash = records.FirstOrDefault(record => record.Id == entryId);

        if (clash is not null)
        {
            CliHost.Error.WriteLine($"Identifier '{entryId}' is already taken by {PathText.Relative(config.Root, clash.File)}.");
            return 2;
        }

        string text;

        try
        {
            text = File.ReadAllText(useCase.File);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to read '{PathText.Relative(config.Root, useCase.File)}': {exception.Message}");
            return 1;
        }

        var placement = kind == "scenario"
            ? Placement.Scenario
            : outcome == "success" ? Placement.Success : Placement.Refusal;
        var entry = kind == "scenario"
            ? ScenarioEntry(entryId, TitleFrom(name), resultRef!)
            : ResultEntry(entryId, TitleFrom(name), outcome!, code);
        var updated = Insert(text, placement, entry, out var error);

        if (updated is null)
        {
            CliHost.Error.WriteLine(error);
            return 2;
        }

        try
        {
            File.WriteAllText(useCase.File, updated);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to write '{PathText.Relative(config.Root, useCase.File)}': {exception.Message}");
            return 1;
        }

        CliHost.Out.WriteLine($"Appended {entryId} to {PathText.Relative(config.Root, useCase.File)}.");
        return 0;
    }

    /// <summary>
    ///     Adds one entry to the file's records in house order and writes the
    ///     file back in canonical form.
    /// </summary>
    /// <param name="text">The record file text to append to.</param>
    /// <param name="placement">Where the entry lands among the file entries, which selects the insertion group.</param>
    /// <param name="entry">The new entry object.</param>
    /// <param name="error">When the result is null, the reason the file cannot accept the entry.</param>
    /// <returns>The updated file in canonical form, or null with the reason in <paramref name="error"/>.</returns>
    internal static string? Insert(string text, Placement placement, JsonObject entry, out string error)
    {
        error = "";

        if (!RecordFile.TryParse(text, out _, out var tree, out var parseError))
        {
            error = $"The file holds no record to append to: {parseError}";
            return null;
        }

        if (RecordFile.NodeOf(tree) is not JsonObject node)
        {
            error = "The file holds no record to append to.";
            return null;
        }

        if (!node.TryGetPropertyValue("records", out var found) || found is null)
        {
            node["records"] = new JsonArray(entry);
            return RecordFile.Write(node);
        }

        if (found is not JsonArray records)
        {
            error = "The file's records field is not an array.";
            return null;
        }

        records.Insert(InsertionIndex(records, placement), entry);

        return RecordFile.Write(node);
    }

    /// <summary>The display title derived from a kebab-case name: words joined, first word capitalized.</summary>
    /// <param name="name">The kebab-case entry name, for example a result or scenario name segment.</param>
    /// <returns>The display title with words separated by spaces and the first word capitalized, or the name unchanged when it holds no word.</returns>
    internal static string TitleFrom(string name)
    {
        var words = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return name;
        }

        words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];

        return string.Join(' ', words);
    }

    /// <summary>The new result entry with every required field filled.</summary>
    /// <param name="id">The full identifier of the result entry.</param>
    /// <param name="title">The display title of the result entry.</param>
    /// <param name="outcome">The outcome kind, either success or refusal.</param>
    /// <param name="code">The refusal code for a refusal, or null for a success without a code.</param>
    /// <returns>The result entry object.</returns>
    static JsonObject ResultEntry(string id, string title, string outcome, string? code)
    {
        var entry = new JsonObject
        {
            ["type"] = "result",
            ["id"] = id,
            ["title"] = title,
            ["outcome"] = outcome,
        };

        if (outcome == "refusal")
        {
            entry["code"] = code!;
        }
        else if (code is not null)
        {
            entry["codes"] = new JsonArray(code);
        }

        entry["meaning"] = $"{title}.";

        return entry;
    }

    /// <summary>The new scenario entry with every required field filled.</summary>
    /// <param name="id">The full identifier of the scenario entry.</param>
    /// <param name="title">The display title of the scenario entry.</param>
    /// <param name="result">The identifier of the result the scenario exercises.</param>
    /// <returns>The scenario entry object.</returns>
    static JsonObject ScenarioEntry(string id, string title, string result) => new JsonObject
    {
        ["type"] = "scenario",
        ["id"] = id,
        ["title"] = title,
        ["given"] = "The starting state holds.",
        ["when"] = "The actor performs the act.",
        ["then"] = "The observable result follows.",
        ["result"] = result,
    };

    /// <summary>Whether the reference names a result id: written and derived results share one form.</summary>
    /// <param name="value">The candidate result reference from the command line.</param>
    /// <returns>True when the reference is a grammatically valid result identifier; otherwise false.</returns>
    static bool ValidResultRef(string value)
    {
        if (!value.StartsWith("result/", StringComparison.Ordinal))
        {
            return false;
        }

        return Identifiers.TrySplit(value, out var prefix, out var home, out var name)
            && prefix == "result"
            && RecordPages.GrammarError("result", home, name, value) is null;
    }

    /// <summary>A refusal code: module and reason in kebab-case.</summary>
    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*\.[a-z0-9][a-z0-9-]*$")]
    private static partial Regex RefusalCodePattern();

    /// <summary>Where the new entry lands: successes first, refusals next, scenarios and list entries last.</summary>
    /// <param name="records">The file's existing records, in file order.</param>
    /// <param name="placement">Where the entry lands, which selects the group ceiling it inserts before.</param>
    /// <returns>The index at which the entry inserts, or the record count when it appends at the end.</returns>
    static int InsertionIndex(JsonArray records, Placement placement)
    {
        if (placement is Placement.Scenario or Placement.Entry)
        {
            return records.Count;
        }

        var ceiling = placement == Placement.Success ? 0 : 1;

        for (var index = 0; index < records.Count; index++)
        {
            if (GroupOf(records[index]) > ceiling)
            {
                return index;
            }
        }

        return records.Count;
    }

    /// <summary>The entry's group in house order: 0 for a success, 1 for a refusal, 2 for anything else.</summary>
    /// <param name="entry">The records entry, or null for a JSON null.</param>
    /// <returns>The group number of the entry.</returns>
    static int GroupOf(JsonNode? entry)
    {
        var type = entry is JsonObject record
            && record.TryGetPropertyValue("type", out var kind)
            && kind is JsonValue value
            && value.TryGetValue<string>(out var name)
                ? name
                : null;

        if (type != "result")
        {
            return 2;
        }

        var outcome = entry is JsonObject result
            && result.TryGetPropertyValue("outcome", out var held)
            && held is JsonValue text
            && text.TryGetValue<string>(out var mode)
                ? mode
                : null;

        return outcome == "success" ? 0 : 1;
    }
}
