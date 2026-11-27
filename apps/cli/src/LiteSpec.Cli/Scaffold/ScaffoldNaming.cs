using LiteSpec.Model;

namespace LiteSpec.Cli.Scaffold;

/// <summary>
///     The names and C# shapes every scaffold renderer shares: the use case, message,
///     result and definition names from the record id, and the C# type of each record field.
/// </summary>
/// <follows>litespec/core/backend-application#organize-application-by-use-case</follows>
static class ScaffoldNaming
{
    /// <summary>The entry point kinds scaffold treats as web: the HTTP-served browser and API surfaces.</summary>
    public static readonly IReadOnlySet<string> WebKinds =
        new HashSet<string>(StringComparer.Ordinal) { "api", "browser" };

    /// <summary>The use case stem, such as BorrowCopy for borrow-copy.</summary>
    /// <param name="useCaseName">The name part of the use case id.</param>
    /// <returns>The stem in PascalCase.</returns>
    public static string Stem(string useCaseName) => Naming.Pascal(useCaseName);

    /// <summary>The aggregate act scaffold expects on the aggregate root, from the use case verb: Borrow for borrow-copy.</summary>
    /// <param name="useCaseName">The name part of the use case id.</param>
    /// <returns>The act name in PascalCase.</returns>
    public static string ActName(string useCaseName)
    {
        var verb = useCaseName.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? useCaseName;

        return Naming.Pascal(verb);
    }

    /// <summary>The C# property name of a record field, such as CopyId for copyId.</summary>
    /// <param name="fieldName">The field name as written.</param>
    /// <returns>The property name in PascalCase.</returns>
    public static string PropertyName(string fieldName) => Naming.Pascal(fieldName);

    /// <summary>The singular of a list field name, so the row reads LoanRow for loans.</summary>
    /// <param name="fieldName">The field name as written.</param>
    /// <returns>The singular name, or the name unchanged when it names none.</returns>
    public static string Singular(string fieldName)
    {
        if (fieldName.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && fieldName.Length > 3)
        {
            return fieldName[..^3] + "y";
        }

        if ((fieldName.EndsWith("sses", StringComparison.OrdinalIgnoreCase)
            || fieldName.EndsWith("xes", StringComparison.OrdinalIgnoreCase)
            || fieldName.EndsWith("zes", StringComparison.OrdinalIgnoreCase)
            || fieldName.EndsWith("ches", StringComparison.OrdinalIgnoreCase)
            || fieldName.EndsWith("shes", StringComparison.OrdinalIgnoreCase))
            && fieldName.Length > 2)
        {
            return fieldName[..^2];
        }

        if (fieldName.EndsWith('s') && !fieldName.EndsWith("ss", StringComparison.OrdinalIgnoreCase) && fieldName.Length > 1)
        {
            return fieldName[..^1];
        }

        return fieldName;
    }

    /// <summary>The C# type of a record contract type, such as Guid for id and string for text.</summary>
    /// <param name="recordType">The record field type.</param>
    /// <param name="required">Whether the field is required; an optional field is nullable.</param>
    /// <param name="itemType">The element type of a list field.</param>
    /// <returns>The C# type name.</returns>
    public static string FieldType(string recordType, bool required, string? itemType = null)
    {
        var body = recordType switch
        {
            "text" or "email" or "choice" => "string",
            "integer" => "int",
            "decimal" => "decimal",
            "money" => "Money",
            "boolean" => "bool",
            "date" => "DateOnly",
            "date-time" => "DateTimeOffset",
            "duration" => "TimeSpan",
            "id" => "Guid",
            "file" => "byte[]",
            "list" => $"IReadOnlyList<{itemType ?? "string"}>",
            _ => "string",
        };

        return required ? body : body + "?";
    }

    /// <summary>Escapes a value for a C# double-quoted string literal.</summary>
    /// <param name="text">The value text.</param>
    /// <returns>The escaped literal body.</returns>
    public static string Escape(string text)
    {
        var escaped = text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

        var built = new System.Text.StringBuilder(escaped.Length);

        foreach (var character in escaped)
        {
            if (char.IsControl(character))
            {
                built.Append("\\u");
                built.Append(((int)character).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                built.Append(character);
            }
        }

        return built.ToString();
    }
}
