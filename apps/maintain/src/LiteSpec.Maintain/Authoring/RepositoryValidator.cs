using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Schemas;
using static LiteSpec.Authoring.ManifestValidator;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The repository-wide checks: identity, citations, templates, links, indexes, and removed forms.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class RepositoryValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public RepositoryValidator(ValidationContext context)
    {
        Context = context;
        ReservedTemplateStems = new HashSet<string>([.. TemplateVirtualPath.Values, "docs/core/template.md"], StringComparer.Ordinal);
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    // A standards template seeds one page class, so it is parsed as the page it
    // produces and held to that page's contract.
    // An authoring template is parsed as the page class it produces, so its
    // placeholder identifiers are normalized into valid ones and its virtual path
    // stands in for a real page path. Both live in the same namespace as real pages,
    // which is what makes the parse meaningful and also what makes the stem
    // reserved. A real page at one of these paths would share a scope with a
    // template and report as a duplicate of a file nobody wrote.
    /// <summary>Gets the virtual page path of each authoring template, by file name.</summary>
    internal readonly Dictionary<string, string> TemplateVirtualPath = new(StringComparer.Ordinal)
    {
        ["extension.md"] = "docs/ext/template.md",
        ["how-to.md"] = "docs/guide/template.md",
        ["tutorial.md"] = "docs/tutorial/template.md",
        ["command.md"] = "docs/tools/template.md",
        ["reference.md"] = "docs/reference/template.md",
    };

    /// <summary>Gets the page paths reserved as authoring template stems.</summary>
    internal readonly HashSet<string> ReservedTemplateStems;

    /// <summary>Checks that every docs path segment is lowercase kebab case.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckPageIdentity(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        // The identifier names the page path, so every folder and file stem on that
        // path is lowercase kebab case and the navigation pages keep their README.md name.
        foreach (var page in parsedPages)
        {
            if (!page.Relative.StartsWith("docs/", StringComparison.Ordinal))
            {
                continue;
            }

            var segments = page.Relative["docs/".Length..].Split('/');
            for (var index = 0; index < segments.Length; index++)
            {
                var last = index == segments.Length - 1;
                if (last && segments[index] == "README.md")
                {
                    continue;
                }

                var name = last && segments[index].EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    ? segments[index][..^".md".Length]
                    : segments[index];
                if (!KebabSegmentPattern().IsMatch(name))
                {
                    add(page.Relative, 1, "ID_PAGE_FILENAME", $"path segment '{name}' is not lowercase kebab case");
                }
            }
        }
    }

    /// <summary>Checks that every provision citation in active content names an active provision.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    /// <param name="activeIds">The active provision identifiers.</param>
    internal void CheckCitations(List<ParsedPage> parsedPages, HashSet<string> activeIds)
    {
        var add = Context.Add;
        foreach (var page in parsedPages)
        {
            var prose = string.Join('\n', page.Reader.Lines.Select((line, index) =>
                page.Reader.FencedLines.Contains(index + 1) || page.Reader.GeneratedLines.Contains(index + 1) ? "" : line));
            foreach (Match match in CitationPattern.Matches(prose))
            {
                if (!activeIds.Contains(match.Value))
                {
                    add(page.Relative, LineNumber(prose, match.Index), "ID_UNKNOWN_REFERENCE", $"active content references unknown provision '{match.Value}'");
                }
            }
        }
    }

    /// <summary>Replaces the placeholder identifiers of an authoring template with valid ones.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="raw">The raw template text.</param>
    /// <returns>The normalized template text.</returns>
    private string NormalizeTemplate(string relative, string raw) => raw
        .Replace("{litespec/page#state-one-action}", "litespec/core/template#state-one-action", StringComparison.Ordinal)
        .Replace("{litespec/page#use-one-default}", "litespec/core/template#use-one-default", StringComparison.Ordinal)
        .Replace("{litespec/page#state-one-extension-action}", "litespec/ext/template#state-one-extension-action", StringComparison.Ordinal)
        .Replace("{litespec/page#use-one-extension-default}", "litespec/ext/template#use-one-extension-default", StringComparison.Ordinal)
        .Replace("{Topic Title}", "Topic Title", StringComparison.Ordinal)
        .Replace("{Extension Title}", "Extension Title", StringComparison.Ordinal)
        .Replace("{How-To Title}", "How-To Title", StringComparison.Ordinal)
        .Replace("{Tutorial Title}", "Tutorial Title", StringComparison.Ordinal)
        .Replace("{Command Title}", "Command Title", StringComparison.Ordinal)
        .Replace("{Reference Title}", "Reference Title", StringComparison.Ordinal);

    /// <summary>Checks every authoring template against the page contract it seeds.</summary>
    internal void CheckAuthoringTemplates()
    {
        var root = Context.Root;
        var add = Context.Add;
        var templateRoot = Path.Combine(root, "templates", "standard");
        foreach (var file in Walk(templateRoot, candidate => candidate.EndsWith(".md", StringComparison.Ordinal)))
        {
            var relative = Slash(Path.GetRelativePath(root, file));
            var virtualRelative = TemplateVirtualPath.GetValueOrDefault(Path.GetFileName(file), "docs/core/template.md");
            var normalized = NormalizeTemplate(relative, ReadText(file));
            var templateReader = PageReader.Parse(normalized);
            var stripped = string.Join('\n', templateReader.Lines.Select((line, index) =>
                templateReader.FencedLines.Contains(index + 1) || templateReader.GeneratedLines.Contains(index + 1) ? "" : line));
            var nonAscii = NonAsciiOffset(stripped);
            if (nonAscii >= 0)
            {
                add(relative, 1, "PROSE_NON_ASCII", NonAsciiMessage(stripped[nonAscii], "authoring template contains non-ASCII character "));
            }

            Context.Pages.ParsePage(virtualRelative, templateReader, @virtual: true);
        }
    }

    /// <summary>Checks that every link and anchor in the given files resolves.</summary>
    /// <param name="files">The files to check.</param>
    internal void CheckLinks(List<string> files)
    {
        var root = Context.Root;
        var add = Context.Add;
        var readers = new Dictionary<string, PageReader>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            readers[file] = PageReader.Parse(ReadText(file));
        }

        foreach (var file in files)
        {
            var reader = readers[file];
            var relative = Slash(Path.GetRelativePath(root, file));
            foreach (var link in reader.Links)
            {
                var href = link.Target.Trim();
                if (AbsoluteUriPattern().IsMatch(href))
                {
                    continue;
                }

                var parts = href.Split('#');
                var targetPart = parts[0];
                var anchor = parts.Length > 1 ? parts[1] : "";
                if (!TryDecodeURIComponent(targetPart, out var decoded))
                {
                    add(relative, link.Line, "LINK_BROKEN", $"link target '{href}' holds a malformed percent escape");
                    continue;
                }

                var target = targetPart.Length > 0
                    ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, decoded))
                    : file;
                if (targetPart.Length > 0 && !File.Exists(target) && !Directory.Exists(target))
                {
                    add(relative, link.Line, "LINK_BROKEN", $"link target '{href}' does not exist");
                    continue;
                }

                if (anchor.Length > 0 && (File.Exists(target) || Directory.Exists(target)) && target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    if (!readers.TryGetValue(target, out var targetReader))
                    {
                        targetReader = PageReader.Parse(ReadText(target));
                    }

                    if (!targetReader.Anchors.Contains(anchor))
                    {
                        add(relative, link.Line, "ANCHOR_BROKEN", $"anchor '#{anchor}' does not exist in '{Slash(Path.GetRelativePath(root, target))}'");
                    }
                }
            }
        }
    }

    /// <summary>Tries to decode a percent-escaped link target.</summary>
    /// <param name="value">The escaped target.</param>
    /// <param name="decoded">The decoded target.</param>
    /// <returns>True when the target decodes.</returns>
    private bool TryDecodeURIComponent(string value, out string decoded)
    {
        try
        {
            decoded = DecodeURIComponent(value);
            return true;
        }
        catch (InvalidOperationException)
        {
            decoded = value;
            return false;
        }
    }

    /// <summary>Decodes a percent-escaped link target.</summary>
    /// <param name="value">The escaped target.</param>
    /// <returns>The decoded target.</returns>
    private string DecodeURIComponent(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '%' && (index + 2 > value.Length - 1 || !Uri.IsHexDigit(value[index + 1]) || !Uri.IsHexDigit(value[index + 2])))
            {
                throw new InvalidOperationException($"Malformed escape in link target '{value}'.");
            }
        }

        return Uri.UnescapeDataString(value);
    }

    /// <summary>Checks that the consumer template index names every tracked template.</summary>
    internal void CheckTemplateIndex()
    {
        var root = Context.Root;
        var add = Context.Add;
        const string relative = "templates/consumer/README.md";
        var indexFile = JoinRoot(root, relative);
        if (!File.Exists(indexFile))
        {
            return;
        }

        var lines = SplitLines(ReadText(indexFile));
        for (var index = 0; index < lines.Length; index++)
        {
            var row = TemplateRowPattern().Match(lines[index]);
            if (!row.Success)
            {
                continue;
            }

            var template = row.Groups[1].Value;
            var target = Path.Combine(root, "templates", "consumer", template);
            if (!File.Exists(target) && !Directory.Exists(target))
            {
                add(relative, index + 1, "TEMPLATE_INDEX_PATH", $"template '{template}' named in the index does not exist");
            }
        }

        // Every tracked template appears in the index. A shipped template that no row
        // names is a template a consumer cannot find.
        var named = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var row = TemplateRowPattern().Match(line);
            if (row.Success)
            {
                named.Add(row.Groups[1].Value);
            }
        }

        var directory = Path.Combine(root, "templates", "consumer");
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var entry in Directory.GetFileSystemEntries(directory).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal))
        {
            if (entry == "README.md" || named.Contains(entry!))
            {
                continue;
            }

            add(relative, 1, "TEMPLATE_INDEX_PATH", $"template '{entry}' is tracked but no index row names it");
        }
    }

    // Every shipped consumer record template teaches a record shape, so its
    // frontmatter meets the record schema it teaches. A placeholder takes the
    // documented skip form, an angle-bracketed hint or a dunder name, and the
    // check normalizes each one to a valid example before validating, so drift
    // reports while placeholders pass. A template naming a kind instead of a
    // type teaches a non-record shape its own schema owns, so the record check
    // skips it. A template whose type matches no record definition teaches nothing.
    // (litespec/core/authoring#run-repeatable-authoring-checks)
    /// <summary>Checks every consumer template against the record schema it teaches.</summary>
    internal void CheckConsumerTemplates()
    {
        var root = Context.Root;
        var add = Context.Add;
        var directory = Path.Combine(root, "templates", "consumer");
        if (!Directory.Exists(directory))
        {
            return;
        }

        var schemaFile = JoinRoot(root, "schemas/record.schema.json");
        if (!File.Exists(schemaFile))
        {
            return;
        }

        JsonNode? schema;
        try
        {
            schema = JsonNode.Parse(TrimBom(ReadText(schemaFile)));
        }
        catch (Exception)
        {
            return;
        }

        var defs = (schema as JsonObject)?["$defs"] as JsonObject;
        if (defs is null)
        {
            return;
        }

        foreach (var file in Directory.GetFileSystemEntries(directory).OrderBy(name => name, StringComparer.Ordinal))
        {
            if (!file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = Slash(Path.GetRelativePath(root, file));
            var raw = ReadText(file);
            if (raw.Split('\n')[0].TrimEnd('\r') != "---")
            {
                continue;
            }

            if (!Context.Records.TryParseRecordFrontmatter(raw, out var frontmatter, out var error))
            {
                add(relative, 1, "TEMPLATE_SCHEMA", $"record frontmatter is invalid: {error}");
                continue;
            }

            var type = JsonString((frontmatter as JsonObject)?["type"]);
            if (type is null || type.Length == 0)
            {
                // A non-record template carries its own schema's discriminator
                // instead of a record type. The design contract names a kind,
                // and its schema owns it, so the record check does not apply.
                if (JsonString((frontmatter as JsonObject)?["kind"]) is not null)
                {
                    continue;
                }

                add(relative, 1, "TEMPLATE_SCHEMA", "template frontmatter names no record type");
                continue;
            }

            var def = defs[TemplateDefName(type)];
            if (def is null)
            {
                add(relative, 1, "TEMPLATE_SCHEMA", $"template type '{type}' matches no record definition");
                continue;
            }

            var normalized = ((JsonObject)frontmatter!).DeepClone();
            NormalizeTemplateNode(normalized, def, defs, schema!);

            var errors = new List<string>();
            try
            {
                JsonSchema.Validate(normalized, def, schema!, relative, errors);
            }
            catch (Exception exception)
            {
                errors.Add($"{relative}: {exception.Message}");
            }

            foreach (var message in errors)
            {
                var text = message.StartsWith(relative + ": ", StringComparison.Ordinal)
                    ? message[(relative.Length + 2)..]
                    : message;
                add(relative, 1, "TEMPLATE_SCHEMA", text);
            }
        }
    }

    /// <summary>Computes the schema definition name of a record type.</summary>
    /// <param name="type">The record type name.</param>
    /// <returns>The definition name.</returns>
    private string TemplateDefName(string type)
    {
        var parts = type.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "Page";
        }

        var name = parts[0];
        for (var index = 1; index < parts.Length; index++)
        {
            name += char.ToUpperInvariant(parts[index][0]) + parts[index][1..];
        }

        return name + "Page";
    }

    /// <summary>Reports whether a template value is a placeholder.</summary>
    /// <param name="value">The template value.</param>
    /// <returns>True when the value is a placeholder.</returns>
    internal static bool IsTemplatePlaceholder(string? value) =>
        value is not null && (value.Contains('<') || value.Contains("__"));

    // A removable placeholder tells the author the line may go. It drops unless the
    // schema requires it, in which case it fills like any other placeholder.
    /// <summary>Reports whether a template value is a removable placeholder.</summary>
    /// <param name="value">The template value.</param>
    /// <returns>True when the value is a removable placeholder.</returns>
    private static bool IsRemovablePlaceholder(string value) =>
        IsTemplatePlaceholder(value) && (value.Contains("remove this ") || value.Contains("remove the "));

    // A bare removal note carries no placeholder form, so it always drops. It sits
    // beside a worked example in the same array.
    /// <summary>Reports whether a template value is a drop element.</summary>
    /// <param name="value">The template value.</param>
    /// <returns>True when the value always drops.</returns>
    private static bool IsTemplateDropElement(string value) =>
        !IsTemplatePlaceholder(value) && (value.Contains("remove this ") || value.Contains("remove the "));

    // One worked example per schema definition whose values follow a pattern no
    // generic word meets: identifiers, dates, durations, and the fixed vocabularies.
    // (litespec/core/authoring#run-repeatable-authoring-checks)
    /// <summary>Gets the worked example for each schema definition whose values follow a fixed pattern.</summary>
    private static readonly Dictionary<string, string> TemplateExamples = new(StringComparer.Ordinal)
    {
        ["isoDate"] = "2026-11-30",
        ["isoDuration"] = "P30D",
        ["nonEmptyString"] = "sample",
        ["ownerName"] = "sample",
        ["productId"] = "product/sample",
        ["goalId"] = "goal/sample",
        ["constraintId"] = "constraint/sample",
        ["researchId"] = "research/sample",
        ["decisionId"] = "decision/0001-sample",
        ["problemId"] = "problem/sample",
        ["featureId"] = "feature/sample",
        ["topicId"] = "topic/sample",
        ["requirementId"] = "requirement/sample.entry",
        ["actorId"] = "actor/sample",
        ["roleId"] = "role/sample",
        ["permissionId"] = "permission/sample",
        ["journeyId"] = "journey/sample",
        ["journeyStepId"] = "journey/sample#s1",
        ["moduleId"] = "module/sample",
        ["aggregateId"] = "aggregate/sample.entry",
        ["ruleId"] = "rule/sample.entry",
        ["useCaseId"] = "use-case/sample.entry",
        ["resultId"] = "result/sample.entry",
        ["derivedResultId"] = "result/sample.forbidden",
        ["scenarioId"] = "scenario/sample.entry",
        ["eventId"] = "event/sample.entry",
        ["entryPointId"] = "entry-point/sample",
        ["screenId"] = "screen/sample.entry",
        ["externalSystemId"] = "external-system/sample",
        ["externalOperationRef"] = "external-system/sample#operation",
        ["reactionId"] = "reaction/sample.entry",
        ["jobId"] = "job/sample.entry",
        ["notificationId"] = "notification/sample.entry",
        ["settingId"] = "setting/sample.entry",
        ["integrationEventId"] = "integration-event/sample",
        ["runbookId"] = "runbook/sample",
        ["exceptionId"] = "exception/sample",
        ["termId"] = "term/sample",
        ["refusalCode"] = "sample.entry",
        ["standardsProvisionId"] = "litespec/core/topic#keep-the-topic-bounded",
        ["genericRecordId"] = "sample/entry",
        ["grantValue"] = "permission/sample",
        ["configurationKey"] = "Sample:Key",
    };

    // The fallback pool for an inline pattern the map names no definition for. The
    // first candidate the pattern accepts wins; a pattern no candidate meets keeps
    // the generic word and reports.
    /// <summary>Gets the fallback example pool for an inline pattern.</summary>
    private static readonly string[] TemplatePatternPool =
        ["sample", .. TemplateExamples.Values, "s1", "P30D", "nuget:Sample", "Sample:Key"];

    /// <summary>Finds a worked example that a pattern accepts.</summary>
    /// <param name="pattern">The pattern to satisfy.</param>
    /// <returns>The first accepted example.</returns>
    private string TemplatePatternExample(string pattern)
    {
        foreach (var candidate in TemplatePatternPool)
        {
            try
            {
                if (Regex.IsMatch(candidate, pattern))
                {
                    return candidate;
                }
            }
            catch (Exception)
            {
                break;
            }
        }

        return "sample";
    }

    /// <summary>Resolves a template schema reference to its definition.</summary>
    /// <param name="schema">The schema to resolve.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>The resolved schema and its definition name.</returns>
    private (JsonObject? Resolved, string? DefName) ResolveTemplateRef(JsonNode? schema, JsonObject defs)
    {
        const string prefix = "#/$defs/";
        var current = schema;
        string? defName = null;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (current is JsonObject obj
            && JsonString(obj["$ref"]) is string reference
            && reference.StartsWith(prefix, StringComparison.Ordinal)
            && visited.Add(reference))
        {
            defName = reference[prefix.Length..];
            current = defs[defName];
        }

        return (current as JsonObject, defName);
    }

    /// <summary>Normalizes one template value against its schema.</summary>
    /// <param name="value">The template value.</param>
    /// <param name="schema">The value schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    private void NormalizeTemplateNode(JsonNode? value, JsonNode? schema, JsonObject defs, JsonNode root)
    {
        var (resolved, _) = ResolveTemplateRef(schema, defs);
        if (resolved is null)
        {
            return;
        }

        if (value is JsonObject obj)
        {
            NormalizeTemplateObject(obj, resolved, defs, root);
        }
        else if (value is JsonArray array)
        {
            NormalizeTemplateArray(array, resolved, defs, root);
        }
    }

    /// <summary>Normalizes one template object against its schema.</summary>
    /// <param name="obj">The template object.</param>
    /// <param name="schema">The object schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    private void NormalizeTemplateObject(JsonObject obj, JsonObject schema, JsonObject defs, JsonNode root)
    {
        // An entry reads inside one branch of a shared list, which carries no shape
        // of its own, so the branch follows the entry's own type. A schema with its
        // own properties keeps them: its branches only constrain.
        if (schema["properties"] is null
            && (schema["oneOf"] as JsonArray ?? schema["anyOf"] as JsonArray) is JsonArray branches
            && branches.Count > 0)
        {
            var selected = SelectTemplateBranch(obj, branches, defs);
            var (resolved, _) = ResolveTemplateRef(selected, defs);
            if (resolved is not null)
            {
                schema = resolved;
            }
        }

        var properties = schema["properties"] as JsonObject;
        var additional = schema["additionalProperties"] as JsonObject;

        // Children normalize before their parent's enum and removal choices, so a
        // conditional rule reads the values it judges rather than placeholders.
        foreach (var key in obj.Select(entry => entry.Key).ToList())
        {
            if (obj[key] is not JsonObject && obj[key] is not JsonArray)
            {
                continue;
            }

            var propSchema = properties?[key];
            if (propSchema is null)
            {
                if (additional is not null && obj[key] is JsonObject)
                {
                    NormalizeTemplateNode(obj[key], additional, defs, root);
                }

                continue;
            }

            NormalizeTemplateNode(obj[key], propSchema, defs, root);
        }

        // An enum placeholder takes the first value under which the object's
        // conditional rules accept the object, so a template that teaches one shape
        // reads under the value that shape needs.
        var enums = new List<string>();
        foreach (var key in obj.Select(entry => entry.Key).ToList())
        {
            if (obj[key] is not JsonValue || JsonString(obj[key]) is not string text || !IsTemplatePlaceholder(text))
            {
                continue;
            }

            var (resolved, _) = ResolveTemplateRef(properties?[key], defs);
            if (resolved is null)
            {
                continue;
            }

            if (resolved["enum"] as JsonArray is { Count: > 0 })
            {
                enums.Add(key);
            }
            else if (!IsRemovablePlaceholder(text))
            {
                obj[key] = SubstituteTemplateScalar(resolved, defs, root);
            }
        }

        foreach (var key in enums)
        {
            var (resolved, _) = ResolveTemplateRef(properties?[key], defs);
            var entries = (resolved?["enum"] as JsonArray)?.Where(entry => entry is not null).ToList();
            if (entries is null || entries.Count == 0)
            {
                continue;
            }

            var picked = entries[0]!.DeepClone();
            foreach (var candidate in entries)
            {
                obj[key] = candidate!.DeepClone();
                if (TemplateAcceptsConditionals(obj, key, schema, root))
                {
                    picked = candidate.DeepClone();
                    break;
                }
            }

            obj[key] = picked;
        }

        foreach (var key in obj.Select(entry => entry.Key).ToList())
        {
            if (obj[key] is not JsonValue || JsonString(obj[key]) is not string text || !IsRemovablePlaceholder(text))
            {
                continue;
            }

            var propSchema = properties?[key];
            if (propSchema is null)
            {
                continue;
            }

            var (resolved, _) = ResolveTemplateRef(propSchema, defs);
            if (resolved?["enum"] as JsonArray is { Count: > 0 })
            {
                continue;
            }

            if (IsTemplateRequired(schema, key, obj, root))
            {
                obj[key] = SubstituteTemplateScalar(resolved, defs, root);
            }
            else
            {
                obj.Remove(key);
            }
        }
    }

    /// <summary>Normalizes one template array against its schema.</summary>
    /// <param name="array">The template array.</param>
    /// <param name="schema">The array schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    private void NormalizeTemplateArray(JsonArray array, JsonObject schema, JsonObject defs, JsonNode root)
    {
        var items = schema["items"];
        if (items is null)
        {
            return;
        }

        var minItems = schema["minItems"] is JsonValue count && count.TryGetValue<int>(out var minimum) ? minimum : 0;
        var dropped = new List<int>();
        for (var index = array.Count - 1; index >= 0; index--)
        {
            if (array[index] is JsonValue && JsonString(array[index]) is string text)
            {
                if (IsTemplateDropElement(text))
                {
                    array.RemoveAt(index);
                }
                else if (IsTemplatePlaceholder(text))
                {
                    if (IsRemovablePlaceholder(text))
                    {
                        dropped.Add(index);
                        array.RemoveAt(index);
                    }
                    else
                    {
                        array[index] = SubstituteTemplateScalar(items, defs, root);
                    }
                }
            }
            else
            {
                NormalizeTemplateNode(array[index], items, defs, root);
            }
        }

        // A dropped element returns when the array would fall below its minimum, so
        // an optional example still teaches the shape the schema insists on.
        dropped.Reverse();
        foreach (var index in dropped)
        {
            if (array.Count >= minItems)
            {
                break;
            }

            array.Insert(Math.Min(index, array.Count), SubstituteTemplateScalar(items, defs, root));
        }
    }

    /// <summary>Substitutes a valid scalar example for a template placeholder.</summary>
    /// <param name="schema">The scalar schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The substitute value.</returns>
    private JsonNode SubstituteTemplateScalar(JsonNode? schema, JsonObject defs, JsonNode root) =>
        SubstituteTemplateScalar(schema, defs, root, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>Substitutes a valid scalar example for a template placeholder.</summary>
    /// <param name="schema">The scalar schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>The substitute value.</returns>
    private JsonNode SubstituteTemplateScalar(JsonNode? schema, JsonObject defs, JsonNode root, HashSet<string> visited)
    {
        const string prefix = "#/$defs/";
        var current = schema;
        string? defName = null;
        while (current is JsonObject obj
            && JsonString(obj["$ref"]) is string reference
            && reference.StartsWith(prefix, StringComparison.Ordinal)
            && visited.Add(reference))
        {
            defName = reference[prefix.Length..];
            current = defs[defName];
        }

        if (current is not JsonObject resolved)
        {
            return JsonValue.Create("sample")!;
        }

        if (resolved.TryGetPropertyValue("const", out var constant) && constant is not null)
        {
            return constant.DeepClone();
        }

        if (resolved["enum"] as JsonArray is { Count: > 0 } entries && entries[0] is not null)
        {
            return entries[0]!.DeepClone();
        }

        if ((resolved["oneOf"] as JsonArray ?? resolved["anyOf"] as JsonArray) is JsonArray branches && branches.Count > 0)
        {
            return SubstituteTemplateScalar(SelectTemplateStringBranch(branches, defs), defs, root, visited);
        }

        var type = JsonString(resolved["type"]);
        if (type is "integer" or "number")
        {
            var value = 1;
            if (resolved["minimum"] is JsonValue floor && floor.TryGetValue<int>(out var lowest))
            {
                value = lowest;
            }

            if (resolved["maximum"] is JsonValue ceiling && ceiling.TryGetValue<int>(out var highest) && value > highest)
            {
                value = highest;
            }

            // The shared schema check reads every number as a double, and an
            // int-backed value reports "expected integer" for a value that is
            // one, so the substitute is created in the form the check reads.
            return JsonValue.Create((double)value)!;
        }

        if (type == "boolean")
        {
            return JsonValue.Create(true)!;
        }

        if (type == "array")
        {
            var items = resolved["items"];
            var least = resolved["minItems"] is JsonValue count && count.TryGetValue<int>(out var fewest) ? Math.Max(fewest, 1) : 1;
            var filled = new JsonArray();
            for (var index = 0; index < least; index++)
            {
                filled.Add(SubstituteTemplateScalar(items, defs, root, visited));
            }

            return filled;
        }

        if (type == "object")
        {
            return new JsonObject();
        }

        if (defName is not null && TemplateExamples.TryGetValue(defName, out var example))
        {
            return JsonValue.Create(example)!;
        }

        if (JsonString(resolved["pattern"]) is string pattern)
        {
            return JsonValue.Create(TemplatePatternExample(pattern))!;
        }

        return JsonValue.Create("sample")!;
    }

    // The branch an object entry reads under: its own type const first, then the
    // first object shape whose required keys the value carries, then the first
    // object shape. A value that fits none still normalizes under the first branch
    // and reports.
    /// <summary>Selects the branch a template object entry reads under.</summary>
    /// <param name="obj">The template object.</param>
    /// <param name="branches">The candidate branches.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>The selected branch.</returns>
    private JsonNode? SelectTemplateBranch(JsonObject obj, JsonArray branches, JsonObject defs)
    {
        var type = JsonString(obj["type"]);
        if (type is not null)
        {
            foreach (var branch in branches)
            {
                var (resolved, _) = ResolveTemplateRef(branch, defs);
                if ((resolved?["properties"] as JsonObject)?["type"] is JsonObject typeSchema
                    && JsonString(typeSchema["const"]) == type)
                {
                    return branch;
                }
            }
        }

        JsonNode? fallback = null;
        foreach (var branch in branches)
        {
            var (resolved, _) = ResolveTemplateRef(branch, defs);
            if (resolved is null || TemplateBranchIsScalar(resolved))
            {
                continue;
            }

            fallback ??= branch;
            var required = resolved["required"] as JsonArray;
            if (required is null || required.All(node => JsonString(node) is string name && obj.ContainsKey(name)))
            {
                return branch;
            }
        }

        return fallback ?? branches[0];
    }

    /// <summary>Reports whether a template branch schema is scalar.</summary>
    /// <param name="schema">The branch schema.</param>
    /// <returns>True when the branch is scalar.</returns>
    private bool TemplateBranchIsScalar(JsonObject schema)
    {
        if (schema.TryGetPropertyValue("const", out _)
            || schema["enum"] is JsonArray
            || JsonString(schema["pattern"]) is not null)
        {
            return true;
        }

        return JsonString(schema["type"]) is string type && type is "string" or "integer" or "number" or "boolean";
    }

    /// <summary>Selects the string branch of a template scalar schema.</summary>
    /// <param name="branches">The candidate branches.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>The selected branch.</returns>
    private JsonNode? SelectTemplateStringBranch(JsonArray branches, JsonObject defs)
    {
        foreach (var branch in branches)
        {
            var (resolved, _) = ResolveTemplateRef(branch, defs);
            if (resolved is null)
            {
                continue;
            }

            if (resolved["enum"] as JsonArray is { Count: > 0 }
                || resolved.TryGetPropertyValue("const", out _)
                || JsonString(resolved["pattern"]) is not null
                || JsonString(resolved["type"]) == "string")
            {
                return branch;
            }
        }

        foreach (var branch in branches)
        {
            var (resolved, _) = ResolveTemplateRef(branch, defs);
            if (resolved is null || TemplateBranchIsScalar(resolved))
            {
                continue;
            }

            return branch;
        }

        return branches[0];
    }

    /// <summary>Reports whether a template object satisfies the conditional rules that mention one property.</summary>
    /// <param name="obj">The template object.</param>
    /// <param name="prop">The property the rules must mention.</param>
    /// <param name="schema">The object schema.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>True when the conditional rules accept the object.</returns>
    private bool TemplateAcceptsConditionals(JsonObject obj, string prop, JsonObject schema, JsonNode root)
    {
        if (schema["allOf"] is not JsonArray conditional)
        {
            return true;
        }

        foreach (var branch in conditional)
        {
            if (branch is not JsonObject rule || rule["if"] is not JsonObject condition)
            {
                continue;
            }

            var mentions = (condition["required"] as JsonArray)?.Any(node => JsonString(node) == prop) == true
                || (condition["properties"] as JsonObject)?.ContainsKey(prop) == true;
            if (!mentions)
            {
                continue;
            }

            var matched = TemplateMatches(obj, condition, root);
            if (matched && rule["then"] is JsonObject then && !TemplateMatches(obj, then, root))
            {
                return false;
            }

            if (!matched && rule["else"] is JsonObject otherwise && !TemplateMatches(obj, otherwise, root))
            {
                return false;
            }
        }

        return true;
    }

    // A removable placeholder fills when the schema insists: a direct requirement,
    // a conditional requirement whose condition holds, or an else requirement whose
    // condition does not.
    /// <summary>Reports whether the schema requires one template property.</summary>
    /// <param name="schema">The object schema.</param>
    /// <param name="prop">The property name.</param>
    /// <param name="obj">The template object.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>True when the property is required.</returns>
    private bool IsTemplateRequired(JsonObject schema, string prop, JsonObject obj, JsonNode root)
    {
        if ((schema["required"] as JsonArray)?.Any(node => JsonString(node) == prop) == true)
        {
            return true;
        }

        if (schema["allOf"] is not JsonArray conditional)
        {
            return false;
        }

        foreach (var branch in conditional)
        {
            if (branch is not JsonObject rule || rule["if"] is not JsonObject condition)
            {
                continue;
            }

            var matched = TemplateMatches(obj, condition, root);
            var required = matched
                ? (rule["then"] as JsonObject)?["required"] as JsonArray
                : (rule["else"] as JsonObject)?["required"] as JsonArray;
            if (required?.Any(node => JsonString(node) == prop) == true)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports whether a template value matches a schema.</summary>
    /// <param name="value">The template value.</param>
    /// <param name="schema">The schema.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>True when the value matches.</returns>
    private bool TemplateMatches(JsonNode? value, JsonNode? schema, JsonNode root)
    {
        var errors = new List<string>();
        try
        {
            JsonSchema.Validate(value, schema, root, "template", errors);
        }
        catch (Exception)
        {
            return false;
        }

        return errors.Count == 0;
    }

    /// <summary>Checks that the documentation root routes the reader before its navigation groups.</summary>
    internal void CheckRootIndexRouting()
    {
        var root = Context.Root;
        var add = Context.Add;
        const string relative = "docs/README.md";
        var file = JoinRoot(root, relative);
        if (!File.Exists(file))
        {
            return;
        }

        var reader = PageReader.Parse(ReadText(file));
        var routingLine = -1;
        var firstGroupLine = -1;
        var h2s = reader.Headings.Where(heading => heading.Level == 2).ToList();
        foreach (var heading in h2s)
        {
            if (heading.Text.Trim() == RoutingHeading && routingLine < 0)
            {
                routingLine = heading.Line;
                break;
            }
        }

        // A navigation group is a section whose body lists links. Intent introduces
        // the page and names no destination, so a link inside it starts no group.
        foreach (var bulletLine in reader.NavBulletLines)
        {
            string? currentHeading = null;
            foreach (var heading in h2s)
            {
                if (heading.Line < bulletLine)
                {
                    currentHeading = heading.Text.Trim();
                }
                else
                {
                    break;
                }
            }

            if (currentHeading is not null && currentHeading.Length > 0 && currentHeading != "Intent" && currentHeading != RoutingHeading)
            {
                firstGroupLine = bulletLine;
                break;
            }
        }

        if (routingLine < 0)
        {
            add(relative, 1, "INDEX_MISSING_ROUTING", $"documentation root index has no '{RoutingHeading}' section, so a reader cannot tell which page class answers their question");
            return;
        }

        if (firstGroupLine >= 0 && routingLine > firstGroupLine)
        {
            add(relative, routingLine, "INDEX_MISSING_ROUTING", $"'{RoutingHeading}' appears after the first navigation group at line {firstGroupLine}");
        }
    }

    /// <summary>Gets the pattern that matches a lowercase kebab path segment.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")] private static partial Regex KebabSegmentPattern();

    /// <summary>Gets the pattern that matches an absolute link target.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^(?:https?:|mailto:)")] private static partial Regex AbsoluteUriPattern();

    /// <summary>Gets the pattern that matches a template index row.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\|\s*`([^`]+)`\s*\|\s*`([^`]+)`\s*\|")] private static partial Regex TemplateRowPattern();

    /// <summary>Gets the pattern that matches a release tag.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^v[0-9]+(?:\.[0-9]+)*$")] private static partial Regex ReleaseTagPattern();

    // A released provision ID may not disappear in silence: every ID from the
    // highest release tag is either active now or listed under Removed and
    // Renamed Provisions in the pending changelog section, with the ID that
    // carries its rule or as removed. Without git history there is nothing
    // released to compare against, so that direction stays quiet. The table
    // itself is checked in both directions in every run: a listed ID must be
    // gone from the current pages, and a named replacement must be present.
    /// <summary>Checks that every removed provision ID is recorded in the changelog.</summary>
    /// <param name="activeIds">The active provision identifiers.</param>
    /// <param name="releasedIds">The released provision identifiers, or null to read them from git.</param>
    internal void CheckRemovedProvisions(HashSet<string> activeIds, HashSet<string>? releasedIds)
    {
        var root = Context.Root;
        var add = Context.Add;
        var (rows, line) = ParseChangelogRenameRows();
        foreach (var row in rows)
        {
            if (activeIds.Contains(row.Removed))
            {
                add("CHANGELOG.md", row.Line, "ID_REMOVED_STILL_PRESENT", $"provision ID '{row.Removed}' is listed under Removed and Renamed Provisions but still exists in the current pages");
            }

            if (row.Replacement is string replacement && !activeIds.Contains(replacement))
            {
                add("CHANGELOG.md", row.Line, "ID_RENAME_TARGET_MISSING", $"replacement provision ID '{replacement}' for '{row.Removed}' does not exist in the current pages");
            }
        }

        var released = releasedIds ?? TryCollectReleasedIds();
        if (released is null)
        {
            return;
        }

        var recorded = new HashSet<string>(rows.Select(row => row.Removed), StringComparer.Ordinal);
        foreach (var id in released.OrderBy(candidate => candidate, StringComparer.Ordinal))
        {
            if (!activeIds.Contains(id) && !recorded.Contains(id))
            {
                add("CHANGELOG.md", line, "ID_REMOVED_UNRECORDED", $"provision ID '{id}' from the latest release is gone and not listed under Removed and Renamed Provisions");
            }
        }
    }

    /// <summary>Collects the provision identifiers of the highest release tag, or null when there is none.</summary>
    /// <returns>The released provision identifiers, or null.</returns>
    private HashSet<string>? TryCollectReleasedIds()
    {
        var root = Context.Root;
        if (!Path.Exists(Path.Combine(root, ".git")))
        {
            return null;
        }

        if (TryHighestReleaseTag() is not string tag)
        {
            return null;
        }

        if (!TryRunGit(["ls-tree", "-r", "--name-only", tag, "--", "docs"], out var listing))
        {
            return null;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var strayContext = new ValidationContext(root, (_, _, _, _) => { });
        foreach (var path in listing.Split('\n').Select(candidate => candidate.Trim()).Where(candidate => candidate.Length > 0))
        {
            if (!path.EndsWith(".md", StringComparison.Ordinal) || !Provisions.IsProvisionPage(path))
            {
                continue;
            }

            if (!TryRunGit(["show", $"{tag}:{path}"], out var content))
            {
                return null;
            }

            var reader = PageReader.Parse(content);
            foreach (var provision in strayContext.Pages.ParsePage(path, reader).Provisions)
            {
                ids.Add(provision.Id);
            }
        }

        return ids;
    }

    /// <summary>Finds the highest release tag, or null when there is none.</summary>
    /// <returns>The highest release tag, or null.</returns>
    private string? TryHighestReleaseTag()
    {
        if (!TryRunGit(["tag", "--list", "v*"], out var listing))
        {
            return null;
        }

        string? best = null;
        var bestParts = new List<int>();
        foreach (var tag in listing.Split('\n').Select(candidate => candidate.Trim()).Where(candidate => candidate.Length > 0))
        {
            if (!ReleaseTagPattern().IsMatch(tag))
            {
                continue;
            }

            var current = new List<int>();
            var valid = true;
            foreach (var part in tag[1..].Split('.'))
            {
                if (!int.TryParse(part, out var number))
                {
                    valid = false;
                    break;
                }

                current.Add(number);
            }

            if (!valid)
            {
                continue;
            }

            if (best is null || CompareVersions(current, bestParts) > 0)
            {
                best = tag;
                bestParts = current;
            }
        }

        return best;
    }

    /// <summary>Compares two version part lists.</summary>
    /// <param name="left">The left version parts.</param>
    /// <param name="right">The right version parts.</param>
    /// <returns>The comparison result.</returns>
    private int CompareVersions(List<int> left, List<int> right)
    {
        for (var index = 0; index < Math.Max(left.Count, right.Count); index++)
        {
            var difference = (index < left.Count ? left[index] : 0) - (index < right.Count ? right[index] : 0);
            if (difference != 0)
            {
                return difference;
            }
        }

        return 0;
    }

    /// <summary>Runs a git command in the repository root.</summary>
    /// <param name="args">The git arguments.</param>
    /// <param name="output">The command output.</param>
    /// <returns>True when the command succeeds.</returns>
    private bool TryRunGit(string[] args, out string output)
    {
        var root = Context.Root;
        output = "";
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add("-C");
            start.ArgumentList.Add(root);
            foreach (var arg in args)
            {
                start.ArgumentList.Add(arg);
            }

            using var process = Process.Start(start);
            if (process is null)
            {
                return false;
            }

            output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(30000))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // The rows of the Removed and Renamed Provisions table in the current
    // release section (the first ## section): each row's removed or renamed ID,
    // its replacement ID or null for `removed`, and its 1-based line. The
    // heading line stands in for the table when a caller reports on the table
    // as a whole.
    /// <summary>Parses the Removed and Renamed Provisions rows of the changelog.</summary>
    /// <returns>The rename rows and the heading line.</returns>
    private (List<(string Removed, string? Replacement, int Line)> Rows, int Line) ParseChangelogRenameRows()
    {
        var root = Context.Root;
        var rows = new List<(string Removed, string? Replacement, int Line)>();
        var file = JoinRoot(root, "CHANGELOG.md");
        if (!File.Exists(file))
        {
            return (rows, 1);
        }

        var lines = SplitLines(ReadText(file));
        var section = 0;
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith("## ", StringComparison.Ordinal))
            {
                section = index + 1;
                break;
            }
        }

        if (section == 0)
        {
            return (rows, 1);
        }

        var heading = 0;
        for (var index = section; index < lines.Length; index++)
        {
            if (lines[index].StartsWith("## ", StringComparison.Ordinal))
            {
                break;
            }

            if (lines[index].Trim() == "### Removed and Renamed Provisions")
            {
                heading = index + 1;
                break;
            }
        }

        if (heading == 0)
        {
            return (rows, section);
        }

        for (var index = heading; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("### ", StringComparison.Ordinal))
            {
                break;
            }

            var codes = InlineCodes(line);
            if (codes.Count == 0 || !Provisions.IsProvisionId(codes[0]))
            {
                continue;
            }

            var replacement = codes.Count > 1 && Provisions.IsProvisionId(codes[1]) ? codes[1] : null;
            rows.Add((codes[0], replacement, index + 1));
        }

        return (rows, heading);
    }
}
