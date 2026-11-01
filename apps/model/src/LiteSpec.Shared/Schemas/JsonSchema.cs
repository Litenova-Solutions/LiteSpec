using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     The local JSON Schema evaluator, and the keyword gate that makes it safe.
///     Three validators read this module, so a second copy of the evaluator would
///     drift from the first invisibly. The gate below is what makes a local
///     evaluator safe: a keyword with no implementation fails the schema instead
///     of passing unread.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class JsonSchema
{
    /// <summary>The schema keywords that assert nothing (SCHEMA_ANNOTATIONS).</summary>
    public static readonly IReadOnlySet<string> Annotations = new HashSet<string>(StringComparer.Ordinal)
    {
        "$schema", "$id", "$defs", "$comment", "title", "description", "default", "examples", "deprecated", "format",
    };

    /// <summary>
    ///     Every keyword the evaluator implements. A keyword listed here without an
    ///     implementation would read as an annotation and assert nothing, so every
    ///     assertive name in this set has a branch in <see cref="Validate"/>, and every
    ///     name outside it fails the schema rather than passing unread (SCHEMA_KEYWORDS).
    /// </summary>
    public static readonly IReadOnlySet<string> Keywords = new HashSet<string>(Annotations.Concat([
        "$ref", "type", "const", "enum", "pattern", "minLength", "maxLength",
        "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum", "multipleOf",
        "minItems", "maxItems", "uniqueItems", "items", "prefixItems", "contains",
        "minProperties", "maxProperties", "required", "dependentRequired",
        "properties", "patternProperties", "propertyNames", "additionalProperties",
        "allOf", "anyOf", "oneOf", "if", "then", "else", "not",
    ]), StringComparer.Ordinal);

    /// <summary>
    ///     Reports each keyword the evaluator does not implement, descending into every
    ///     subschema position. <paramref name="add"/> takes the relative path, line, code
    ///     and message.
    /// </summary>
    /// <param name="schema">The schema to gate.</param>
    /// <param name="relative">The schema file, relative to its root.</param>
    /// <param name="pointer">The JSON pointer of the schema.</param>
    /// <param name="add">The diagnostic sink, taking the relative path, line, code, and message.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "The pointer parameter names the JSON Pointer of the schema in the JSON Schema specification; renaming the public parameter breaks callers that pass it by name.")]
    public static void UnsupportedKeywords(JsonNode? schema, string relative, [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "The pointer parameter names the JSON Pointer of the schema in the JSON Schema specification; renaming the public parameter breaks callers that pass it by name.")] string pointer, Action<string, int, string, string> add) =>
        SchemaGate.UnsupportedKeywords(schema, relative, pointer, add);

    /// <summary>
    ///     Resolves a local <c>#/...</c> reference against the schema root, unescaping
    ///     <c>~1</c> to <c>/</c> and <c>~0</c> to <c>~</c> in each pointer segment.
    /// </summary>
    /// <param name="root">The schema root.</param>
    /// <param name="reference">The local reference.</param>
    /// <returns>The referenced schema, or null when it does not resolve.</returns>
    /// <exception cref="InvalidOperationException">The reference does not start with <c>#/</c>.</exception>
    public static JsonNode? ResolveRef(JsonNode root, string reference) =>
        SchemaReferences.ResolveRef(root, reference);

    /// <summary>
    ///     Validates <paramref name="value"/> against <paramref name="schema"/>, appending
    ///     one <c>{location}: ...</c> message per violation to <paramref name="errors"/>.
    ///     A missing property is distinct from an explicit JSON null.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema to validate against.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    public static void Validate(JsonNode? value, JsonNode? schema, JsonNode root, string location, List<string> errors)
    {
        if (schema is not JsonObject obj)
        {
            return;
        }

        SchemaReferences.ValidateReference(value, obj, root, location, errors);
        SchemaComposition.ValidateComposition(value, obj, root, location, errors);

        if (!SchemaTypes.ValidateTypes(value, obj, location, errors))
        {
            return;
        }

        SchemaStrings.ValidateStrings(value, obj, location, errors);
        SchemaNumbers.ValidateNumbers(value, obj, location, errors);
        SchemaArrays.ValidateArrays(value, obj, root, location, errors);
        SchemaObjects.ValidateObjects(value, obj, root, location, errors);
    }

    /// <summary>
    ///     Reads one JSON value against one schema file and returns the problems it
    ///     found. The caller decides how to report them, because a standards diagnostic
    ///     carries a code and a consumer problem carries a provision identifier.
    /// </summary>
    /// <param name="schemaFile">The schema file name.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="location">The value location.</param>
    /// <param name="readJson">The reader that loads a schema file as JSON.</param>
    /// <returns>The problems found.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Problems reports a schema validation as problem text rather than crashing the caller, so every failure of the local evaluator becomes one problem message.")]
    public static List<string> Problems(string schemaFile, JsonNode? value, string location, Func<string, JsonNode?> readJson)
    {
        var schema = readJson(schemaFile);

        if (!IsTruthy(schema))
        {
            return [$"{location}: schema '{schemaFile}' is unreadable"];
        }

        var problems = new List<string>();

        foreach (var message in SchemaGate.GateMessages(schema!, schemaFile))
        {
            problems.Add($"{location}: {message}");
        }

        try
        {
            Validate(value, schema, schema!, location, problems);
        }
        catch (Exception cause)
        {
            problems.Add($"{location}: {cause.Message}");
        }

        return problems;
    }
}
