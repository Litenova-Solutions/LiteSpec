using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     The scenario probes the project model holds: every scenario record with a
///     probe object, optionally narrowed to one carrying use case.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal static class ProbeCollector
{
    /// <summary>Collects the scenario probes of the model.</summary>
    /// <param name="model">The built project model holding the scenario records.</param>
    /// <param name="useCase">The use-case id to keep, or null for every probe.</param>
    /// <param name="error">The usage error when the use case is unknown, or null.</param>
    /// <returns>The probes in file then line order.</returns>
    public static IReadOnlyList<ProbeCase> Collect(ProjectModel model, string? useCase, out string? error)
    {
        error = null;

        if (useCase is { Length: > 0 })
        {
            var known = model.Records.Any(record =>
                !record.Derived
                && string.Equals(record.Id, useCase, StringComparison.Ordinal)
                && string.Equals(record.Type, RecordTypes.UseCase, StringComparison.Ordinal));

            if (!known)
            {
                var names = model.Records
                    .Where(record => !record.Derived && string.Equals(record.Type, RecordTypes.UseCase, StringComparison.Ordinal))
                    .Select(record => record.Id)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .Take(5)
                    .ToList();

                error = names.Count == 0
                    ? $"Unknown use case '{useCase}': this project declares no use cases."
                    : $"Unknown use case '{useCase}'. Use one of: {string.Join(", ", names)}.";

                return [];
            }
        }

        var probes = new List<ProbeCase>();

        foreach (var record in model.Records
            .Where(record => !record.Derived && string.Equals(record.Type, RecordTypes.Scenario, StringComparison.Ordinal))
            .OrderBy(record => record.File, StringComparer.Ordinal)
            .ThenBy(record => record.Line))
        {
            if (record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty("probe", out var probe)
                || probe.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (useCase is { Length: > 0 } && !string.Equals(record.PageId, useCase, StringComparison.Ordinal))
            {
                continue;
            }

            probes.Add(ReadProbe(record, probe));
        }

        return probes;
    }

    /// <summary>Reads one scenario probe from its JSON object.</summary>
    /// <param name="record">The scenario record holding the probe.</param>
    /// <param name="probe">The probe JSON object.</param>
    /// <returns>The probe case.</returns>
    private static ProbeCase ReadProbe(Record record, JsonElement probe)
    {
        var asPerson = TextOf(probe, "as");
        var request = probe.TryGetProperty("request", out var current) && current.ValueKind == JsonValueKind.Object
            ? current
            : default;
        var expect = probe.TryGetProperty("expect", out var wanted) && wanted.ValueKind == JsonValueKind.Object
            ? wanted
            : default;
        var method = request.ValueKind == JsonValueKind.Object ? TextOf(request, "method") ?? "GET" : "GET";
        var path = request.ValueKind == JsonValueKind.Object ? TextOf(request, "path") ?? "/" : "/";
        JsonNode? body = null;

        if (request.ValueKind == JsonValueKind.Object
            && request.TryGetProperty("body", out var rawBody)
            && rawBody.ValueKind != JsonValueKind.Undefined
            && rawBody.ValueKind != JsonValueKind.Null)
        {
            body = JsonNode.Parse(rawBody.GetRawText());
        }

        var status = 200;

        if (expect.ValueKind == JsonValueKind.Object
            && expect.TryGetProperty("status", out var rawStatus)
            && rawStatus.ValueKind == JsonValueKind.Number
            && rawStatus.TryGetInt32(out var parsed))
        {
            status = parsed;
        }

        var refusal = expect.ValueKind == JsonValueKind.Object ? TextOf(expect, "refusal") : null;
        var fields = new List<ProbeFieldCheck>();

        if (expect.ValueKind == JsonValueKind.Object
            && expect.TryGetProperty("fields", out var rawFields)
            && rawFields.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in rawFields.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var fieldPath = TextOf(entry, "path") ?? "/";
                JsonNode? equals = null;
                var hasEquals = false;
                bool? present = null;

                if (entry.TryGetProperty("equals", out var rawEquals)
                    && rawEquals.ValueKind != JsonValueKind.Undefined)
                {
                    hasEquals = true;
                    equals = rawEquals.ValueKind == JsonValueKind.Null ? null : JsonNode.Parse(rawEquals.GetRawText());
                }

                if (entry.TryGetProperty("present", out var rawPresent)
                    && (rawPresent.ValueKind == JsonValueKind.True || rawPresent.ValueKind == JsonValueKind.False))
                {
                    present = rawPresent.ValueKind == JsonValueKind.True;
                }

                fields.Add(new ProbeFieldCheck(fieldPath, equals, hasEquals, present));
            }
        }

        return new ProbeCase(
            record.Id,
            record.PageId ?? "",
            record.File,
            record.Line,
            asPerson,
            method,
            path,
            body,
            status,
            refusal,
            fields);
    }

    /// <summary>Reads one string member of a JSON object, or null when it is missing or not a string.</summary>
    /// <param name="holder">The object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The string value, or null.</returns>
    private static string? TextOf(JsonElement holder, string name) =>
        holder.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
