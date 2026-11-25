using System.Text.Json;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Records;

/// <summary>
///     The closed value sets the records declare: the normalized choice texts of a
///     reference-data table or a setting, with the meaning each value states. The
///     generator and the value-meaning checks read this one source, so both agree
///     on which text names which value.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class ClosedSets
{
    /// <summary>The normalized choice texts of a closed value set, or null when the record states no choices list.</summary>
    /// <param name="record">The reference-data or setting record.</param>
    /// <returns>The value texts in written order, or null when the record holds no choices array.</returns>
    public static List<string>? ChoiceTexts(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Choices, out var choices)
            || choices.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();

        foreach (var choice in choices.EnumerateArray())
        {
            if (choice.ValueKind == JsonValueKind.String && choice.GetString() is { } text)
            {
                values.Add(text);
            }
            else if (choice.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                values.Add(choice.GetRawText());
            }
        }

        return values;
    }

    /// <summary>The stated meaning by choice text, or null when the record states no meanings map.</summary>
    /// <param name="record">The reference-data or setting record.</param>
    /// <returns>The meaning by written choice text, or null when the record holds no valueMeanings object.</returns>
    public static Dictionary<string, string>? ValueMeanings(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.ValueMeanings, out var meanings)
            || meanings.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var stated = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in meanings.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.String && entry.Value.GetString() is { } meaning)
            {
                stated[entry.Name] = meaning;
            }
        }

        return stated;
    }
}
