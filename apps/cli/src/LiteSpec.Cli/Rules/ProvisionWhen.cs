using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     Evaluates a provision's applicability condition against the records a rules target marks.
///     Query and command read the use-case kind, upload reads a caller file input, document-number
///     reads a legal number signal, erasure reads an erase, pseudonymize or anonymize signal,
///     cross-module reads a queries or runs edge to another module's public use case,
///     project holds for no record, and audit-adoption reads the decision the audit selection names.
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
    /// <param name="Inputs">The use-case inputs with their origin.</param>
    /// <param name="Outputs">The output names and meanings.</param>
    /// <param name="Queries">The effects.queries targets.</param>
    /// <param name="Runs">The effects.runs targets.</param>
    /// <param name="IsAudited">Whether the use case declares an audit entry that is not exempt.</param>
    /// <param name="HasConcurrency">Whether the use case declares conflicts or client-key idempotency.</param>
    /// <param name="Tenant">The declared access.tenant scope, or empty.</param>
    internal sealed record RecordFields(
        string Type,
        string Id,
        string Kind,
        string Title,
        string Summary,
        IReadOnlyList<RecordInput> Inputs,
        IReadOnlyList<string> Outputs,
        IReadOnlyList<string> Queries,
        IReadOnlyList<string> Runs,
        bool IsAudited,
        bool HasConcurrency,
        string Tenant);

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
    /// <param name="config">The consumer configuration holding the project root.</param>
    /// <returns>True when any record meets the condition; false for a project condition, an unnamed decision, or an unknown condition.</returns>
    public static bool Holds(string when, IReadOnlyList<RecordFields> records, ConsumerConfig config)
    {
        if (string.Equals(when, "audit-adoption", StringComparison.Ordinal))
        {
            var named = AuditDecisions(config);
            return records.Any(record =>
                record.Type == "decision" && named.Contains(record.Id, StringComparer.Ordinal));
        }

        return when switch
        {
            "audited" => records.Any(record =>
                record.Type == "use-case" && record.IsAudited),
            "query" => records.Any(record =>
                record.Type == "use-case" && record.Kind == "query"),
            "command" => records.Any(record =>
                record.Type == "use-case" && record.Kind == "command"),
            "concurrency" => records.Any(record =>
                record.Type == "use-case" && record.HasConcurrency),
            "upload" => records.Any(record =>
                record.Type == "use-case" && HasFileInput(record)),
            "document-number" => records.Any(record =>
                record.Type == "use-case" && IssuesDocumentNumber(record)),
            "erasure" => records.Any(record =>
                (record.Type == "use-case" || record.Type == "job") && ErasesPersonalData(record)),
            "cross-module" => records.Any(record =>
                (record.Type == "use-case" || record.Type == "reaction" || record.Type == "job") && (record.Queries.Count > 0 || record.Runs.Count > 0)),
            "cross-tenant" => records.Any(record =>
                record.Type == "use-case" && record.Tenant.Length > 0 && record.Tenant is not "own" and not "none"),
            "project" => false,
            _ => false,
        };
    }

    /// <summary>The decisions the audit extension selection names in its records entry.</summary>
    /// <param name="config">The consumer configuration holding the project root.</param>
    /// <returns>The named decision identifiers, or empty when the selection names none.</returns>
    public static IReadOnlyList<string> AuditDecisions(ConsumerConfig config)
    {
        var file = ConsumerRoot.ProjectFile(config.Root);
        string text;

        try
        {
            if (!File.Exists(file))
            {
                return [];
            }

            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        JsonNode? project;

        try
        {
            project = RecordText.Parse(text, RecordText.IsYaml(file));
        }
        catch (Exception exception) when (exception is JsonTextException or IOException)
        {
            return [];
        }

        if (project is not JsonObject root || root["selectedExtensions"] is not JsonArray extensions)
        {
            return [];
        }

        foreach (var entry in extensions)
        {
            if (entry is JsonObject selection
                && string.Equals(JsonValues.PropString(selection, "id"), "audit", StringComparison.Ordinal))
            {
                return AdoptionDecisionOf(selection);
            }
        }

        return [];
    }

    /// <summary>The adoption decision the audit selection names in its <c>decision</c> field.</summary>
    /// <param name="selection">The audit extension selection.</param>
    /// <returns>The decision identifier, or none when the selection names none.</returns>
    static List<string> AdoptionDecisionOf(JsonObject selection) =>
        JsonValues.AsString(selection["decision"]) is { Length: > 0 } decision ? [decision] : [];

    /// <summary>Reads one record file's identifier and condition fields, for the record index.</summary>
    /// <param name="text">The file text, or null when the file cannot be read.</param>
    /// <param name="id">The record identifier, or empty when the file holds no record.</param>
    /// <param name="fields">The record's condition fields, when the file holds a record.</param>
    /// <returns>True when the file holds a record.</returns>
    internal static bool TryReadFields(string? text, out string id, out RecordFields fields)
    {
        id = "";
        fields = null!;

        if (text is null)
        {
            return false;
        }

        if (!RecordFile.TryParse(text, out var parsed, out _, out _)
            || parsed.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        id = Text(parsed, "id");

        if (id.Length == 0)
        {
            return false;
        }

        fields = ReadFields(parsed);
        return true;
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
        var text = record.Id + " " + record.Title + " " + record.Summary;

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
        InputsOf(root),
        OutputsOf(root),
        QueriesOf(root),
        RunsOf(root),
        IsAuditedOf(root),
        HasConcurrencyOf(root),
        TenantOf(root));

    /// <summary>Whether the use case declares an audit block that is not exempt.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>True when the use case is audited.</returns>
    static bool IsAuditedOf(JsonElement root) =>
        root.TryGetProperty("audit", out var audit)
        && audit.ValueKind == JsonValueKind.Object
        && Text(audit, "exemptBecause").Length == 0;

    /// <summary>Whether the use case declares conflicts or client-key idempotency.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>True when the use case uses conflicts or client-key idempotency.</returns>
    static bool HasConcurrencyOf(JsonElement root) =>
        (root.TryGetProperty("conflicts", out var conflicts) && conflicts.ValueKind == JsonValueKind.Object)
        || (root.TryGetProperty("idempotency", out var idempotency)
            && idempotency.ValueKind == JsonValueKind.Object
            && string.Equals(Text(idempotency, "mode"), "key", StringComparison.Ordinal));

    /// <summary>The declared access.tenant scope of a use case, or empty when none is declared.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The tenant scope text, or empty.</returns>
    static string TenantOf(JsonElement root) =>
        root.TryGetProperty("access", out var access) && access.ValueKind == JsonValueKind.Object
            ? Text(access, "tenant")
            : "";

    /// <summary>The use-case inputs with their origin.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The inputs, or empty when the record holds none.</returns>
    static List<RecordInput> InputsOf(JsonElement root)
    {
        if (!root.TryGetProperty("inputs", out var inputs) || inputs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return inputs.EnumerateArray()
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
        if (!root.TryGetProperty("outputs", out var outputs))
        {
            return [];
        }

        if (outputs.ValueKind == JsonValueKind.Array)
        {
            return outputs.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.Object)
                .SelectMany(entry => new[] { Text(entry, "name"), Text(entry, "meaning") })
                .Where(text => text.Length > 0)
                .ToList();
        }

        if (outputs.ValueKind == JsonValueKind.Object
            && outputs.TryGetProperty("include", out var include)
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

    /// <summary>The effects.queries targets of a use case, reaction or job.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>Each queries edge target.</returns>
    static List<string> QueriesOf(JsonElement root) => EffectsVerbOf(root, "queries");

    /// <summary>The effects.runs targets of a reaction or job.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>Each runs edge target.</returns>
    static List<string> RunsOf(JsonElement root) => EffectsVerbOf(root, "runs");

    /// <summary>The targets of one edge verb in the effects entries of a use case, reaction or job.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <param name="verb">The edge verb to read: queries or runs.</param>
    /// <returns>Each edge target the verb names.</returns>
    static List<string> EffectsVerbOf(JsonElement root, string verb)
    {
        if (!root.TryGetProperty("effects", out var effects) || effects.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return effects.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.Object)
            .Select(entry => entry.TryGetProperty(verb, out var target)
                && target.ValueKind == JsonValueKind.String
                ? target.GetString() ?? ""
                : "")
            .Where(id => id.Length > 0)
            .ToList();
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
