using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Maintain.Validate.PageText;
using static LiteSpec.Maintain.Validate.Repository.ManifestValidator;
using static LiteSpec.Maintain.Validate.RepositoryWalk;

namespace LiteSpec.Maintain.Validate.Repository;

/// <summary>The template checks: every authoring template parses as the page it seeds, the template index lists every template, and every consumer template, its placeholders filled, is a valid record of its type.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class TemplateValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TemplateValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public TemplateValidator(ValidationContext context)
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
        ["stack.md"] = "docs/backend/template/stack.md",
        ["pattern.md"] = "docs/frontend/patterns/template.md",
        ["explanation.md"] = "docs/learn/template.md",
        ["index.md"] = "docs/template/README.md",
    };

    /// <summary>Gets the page paths reserved as authoring template stems.</summary>
    internal readonly HashSet<string> ReservedTemplateStems;

    // The container page styles live in record files beside the record types: they
    // hold entries, not documents.
    /// <summary>Gets the container page styles held in record files.</summary>
    private static readonly HashSet<string> ContainerPageStyles = new(
        ["permission-list", "entry-point-list", "setting-set", "rule-set", "retention-schedule"],
        StringComparer.Ordinal);

    /// <summary>Replaces the placeholder identifiers of an authoring template with valid ones.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="raw">The raw template text.</param>
    /// <returns>The normalized template text.</returns>
    private static string NormalizeTemplate(string relative, string raw) => raw
        .Replace("{litespec/page#state-one-action}", "litespec/core/template#state-one-action", StringComparison.Ordinal)
        .Replace("{litespec/page#use-one-default}", "litespec/core/template#use-one-default", StringComparison.Ordinal)
        .Replace("{litespec/page#state-one-extension-action}", "litespec/ext/template#state-one-extension-action", StringComparison.Ordinal)
        .Replace("{litespec/page#use-one-extension-default}", "litespec/ext/template#use-one-extension-default", StringComparison.Ordinal)
        .Replace("{Topic Title}", "Topic Title", StringComparison.Ordinal)
        .Replace("{Extension Title}", "Extension Title", StringComparison.Ordinal)
        .Replace("{How-To Title}", "How-To Title", StringComparison.Ordinal)
        .Replace("{Tutorial Title}", "Tutorial Title", StringComparison.Ordinal)
        .Replace("{Command Title}", "Command Title", StringComparison.Ordinal)
        .Replace("{Reference Title}", "Reference Title", StringComparison.Ordinal)
        .Replace("{Stack Title}", "Stack Title", StringComparison.Ordinal)
        .Replace("{Pattern Title}", "Pattern Title", StringComparison.Ordinal)
        .Replace("{Explanation Title}", "Explanation Title", StringComparison.Ordinal)
        .Replace("{litespec/page#apply-one-stack-obligation}", "litespec/backend/template/stack#apply-one-stack-obligation", StringComparison.Ordinal)
        .Replace("{litespec/page#use-one-stack-default}", "litespec/backend/template/stack#use-one-stack-default", StringComparison.Ordinal)
        .Replace("{litespec/page#record-header}", "litespec/frontend/patterns/template#record-header", StringComparison.Ordinal);

    /// <summary>Checks every authoring template against the page contract it seeds.</summary>
    internal void CheckAuthoringTemplates()
    {
        var root = Context.Root;
        var add = Context.Add;
        var templateRoot = Path.Combine(root, "templates", "standard");
        foreach (var file in Walk(templateRoot, candidate => candidate.EndsWith(".md", StringComparison.Ordinal)))
        {
            var relative = PathText.Relative(root, file);
            var virtualRelative = TemplateVirtualPath.GetValueOrDefault(Path.GetFileName(file), "docs/core/template.md");
            var normalized = NormalizeTemplate(relative, FileText.ReadText(file));
            var templateReader = PageReader.Parse(normalized);
            var stripped = string.Join('\n', templateReader.Lines.Select((line, index) =>
                templateReader.FencedLines.Contains(index + 1) || templateReader.GeneratedLines.Contains(index + 1) ? "" : line));
            var nonAscii = NonAsciiOffset(stripped);
            if (nonAscii >= 0)
            {
                add(relative, 1, MaintainerDiagnostics.Codes.ProseNonAscii, NonAsciiMessage(stripped[nonAscii], "authoring template contains non-ASCII character "));
            }

            Context.Pages.ParsePage(virtualRelative, templateReader, @virtual: true);
        }
    }

    // A template cites the provisions it teaches or follows, so every
    // litespec/... citation in templates/ resolves to a provision in the
    // index. A {placeholder} is a shape, not a citation.
    /// <summary>Checks that every provision citation in templates resolves to an active provision.</summary>
    /// <param name="activeIds">The active provision identifiers.</param>
    internal void CheckProvisionCitations(HashSet<string> activeIds)
    {
        var root = Context.Root;
        var add = Context.Add;
        var directory = Path.Combine(root, "templates");
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Walk(directory, _ => true))
        {
            var relative = PathText.Relative(root, file);
            var text = FileText.ReadText(file);
            var stripped = PlaceholderPattern().Replace(text, match => BlankPlaceholder(match));
            foreach (Match match in CitationPattern.Matches(stripped))
            {
                if (!activeIds.Contains(match.Value))
                {
                    add(relative, LineNumber(stripped, match.Index), MaintainerDiagnostics.Codes.TemplateCitationUnknown, $"template references unknown provision '{match.Value}'");
                }
            }
        }
    }

    /// <summary>Blanks a placeholder match, keeping line breaks so reported lines stay exact.</summary>
    /// <param name="match">The placeholder match.</param>
    /// <returns>The blanked text.</returns>
    private static string BlankPlaceholder(Match match)
    {
        var chars = match.Value.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            if (chars[index] is not '\r' and not '\n')
            {
                chars[index] = ' ';
            }
        }

        return new string(chars);
    }

    // A record template is a copy of one Claimwick record, and its identifier names
    // that record. A template without an identifier is not a record (the glossary, the
    // project file and the UI catalogs), so the copy check does not apply to it.
    // (litespec/core/authoring#copy-each-template-from-the-example)
    /// <summary>Checks that each record template equals the Claimwick example record its identifier names.</summary>
    internal void CheckTemplateSources()
    {
        var root = Context.Root;
        var add = Context.Add;
        var directory = Path.Combine(root, "templates", "consumer");
        if (!Directory.Exists(directory))
        {
            return;
        }

        var examples = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        foreach (var file in Walk(Path.Combine(root, "examples", "claimwick", ".litespec"), candidate => candidate.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)))
        {
            if (RecordsValidator.TryParseRecordFile(FileText.ReadText(file), out var node, out _)
                && node is JsonObject
                && JsonString(node["id"]) is { } id)
            {
                examples.TryAdd(id, node);
            }
        }

        foreach (var file in Directory.GetFiles(directory, "*.yaml").OrderBy(name => name, StringComparer.Ordinal))
        {
            var relative = PathText.Relative(root, file);
            if (!RecordsValidator.TryParseRecordFile(FileText.ReadText(file), out var template, out _)
                || template is not JsonObject
                || JsonString(template["id"]) is not { } id)
            {
                continue;
            }

            if (!examples.TryGetValue(id, out var source))
            {
                add(relative, 1, MaintainerDiagnostics.Codes.TemplateSourceMissing, $"template names record '{id}', which the Claimwick example lacks");
            }
            else if (!JsonNode.DeepEquals(template, source))
            {
                add(relative, 1, MaintainerDiagnostics.Codes.TemplateSourceDiffers, $"template differs from example record '{id}'; copy the record again");
            }
        }
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

        var lines = SplitLines(FileText.ReadText(indexFile));
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
                add(relative, index + 1, MaintainerDiagnostics.Codes.TemplateIndexPath, $"template '{template}' named in the index does not exist");
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

            add(relative, 1, MaintainerDiagnostics.Codes.TemplateIndexPath, $"template '{entry}' is tracked but no index row names it");
        }
    }

    // Every shipped consumer record template teaches a record shape, so its record
    // file meets the record schema it teaches. A placeholder takes the documented
    // skip form, an angle-bracketed hint or a dunder name, and the check normalizes
    // each one to a valid example before validating, so drift reports while
    // placeholders pass. A document template teaches a document shape the same way
    // through its frontmatter. A template naming a kind instead of a type teaches a
    // non-record shape its own schema owns, so the check skips it. A template whose
    // type matches no definition teaches nothing.
    // (litespec/core/authoring#run-repeatable-authoring-checks)
    /// <summary>Checks every consumer template against the schema it teaches.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CheckConsumerTemplates reports a record schema that cannot be read or parsed, or a validation failure over an arbitrary template, as collected TEMPLATE_SCHEMA errors instead of crashing the validation run.")]
    internal void CheckConsumerTemplates()
    {
        var root = Context.Root;
        var add = Context.Add;
        var directory = Path.Combine(root, "templates", "consumer");
        if (!Directory.Exists(directory))
        {
            return;
        }

        var schemaFile = JoinRoot(root, "schemas/record.schema.yaml");
        if (!File.Exists(schemaFile))
        {
            return;
        }

        JsonNode? schema;
        try
        {
            schema = RecordText.Read(schemaFile);
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
            var relative = PathText.Relative(root, file);
            JsonObject? template = null;
            string? type = null;

            if (file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            {
                // The term entries live beside the record templates but follow the
                // glossary schema, so the record check does not apply.
                if (string.Equals(Path.GetFileName(file), "glossary.yaml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!RecordsValidator.TryParseRecordFile(FileText.ReadText(file), out var record, out var error))
                {
                    add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, $"record file is invalid: {error}");
                    continue;
                }

                template = record as JsonObject;
                type = JsonString(template?["type"]);
                if (type is null || type.Length == 0)
                {
                    // A LiteSpec file that is not a record, such as the project file or the
                    // UI catalog, carries schemaVersion and its own schema, which the schema
                    // checks read, so the record check does not apply.
                    if (template?["schemaVersion"] is not null)
                    {
                        continue;
                    }

                    add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, "template names no record type");
                    continue;
                }

                // A document style template is a Markdown file beside the record
                // templates; a .yaml file teaches a record or a container page.
                if (!RecordTypes.All.Contains(type) && !ContainerPageStyles.Contains(type))
                {
                    if (RecordTypes.PageStyles.Contains(type))
                    {
                        add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, $"type '{type}' is a document style; its template lives in '{type}.md', not in this YAML file");
                    }
                    else
                    {
                        add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, $"template type '{type}' matches no record definition");
                    }

                    continue;
                }
            }
            else if (file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                // An explanation template carries no frontmatter; a document
                // template carries its document style between the fences.
                var raw = FileText.ReadText(file);
                if (raw.Split('\n')[0].TrimEnd('\r') != "---")
                {
                    continue;
                }

                if (!TryParseDocumentFrontmatter(raw, out var frontmatter, out var error))
                {
                    add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, $"document frontmatter is invalid: {error}");
                    continue;
                }

                template = frontmatter as JsonObject;
                type = JsonString(template?["type"]);
                if (type is null || type.Length == 0)
                {
                    // A LiteSpec document that is not a page, such as the design contract,
                    // carries schemaVersion and its own schema, so the check does not apply.
                    if (template?["schemaVersion"] is not null)
                    {
                        continue;
                    }

                    add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, "template frontmatter names no document style");
                    continue;
                }

                // A record template lives in its .yaml file; a Markdown file
                // holding a record type is the shape the record files replaced.
                if (RecordTypes.All.Contains(type) || ContainerPageStyles.Contains(type))
                {
                    add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, $"type '{type}' is a record type; its template lives in '{type}.yaml', not in this Markdown file");
                    continue;
                }
            }
            else
            {
                continue;
            }

            if (template is null || type is null)
            {
                continue;
            }

            var def = defs[TemplateDefName(type)];
            if (def is null)
            {
                add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, $"template type '{type}' matches no record definition");
                continue;
            }

            var normalized = template.DeepClone();
            TemplateNormalizer.NormalizeTemplateNode(normalized, def, defs, schema!);

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
                add(relative, 1, MaintainerDiagnostics.Codes.TemplateSchema, text);
            }
        }
    }

    /// <summary>Parses the YAML frontmatter block of a document template.</summary>
    /// <param name="raw">The raw template text.</param>
    /// <param name="node">The parsed frontmatter, or null when it does not parse.</param>
    /// <param name="error">The parse failure message.</param>
    /// <returns>True when the frontmatter parses.</returns>
    private static bool TryParseDocumentFrontmatter(string raw, out JsonNode? node, out string error)
    {
        node = null;
        error = "";

        var lines = raw.Split('\n');
        var closing = -1;
        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].TrimEnd('\r') == "---")
            {
                closing = index;
                break;
            }
        }

        if (closing < 0)
        {
            error = "unterminated frontmatter block";
            return false;
        }

        return RecordsValidator.TryParseRecordFile(string.Join('\n', lines[1..closing]), out node, out error);
    }

    /// <summary>Computes the schema definition name of a record type.</summary>
    /// <param name="type">The record type name.</param>
    /// <returns>The definition name.</returns>
    private static string TemplateDefName(string type)
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

    /// <summary>Gets the pattern that matches a template index row.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\|\s*`([^`]+)`\s*\|\s*`([^`]+)`\s*\|")] private static partial Regex TemplateRowPattern();

    /// <summary>Gets the pattern that matches one brace placeholder in a template.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\{[^{}]*\}")] private static partial Regex PlaceholderPattern();
}
