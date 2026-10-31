using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     The one canonical JSON writer: object keys sorted, strings re-encoded and
///     numbers written by value, so the approval hash and the model diff read
///     the same bytes for the same meaning.
/// </summary>
/// <follows>litespec/core/model#record-approval-as-data</follows>
static class CanonicalJson
{
    /// <summary>The canonical text of one JSON value, with no field left out.</summary>
    /// <param name="value">The JSON value to canonicalize.</param>
    /// <returns>The canonical text of the value, with object keys in ordinal order.</returns>
    public static string Canonical(JsonElement value)
    {
        var built = new StringBuilder();

        Write(value, built);

        return built.ToString();
    }

    /// <summary>Writes one JSON value in canonical form.</summary>
    /// <param name="value">The JSON value to write in canonical form.</param>
    /// <param name="built">The builder the canonical text is appended to.</param>
    public static void Write(JsonElement value, StringBuilder built)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                built.Append('{');

                var first = true;

                foreach (var property in value.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        built.Append(',');
                    }

                    first = false;
                    built.Append(JsonSerializer.Serialize(property.Name));
                    built.Append(':');
                    Write(property.Value, built);
                }

                built.Append('}');
                break;

            case JsonValueKind.Array:
                built.Append('[');

                var head = true;

                foreach (var entry in value.EnumerateArray())
                {
                    if (!head)
                    {
                        built.Append(',');
                    }

                    head = false;
                    Write(entry, built);
                }

                built.Append(']');
                break;

            case JsonValueKind.String:
                built.Append(JsonSerializer.Serialize(value.GetString()));
                break;

            case JsonValueKind.Number:
                built.Append(CanonicalNumber(value.GetRawText()));
                break;

            case JsonValueKind.True:
                built.Append("true");
                break;

            case JsonValueKind.False:
                built.Append("false");
                break;

            default:
                built.Append("null");
                break;
        }
    }

    /// <summary>
    ///     Writes one JSON value in canonical form, leaving the named fields out
    ///     when the value is the top-level object. Nested objects keep every field.
    /// </summary>
    /// <param name="value">The JSON value to write, with exclusions applied only at the top-level object.</param>
    /// <param name="built">The builder the canonical text is appended to.</param>
    /// <param name="excluded">The top-level field names to leave out; nested objects keep every field.</param>
    public static void WriteTopLevel(JsonElement value, StringBuilder built, IReadOnlySet<string> excluded)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            Write(value, built);
            return;
        }

        built.Append('{');

        var first = true;

        foreach (var property in value.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            if (excluded.Contains(property.Name))
            {
                continue;
            }

            if (!first)
            {
                built.Append(',');
            }

            first = false;
            built.Append(JsonSerializer.Serialize(property.Name));
            built.Append(':');
            Write(property.Value, built);
        }

        built.Append('}');
    }

    /// <summary>
    ///     The canonical text of one number: the shortest invariant form with no
    ///     trailing zeros and no exponent, so <c>1</c>, <c>1.0</c> and <c>1e0</c>
    ///     write the same text. A number that fits no <see cref="decimal" />
    ///     keeps its written text.
    /// </summary>
    /// <param name="raw">The number text as written in the source JSON.</param>
    /// <returns>The shortest invariant form of the number, or the raw text when it fits no decimal.</returns>
    private static string CanonicalNumber(string raw)
    {
        if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return raw;
        }

        if (number == 0m)
        {
            return "0";
        }

        var text = number.ToString(CultureInfo.InvariantCulture);

        if (text.Contains('.'))
        {
            text = text.TrimEnd('0').TrimEnd('.');
        }

        return text;
    }
}
