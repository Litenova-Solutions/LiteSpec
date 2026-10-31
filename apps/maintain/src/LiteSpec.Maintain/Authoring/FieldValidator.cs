using System.Text.Json.Nodes;
using static LiteSpec.Authoring.ManifestValidator;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The field-naming contract: one concept, one name, and one shape on every record that carries it.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed class FieldValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FieldValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public FieldValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    // One concept keeps one name and one shape everywhere it appears, so an
    // author who has learned a field on one record has learned it on them all.
    // The names below are the declared exceptions beside the provision; every
    // other field follows the rule.
    // (litespec/core/language#name-each-field-by-its-word-class)
    /// <summary>Gets the fields excepted from the same-shape rule.</summary>
    private static readonly HashSet<string> SameShapeExceptions = new(["addresses", "code", "subject"], StringComparer.Ordinal);

    /// <summary>Gets the fields excepted from the ISO date name rule.</summary>
    private static readonly HashSet<string> IsoDateNameExceptions = new(["effectiveFrom", "expiresOn"], StringComparer.Ordinal);

    /// <summary>Gets the fields declared as a date or a milestone.</summary>
    private static readonly HashSet<string> DateOrMilestoneFields = new(["dueBy"], StringComparer.Ordinal);

    // The word-class names the schemas carry: a link verb holds links, a
    // closed-set noun holds an enum, and a prose noun holds text. A name
    // outside every set faces no class rule.
    // (litespec/core/language#name-each-field-by-its-word-class)
    /// <summary>Gets the field names that read as link verbs.</summary>
    private static readonly HashSet<string> LinkVerbFields = new(
        ["serves", "fulfills", "solves", "advances", "cites", "governs", "raises", "reads", "changes", "covers", "addresses",
            "allows", "affects", "holds", "offers", "sends", "receives", "translates", "admits", "decides", "carries", "replaces",
            "binds", "exercises"],
        StringComparer.Ordinal);

    /// <summary>Gets the field names that name a closed set.</summary>
    private static readonly HashSet<string> ClosedSetNouns = new(
        ["kind", "mode", "direction", "trust", "reach", "lifecycle", "level"],
        StringComparer.Ordinal);

    /// <summary>Gets the field names that name prose.</summary>
    private static readonly HashSet<string> ProseNouns = new(
        ["statement", "meaning", "rationale", "summary", "conclusion", "failure", "exclusions"],
        StringComparer.Ordinal);

    // The participle stems a suffix test cannot see: irregular verbs whose
    // past form spells nothing like the base.
    /// <summary>Gets the irregular participle stems.</summary>
    private static readonly HashSet<string> IrregularParticiples = new(["set", "read"], StringComparer.Ordinal);

    // The entry records the records array carries. Nested object defs stay out
    // of scope: their fields are read inside their parent object, so they
    // cannot collide with record-level names.
    /// <summary>Gets the entry definition names in field-naming scope.</summary>
    private static readonly HashSet<string> FieldNamingEntries = new(
        ["requirementEntry", "permissionEntry", "ruleEntry", "resultEntry", "scenarioEntry", "eventEntry", "entryPointEntry", "settingEntry"],
        StringComparer.Ordinal);

    /// <summary>Gets the schemas whose fields face the naming rules.</summary>
    private static readonly string[] FieldNamingSchemas = ["schemas/record.schema.json", "schemas/glossary.schema.json"];

    /// <summary>Checks the field names of every field-naming schema.</summary>
    internal void CheckFieldNaming()
    {
        var root = Context.Root;
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
                CheckFieldNamingFile(relative, defs);
            }
        }
    }

    /// <summary>Checks the field names of one schema file.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="defs">The schema definitions.</param>
    private void CheckFieldNamingFile(string relative, JsonObject defs)
    {
        var add = Context.Add;
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

                // A link verb holds one link or a list of links: an identifier
                // reference or an array. A verb holding prose or a flag names
                // the wrong class.
                if (LinkVerbFields.Contains(field))
                {
                    var shape = FieldShape(node, defs, []);
                    if (shape is not null && shape != "array" && !ReachesAnyId(node, idDefs, defs, []))
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} reads as a link verb but holds {shape}; a link verb holds one link or a list of links");
                    }
                }

                // A name ending in By either marks a deadline or reads as
                // <participle>By: the target acts on this record. A deadline
                // holds a date; anything else carries a participle stem.
                if (field.Length > 2
                    && field.EndsWith("By", StringComparison.Ordinal)
                    && !DateOrMilestoneFields.Contains(field)
                    && !ResolvesToIsoDate(node, defs, []))
                {
                    var stem = field[..^2];
                    if (!IsParticipleStem(stem))
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} ends in By but `{stem}` is no participle; name it `<participle>By` or hold a date under a deadline name");
                    }
                }

                // A closed-set noun holds an enum: the dimension it names takes
                // one of fixed values.
                if (ClosedSetNouns.Contains(field) && !HoldsEnum(node, defs, []))
                {
                    add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names a closed set but holds no enum; a closed-set noun holds one of its values");
                }

                // A prose noun holds text, or a list of sentences under a plural
                // name. Anything else names the wrong class.
                if (ProseNouns.Contains(field))
                {
                    var shape = FieldShape(node, defs, []);
                    if (shape is not null && shape != "string" && !(shape == "array" && ArrayItemsAreStrings(node, defs)))
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names prose but holds {shape}; a prose noun holds text or a list of sentences");
                    }
                }

                // A condition holds one clause completing "when ...": text under
                // a *When name, or `condition` inside an entry.
                if ((field.Length > 4 && field.EndsWith("When", StringComparison.Ordinal)) || field == "condition")
                {
                    var shape = FieldShape(node, defs, []);
                    if (shape is not null && shape != "string")
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names a condition but holds {shape}; a condition holds one clause");
                    }
                }

                // A reason holds one clause completing "because ...": text under
                // a *Because name, or `reason` alone.
                if ((field.Length > 7 && field.EndsWith("Because", StringComparison.Ordinal)) || field == "reason")
                {
                    var shape = FieldShape(node, defs, []);
                    if (shape is not null && shape != "string")
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names a reason but holds {shape}; a reason holds one clause");
                    }
                }

                // A recorded date reads as <verb>edOn: the past verb says what
                // happened, and On says when it was recorded. A name beside the
                // provision states what its date does instead: `effectiveFrom`
                // opens a validity span, and `expiresOn` is the date the
                // exception lapses.
                // (litespec/core/language#name-each-date-by-what-it-marks)
                if (field.Length > 2
                    && field.EndsWith("On", StringComparison.Ordinal)
                    && ResolvesToIsoDate(node, defs, [])
                    && !IsoDateNameExceptions.Contains(field))
                {
                    var stem = field[..^2];
                    if (!IsPastVerbStem(stem))
                    {
                        add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} names a recorded date but `{stem}` is no past verb; name it `<verb>edOn`");
                    }
                }

                // A boolean reads as a present-tense predicate that is true
                // about the record when true, with no `is` or `has` prefix.
                // (litespec/core/language#name-a-boolean-as-a-predicate)
                if (FieldShape(node, defs, []) == "boolean" && HasIsHasPrefix(field))
                {
                    add(relative, 1, "FIELD_NAME_SHAPE", $"field `{field}` in {def} is a boolean with an `is` or `has` prefix; name it a present-tense predicate");
                }
            }
        }
    }

    /// <summary>Reports whether a definition name is in field-naming scope.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="name">The definition name.</param>
    /// <returns>True when the definition is in scope.</returns>
    private bool IsFieldNamingScope(string relative, string name) =>
        name.EndsWith("Page", StringComparison.Ordinal)
        || FieldNamingEntries.Contains(name)
        || (relative == "schemas/glossary.schema.json" && name == "term");

    /// <summary>Reports whether a field name ends in By or On.</summary>
    /// <param name="field">The field name.</param>
    /// <returns>True when the name ends in By or On.</returns>
    private bool EndsByOrOn(string field) =>
        field.EndsWith("By", StringComparison.Ordinal) || field.EndsWith("On", StringComparison.Ordinal);

    /// <summary>Reads the definition name a field schema references, or null when it references none.</summary>
    /// <param name="node">The field schema.</param>
    /// <returns>The referenced definition name, or null.</returns>
    private string? FieldRefTarget(JsonNode? node)
    {
        const string prefix = "#/$defs/";
        var reference = node is JsonObject obj ? JsonString(obj["$ref"]) : null;
        return reference is not null && reference.StartsWith(prefix, StringComparison.Ordinal) ? reference[prefix.Length..] : null;
    }

    // A shape is the JSON kind the $ref chain lands on. Enum, const, and
    // pattern forms are strings; a branch that mixes kinds resolves to no
    // shape, and the same-name rule skips it instead of guessing.
    /// <summary>Computes the JSON kind a field schema resolves to, or null when it mixes kinds.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>The JSON kind, or null.</returns>
    private string? FieldShape(JsonNode? node, JsonObject defs, HashSet<string> visited)
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

    /// <summary>Reports whether a field schema resolves to one named definition.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="defName">The definition name.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>True when every branch resolves to the definition.</returns>
    private bool ResolvesToDef(JsonNode? node, string defName, JsonObject defs, HashSet<string> visited)
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
    /// <summary>Lists the identifier definitions of one record type.</summary>
    /// <param name="noun">The record type noun.</param>
    /// <param name="idDefs">The identifier definition names.</param>
    /// <returns>The identifier definitions of the record type.</returns>
    private HashSet<string> TypeIdsFor(string noun, HashSet<string> idDefs) =>
        idDefs.Where(name => name.EndsWith(noun + "Id", StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.Ordinal);

    /// <summary>Reports whether a field schema reaches one of a record type's identifier definitions.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="typeIds">The identifier definitions of the record type.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>True when every branch reaches an identifier definition.</returns>
    private bool ReachesTypeId(JsonNode? node, HashSet<string> typeIds, JsonObject defs, HashSet<string> visited)
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

    // A single link: the $ref chain lands on an identifier def, as a verb
    // naming one record does.
    /// <summary>Reports whether a field schema reaches any identifier definition.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="idDefs">The identifier definition names.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>True when every branch reaches an identifier definition.</returns>
    private bool ReachesAnyId(JsonNode? node, HashSet<string> idDefs, JsonObject defs, HashSet<string> visited)
    {
        if (node is not JsonObject obj)
        {
            return false;
        }

        var target = FieldRefTarget(obj);
        if (target is not null)
        {
            if (idDefs.Contains(target))
            {
                return true;
            }

            if (!visited.Add(target))
            {
                return false;
            }

            try
            {
                return defs[target] is JsonObject resolved && ReachesAnyId(resolved, idDefs, defs, visited);
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
                return branches.All(branch => ReachesAnyId(branch, idDefs, defs, visited));
            }
        }

        return false;
    }

    // An ISO date either references the shared isoDate def or spells the date
    // shape inline, as the glossary schema does: a date format or a
    // year-month-day pattern.
    /// <summary>Reports whether a field schema resolves to an ISO date.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>True when the schema resolves to an ISO date.</returns>
    private bool ResolvesToIsoDate(JsonNode? node, JsonObject defs, HashSet<string> visited)
    {
        if (node is not JsonObject obj)
        {
            return false;
        }

        var target = FieldRefTarget(obj);
        if (target is not null)
        {
            if (target == "isoDate")
            {
                return true;
            }

            if (!visited.Add(target))
            {
                return false;
            }

            try
            {
                return defs[target] is JsonObject resolved && ResolvesToIsoDate(resolved, defs, visited);
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
                return branches.All(branch => ResolvesToIsoDate(branch, defs, visited));
            }
        }

        return JsonString(obj["format"]) == "date" || IsInlineDatePattern(JsonString(obj["pattern"]));
    }

    /// <summary>Reports whether a pattern spells a year-month-day date inline.</summary>
    /// <param name="pattern">The pattern text.</param>
    /// <returns>True when the pattern spells a date.</returns>
    private bool IsInlineDatePattern(string? pattern) =>
        pattern is not null
        && (pattern.Contains("[0-9]{4}-[0-9]{2}-[0-9]{2}", StringComparison.Ordinal)
            || pattern.Contains(@"\d{4}-\d{2}-\d{2}", StringComparison.Ordinal));

    /// <summary>Reports whether a field schema holds an enum.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>True when every branch holds an enum.</returns>
    private bool HoldsEnum(JsonNode? node, JsonObject defs, HashSet<string> visited)
    {
        if (node is not JsonObject obj)
        {
            return false;
        }

        var target = FieldRefTarget(obj);
        if (target is not null)
        {
            if (!visited.Add(target))
            {
                return false;
            }

            try
            {
                return defs[target] is JsonObject resolved && HoldsEnum(resolved, defs, visited);
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
                return branches.All(branch => HoldsEnum(branch, defs, visited));
            }
        }

        return obj["enum"] is JsonArray { Count: > 0 };
    }

    /// <summary>Reports whether a field schema is an array whose items are strings.</summary>
    /// <param name="node">The field schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>True when the array items are strings.</returns>
    private bool ArrayItemsAreStrings(JsonNode? node, JsonObject defs)
    {
        const string prefix = "#/$defs/";
        var current = node as JsonObject;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (current is not null && FieldRefTarget(current) is string reference)
        {
            if (!reference.StartsWith(prefix, StringComparison.Ordinal) || !visited.Add(reference))
            {
                return false;
            }

            current = defs[reference[prefix.Length..]] as JsonObject;
        }

        return current?["items"] is JsonNode items && FieldShape(items, defs, []) == "string";
    }

    /// <summary>Reports whether a stem reads as a participle.</summary>
    /// <param name="stem">The name stem.</param>
    /// <returns>True when the stem is a participle.</returns>
    private bool IsParticipleStem(string stem) =>
        stem.EndsWith("ed", StringComparison.Ordinal)
        || stem.EndsWith("en", StringComparison.Ordinal)
        || IrregularParticiples.Contains(stem);

    /// <summary>Reports whether a stem reads as a past-tense verb.</summary>
    /// <param name="stem">The name stem.</param>
    /// <returns>True when the stem is a past-tense verb.</returns>
    private bool IsPastVerbStem(string stem) =>
        stem.EndsWith("ed", StringComparison.Ordinal) || stem == "read";

    /// <summary>Reports whether a field name starts with an is or has prefix.</summary>
    /// <param name="field">The field name.</param>
    /// <returns>True when the name carries the prefix.</returns>
    private bool HasIsHasPrefix(string field) =>
        (field.StartsWith("is", StringComparison.Ordinal) && field.Length > 2 && char.IsUpper(field[2]))
        || (field.StartsWith("has", StringComparison.Ordinal) && field.Length > 3 && char.IsUpper(field[3]));
}
