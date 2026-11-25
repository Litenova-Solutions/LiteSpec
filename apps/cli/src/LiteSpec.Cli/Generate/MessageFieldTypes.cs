using System.Text.Json;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Model;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     The C# types the message and result generators write for contract fields: one mapped
///     property per use-case input, output, or row field, from the exact type facts the way the
///     message field checks read them. An <c>id</c> becomes the typed identifier of what it
///     identifies, a <c>choice</c> the closed set it names, a <c>money</c> the shared money
///     object, a <c>file</c> the carried bytes, an <c>integer</c> with size 64 a <c>long</c>,
///     a <c>map</c> a read-only dictionary of its value type by its key type, an <c>object</c>
///     its named row type, and every other type its fixed .NET form. A row type naming an
///     aggregate part reuses the part type and writes no local row. A money input stating
///     <c>currencyHome</c> rides a decimal instead: the amount travels without its currency,
///     and the handler joins the currency from the loaded aggregate. The property takes the
///     PascalCase field name, as events do.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class MessageFieldTypes
{
    /// <summary>One contract field mapped to C#: its property name, its property type with nullability, and its row type with its row fields when the type lists a named row.</summary>
    /// <param name="Property">The property name in PascalCase.</param>
    /// <param name="Type">The C# property type.</param>
    /// <param name="RowType">The row type name the record states in rowType, or null when the field lists no multi-field rows or reuses an aggregate part.</param>
    /// <param name="Row">The row fields of a multi-field list or a single object, or an empty list when the field writes no local row.</param>
    /// <param name="Meaning">The meaning the record states, or null when the record states none.</param>
    public sealed record MappedField(
        string Property,
        string Type,
        string? RowType,
        IReadOnlyList<MappedField> Row,
        string? Meaning);

    /// <summary>Maps one use-case input entry to its C# property: an input stating required false rides nullable whatever its origin, while any other input stays non-nullable. A money input stating currencyHome rides a decimal, since the currency joins in the handler.</summary>
    /// <param name="input">The input entry holding the field name, its record type, and its required flag with its origin.</param>
    /// <param name="field">The mapped property, or null when the entry states no known shape.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <returns>True when the entry maps; otherwise false, and the message renders no file.</returns>
    public static bool TryMapInput(JsonElement input, out MappedField? field, Func<string, string?>? qualify = null)
    {
        field = null;

        if (RecordEntries.Str(input, RecordFields.Name) is not { Length: > 0 } name
            || RecordEntries.Str(input, RecordFields.Type) is not { Length: > 0 } type)
        {
            return false;
        }

        // An optional input may be absent whatever its origin: the pipeline
        // answers a missing derived or claim value with null, as the caller
        // omits an optional caller value.
        var nullable = !IsRequired(input);

        return TryMap(input, name, type, nullable, isInput: true, qualify, out field);
    }

    /// <summary>Maps one use-case output entry to its C# property: an optional output field may be absent, as an event carry may. The handler answers outputs from the loaded aggregate, so money keeps the shared object even with a currencyHome.</summary>
    /// <param name="output">The output entry holding the field name and its record type.</param>
    /// <param name="field">The mapped property, or null when the entry states no known shape.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <returns>True when the entry maps; otherwise false, and the result renders no file.</returns>
    public static bool TryMapOutput(JsonElement output, out MappedField? field, Func<string, string?>? qualify = null)
    {
        field = null;

        if (RecordEntries.Str(output, RecordFields.Name) is not { Length: > 0 } name
            || RecordEntries.Str(output, RecordFields.Type) is not { Length: > 0 } type)
        {
            return false;
        }

        return TryMap(output, name, type, !IsRequired(output), isInput: false, qualify, out field);
    }

    /// <summary>Maps one list-row entry to its C# property: a scalar of its record type, a read-only list for a row field of type list, or the named row type for a row field of type object, nullable when the row states no required.</summary>
    /// <param name="row">The row entry holding the field name and its record type.</param>
    /// <param name="field">The mapped property, or null when the entry states no known shape.</param>
    /// <param name="isInput">True when the row belongs to an input list, so money stating currencyHome rides a decimal.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <returns>True when the entry maps; otherwise false, and the row renders no file.</returns>
    public static bool TryMapRow(JsonElement row, out MappedField? field, bool isInput = false, Func<string, string?>? qualify = null)
    {
        field = null;

        if (RecordEntries.Str(row, RecordFields.Name) is not { Length: > 0 } name
            || RecordEntries.Str(row, RecordFields.Type) is not { Length: > 0 } type)
        {
            return false;
        }

        string scalar;

        if (type == "list")
        {
            if (!TryNestedList(row, isInput, qualify, out var nested) || nested is null)
            {
                return false;
            }

            scalar = nested;
        }
        else if (type == "object")
        {
            if (!TryNestedObject(row, isInput, qualify, out var nested) || nested is null)
            {
                return false;
            }

            scalar = nested;
        }
        else
        {
            if (ScalarOf(row, type, isInput) is not { } single)
            {
                return false;
            }

            scalar = single;

            if (IsMany(row))
            {
                scalar = $"IReadOnlyList<{scalar}>";
            }

            if (!IsRequired(row))
            {
                scalar += "?";
            }
        }

        field = new MappedField(
            PropertyName(name),
            scalar,
            null,
            [],
            RecordEntries.Str(row, RecordFields.Meaning));

        return true;
    }

    /// <summary>Maps one list row field to its read-only list type: the row element for a single-field inner row, or the named row type for a multi-field inner row or a single-field one stating rowType. Inner rows hold scalars only, one level at a time.</summary>
    /// <param name="row">The row field entry holding the nested row shape.</param>
    /// <param name="isInput">True when the row belongs to an input list, so money stating currencyHome rides a decimal.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <param name="type">The mapped C# type, or null when the nested shape states no known shape.</param>
    /// <returns>True when the nested shape maps; otherwise false.</returns>
    private static bool TryNestedList(JsonElement row, bool isInput, Func<string, string?>? qualify, out string? type)
    {
        type = null;

        if (!row.TryGetProperty(RecordFields.RowShape, out var shape)
            || shape.ValueKind != JsonValueKind.Object
            || !shape.TryGetProperty(RecordFields.Fields, out var leaves)
            || leaves.ValueKind != JsonValueKind.Array
            || leaves.GetArrayLength() == 0)
        {
            return false;
        }

        var members = new List<string>();

        foreach (var leaf in leaves.EnumerateArray())
        {
            if (leaf.ValueKind != JsonValueKind.Object
                || RecordEntries.Str(leaf, RecordFields.Type) is not { Length: > 0 } leafType
                || leafType is "list" or "object"
                || ScalarOf(leaf, leafType, isInput) is not { } scalar)
            {
                return false;
            }

            members.Add(IsRequired(leaf) ? scalar : scalar + "?");
        }

        var optional = !IsRequired(row);

        if (members.Count == 1)
        {
            if (RecordEntries.Str(row, RecordFields.RowType) is { Length: > 0 } rowType)
            {
                if (EventFieldTypes.ObjectTypeOf(rowType, qualify) is not { } single)
                {
                    return false;
                }

                type = $"IReadOnlyList<{single}>" + (optional ? "?" : "");

                return true;
            }

            type = $"IReadOnlyList<{members[0]}>" + (optional ? "?" : "");

            return true;
        }

        if (RecordEntries.Str(row, RecordFields.RowType) is { Length: > 0 } multiRowType
            && EventFieldTypes.ObjectTypeOf(multiRowType, qualify) is { } shared)
        {
            type = $"IReadOnlyList<{shared}>" + (optional ? "?" : "");

            return true;
        }

        return false;
    }

    /// <summary>Maps one single-object row field to its row type: the named row type, qualified when it names an aggregate part the qualifier resolves. Object rows hold scalars only, one level at a time, and always name their row type, since a single object never collapses onto its field.</summary>
    /// <param name="row">The row field entry holding the nested row shape.</param>
    /// <param name="isInput">True when the row belongs to an input object, so money stating currencyHome rides a decimal.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <param name="type">The mapped C# type, or null when the nested shape states no known shape.</param>
    /// <returns>True when the nested shape maps; otherwise false.</returns>
    private static bool TryNestedObject(JsonElement row, bool isInput, Func<string, string?>? qualify, out string? type)
    {
        type = null;

        if (!row.TryGetProperty(RecordFields.RowShape, out var shape)
            || shape.ValueKind != JsonValueKind.Object
            || !shape.TryGetProperty(RecordFields.Fields, out var leaves)
            || leaves.ValueKind != JsonValueKind.Array
            || leaves.GetArrayLength() == 0
            || RecordEntries.Str(row, RecordFields.RowType) is not { Length: > 0 } rowType
            || EventFieldTypes.ObjectTypeOf(rowType, qualify) is not { } shared)
        {
            return false;
        }

        foreach (var leaf in leaves.EnumerateArray())
        {
            if (leaf.ValueKind != JsonValueKind.Object
                || RecordEntries.Str(leaf, RecordFields.Type) is not { Length: > 0 } leafType
                || leafType is "list" or "object"
                || ScalarOf(leaf, leafType, isInput) is null)
            {
                return false;
            }
        }

        type = shared + (!IsRequired(row) ? "?" : "");

        return true;
    }

    /// <summary>Maps one contract field entry to its C# property.</summary>
    /// <param name="entry">The field entry holding the type facts.</param>
    /// <param name="name">The field name.</param>
    /// <param name="type">The coarse record type.</param>
    /// <param name="nullable">True when the property may be absent.</param>
    /// <param name="isInput">True when the entry belongs to an input, so money stating currencyHome rides a decimal.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <param name="field">The mapped property, or null when the entry states no known shape.</param>
    /// <returns>True when the entry maps; otherwise false.</returns>
    private static bool TryMap(JsonElement entry, string name, string type, bool nullable, bool isInput, Func<string, string?>? qualify, out MappedField? field)
    {
        field = null;

        var property = PropertyName(name);
        var meaning = RecordEntries.Str(entry, RecordFields.Meaning);

        if (type == "list")
        {
            return TryList(entry, property, meaning, nullable, isInput, qualify, out field);
        }

        if (type == "object")
        {
            return TryObject(entry, property, meaning, nullable, isInput, qualify, out field);
        }

        if (ScalarOf(entry, type, isInput) is not { } scalar)
        {
            return false;
        }

        if (IsMany(entry))
        {
            scalar = $"IReadOnlyList<{scalar}>";
        }

        if (nullable)
        {
            scalar += "?";
        }

        field = new MappedField(property, scalar, null, [], meaning);

        return true;
    }

    /// <summary>Maps one list field to its read-only list type: the row element for a single-field row, the named row type for a multi-field row or a single-field row stating rowType, or the part type when rowType names an aggregate part, writing no local row then.</summary>
    /// <param name="entry">The field entry holding the row shape.</param>
    /// <param name="property">The property name in PascalCase.</param>
    /// <param name="meaning">The meaning the record states, or null.</param>
    /// <param name="nullable">True when the property may be absent.</param>
    /// <param name="isInput">True when the entry belongs to an input, so row money stating currencyHome rides a decimal.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <param name="field">The mapped property, or null when the row shape states no known shape.</param>
    /// <returns>True when the row shape maps; otherwise false.</returns>
    private static bool TryList(JsonElement entry, string property, string? meaning, bool nullable, bool isInput, Func<string, string?>? qualify, out MappedField? field)
    {
        field = null;

        if (!entry.TryGetProperty(RecordFields.RowShape, out var shape)
            || shape.ValueKind != JsonValueKind.Object
            || !shape.TryGetProperty(RecordFields.Fields, out var rows)
            || rows.ValueKind != JsonValueKind.Array
            || rows.GetArrayLength() == 0)
        {
            return false;
        }

        var fields = new List<MappedField>();

        foreach (var row in rows.EnumerateArray())
        {
            if (!TryMapRow(row, out var mapped, isInput, qualify) || mapped is null)
            {
                return false;
            }

            fields.Add(mapped);
        }

        if (RecordEntries.Str(entry, RecordFields.RowType) is { Length: > 0 } rowType)
        {
            if (EventFieldTypes.ObjectTypeOf(rowType, qualify) is not { } shared)
            {
                return false;
            }

            var list = $"IReadOnlyList<{shared}>" + (nullable ? "?" : "");

            // A rowType naming an aggregate part reuses the part type, so no local row is written.
            field = EventFieldTypes.IsPartReference(rowType)
                ? new MappedField(property, list, null, [], meaning)
                : new MappedField(property, list, shared, fields, meaning);

            return true;
        }

        if (fields.Count == 1)
        {
            var single = $"IReadOnlyList<{fields[0].Type}>" + (nullable ? "?" : "");

            field = new MappedField(property, single, null, [], meaning);

            return true;
        }

        return false;
    }

    /// <summary>Maps one single-object field to its row type: the named row type, or the part type when rowType names an aggregate part, writing no local row then. A single object always names its row type, since it never collapses onto its field.</summary>
    /// <param name="entry">The field entry holding the row shape.</param>
    /// <param name="property">The property name in PascalCase.</param>
    /// <param name="meaning">The meaning the record states, or null.</param>
    /// <param name="nullable">True when the property may be absent.</param>
    /// <param name="isInput">True when the entry belongs to an input, so row money stating currencyHome rides a decimal.</param>
    /// <param name="qualify">The qualified C# type of one aggregate part reference, or null when the reference resolves to no single declaring type.</param>
    /// <param name="field">The mapped property, or null when the row shape states no known shape.</param>
    /// <returns>True when the row shape maps; otherwise false.</returns>
    private static bool TryObject(JsonElement entry, string property, string? meaning, bool nullable, bool isInput, Func<string, string?>? qualify, out MappedField? field)
    {
        field = null;

        if (!entry.TryGetProperty(RecordFields.RowShape, out var shape)
            || shape.ValueKind != JsonValueKind.Object
            || !shape.TryGetProperty(RecordFields.Fields, out var rows)
            || rows.ValueKind != JsonValueKind.Array
            || rows.GetArrayLength() == 0
            || RecordEntries.Str(entry, RecordFields.RowType) is not { Length: > 0 } rowType
            || EventFieldTypes.ObjectTypeOf(rowType, qualify) is not { } shared)
        {
            return false;
        }

        var fields = new List<MappedField>();

        foreach (var row in rows.EnumerateArray())
        {
            if (!TryMapRow(row, out var mapped, isInput, qualify) || mapped is null)
            {
                return false;
            }

            fields.Add(mapped);
        }

        // A rowType naming an aggregate part reuses the part type, so no local row is written.
        field = EventFieldTypes.IsPartReference(rowType)
            ? new MappedField(property, shared + (nullable ? "?" : ""), null, [], meaning)
            : new MappedField(property, shared + (nullable ? "?" : ""), shared, fields, meaning);

        return true;
    }

    /// <summary>Maps one scalar record type to its C# type. Money stating currencyHome rides a decimal on an input, since the amount travels without its currency and the handler joins it from the loaded aggregate; everywhere else money rides the shared object.</summary>
    /// <param name="entry">The field entry holding the type facts.</param>
    /// <param name="type">The coarse record type.</param>
    /// <param name="isInput">True when the entry belongs to an input.</param>
    /// <returns>The C# type, or null when the type is unknown or its facts are missing.</returns>
    private static string? ScalarOf(JsonElement entry, string type, bool isInput) => type switch
    {
        "text" or "email" => "string",
        "integer" => IntegerOf(entry),
        "decimal" => "decimal",
        "money" => MoneyOf(entry, isInput),
        "boolean" => "bool",
        "date" => "DateOnly",
        "instant" => "DateTimeOffset",
        "duration" => "TimeSpan",
        "file" => "byte[]",
        "choice" => SetOf(entry),
        "id" => IdentifierOf(entry),
        "map" => MapOf(entry, isInput),
        _ => null,
    };

    /// <summary>The C# type a money field rides as: a decimal for an input stating currencyHome, else the shared money object.</summary>
    /// <param name="entry">The field entry holding the currency facts.</param>
    /// <param name="isInput">True when the entry belongs to an input.</param>
    /// <returns>The money type.</returns>
    private static string MoneyOf(JsonElement entry, bool isInput) =>
        isInput && RecordEntries.Str(entry, RecordFields.CurrencyHome) is { Length: > 0 } ? "decimal" : "Money";

    /// <summary>The C# type an integer field rides as: a long with size 64, else an int.</summary>
    /// <param name="entry">The field entry holding the size fact.</param>
    /// <returns>The integer type.</returns>
    private static string IntegerOf(JsonElement entry) =>
        entry.TryGetProperty(RecordFields.Size, out var size)
        && size.ValueKind == JsonValueKind.Number
        && size.TryGetInt32(out var bits)
        && bits == 64
            ? "long"
            : "int";

    /// <summary>The read-only dictionary a map field rides as, keyed by its key type over its value type.</summary>
    /// <param name="entry">The field entry holding the key and value types.</param>
    /// <param name="isInput">True when the entry belongs to an input.</param>
    /// <returns>The dictionary type, or null when either side states no known shape.</returns>
    private static string? MapOf(JsonElement entry, bool isInput)
    {
        if (RecordEntries.Str(entry, RecordFields.KeyType) is not { Length: > 0 } key
            || RecordEntries.Str(entry, RecordFields.ValueType) is not { Length: > 0 } value
            || value is "map" or "list")
        {
            return null;
        }

        var keyType = key switch
        {
            "text" => "string",
            "integer" => "int",
            _ => null,
        };

        if (keyType is null || ScalarOf(entry, value, isInput) is not { } valueType)
        {
            return null;
        }

        return $"IReadOnlyDictionary<{keyType}, {valueType}>";
    }

    /// <summary>The property name of one contract field: its PascalCase name, as events name theirs.</summary>
    /// <param name="name">The field name.</param>
    /// <returns>The property name.</returns>
    public static string PropertyName(string name) => GenerateNaming.ConstName(name);

    /// <summary>The closed-set type a choice field takes its values from.</summary>
    /// <param name="entry">The field entry holding the set reference.</param>
    /// <returns>The set base name the closed-sets generator writes, or null when the field names no set.</returns>
    private static string? SetOf(JsonElement entry)
    {
        if (RecordEntries.Str(entry, RecordFields.Set) is not { Length: > 0 } set
            || !Identifiers.TrySplit(set, out _, out _, out var name)
            || name.Length == 0)
        {
            return null;
        }

        return GenerateNaming.ConstName(name);
    }

    /// <summary>The typed identifier an id field names: the aggregate or part identifier, or a Guid for any other target or with no identifies.</summary>
    /// <param name="entry">The field entry holding the identifies reference.</param>
    /// <returns>The identifier type, or null when the identifies reference is malformed.</returns>
    private static string? IdentifierOf(JsonElement entry)
    {
        if (RecordEntries.Str(entry, RecordFields.Identifies) is not { Length: > 0 } identifies)
        {
            return "Guid";
        }

        if (!identifies.StartsWith("aggregate/", StringComparison.Ordinal))
        {
            return "Guid";
        }

        var rest = identifies["aggregate/".Length..];
        var hash = rest.IndexOf('#');

        if (hash >= 0)
        {
            var part = rest[(hash + 1)..];

            return part.Length > 0 ? GenerateNaming.ConstName(part) + "Id" : null;
        }

        var dot = rest.IndexOf('.');

        if (dot < 0 || dot == rest.Length - 1)
        {
            return null;
        }

        return GenerateNaming.ConstName(rest[(dot + 1)..]) + "Id";
    }

    /// <summary>Whether a field is required: true when it states so or states nothing, false only when it states false.</summary>
    /// <param name="entry">The field entry.</param>
    /// <returns>True when the entry requires the field; otherwise false.</returns>
    private static bool IsRequired(JsonElement entry) =>
        !entry.TryGetProperty(RecordFields.Required, out var required) || required.ValueKind != JsonValueKind.False;

    /// <summary>Whether a field holds a list of values of its type.</summary>
    /// <param name="entry">The field entry.</param>
    /// <returns>True when the entry states many; otherwise false.</returns>
    private static bool IsMany(JsonElement entry) =>
        entry.TryGetProperty(RecordFields.Many, out var many) && many.ValueKind == JsonValueKind.True;
}
