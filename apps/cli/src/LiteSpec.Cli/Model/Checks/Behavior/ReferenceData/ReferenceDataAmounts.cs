using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.ReferenceData;

/// <summary>The reference-data amount checks: every money and decimal field states a known sign convention, and the use cases one calculation rule computes agree on the sign of each amount they share.</summary>
/// <follows>litespec/core/domain-model#state-the-sign-of-every-amount</follows>
internal static class ReferenceDataAmounts
{
    /// <summary>Gets the name of the `sign` field.</summary>
    const string Sign = "sign";

    /// <summary>The sign conventions an amount field states.</summary>
    private static readonly string[] AmountSigns = ["money-in-positive", "money-out-positive"];

    /// <summary>The field types that may state a sign convention.</summary>
    private static readonly string[] AmountTypes = ["money", "decimal"];

    /// <summary>Reports amount fields one calculation rule computes whose sign conventions differ or are missing.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAmountSign(ProjectModel model, List<Finding> findings)
    {
        var rules = model.Records
            .Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "calculation")
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        var byRule = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var useCase in model.Records
            .Where(record => record.Type == RecordTypes.UseCase)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            CheckFieldSigns(useCase, findings);

            foreach (var cited in useCase.Ids(RecordFields.ComputedBy).Order(StringComparer.Ordinal))
            {
                if (!byRule.TryGetValue(cited, out var citedBy))
                {
                    citedBy = [];
                    byRule[cited] = citedBy;
                }

                citedBy.Add(useCase);
            }
        }

        foreach (var rule in rules)
        {
            if (!byRule.TryGetValue(rule.Id, out var useCases) || useCases.Count < 2)
            {
                continue;
            }

            var voices = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);

            foreach (var useCase in useCases)
            {
                foreach (var field in AmountFieldsOf(useCase))
                {
                    if (!voices.TryGetValue(field.Name, out var holders))
                    {
                        holders = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                        voices[field.Name] = holders;
                    }

                    if (!holders.TryGetValue(useCase.Id, out var signs))
                    {
                        signs = [];
                        holders[useCase.Id] = signs;
                    }

                    if (!signs.Contains(field.Sign, StringComparer.Ordinal))
                    {
                        signs.Add(field.Sign);
                    }
                }
            }

            foreach (var name in voices.Keys.Order(StringComparer.Ordinal))
            {
                var holders = voices[name];

                if (holders.Count < 2)
                {
                    continue;
                }

                var conventions = holders.Values
                    .SelectMany(signs => signs)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToList();

                if (conventions.Count < 2)
                {
                    continue;
                }

                var stated = holders
                    .OrderBy(holder => holder.Key, StringComparer.Ordinal)
                    .Select(holder => $"'{holder.Key}' states {string.Join(" and ", holder.Value.Order(StringComparer.Ordinal))}");

                findings.Add(new Finding(
                    "amount-sign-drift",
                    $"Rule '{rule.Id}' computes '{name}' from use cases stating differing sign conventions: {string.Join("; ", stated)}.",
                    rule.File,
                    rule.Line,
                    SubjectOf(rule)));
            }
        }
    }

    /// <summary>One amount field of a use case: its name with the sign convention it states.</summary>
    /// <param name="Name">The field name.</param>
    /// <param name="Sign">The convention the field states, with a missing sign reading as none.</param>
    private sealed record AmountField(string Name, string Sign);

    /// <summary>Reports a use-case field whose sign is outside the sign conventions, or that states a sign on a type that is no amount.</summary>
    /// <param name="useCase">The use-case record whose fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFieldSigns(Record useCase, List<Finding> findings)
    {
        foreach (var field in FieldEntriesOf(useCase))
        {
            if (Str(field, Sign) is not { } sign)
            {
                continue;
            }

            var name = Str(field, RecordFields.Name) ?? "";
            var type = Str(field, RecordFields.Type);

            if (type is not null && !AmountTypes.Contains(type, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' holds field '{name}' with a sign on type '{type}'; only money and decimal fields state a sign.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
            else if (!AmountSigns.Contains(sign, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' holds field '{name}' with sign '{sign}' outside money-in-positive, money-out-positive.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The amount fields of a use case: its money and decimal input and output fields with the convention each states.</summary>
    /// <param name="useCase">The use-case record whose fields are read.</param>
    /// <returns>Each amount field with its stated convention, a missing sign reading as none; a field with an unknown sign is left to the shape check.</returns>
    private static List<AmountField> AmountFieldsOf(Record useCase)
    {
        var found = new List<AmountField>();

        foreach (var field in FieldEntriesOf(useCase))
        {
            if (Str(field, RecordFields.Name) is not { Length: > 0 } name)
            {
                continue;
            }

            if (Str(field, RecordFields.Type) is not { } type || !AmountTypes.Contains(type, StringComparer.Ordinal))
            {
                continue;
            }

            var sign = Str(field, Sign);

            if (sign is not null && !AmountSigns.Contains(sign, StringComparer.Ordinal))
            {
                continue;
            }

            found.Add(new AmountField(name, sign ?? "none"));
        }

        return found;
    }

    /// <summary>The inputs and outputs field entries of a use case: its inputs list with its command outputs list or its query outputs groups.</summary>
    /// <param name="useCase">The use-case record whose fields are read.</param>
    /// <returns>The field entries in written order.</returns>
    private static List<JsonElement> FieldEntriesOf(Record useCase)
    {
        var fields = new List<JsonElement>();
        fields.AddRange(useCase.Objects(RecordFields.Inputs));

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Outputs, out var output))
        {
            return fields;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            fields.AddRange(output.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object));

            return fields;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind == JsonValueKind.Object
                    && group.Value.TryGetProperty(RecordFields.Fields, out var grouped)
                    && grouped.ValueKind == JsonValueKind.Array)
                {
                    fields.AddRange(grouped.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object));
                }
            }
        }

        return fields;
    }
}
