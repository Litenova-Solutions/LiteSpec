using System.Text.Json;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     Evaluates a provision's applicability condition against the records a rules target marks.
///     Query and command read the use-case kind, upload reads a caller file input, document-number
///     reads a legal number signal, and erasure reads an erase, pseudonymize or anonymize signal.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class ProvisionWhen
{
    /// <summary>One marked record's fields the conditions read.</summary>
    /// <param name="Type">The record type, such as use-case or job.</param>
    /// <param name="Id">The record identifier.</param>
    /// <param name="Kind">The use-case or job kind, or empty.</param>
    /// <param name="Title">The record title.</param>
    /// <param name="Summary">The record summary, or empty.</param>
    /// <param name="Exposure">The record exposure, or empty.</param>
    /// <param name="Inputs">The use-case inputs with their origin.</param>
    /// <param name="Outputs">The output names and meanings.</param>
    internal sealed record RecordFields(
        string Type,
        string Id,
        string Kind,
        string Title,
        string Summary,
        string Exposure,
        IReadOnlyList<RecordInput> Inputs,
        IReadOnlyList<string> Outputs);

    /// <summary>One use-case input with its origin.</summary>
    /// <param name="Name">The input name.</param>
    /// <param name="Meaning">The input meaning.</param>
    /// <param name="Origin">The input origin, such as caller, claim or derived.</param>
    internal sealed record RecordInput(string Name, string Meaning, string Origin);

    /// <summary>The file words a caller input names when it takes a file.</summary>
    static readonly string[] FileWords =
    [
        "file", "upload", "photo", "image", "picture", "attachment", "receipt", "scan", "pdf", "document",
    ];

    /// <summary>Whether the condition holds for any marked record.</summary>
    /// <param name="when">The normalized condition, such as query or upload.</param>
    /// <param name="records">The marked records' fields.</param>
    /// <returns>True when any record meets the condition; false for an unknown condition.</returns>
    public static bool Holds(string when, IReadOnlyList<RecordFields> records) =>
        when switch
        {
            "query" => records.Any(record =>
                record.Type == "use-case" && record.Kind == "query"),
            "command" => records.Any(record =>
                record.Type == "use-case" && record.Kind == "command"),
            "upload" => records.Any(record =>
                record.Type == "use-case" && HasFileInput(record)),
            "document-number" => records.Any(record =>
                record.Type == "use-case" && IssuesDocumentNumber(record)),
            "erasure" => records.Any(record =>
                (record.Type == "use-case" || record.Type == "job") && ErasesPersonalData(record)),
            _ => false,
        };

    /// <summary>Loads the marked records' fields from the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="target">The rules target with its marked identifiers.</param>
    /// <returns>The fields of each marked record found, in marked order.</returns>
    public static IReadOnlyList<RecordFields> LoadMarked(ConsumerConfig config, PathContext target)
    {
        var wanted = target.Marked
            .Where(marked => PathContext.ArtifactOf(marked).Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (wanted.Count == 0)
        {
            return [];
        }

        var found = new Dictionary<string, RecordFields>(StringComparer.Ordinal);

        foreach (var root in config.RecordRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.OrderBy(name => name, StringComparer.Ordinal))
            {
                string text;

                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (!RecordFile.TryParse(text, out var parsed, out _, out _)
                    || parsed.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var id = Text(parsed, "id");

                if (id.Length > 0 && wanted.Contains(id, StringComparer.Ordinal) && !found.ContainsKey(id))
                {
                    found[id] = ReadFields(parsed);
                }

                if (found.Count == wanted.Count)
                {
                    break;
                }
            }

            if (found.Count == wanted.Count)
            {
                break;
            }
        }

        return wanted.Where(found.ContainsKey).Select(id => found[id]).ToList();
    }

    /// <summary>Whether the use case takes a file from the caller.</summary>
    /// <param name="record">The record fields.</param>
    /// <returns>True when a caller input names a file without naming a number.</returns>
    static bool HasFileInput(RecordFields record) =>
        record.Inputs.Any(input =>
            input.Origin == "caller"
            && !Contains(input.Name + " " + input.Meaning, "number")
            && FileWords.Any(word => Contains(input.Name, word) || Contains(input.Meaning, word)));

    /// <summary>Whether the use case issues a legal document number.</summary>
    /// <param name="record">The record fields.</param>
    /// <returns>True when the record names an invoice, payout, reimbursement, voucher or numbered document.</returns>
    static bool IssuesDocumentNumber(RecordFields record)
    {
        var text = record.Id + " " + record.Title + " " + record.Summary + " "
            + string.Join(" ", record.Inputs.SelectMany(input => new[] { input.Name, input.Meaning })) + " "
            + string.Join(" ", record.Outputs);

        return Contains(text, "invoice")
            || Contains(text, "payout")
            || Contains(text, "reimburse")
            || Contains(text, "voucher")
            || Contains(text, "document number")
            || Contains(text, "legal number");
    }

    /// <summary>Whether the use case or job erases, pseudonymizes or anonymizes personal data.</summary>
    /// <param name="record">The record fields.</param>
    /// <returns>True when the record names erasure, pseudonymizing or anonymizing.</returns>
    static bool ErasesPersonalData(RecordFields record)
    {
        var text = record.Id + " " + record.Title + " " + record.Summary + " " + record.Exposure;

        return Contains(text, "erase") || Contains(text, "pseudonym") || Contains(text, "anonym");
    }

    /// <summary>Whether the text contains the word, ignoring case.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="word">The word to find.</param>
    /// <returns>True when the text contains the word.</returns>
    static bool Contains(string text, string word) =>
        text.Contains(word, StringComparison.OrdinalIgnoreCase);

    /// <summary>One record file's fields the conditions read.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The fields.</returns>
    static RecordFields ReadFields(JsonElement root) => new(
        Text(root, "type"),
        Text(root, "id"),
        Text(root, "kind"),
        Text(root, "title"),
        Text(root, "summary"),
        Text(root, "exposure"),
        InputsOf(root),
        OutputsOf(root));

    /// <summary>The use-case inputs with their origin.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The inputs, or empty when the record holds none.</returns>
    static List<RecordInput> InputsOf(JsonElement root)
    {
        if (!root.TryGetProperty("input", out var input) || input.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return input.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.Object)
            .Select(entry => new RecordInput(Text(entry, "name"), Text(entry, "meaning"), Text(entry, "origin")))
            .Where(entry => entry.Name.Length > 0)
            .ToList();
    }

    /// <summary>The output names and meanings of a use case.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>Each output name and meaning.</returns>
    static List<string> OutputsOf(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output))
        {
            return [];
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            return output.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.Object)
                .SelectMany(entry => new[] { Text(entry, "name"), Text(entry, "meaning") })
                .Where(text => text.Length > 0)
                .ToList();
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty("include", out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            var texts = new List<string>();

            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind == JsonValueKind.Object
                    && group.Value.TryGetProperty("fields", out var fields)
                    && fields.ValueKind == JsonValueKind.Array)
                {
                    texts.AddRange(fields.EnumerateArray()
                        .Where(entry => entry.ValueKind == JsonValueKind.Object)
                        .SelectMany(entry => new[] { Text(entry, "name"), Text(entry, "meaning") })
                        .Where(text => text.Length > 0));
                }
            }

            return texts;
        }

        return [];
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
