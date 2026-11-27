using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.Contracts;

/// <summary>The contract part resolution: every identifies naming an aggregate part resolves to a declared part with its own identity, and no aggregate declares one part name twice.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
internal static class ContractPartResolution
{
    /// <summary>Splits an identifies value naming an aggregate part into its aggregate and its part.</summary>
    /// <param name="identifies">The identifies value to split.</param>
    /// <param name="aggregateId">The identifier before the number sign.</param>
    /// <param name="part">The part name after the number sign.</param>
    /// <returns>True when the value names a part after a number sign; otherwise false.</returns>
    internal static bool TrySplitPart(string identifies, out string aggregateId, out string part)
    {
        aggregateId = "";
        part = "";

        var hash = identifies.IndexOf('#');

        if (hash <= 0 || hash == identifies.Length - 1)
        {
            return false;
        }

        aggregateId = identifies[..hash];
        part = identifies[(hash + 1)..];

        return true;
    }

    /// <summary>Whether the aggregate declares the part in its parts, identified or not.</summary>
    /// <param name="aggregate">The aggregate record whose parts are read.</param>
    /// <param name="part">The part name to find.</param>
    /// <returns>True when a parts entry carries the name; otherwise false.</returns>
    internal static bool DeclaresPart(Record aggregate, string part) =>
        PartsOf(aggregate).Any(entry => entry.Name.Equals(part, StringComparison.Ordinal));

    /// <summary>Whether the aggregate declares the part with its own identity.</summary>
    /// <param name="aggregate">The aggregate record whose parts are read.</param>
    /// <param name="part">The part name to find.</param>
    /// <returns>True when a parts entry carries the name with identity true; otherwise false.</returns>
    internal static bool IsIdentifiedPart(Record aggregate, string part) =>
        PartsOf(aggregate).Any(entry => entry.Name.Equals(part, StringComparison.Ordinal) && entry.Identity);

    /// <summary>The parts an aggregate declares: each parts entry name with whether it carries its own identity.</summary>
    /// <param name="aggregate">The aggregate record whose parts are read.</param>
    /// <returns>Each well-formed parts entry once, in written order.</returns>
    internal static List<(string Name, bool Identity)> PartsOf(Record aggregate)
    {
        var parts = new List<(string Name, bool Identity)>();

        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Parts, out var entries)
            || entries.ValueKind != JsonValueKind.Array)
        {
            return parts;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object && Str(entry, RecordFields.Name) is { Length: > 0 } name)
            {
                var identity = entry.TryGetProperty(RecordFields.Identity, out var flag) && flag.ValueKind == JsonValueKind.True;
                parts.Add((name, identity));
            }
        }

        return parts;
    }

    /// <summary>Reports an aggregate that declares one part name twice.</summary>
    /// <param name="record">The record whose parts are judged, when it is an aggregate.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckDuplicateParts(Record record, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Aggregate)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (name, _) in PartsOf(record))
        {
            if (!seen.Add(name) && reported.Add(name))
            {
                findings.Add(new Finding("record-invalid", $"Aggregate '{record.Id}' declares part '{name}' more than once.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The identifies values a record holds: each contract field and list row of its inputs, fields and outputs, with its field path.</summary>
    /// <param name="record">The record whose contract fields are read.</param>
    /// <returns>Each identifies value with its array, its record label and its dotted field path.</returns>
    internal static IEnumerable<(string Array, string Label, string Path, string Target)> IdentifiesFields(Record record)
    {
        var label = record.Type switch
        {
            RecordTypes.UseCase => "Use case",
            RecordTypes.IntegrationEvent => "Integration event",
            RecordTypes.DomainEvent => "Domain event",
            RecordTypes.ReadModel => "Read model",
            _ => "Record",
        };

        foreach (var array in new[] { RecordFields.Inputs, RecordFields.Fields })
        {
            if (record.Raw.TryGetProperty(array, out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var found in FieldIdentifies(entries, ""))
                {
                    yield return (array, label, found.Path, found.Target);
                }
            }
        }

        if (!record.Raw.TryGetProperty(RecordFields.Outputs, out var output))
        {
            yield break;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var found in FieldIdentifies(output, ""))
            {
                yield return ("outputs", label, found.Path, found.Target);
            }

            yield break;
        }

        if (output.ValueKind != JsonValueKind.Object
            || !output.TryGetProperty(RecordFields.Include, out var include)
            || include.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var group in include.EnumerateObject())
        {
            if (group.Value.ValueKind == JsonValueKind.Object
                && group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                && fields.ValueKind == JsonValueKind.Array)
            {
                foreach (var found in FieldIdentifies(fields, ""))
                {
                    yield return ("outputs", label, found.Path, found.Target);
                }
            }
        }
    }

    /// <summary>The identifies values of contract fields: each field's identifies with its dotted path, descending into list rows.</summary>
    /// <param name="entries">The contract field array whose identifies values are read.</param>
    /// <param name="prefix">The dotted path of the enclosing field, or empty at the top.</param>
    /// <returns>Each identifies value with its dotted field path.</returns>
    internal static IEnumerable<(string Path, string Target)> FieldIdentifies(JsonElement entries, string prefix)
    {
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(entry, RecordFields.Name) ?? "(no name)";
            var path = prefix.Length > 0 ? $"{prefix}.{name}" : name;

            if (Str(entry, RecordFields.Identifies) is { Length: > 0 } target)
            {
                yield return (path, target);
            }

            if (entry.TryGetProperty(RecordFields.Items, out var shape)
                && shape.ValueKind == JsonValueKind.Object
                && shape.TryGetProperty(RecordFields.Fields, out var rows)
                && rows.ValueKind == JsonValueKind.Array)
            {
                foreach (var found in FieldIdentifies(rows, path))
                {
                    yield return found;
                }
            }
        }
    }
}
