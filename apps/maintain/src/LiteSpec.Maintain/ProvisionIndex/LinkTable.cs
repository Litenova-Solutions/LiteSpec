using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Maintain.Validate;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.ProvisionIndex;

/// <summary>
///     The Links table of the model page. Each link property of the record schema carries its reads-as
///     and read-back-as phrases as annotations, so the table is generated from the schema between the
///     page's generated markers, and a link property without a phrase is reported.
/// </summary>
/// <follows>litespec/core/model#write-links-on-the-record-that-points</follows>
static partial class LinkTable
{
    /// <summary>The model page holding the generated Links table, relative to the repository root.</summary>
    public const string PagePath = "docs/core/model.md";

    /// <summary>The record schema whose link properties the table reads, relative to the repository root.</summary>
    public const string SchemaPath = "schemas/record.schema.yaml";

    /// <summary>The generated-region name of the Links table.</summary>
    public const string RegionName = "links";

    /// <summary>The annotation holding the phrase a link reads as, from the record that writes it.</summary>
    public const string ReadsAsKey = "x-reads-as";

    /// <summary>The annotation holding the phrase a link reads as, from the record it points at.</summary>
    public const string ReadBackAsKey = "x-read-back-as";

    /// <summary>The annotation keys a link property carries.</summary>
    static readonly string[] AnnotationKeys = [ReadsAsKey, ReadBackAsKey];

    /// <summary>The properties that hold no link: the record's own identifier and type, and the value shapes of inputs and outputs.</summary>
    static readonly HashSet<string> ShapeProperties = new(["id", "type", "input", "output", "fields", "of"], StringComparer.Ordinal);

    /// <summary>The prefix of a local schema reference.</summary>
    const string DefPrefix = "#/$defs/";

    /// <summary>The deepest schema nesting the walk follows, so a recursive definition ends.</summary>
    const int MaxDepth = 12;

    /// <summary>One link property: its field path, the record type that writes it, the words of what it points at, and its two phrases.</summary>
    /// <param name="Field">The field path, with nested names joined by dots.</param>
    /// <param name="WrittenOn">The record type that carries the field.</param>
    /// <param name="PointsAt">The record types it points at, in schema order.</param>
    /// <param name="ReadsAs">The phrase read from the record that writes it, or empty when missing.</param>
    /// <param name="ReadBackAs">The phrase read from the record it points at, or empty when missing.</param>
    sealed record Site(string Field, string WrittenOn, string PointsAt, string ReadsAs, string ReadBackAs);

    /// <summary>A link property with an annotation missing.</summary>
    /// <param name="Field">The field path.</param>
    /// <param name="WrittenOn">The record type that carries the field.</param>
    /// <param name="Missing">The annotation keys it lacks, joined in words.</param>
    sealed record Gap(string Field, string WrittenOn, string Missing);

    /// <summary>One table row being built: the link it names and the record types that write it.</summary>
    /// <param name="Field">The field path.</param>
    /// <param name="PointsAt">The record types it points at.</param>
    /// <param name="ReadsAs">The phrase read from the writing record.</param>
    /// <param name="ReadBackAs">The phrase read from the pointed-at record.</param>
    /// <param name="WrittenOn">The record types that write it, in schema order.</param>
    sealed record Group(string Field, string PointsAt, string ReadsAs, string ReadBackAs, List<string> WrittenOn);

    /// <summary>The link properties, the annotations they lack, and how many record types the schema declares.</summary>
    /// <param name="Sites">The link properties in schema order.</param>
    /// <param name="Gaps">The link properties with an annotation missing.</param>
    /// <param name="RecordTypes">The number of distinct record types.</param>
    sealed record Survey(List<Site> Sites, List<Gap> Gaps, int RecordTypes);

    /// <summary>Builds the model page with its Links table replaced by the one the schema generates.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The page path and the expected page text, or null when the repository holds no record schema.</returns>
    /// <exception cref="InvalidOperationException">The page or the schema is unreadable, or the page is missing or holds no links region.</exception>
    public static (string Path, string Text)? Output(string root)
    {
        if (!File.Exists(RepositoryWalk.JoinRoot(root, SchemaPath)))
        {
            return null;
        }

        var survey = Read(root);
        var region = $"<!-- generated:{RegionName} -->\n{Table(survey)}<!-- /generated:{RegionName} -->";
        var file = RepositoryWalk.JoinRoot(root, PagePath);

        if (!File.Exists(file))
        {
            throw new InvalidOperationException($"{PagePath} is missing.");
        }

        var page = FileText.ReadText(file).Replace("\r\n", "\n", StringComparison.Ordinal);
        var patched = ApplyRegion(page, region) ?? throw new InvalidOperationException($"{PagePath} holds no generated:{RegionName} region.");
        return (PagePath, patched);
    }

    /// <summary>Reports each link property whose reads-as or read-back-as annotation is missing.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="add">The diagnostic sink, taking the relative path, line, code, and message.</param>
    public static void Check(string root, Action<string, int, string, string> add)
    {
        if (!File.Exists(RepositoryWalk.JoinRoot(root, SchemaPath)))
        {
            return;
        }

        Survey survey;

        try
        {
            survey = Read(root);
        }
        catch (InvalidOperationException exception)
        {
            add(SchemaPath, 1, MaintainerDiagnostics.Codes.SchemaYaml, exception.Message);
            return;
        }

        foreach (var group in survey.Gaps.GroupBy(gap => (gap.Field, gap.Missing)))
        {
            var writers = string.Join(", ", group.Select(gap => gap.WrittenOn).Distinct(StringComparer.Ordinal));
            add(
                SchemaPath,
                1,
                MaintainerDiagnostics.Codes.LinkAnnotationMissing,
                $"link '{group.Key.Field}' on {writers} lacks {group.Key.Missing}; annotate every link property with {ReadsAsKey} and {ReadBackAsKey}");
        }
    }

    /// <summary>
    ///     Returns a copy of a schema without the link annotations. They annotate the schema and assert nothing, so
    ///     a keyword check that reads the copy does not report them.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <returns>The schema copy without the annotations.</returns>
    public static JsonNode? WithoutAnnotations(JsonNode? schema)
    {
        var copy = schema?.DeepClone();
        StripAnnotations(copy);
        return copy;
    }

    /// <summary>Removes the link annotations from a schema node and every node below it.</summary>
    /// <param name="node">The node to strip in place.</param>
    static void StripAnnotations(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in AnnotationKeys)
                {
                    obj.Remove(key);
                }

                foreach (var child in obj.Select(pair => pair.Value).ToList())
                {
                    StripAnnotations(child);
                }

                break;
            case JsonArray list:
                foreach (var child in list)
                {
                    StripAnnotations(child);
                }

                break;
        }
    }

    /// <summary>Reads the schema and surveys every link property of the record types it declares.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The survey.</returns>
    /// <exception cref="InvalidOperationException">The schema is missing or unreadable.</exception>
    static Survey Read(string root)
    {
        var schema = Load(root);
        var defs = schema["$defs"] as JsonObject ?? new JsonObject();
        var sites = new List<Site>();
        var gaps = new List<Gap>();
        var types = new List<string>();

        if (schema["oneOf"] is JsonArray pages)
        {
            foreach (var page in pages)
            {
                foreach (var definition in Objects(page, defs, 0))
                {
                    var type = TypeOf(definition);

                    if (type is null)
                    {
                        continue;
                    }

                    if (!types.Contains(type, StringComparer.Ordinal))
                    {
                        types.Add(type);
                    }

                    Walk(definition, "", type, defs, sites, gaps, 0);
                }
            }
        }

        return new Survey(sites, gaps, types.Count);
    }

    /// <summary>Reads the record schema as a JSON object.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The schema object.</returns>
    /// <exception cref="InvalidOperationException">The schema is missing, unreadable, or not an object.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A schema that cannot be read is reported as one error by the caller, so the generated table never crashes a validation run.")]
    static JsonObject Load(string root)
    {
        var file = RepositoryWalk.JoinRoot(root, SchemaPath);

        if (!File.Exists(file))
        {
            throw new InvalidOperationException($"{SchemaPath} is missing.");
        }

        try
        {
            return RecordText.Read(file) as JsonObject ?? throw new InvalidOperationException($"{SchemaPath} is not a schema object.");
        }
        catch (Exception exception) when (exception is not InvalidOperationException and not OutOfMemoryException)
        {
            throw new InvalidOperationException($"{SchemaPath} cannot be read: {exception.Message}", exception);
        }
    }

    /// <summary>Walks one record definition and its nested objects, collecting the link properties it writes.</summary>
    /// <param name="definition">The record definition, or an object nested in one.</param>
    /// <param name="prefix">The field path of the object, with a trailing dot, or empty at a record.</param>
    /// <param name="writtenOn">The record type that carries the object's fields.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="sites">The link properties found so far.</param>
    /// <param name="gaps">The link properties with an annotation missing.</param>
    /// <param name="depth">The nesting depth.</param>
    static void Walk(JsonObject definition, string prefix, string writtenOn, JsonObject defs, List<Site> sites, List<Gap> gaps, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        // A record that is itself typed, such as a rule or a permission held in a records list,
        // writes its own fields under its own type, and its field names start at the record.
        var own = TypeOf(definition);

        if (own is not null)
        {
            writtenOn = own;
            prefix = "";
        }

        foreach (var (name, schema) in Properties(definition))
        {
            if (ShapeProperties.Contains(name))
            {
                continue;
            }

            var targets = Identifiers(schema, defs, 0);

            if (targets.Count > 0)
            {
                Record(prefix + name, writtenOn, targets, schema, sites, gaps);
                continue;
            }

            foreach (var nested in Objects(schema, defs, 0))
            {
                Walk(nested, prefix + name + ".", writtenOn, defs, sites, gaps, depth + 1);
            }
        }
    }

    /// <summary>Records one link property and its annotations.</summary>
    /// <param name="field">The field path.</param>
    /// <param name="writtenOn">The record type that carries the field.</param>
    /// <param name="targets">The identifier definitions the field refers to.</param>
    /// <param name="schema">The property schema holding the annotations.</param>
    /// <param name="sites">The link properties found so far.</param>
    /// <param name="gaps">The link properties with an annotation missing.</param>
    static void Record(string field, string writtenOn, List<string> targets, JsonNode? schema, List<Site> sites, List<Gap> gaps)
    {
        var readsAs = Annotation(schema, ReadsAsKey);
        var readBackAs = Annotation(schema, ReadBackAsKey);
        var pointsAt = string.Join(", ", targets.Select(Words).Distinct(StringComparer.Ordinal));
        sites.Add(new Site(field, writtenOn, pointsAt, readsAs ?? "", readBackAs ?? ""));

        var missing = new List<string>();

        if (readsAs is null)
        {
            missing.Add(ReadsAsKey);
        }

        if (readBackAs is null)
        {
            missing.Add(ReadBackAsKey);
        }

        if (missing.Count > 0)
        {
            gaps.Add(new Gap(field, writtenOn, string.Join(" and ", missing)));
        }
    }

    /// <summary>Builds the Links table from the link properties.</summary>
    /// <param name="survey">The survey.</param>
    /// <returns>The table lines, each ending in a newline.</returns>
    static string Table(Survey survey)
    {
        var groups = new List<Group>();
        var index = new Dictionary<string, Group>(StringComparer.Ordinal);

        foreach (var site in survey.Sites)
        {
            var key = string.Join('\u001f', site.Field, site.PointsAt, site.ReadsAs, site.ReadBackAs);

            if (!index.TryGetValue(key, out var group))
            {
                group = new Group(site.Field, site.PointsAt, site.ReadsAs, site.ReadBackAs, []);
                index.Add(key, group);
                groups.Add(group);
            }

            if (!group.WrittenOn.Contains(site.WrittenOn, StringComparer.Ordinal))
            {
                group.WrittenOn.Add(site.WrittenOn);
            }
        }

        var text = new StringBuilder();
        text.Append("| Field | Written on | Points at | Reads as | Read back as |\n");
        text.Append("|:---|:---|:---|:---|:---|\n");

        foreach (var group in groups)
        {
            // A link that every record type but a few carries, such as the glossary terms, reads as any record.
            var writtenOn = group.WrittenOn.Count * 2 > survey.RecordTypes ? "any record" : string.Join(", ", group.WrittenOn);
            text.Append(CultureInfo.InvariantCulture, $"| `{Cell(group.Field)}` | {Cell(writtenOn)} | {Cell(group.PointsAt)} | {Cell(group.ReadsAs)} | {Cell(group.ReadBackAs)} |\n");
        }

        return text.ToString();
    }

    /// <summary>Replaces the links region of the model page.</summary>
    /// <param name="page">The page text.</param>
    /// <param name="region">The region, fenced by its generated markers.</param>
    /// <returns>The page with its region replaced, or null when the page holds no region.</returns>
    static string? ApplyRegion(string page, string region)
    {
        var open = $"<!-- generated:{RegionName} -->";
        var close = $"<!-- /generated:{RegionName} -->";
        var start = page.IndexOf(open, StringComparison.Ordinal);
        var end = page.IndexOf(close, StringComparison.Ordinal);

        if (start < 0 || end < start)
        {
            return null;
        }

        return page[..start] + region + page[(end + close.Length)..];
    }

    /// <summary>Reads the record type a definition carries in its type property.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The record type with spaces for hyphens, or null when the definition carries none.</returns>
    static string? TypeOf(JsonObject definition) =>
        definition["properties"] is JsonObject properties
        && properties["type"] is JsonObject type
        && type["const"] is JsonValue value
        && value.TryGetValue<string>(out var text)
            ? text.Replace('-', ' ')
            : null;

    /// <summary>Lists the properties a definition declares, including those its conditional and combined parts declare.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>Each property name with its schema, the first declaration winning.</returns>
    static Dictionary<string, JsonNode?> Properties(JsonObject definition)
    {
        var found = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        Collect(definition, found, 0);
        return found;
    }

    /// <summary>Adds the properties of one definition and of its combined and conditional parts.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="found">The properties found so far.</param>
    /// <param name="depth">The nesting depth.</param>
    static void Collect(JsonObject definition, Dictionary<string, JsonNode?> found, int depth)
    {
        if (depth > 6)
        {
            return;
        }

        if (definition["properties"] is JsonObject properties)
        {
            foreach (var pair in properties)
            {
                found.TryAdd(pair.Key, pair.Value);
            }
        }

        foreach (var key in new[] { "allOf", "then", "else" })
        {
            if (definition[key] is JsonArray parts)
            {
                foreach (var part in parts)
                {
                    if (part is JsonObject partObject)
                    {
                        Collect(partObject, found, depth + 1);
                    }
                }
            }
            else if (definition[key] is JsonObject single)
            {
                Collect(single, found, depth + 1);
            }
        }
    }

    /// <summary>Finds the identifier definitions a schema refers to, through references, arrays and alternatives.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="depth">The nesting depth.</param>
    /// <returns>The identifier definition names, in schema order; empty when the schema refers to none.</returns>
    static List<string> Identifiers(JsonNode? schema, JsonObject defs, int depth)
    {
        var found = new List<string>();

        if (schema is not JsonObject node || depth > MaxDepth)
        {
            return found;
        }

        if (RefName(node) is { } name)
        {
            if (IsIdentifierDef(name))
            {
                found.Add(name);
                return found;
            }

            if (defs[name] is { } target)
            {
                found.AddRange(Identifiers(target, defs, depth + 1));
            }

            return found;
        }

        if (node["items"] is { } items)
        {
            found.AddRange(Identifiers(items, defs, depth + 1));
        }

        foreach (var alternative in Alternatives(node))
        {
            found.AddRange(Identifiers(alternative, defs, depth + 1));
        }

        return found;
    }

    /// <summary>Finds the object definitions a schema refers to, through references, arrays and alternatives.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="depth">The nesting depth.</param>
    /// <returns>The object schemas, in schema order.</returns>
    static List<JsonObject> Objects(JsonNode? schema, JsonObject defs, int depth)
    {
        var found = new List<JsonObject>();

        if (schema is not JsonObject node || depth > MaxDepth)
        {
            return found;
        }

        if (RefName(node) is { } name)
        {
            if (!IsIdentifierDef(name) && defs[name] is { } target)
            {
                found.AddRange(Objects(target, defs, depth + 1));
            }

            return found;
        }

        if (node["properties"] is JsonObject || node["allOf"] is JsonArray)
        {
            found.Add(node);
            return found;
        }

        if (node["items"] is { } items)
        {
            found.AddRange(Objects(items, defs, depth + 1));
        }

        foreach (var alternative in Alternatives(node))
        {
            found.AddRange(Objects(alternative, defs, depth + 1));
        }

        return found;
    }

    /// <summary>Reads the definition name a local reference names.</summary>
    /// <param name="node">The schema object.</param>
    /// <returns>The definition name, or null when the object holds no local definition reference.</returns>
    static string? RefName(JsonObject node) =>
        node["$ref"] is JsonValue value
        && value.TryGetValue<string>(out var reference)
        && reference.StartsWith(DefPrefix, StringComparison.Ordinal)
            ? reference[DefPrefix.Length..]
            : null;

    /// <summary>Tells whether a definition names a record identifier or a record reference.</summary>
    /// <param name="name">The definition name.</param>
    /// <returns>True for a definition named with an <c>Id</c> or <c>Ref</c> suffix.</returns>
    static bool IsIdentifierDef(string name) => name.EndsWith("Id", StringComparison.Ordinal) || name.EndsWith("Ref", StringComparison.Ordinal);

    /// <summary>Lists the alternatives of a combined schema.</summary>
    /// <param name="node">The schema object.</param>
    /// <returns>Each alternative under <c>anyOf</c> or <c>oneOf</c>.</returns>
    static IEnumerable<JsonNode?> Alternatives(JsonObject node)
    {
        foreach (var key in new[] { "anyOf", "oneOf" })
        {
            if (node[key] is JsonArray list)
            {
                foreach (var item in list)
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>Reads one annotation of a schema object.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="key">The annotation key.</param>
    /// <returns>The annotation text, or null when it is missing or not a string.</returns>
    static string? Annotation(JsonNode? schema, string key) =>
        schema is JsonObject node && node[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Writes an identifier definition name as the words of the record type it names.</summary>
    /// <param name="definition">The definition name, such as <c>journeyStepId</c>.</param>
    /// <returns>The lowercase words, such as <c>journey step</c>.</returns>
    static string Words(string definition)
    {
        var stem = definition.EndsWith("Ref", StringComparison.Ordinal)
            ? definition[..^3]
            : definition.EndsWith("Id", StringComparison.Ordinal)
                ? definition[..^2]
                : definition;
        return WordBoundary().Replace(stem, "$1 $2").ToLowerInvariant();
    }

    /// <summary>Escapes a table cell so a pipe in the text does not start a new column.</summary>
    /// <param name="value">The cell text.</param>
    /// <returns>The escaped cell text.</returns>
    static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    /// <summary>Gets the pattern that finds a lowercase letter or digit followed by an uppercase letter.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
