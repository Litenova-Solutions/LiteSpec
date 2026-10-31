using System.Text.Json.Nodes;

namespace LiteSpec.Authoring;

/// <summary>
///     The field-naming contract: one concept, one name, and one shape on every record that carries it.
/// </summary>
static partial class StandardsValidator
{
    // One concept keeps one name and one shape everywhere it appears, so an
    // author who has learned a field on one record has learned it on them all.
    // The names below are the declared exceptions beside the provision; every
    // other field follows the rule.
    // (litespec/core/language#name-each-field-by-its-word-class)
    static readonly HashSet<string> SameShapeExceptions = new(["addresses", "code", "subject"], StringComparer.Ordinal);
    static readonly HashSet<string> IsoDateNameExceptions = new(["effectiveFrom"], StringComparer.Ordinal);
    static readonly HashSet<string> DateOrMilestoneFields = new(["dueBy"], StringComparer.Ordinal);

    // The entry records the records array carries. Nested object defs stay out
    // of scope: their fields are read inside their parent object, so they
    // cannot collide with record-level names.
    static readonly HashSet<string> FieldNamingEntries = new(
        ["requirementEntry", "permissionEntry", "ruleEntry", "resultEntry", "scenarioEntry", "eventEntry", "entryPointEntry", "settingEntry"],
        StringComparer.Ordinal);

    static readonly string[] FieldNamingSchemas = ["schemas/record.schema.json", "schemas/glossary.schema.json"];

    static void CheckFieldNaming(string root, Action<string, int, string, string> add)
    {
        foreach (var relative in FieldNamingSchemas)
        {
            var file = JoinRoot(root, relative);
            if (!File.Exists(file))
            {
                continue;
            }

            JsonNode? schema;
            try
            {
                schema = JsonNode.Parse(TrimBom(ReadText(file)));
            }
            catch (Exception)
            {
                continue;
            }

            if ((schema as JsonObject)?["$defs"] is JsonObject defs)
            {
                CheckFieldNamingFile(relative, defs, add);
            }
        }
    }

    static void CheckFieldNamingFile(string relative, JsonObject defs, Action<string, int, string, string> add)
    {
        var scope = defs
            .Select(entry => entry.Key)
            .Where(name => IsFieldNamingScope(relative, name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => (Def: name, Properties: (defs[name] as JsonObject)?["properties"] as JsonObject))
            .Where(entry => entry.Properties is not null)
            .ToList();

        // The record-type nouns come from the schema's own identifier defs, so
        // the set follows the schema instead of a copied list.
        var idDefs = defs
            .Select(entry => entry.Key)
            .Where(name => name.Length > 2 && name.EndsWith("Id", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var nouns = idDefs.Select(name => name[..^2]).ToHashSet(StringComparer.Ordinal);

        var shapes = new Dictionary<string, List<(string Def, string? Shape)>>(StringComparer.Ordinal);
        foreach (var (def, properties) in scope)
        {
            foreach (var (field, node) in properties!.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (!shapes.TryGetValue(field, out var seen))
                {
                    seen = [];
                    shapes[field] = seen;
                }

                seen.Add((def, FieldShape(node, defs, [])));
            }
        }

        foreach (var field in shapes.Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            if (SameShapeExceptions.Contains(field))
            {
                continue;
            }

            string? shapeA = null;
            string? defA = null;
            string? shapeB = null;
            string? defB = null;
            foreach (var (def, shape) in shapes[field])
            {
                if (shape is null)
                {
                    continue;
                }

                if (shapeA is null)
                {
                    shapeA = shape;
                    defA = def;
                }
                else if (shape != shapeA)
                {
                    shapeB = shape;
                    defB = def;
                    break;
                }
            }

            if (shapeB is not null)
            {
                add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` holds {shapeA} in {defA} and {shapeB} in {defB}; rename one concept or record the exception in litespec/core/language#name-each-field-by-its-word-class");
            }
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (def, properties) in scope)
        {
            foreach (var (field, node) in properties!.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (FieldRefTarget(node) == "isoDate"
                    && !IsoDateNameExceptions.Contains(field)
                    && !EndsByOrOn(field)
                    && reported.Add("date:" + field))
                {
                    add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` holds a date but its name ends in neither By nor On");
                }

                if (ResolvesToDef(node, "nonEmptyString", defs, [])
                    && !DateOrMilestoneFields.Contains(field)
                    && EndsByOrOn(field)
                    && reported.Add("string:" + field))
                {
                    add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` reads as a deadline or a recorded date but holds a plain string; make it an ISO date or declare it a date-or-milestone field");
                }

                if (nouns.Contains(field))
                {
                    var shape = FieldShape(node, defs, []);
                    if (shape == "array")
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names a record type but holds an array under a singular name; use the plural name for a list and another name for prose");
                    }
                    else if (shape == "string" && !ReachesTypeId(node, TypeIdsFor(field, idDefs), defs, []))
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names a record type but holds prose; use the plural name for a list and another name for prose");
                    }
                }
            }
        }
    }

    static bool IsFieldNamingScope(string relative, string name) =>
        name.EndsWith("Page", StringComparison.Ordinal)
        || FieldNamingEntries.Contains(name)
        || (relative == "schemas/glossary.schema.json" && name == "term");

    static bool EndsByOrOn(string field) =>
        field.EndsWith("By", StringComparison.Ordinal) || field.EndsWith("On", StringComparison.Ordinal);

    static string? FieldRefTarget(JsonNode? node)
    {
        const string prefix = "#/$defs/";
        var reference = node is JsonObject obj ? JsonString(obj["$ref"]) : null;
        return reference is not null && reference.StartsWith(prefix, StringComparison.Ordinal) ? reference[prefix.Length..] : null;
    }

    // A shape is the JSON kind the $ref chain lands on. Enum, const, and
    // pattern forms are strings; a branch that mixes kinds resolves to no
    // shape, and the same-name rule skips it instead of guessing.
    static string? FieldShape(JsonNode? node, JsonObject defs, HashSet<string> visited)
    {
        if (node is not JsonObject obj)
        {
            return null;
        }

        if (JsonString(obj["type"]) is string type)
        {
            return type switch
            {
                "string" => "string",
                "array" => "array",
                "object" => "object",
                "boolean" => "boolean",
                "integer" => "integer",
                _ => null,
            };
        }

        var target = FieldRefTarget(obj);
        if (target is not null)
        {
            if (!visited.Add(target))
            {
                return null;
            }

            try
            {
                return defs[target] is JsonObject resolved ? FieldShape(resolved, defs, visited) : null;
            }
            finally
            {
                visited.Remove(target);
            }
        }

        foreach (var key in new[] { "anyOf", "allOf", "oneOf" })
        {
            if (obj[key] is JsonArray branches && branches.Count > 0)
            {
                string? shape = null;
                foreach (var branch in branches)
                {
                    var next = FieldShape(branch, defs, visited);
                    if (next is null || (shape is not null && next != shape))
                    {
                        return null;
                    }

                    shape ??= next;
                }

                return shape;
            }
        }

        if (obj["properties"] is JsonObject)
        {
            return "object";
        }

        return obj["enum"] is JsonArray || obj["const"] is not null || JsonString(obj["pattern"]) is not null ? "string" : null;
    }

    static bool ResolvesToDef(JsonNode? node, string defName, JsonObject defs, HashSet<string> visited)
    {
        if (node is not JsonObject obj)
        {
            return false;
        }

        var target = FieldRefTarget(obj);
        if (target is not null)
        {
            if (target == defName)
            {
                return true;
            }

            if (!visited.Add(target))
            {
                return false;
            }

            try
            {
                return defs[target] is JsonObject resolved && ResolvesToDef(resolved, defName, defs, visited);
            }
            finally
            {
                visited.Remove(target);
            }
        }

        foreach (var key in new[] { "anyOf", "allOf", "oneOf" })
        {
            if (obj[key] is JsonArray branches && branches.Count > 0)
            {
                return branches.All(branch => ResolvesToDef(branch, defName, defs, visited));
            }
        }

        return false;
    }

    // The identifier defs of one record type: its own $def and the derived
    // forms that end in the same name, such as derivedResultId beside resultId.
    static HashSet<string> TypeIdsFor(string noun, HashSet<string> idDefs) =>
        idDefs.Where(name => name.EndsWith(noun + "Id", StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.Ordinal);

    static bool ReachesTypeId(JsonNode? node, HashSet<string> typeIds, JsonObject defs, HashSet<string> visited)
    {
        if (node is not JsonObject obj)
        {
            return false;
        }

        var target = FieldRefTarget(obj);
        if (target is not null)
        {
            if (typeIds.Contains(target))
            {
                return true;
            }

            if (!visited.Add(target))
            {
                return false;
            }

            try
            {
                return defs[target] is JsonObject resolved && ReachesTypeId(resolved, typeIds, defs, visited);
            }
            finally
            {
                visited.Remove(target);
            }
        }

        foreach (var key in new[] { "anyOf", "allOf", "oneOf" })
        {
            if (obj[key] is JsonArray branches && branches.Count > 0)
            {
                return branches.All(branch => ReachesTypeId(branch, typeIds, defs, visited));
            }
        }

        return false;
    }
}
