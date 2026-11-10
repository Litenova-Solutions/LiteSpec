using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     The canonical key order of a record: identity, the type's own fields in
///     schema order, the shared links, then the entries. A key the order does
///     not name keeps its written place after the named keys.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RecordKeyOrder
{
    /// <summary>The keys that open every record.</summary>
    static readonly string[] Head = ["type", "id", "title"];

    /// <summary>The keys that follow the type's own fields.</summary>
    static readonly string[] Links = ["risk", "terms"];

    /// <summary>The keys that close every record.</summary>
    static readonly string[] Tail = ["records"];

    /// <summary>Each type's own fields in schema order, read once.</summary>
    static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> Orders = new(Load);

    /// <summary>The key order for one object: a record's canonical order, or none.</summary>
    /// <param name="value">The object.</param>
    /// <returns>The ordered keys.</returns>
    public static IReadOnlyList<string> For(JsonText value) =>
        value.TryGetProperty("type", out var type) && type.GetString() is { } name && Orders.Value.TryGetValue(name, out var order)
            ? order
            : [];

    /// <summary>Reads the record schema's per-type property order.</summary>
    /// <returns>The order per record type and page style.</returns>
    static Dictionary<string, IReadOnlyList<string>> Load()
    {
        var orders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var path = LiteSpecHome.PathTo("schemas/record.schema.yaml");

        if (!File.Exists(path))
        {
            return orders;
        }

        if (DataFile.Read(path) is not JsonObject schema || schema["$defs"] is not JsonObject definitions)
        {
            return orders;
        }

        var fixedKeys = new HashSet<string>(Head.Concat(Links).Concat(Tail), StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            if (definition.Value?["properties"] is not JsonObject properties
                || properties["type"]?["const"] is not JsonValue constant
                || !constant.TryGetValue<string>(out var type))
            {
                continue;
            }

            orders[type] = Head
                .Concat(properties.Select(property => property.Key).Where(key => !fixedKeys.Contains(key)))
                .Concat(Links)
                .Concat(Tail)
                .ToList();
        }

        return orders;
    }
}
