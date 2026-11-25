using System.Text.Json;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Model;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     The C# types the event generators write for record fields: one mapped property per
///     carried field, from the coarse record type the way the message field checks read it.
///     An <c>id</c> becomes the typed identifier of what it identifies, or a <c>Guid</c>
///     with no <c>identifies</c>, a <c>choice</c> the closed set it names, a <c>file</c> the
///     carried bytes, an object field the part or code type its <c>type</c> names, and every
///     other type its fixed .NET form. A field with <c>required: false</c> rides nullable.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class EventFieldTypes
{
    /// <summary>One record field mapped to C#: its property name, its property type, and its row fields when the type lists multi-field rows.</summary>
    /// <param name="Property">The property name in PascalCase.</param>
    /// <param name="Type">The C# property type.</param>
    /// <param name="Row">The row fields of a multi-field list, or an empty list when the field is no multi-field list.</param>
    /// <param name="Meanings">The row field meanings by property name, for the row documentation.</param>
    public sealed record MappedField(
        string Property,
        string Type,
        IReadOnlyList<MappedField> Row,
        IReadOnlyDictionary<string, string> Meanings);

    /// <summary>Maps one domain-event carries entry to its C# property.</summary>
    /// <param name="carry">The carries entry holding the field name and its record type.</param>
    /// <param name="field">The mapped property, or null when the entry states no known shape.</param>
    /// <returns>True when the entry maps; otherwise false, and the event renders no file.</returns>
    public static bool TryMap(JsonElement carry, out MappedField? field)
    {
        field = null;

        if (RecordEntries.Str(carry, RecordFields.Name) is not { Length: > 0 } name
            || RecordEntries.Str(carry, RecordFields.Type) is not { Length: > 0 } type)
        {
            return false;
        }

        var property = GenerateNaming.ConstName(name);
        var optional = !IsRequired(carry);

        if (type == "list")
        {
            return TryList(carry, property, optional, out field);
        }

        if (ScalarOf(carry, type) is { } scalar)
        {
            field = new MappedField(property, optional ? scalar + "?" : scalar, [], new Dictionary<string, string>(StringComparer.Ordinal));

            return true;
        }

        if (ObjectTypeOf(type) is { } objectType)
        {
            field = new MappedField(property, optional ? objectType + "?" : objectType, [], new Dictionary<string, string>(StringComparer.Ordinal));

            return true;
        }

        return false;
    }

    /// <summary>Maps one list carry to its read-only list type: the row element for a single-field row, the named row type for a multi-field row stating rowType or a single-field one stating it, a nested row record otherwise.</summary>
    /// <param name="carry">The carries entry holding the row shape.</param>
    /// <param name="property">The property name in PascalCase.</param>
    /// <param name="optional">True when the event may arrive without the field.</param>
    /// <param name="field">The mapped property, or null when the row shape states no known shape.</param>
    /// <returns>True when the row shape maps; otherwise false.</returns>
    private static bool TryList(JsonElement carry, string property, bool optional, out MappedField? field)
    {
        field = null;

        if (!carry.TryGetProperty(RecordFields.RowShape, out var shape)
            || shape.ValueKind != JsonValueKind.Object
            || !shape.TryGetProperty(RecordFields.Fields, out var rows)
            || rows.ValueKind != JsonValueKind.Array
            || rows.GetArrayLength() == 0)
        {
            return false;
        }

        var fields = new List<MappedField>();
        var meanings = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object
                || RecordEntries.Str(row, RecordFields.Name) is not { Length: > 0 } name
                || RecordEntries.Str(row, RecordFields.Type) is not { Length: > 0 } type)
            {
                return false;
            }

            var named = GenerateNaming.ConstName(name);
            string member;

            if (type == "list")
            {
                if (!TryNestedList(row, out var nested) || nested is null)
                {
                    return false;
                }

                member = nested;
            }
            else
            {
                if (ScalarOf(row, type) is not { } scalar)
                {
                    return false;
                }

                member = scalar;

                if (!IsRequired(row))
                {
                    member += "?";
                }
            }

            fields.Add(new MappedField(named, member, [], new Dictionary<string, string>(StringComparer.Ordinal)));

            if (RecordEntries.Str(row, RecordFields.Meaning) is { Length: > 0 } meaning)
            {
                meanings[named] = meaning;
            }
        }

        if (fields.Count == 1 && RecordEntries.Str(carry, RecordFields.RowType) is not { Length: > 0 })
        {
            field = new MappedField(
                property,
                $"IReadOnlyList<{fields[0].Type}>" + (optional ? "?" : ""),
                [],
                new Dictionary<string, string>(StringComparer.Ordinal));

            return true;
        }

        if (RecordEntries.Str(carry, RecordFields.RowType) is { Length: > 0 } rowType)
        {
            if (ObjectTypeOf(rowType) is not { } shared)
            {
                return false;
            }

            field = new MappedField(
                property,
                $"IReadOnlyList<{shared}>" + (optional ? "?" : ""),
                [],
                new Dictionary<string, string>(StringComparer.Ordinal));

            return true;
        }

        field = new MappedField(property, $"IReadOnlyList<{property}Row>" + (optional ? "?" : ""), fields, meanings);

        return true;
    }

    /// <summary>Maps one list row field to its read-only list type: the row element for a single-field inner row, or the named row type for a multi-field inner row or a single-field one stating rowType. Inner rows hold scalars only, one level at a time.</summary>
    /// <param name="row">The row field entry holding the nested row shape.</param>
    /// <param name="type">The mapped C# type, or null when the nested shape states no known shape.</param>
    /// <returns>True when the nested shape maps; otherwise false.</returns>
    private static bool TryNestedList(JsonElement row, out string? type)
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
                || leafType == "list"
                || ScalarOf(leaf, leafType) is not { } scalar)
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
                if (ObjectTypeOf(rowType) is not { } single)
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
            && ObjectTypeOf(multiRowType) is { } shared)
        {
            type = $"IReadOnlyList<{shared}>" + (optional ? "?" : "");

            return true;
        }

        return false;
    }

    /// <summary>Maps one scalar record type to its C# type.</summary>
    /// <param name="entry">The field entry holding the type facts.</param>
    /// <param name="type">The coarse record type.</param>
    /// <returns>The C# type, or null when the type is unknown or its facts are missing.</returns>
    private static string? ScalarOf(JsonElement entry, string type) => type switch
    {
        "text" or "email" => "string",
        "integer" => "int",
        "decimal" => "decimal",
        "money" => "Money",
        "boolean" => "bool",
        "date" => "DateOnly",
        "instant" => "DateTimeOffset",
        "duration" => "TimeSpan",
        "file" => "byte[]",
        "choice" => SetOf(entry),
        "id" => IdentifierOf(entry),
        _ => null,
    };

    /// <summary>The closed-set type a choice field takes its values from, or a string when the field names no set.</summary>
    /// <param name="entry">The field entry holding the set reference.</param>
    /// <returns>The set base name the closed-sets generator writes, or a string when the field names no set.</returns>
    private static string SetOf(JsonElement entry)
    {
        if (RecordEntries.Str(entry, RecordFields.Set) is { Length: > 0 } set
            && Identifiers.TrySplit(set, out _, out _, out var name)
            && name.Length > 0)
        {
            return GenerateNaming.ConstName(name);
        }

        return "string";
    }

    /// <summary>The C# type an existing type reference names: the part name for an aggregate part, or the named code type.</summary>
    /// <param name="reference">The carried type or rowType value.</param>
    /// <returns>The C# type, or null when the value names neither a part nor a code type.</returns>
    internal static string? ObjectTypeOf(string reference)
    {
        if (IsCodeTypeName(reference))
        {
            return reference;
        }

        if (!reference.StartsWith("aggregate/", StringComparison.Ordinal))
        {
            return null;
        }

        var hash = reference.IndexOf('#');

        if (hash < 0 || hash == reference.Length - 1)
        {
            return null;
        }

        return GenerateNaming.ConstName(reference[(hash + 1)..]);
    }

    /// <summary>Whether a carried type names a C# type: one PascalCase word of letters and digits.</summary>
    /// <param name="reference">The carried type value.</param>
    /// <returns>True for a PascalCase name; otherwise false.</returns>
    private static bool IsCodeTypeName(string reference) =>
        reference.Length > 0
        && reference[0] is >= 'A' and <= 'Z'
        && reference.All(character => character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9'));

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

    /// <summary>Whether a carried or row field is required: true when it states so or states nothing, false only when it states false.</summary>
    /// <param name="entry">The field entry.</param>
    /// <returns>True when the entry requires the field; otherwise false.</returns>
    private static bool IsRequired(JsonElement entry) =>
        !entry.TryGetProperty(RecordFields.Required, out var required) || required.ValueKind != JsonValueKind.False;
}
