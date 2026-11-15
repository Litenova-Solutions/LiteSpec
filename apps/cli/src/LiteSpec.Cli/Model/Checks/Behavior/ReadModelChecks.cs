using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>
///     The read model checks: each read model field is taken from a payload field of an outbound integration event,
///     each personal or sensitive field names the event that erases it, each read model is read by a use case,
///     and each read model is keyed by an aggregate of another module.
/// </summary>
/// <follows>litespec/core/automation#declare-each-read-model-field-from-a-published-field</follows>
/// <follows>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</follows>
/// <follows>litespec/backend/application#read-another-modules-state-through-a-read-model</follows>
internal static class ReadModelChecks
{
    /// <summary>Runs the read model checks on every read model record.</summary>
    /// <param name="model">The loaded project model that holds every read model, use case and integration event.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var read = ReadReadModels(model);

        foreach (var readModel in model.Records.Where(record => record.Type == RecordTypes.ReadModel))
        {
            CheckSources(model, readModel, findings);
            CheckErasure(readModel, findings);
            CheckKeyLocal(readModel, findings);

            if (!read.Contains(readModel.Id))
            {
                findings.Add(new Finding(
                    "read-model-unused",
                    $"ReadModel '{readModel.Id}' is read by no use case.",
                    readModel.File,
                    readModel.Line,
                    SubjectOf(readModel)));
            }
        }
    }

    /// <summary>The read models a use case names in its reads.</summary>
    /// <param name="model">The loaded project model that holds every use case.</param>
    /// <returns>The identifiers of the read models that at least one use case reads.</returns>
    private static HashSet<string> ReadReadModels(ProjectModel model)
    {
        var read = new HashSet<string>(StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            foreach (var id in useCase.Ids(RecordFields.Reads))
            {
                if (id.StartsWith(RecordTypes.ReadModel + "/", StringComparison.Ordinal))
                {
                    read.Add(id);
                }
            }
        }

        return read;
    }

    /// <summary>
    ///     Reports each field whose source is not a payload field of an outbound integration event, and each
    ///     erasedBy or removedBy entry that names no outbound integration event.
    /// </summary>
    /// <param name="model">The loaded project model that resolves each integration event.</param>
    /// <param name="readModel">The read model record whose fields and removal events are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSources(ProjectModel model, Record readModel, List<Finding> findings)
    {
        foreach (var field in ReadModelFields(readModel))
        {
            var name = Str(field, RecordFields.Name) ?? "";

            if (Str(field, RecordFields.From) is { Length: > 0 } from)
            {
                var hash = from.IndexOf('#');
                var eventId = hash < 0 ? from : from[..hash];
                var payloadField = hash < 0 ? "" : from[(hash + 1)..];

                if (!IsOutbound(model, eventId, out var source))
                {
                    findings.Add(SourceFinding(readModel, $"ReadModel '{readModel.Id}' field '{name}' takes from '{from}', which is no outbound integration event."));
                }
                else if (payloadField.Length == 0 || !PayloadNames(source!).Contains(payloadField, StringComparer.Ordinal))
                {
                    findings.Add(SourceFinding(readModel, $"ReadModel '{readModel.Id}' field '{name}' takes from '{from}', which names a payload field '{payloadField}' the event does not carry."));
                }
            }

            if (Str(field, RecordFields.ErasedBy) is { Length: > 0 } erasedBy && !IsOutbound(model, erasedBy, out _))
            {
                findings.Add(SourceFinding(readModel, $"ReadModel '{readModel.Id}' field '{name}' is erased by '{erasedBy}', which is no outbound integration event."));
            }
        }

        if (readModel.Raw.ValueKind != JsonValueKind.Object || !readModel.Raw.TryGetProperty(RecordFields.RemovedBy, out var removed) || removed.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in removed.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String
                && entry.GetString() is { Length: > 0 } removedBy
                && !IsOutbound(model, removedBy, out _))
            {
                findings.Add(SourceFinding(readModel, $"ReadModel '{readModel.Id}' is removed by '{removedBy}', which is no outbound integration event."));
            }
        }
    }

    /// <summary>Reports each personal or sensitive field that names no erasedBy integration event.</summary>
    /// <param name="readModel">The read model record whose fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckErasure(Record readModel, List<Finding> findings)
    {
        foreach (var field in ReadModelFields(readModel))
        {
            var classification = Str(field, RecordFields.Classification);

            if (classification is not ("personal" or "sensitive") || Str(field, RecordFields.ErasedBy) is { Length: > 0 })
            {
                continue;
            }

            findings.Add(new Finding(
                "read-model-erasure-missing",
                $"ReadModel '{readModel.Id}' field '{Str(field, RecordFields.Name)}' is {classification} and names no erasedBy integration event.",
                readModel.File,
                readModel.Line,
                SubjectOf(readModel)));
        }
    }

    /// <summary>Reports a read model keyed by an aggregate of its own module, which the module reads directly.</summary>
    /// <param name="readModel">The read model record whose keyedBy names an aggregate.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckKeyLocal(Record readModel, List<Finding> findings)
    {
        if (readModel.Field(RecordFields.KeyedBy) is not { Length: > 0 } keyedBy
            || !Identifiers.TrySplit(keyedBy, out _, out var keyHome, out _)
            || keyHome is null
            || !Identifiers.TrySplit(readModel.Id, out _, out var readModelHome, out _)
            || keyHome != readModelHome)
        {
            return;
        }

        findings.Add(new Finding(
            "read-model-key-invalid",
            $"ReadModel '{readModel.Id}' is keyed by '{keyedBy}', an aggregate of its own module.",
            readModel.File,
            readModel.Line,
            SubjectOf(readModel)));
    }

    /// <summary>The field entries of a read model, each as its JSON object.</summary>
    /// <param name="readModel">The read model record whose fields array is read.</param>
    /// <returns>Each object entry of the fields array, or an empty list when the read model holds none.</returns>
    private static List<JsonElement> ReadModelFields(Record readModel)
    {
        var fields = new List<JsonElement>();

        if (readModel.Raw.ValueKind == JsonValueKind.Object
            && readModel.Raw.TryGetProperty(RecordFields.Fields, out var list)
            && list.ValueKind == JsonValueKind.Array)
        {
            fields.AddRange(list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object));
        }

        return fields;
    }

    /// <summary>The payload field names an integration event carries.</summary>
    /// <param name="integration">The integration event record whose payload is read.</param>
    /// <returns>Each payload field name in declared order.</returns>
    private static List<string> PayloadNames(Record integration)
    {
        var names = new List<string>();

        if (integration.Raw.ValueKind == JsonValueKind.Object
            && integration.Raw.TryGetProperty(RecordFields.Payload, out var payload)
            && payload.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in payload.EnumerateArray())
            {
                if (Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    /// <summary>Whether an identifier names an outbound integration event, and the record it names.</summary>
    /// <param name="model">The loaded project model that resolves the identifier.</param>
    /// <param name="id">The identifier, with an optional fragment, that the reference names.</param>
    /// <param name="integration">The integration event record when the identifier names one that is outbound; otherwise null.</param>
    /// <returns>True when the identifier names an outbound integration event; otherwise false.</returns>
    private static bool IsOutbound(ProjectModel model, string id, out Record? integration)
    {
        if (model.ById.TryGetValue(Identifiers.BaseId(id), out var record)
            && record.Type == RecordTypes.IntegrationEvent
            && record.Field(RecordFields.Direction) == "out")
        {
            integration = record;
            return true;
        }

        integration = null;
        return false;
    }

    /// <summary>A read-model-source-unknown finding on one read model.</summary>
    /// <param name="readModel">The read model record the finding is raised on.</param>
    /// <param name="message">The finding's message.</param>
    /// <returns>The finding, at the read model's file and line.</returns>
    private static Finding SourceFinding(Record readModel, string message) =>
        new("read-model-source-unknown", message, readModel.File, readModel.Line, SubjectOf(readModel));
}
