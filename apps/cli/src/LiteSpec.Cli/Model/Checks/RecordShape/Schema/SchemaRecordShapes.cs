using System.Text.Json;
using LiteSpec.Cli.Model.Checks.Behavior;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema record shapes: every goal observation holds its date, value and source, every product requirement cites its origin, every read model states its freshness, every use case input names a known verifier and rule, every outputs field states its type, and every external system names a role rather than its vendor.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
internal static class SchemaRecordShapes
{
    /// <summary>The observation date key on a goal.</summary>
    const string DateField = "observedOn";

    /// <summary>The unknown-outcome key of an operation.</summary>
    const string UnknownField = "unknown";

    /// <summary>The modes a read model's freshness may state.</summary>
    private static readonly string[] FreshnessModes = ["immediate", "snapshot", "eventual"];

    /// <summary>The handling an unknown external outcome may name.</summary>
    private static readonly string[] UnknownHandlings = ["dedupe-key", "reconcile", "retry-safe"];

    /// <summary>Whether a requirement sits at product level, outside every feature folder.</summary>
    /// <param name="record">The requirement record whose file path is read.</param>
    /// <returns>True when the requirement file lies under the product requirements folder.</returns>
    internal static bool IsProductRequirement(Record record) =>
        record.File.Replace('\\', '/').Contains("/intent/requirements/", StringComparison.Ordinal);

    /// <summary>Whether a product requirement names in basedOn a constraint, goal or problem it comes from.</summary>
    /// <param name="model">The loaded project model that resolves each named identifier.</param>
    /// <param name="record">The requirement record whose basedOn is read.</param>
    /// <returns>True when one named record is a constraint, a goal or a problem.</returns>
    internal static bool CitesOriginOf(ProjectModel model, Record record) =>
        record.Ids(RecordFields.BasedOn).Any(id =>
            model.ById.TryGetValue(Identifiers.BaseId(id), out var resolved)
            && resolved.Type is RecordTypes.Constraint or RecordTypes.Goal or RecordTypes.Problem);

    /// <summary>Reports each goal observation that holds no date, value and source, or a date that is no date.</summary>
    /// <param name="record">The goal record whose observations are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckObservations(Record record, List<Finding> findings)
    {
        if (!record.Has(RecordFields.Observations))
        {
            return;
        }

        SchemaFieldRules.RequireArray(record, RecordFields.Observations, 0, findings);

        if (!record.Raw.TryGetProperty(RecordFields.Observations, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in list.EnumerateArray())
        {
            var date = Str(entry, DateField);
            var value = Str(entry, RecordFields.Value);
            var source = Str(entry, RecordFields.Source);

            if (date is null || value is null || source is null)
            {
                findings.Add(new Finding("record-invalid", $"Goal '{record.Id}' holds an observation with no date, value and source.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!SchemaFieldRules.ValidDate(date))
            {
                findings.Add(new Finding("record-invalid", $"Goal '{record.Id}' holds observation date '{date}', which is not a date.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a read model whose freshness is not an object with a known mode and an ISO 8601 within.</summary>
    /// <param name="record">The read model record whose freshness is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckFreshness(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Freshness, out var freshness) || freshness.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' states no freshness object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var mode = Str(freshness, RecordFields.Mode);

        if (mode is null || !FreshnessModes.Contains(mode, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' holds freshness mode '{mode ?? "none"}' outside {string.Join(", ", FreshnessModes)}.", record.File, record.Line, SubjectOf(record)));
        }

        if (Str(freshness, RecordFields.Within) is { } within && !IsoDuration.IsMatch(within))
        {
            findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' holds freshness within '{within}', which is not an ISO 8601 duration.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports an input that is a verified claim with no verifier, an unknown verifier, or a rule that is no rule record.</summary>
    /// <param name="model">The loaded project model that resolves each verifier and rule.</param>
    /// <param name="record">The use case record whose inputs array is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckInputs(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Inputs, out var inputs) || inputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var input in inputs.EnumerateArray())
        {
            if (input.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(input, RecordFields.Name) ?? "";
            var verifiedBy = Str(input, RecordFields.VerifiedBy);

            if (verifiedBy is { Length: > 0 } verifier && !model.ById.ContainsKey(Identifiers.BaseId(verifier)))
            {
                findings.Add(new Finding("reference-unknown", $"Use case '{record.Id}' input '{name}' names verifiedBy '{verifier}', which no record holds.", record.File, record.Line, SubjectOf(record)));
            }

            foreach (var rule in LinkStrings(input, RecordFields.Rules))
            {
                if (!model.ById.TryGetValue(Identifiers.BaseId(rule), out var resolved))
                {
                    findings.Add(new Finding("reference-unknown", $"Use case '{record.Id}' input '{name}' names rule '{rule}', which no record holds.", record.File, record.Line, SubjectOf(record)));
                }
                else if (resolved.Type != RecordTypes.Rule)
                {
                    findings.Add(new Finding("record-invalid", $"Use case '{record.Id}' input '{name}' names rule '{rule}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Reports an outputs field that states no type, when the outputs are a list of fields.</summary>
    /// <param name="record">The use case record whose outputs list is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckOutputs(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Outputs, out var outputs) || outputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var output in outputs.EnumerateArray())
        {
            if (output.ValueKind == JsonValueKind.Object
                && Str(output, RecordFields.Name) is { Length: > 0 } name
                && Str(output, RecordFields.Type) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Use case '{record.Id}' outputs field '{name}' states no type.", record.File, record.Line, SubjectOf(record)));
            }

            if (output.ValueKind == JsonValueKind.Object
                && Str(output, RecordFields.Type) == "list"
                && output.TryGetProperty(RecordFields.Items, out var rows))
            {
                ContractChecks.CheckRowShape(record, "Use case", "outputs", Str(output, RecordFields.Name) ?? "(no name)", rows, findings);
            }
        }
    }

    /// <summary>Reports an external system whose represents names no system actor, an external system's prose send, receive or failure field, and each operation with an unknown outcome that names no handling.</summary>
    /// <param name="model">The loaded project model that resolves each represented actor.</param>
    /// <param name="record">The external-system record whose represents and operations are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckExternalSystem(ProjectModel model, Record record, List<Finding> findings)
    {
        // A record named for its vendor ties the specification to one provider; the role name stays while vendors change.
        if (record.Field(RecordFields.Vendor) is { Length: > 0 } vendor
            && Identifiers.TrySplit(record.Id, out _, out _, out var name)
            && SameVendorName(name, vendor))
        {
            findings.Add(new Finding("record-invalid", $"External system '{record.Id}' is named for its vendor '{vendor}'; name it for the role the system plays.", record.File, record.Line, SubjectOf(record)));
        }

        // The system names the system actor it represents; a name no record holds
        // is reference-unknown through the contract and citation checks, so only a held record of another kind reports here.
        foreach (var represented in record.Ids(RecordFields.Represents))
        {
            if (model.ById.TryGetValue(Identifiers.BaseId(represented), out var actor)
                && (actor.Type != RecordTypes.Actor || actor.Field(RecordFields.Kind) != "system"))
            {
                findings.Add(new Finding("record-invalid", $"External system '{record.Id}' represents '{represented}', which is no system actor.", record.File, record.Line, SubjectOf(record)));
            }
        }

        foreach (var field in new[] { RecordFields.Sends, RecordFields.Receives, RecordFields.Failure })
        {
            if (record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"External system '{record.Id}' holds {field} as prose; its typed operations hold it.", record.File, record.Line, SubjectOf(record)));
            }
        }

        if (!record.Raw.TryGetProperty(RecordFields.Operations, out var operations) || operations.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var operation in operations.EnumerateArray())
        {
            if (operation.ValueKind != JsonValueKind.Object || !operation.TryGetProperty(UnknownField, out _))
            {
                continue;
            }

            var handling = Str(operation, RecordFields.OnUnknown);

            if (handling is null || !UnknownHandlings.Contains(handling, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"External system '{record.Id}' holds an operation with an unknown outcome and no onUnknown of {string.Join(", ", UnknownHandlings)}.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Whether the record name equals the vendor once case and separators are ignored: the record is named for its vendor.</summary>
    /// <param name="name">The identifier name segment of the external system.</param>
    /// <param name="vendor">The vendor the record states.</param>
    /// <returns>True when both normalize to one name; otherwise false.</returns>
    internal static bool SameVendorName(string name, string vendor) =>
        NormalizeName(name).Length > 0
        && string.Equals(NormalizeName(name), NormalizeName(vendor), StringComparison.Ordinal);

    /// <summary>The name as letters and digits in lower case: separators, spaces and casing carry no meaning.</summary>
    /// <param name="value">The name or vendor under test.</param>
    /// <returns>The normalized name.</returns>
    internal static string NormalizeName(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    /// <summary>Reports a scenario whose example rows do not share one set of keys.</summary>
    /// <param name="record">The scenario record whose example rows are compared.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckExampleKeys(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Examples, out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var shapes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
            {
                shapes.Add(string.Join(",", row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)));
            }
        }

        if (shapes.Count > 1)
        {
            findings.Add(new Finding("example-keys-drift", $"Scenario '{record.Id}' holds example rows with differing keys.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a domain event field entry that names no field or no type.</summary>
    /// <param name="record">The domain event record whose fields list is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckCarries(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Fields, out var carries) || carries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in carries.EnumerateArray())
        {
            if (Str(entry, RecordFields.Name) is not { Length: > 0 } || Str(entry, RecordFields.Type) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Domain event '{record.Id}' holds a field with no name and no type.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }
}
