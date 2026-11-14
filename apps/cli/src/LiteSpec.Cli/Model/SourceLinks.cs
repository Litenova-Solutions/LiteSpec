using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The source-links region: one Markdown link definition for each source a research or constraint record
///     declares, written at the end of its explanation file. A reader of the file, on GitHub or in an editor,
///     follows each <c>[text][key]</c> citation without the book, so the region holds the URL the record states.
/// </summary>
/// <follows>litespec/core/intent#cite-a-source-for-each-constraint-rule</follows>
/// <follows>litespec/core/model#render-generated-regions-from-the-model</follows>
internal static partial class SourceLinks
{
    /// <summary>The region name, as the fence and the render target name it.</summary>
    internal const string RegionName = "source-links";

    /// <summary>Whether a record declares sources whose link definitions belong in its explanation file.</summary>
    /// <param name="record">The record to test.</param>
    /// <returns>True for a page record of type research or constraint that declares at least one source.</returns>
    internal static bool Carries(Record record) =>
        !record.IsEntry
        && !record.Derived
        && (record.Type is RecordTypes.Research or RecordTypes.Constraint)
        && Sources(record).Count > 0;

    /// <summary>The region content for a record: one link definition per source, in the order the record lists them.</summary>
    /// <param name="record">The record whose sources the region links.</param>
    /// <returns>The link definitions, one per line, with no fence.</returns>
    internal static string Content(Record record) =>
        string.Join('\n', Sources(record).Select(source => $"[{source.Key}]: {source.Target} \"{Describe(source)}\""));

    /// <summary>Writes the region into a text: it rewrites the region a text holds, or appends one at its end.</summary>
    /// <param name="text">The explanation file text.</param>
    /// <param name="content">The region content from <see cref="Content"/>.</param>
    /// <returns>The text with the region in place.</returns>
    internal static string Place(string text, string content)
    {
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var region = $"<!-- generated:{RegionName} -->{newline}{content.ReplaceLineEndings(newline)}{newline}<!-- /generated:{RegionName} -->";
        var match = Pattern().Match(text);

        if (match.Success)
        {
            return text[..match.Index] + region + text[(match.Index + match.Length)..];
        }

        return text.TrimEnd('\r', '\n') + newline + newline + region + newline;
    }

    /// <summary>The body of the region in a text, or null when the text holds no source-links region.</summary>
    /// <param name="text">The explanation file text.</param>
    /// <returns>The region body with its line ends normalized to LF and trimmed, or null.</returns>
    internal static string? BodyOf(string text)
    {
        var match = Pattern().Match(text);

        return match.Success ? match.Groups["body"].Value.ReplaceLineEndings("\n").Trim() : null;
    }

    /// <summary>Blanks the region in a text, keeping every line and offset, so the checks read the prose only.</summary>
    /// <param name="text">The explanation file text.</param>
    /// <returns>The text with each character of the region replaced by a space, and its line ends kept.</returns>
    internal static string Strip(string text) =>
        Pattern().Replace(text, match => new string(match.Value.Select(character => character is '\r' or '\n' ? character : ' ').ToArray()));

    /// <summary>The label a link definition carries: when the source was read, and when it says it was published.</summary>
    /// <param name="source">The source to describe.</param>
    /// <returns>The title text, such as "Read on 2026-02-10".</returns>
    private static string Describe(SourceEntry source) =>
        source.PublishedOn is null
            ? $"Read on {source.ReadOn}"
            : $"Published {source.PublishedOn}, read on {source.ReadOn}";

    /// <summary>The sources a record declares, in written order, with an entry lacking a key or a target left out.</summary>
    /// <param name="record">The record whose sources are read.</param>
    /// <returns>Each declared source.</returns>
    private static List<SourceEntry> Sources(Record record)
    {
        var sources = new List<SourceEntry>();

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Sources, out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return sources;
        }

        foreach (var entry in list.EnumerateArray())
        {
            var key = Text(entry, RecordFields.Key);
            var target = Text(entry, RecordFields.Url) ?? Text(entry, RecordFields.Path);

            if (key is null || target is null)
            {
                continue;
            }

            sources.Add(new SourceEntry(key, target, Text(entry, RecordFields.ReadOn), Text(entry, RecordFields.PublishedOn)));
        }

        return sources;
    }

    /// <summary>A string property of a JSON object, or null when it is absent or not a non-empty string.</summary>
    /// <param name="element">The object to read.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string value, or null.</returns>
    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { Length: > 0 } text
            ? text
            : null;

    /// <summary>Finds the source-links region in an explanation file text.</summary>
    /// <returns>The region match, with the body in its group.</returns>
    [GeneratedRegex("<!-- generated:source-links -->(?<body>.*?)<!-- /generated:source-links -->", RegexOptions.Singleline)]
    private static partial Regex Pattern();

    /// <summary>One declared source as the region reads it.</summary>
    /// <param name="Key">The source key.</param>
    /// <param name="Target">The URL, or the path when the source is a local file.</param>
    /// <param name="ReadOn">The date the source was read, or null when absent.</param>
    /// <param name="PublishedOn">The date the source states it was published, or null when absent.</param>
    private sealed record SourceEntry(string Key, string Target, string? ReadOn, string? PublishedOn);
}
