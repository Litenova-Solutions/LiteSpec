using System.Text.Json.Nodes;
using LiteSpec.Schemas;

namespace LiteSpec.Consumer;

/// <summary>
///     The project record checks of the consumer validator: the record against
///     its schema, the documentation root, and the extension selections.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/scope#select-a-size-a-shape-and-stacks</follows>
static class ProjectFileChecks
{
    /// <summary>
    ///     Validates the project record against the project schema, resolves the documentation
    ///     root, and normalizes the extension selections.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckProjectRecord(ConsumerRun run)
    {
        // The project record is validated through the shared schema module, so the
        // schema is the one statement of its shape. A hand table beside it would need
        // the same edit twice and drift on the third.
        foreach (var problem in JsonSchema.Problems(run.ProjectSchemaFile, run.Project, "litespec.project.json", run.ReadSchema))
        {
            run.Err(problem);
        }

        // A placeholder left from the template, such as __OWNER__, reads as a real value to
        // every later check, so the run names each one instead of passing over it.
        foreach (var placeholder in Init.InitCommand.Placeholders(run.Project).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            run.Err($"litespec.project.json: template placeholder '{placeholder}' is not filled [litespec/core/scope#select-a-size-a-shape-and-stacks]");
        }

        var paths = (run.Project as JsonObject)?["paths"] as JsonObject;

        // The documentation root is configuration, not a constant. A hard-coded docs/
        // scans nothing in a consumer that keeps its pages elsewhere, and a scan of
        // nothing reports PASS. 'docs' stays the default because that is the layout the
        // standards describe.
        run.DocsPath = JsonRead.PropString(paths, "docs") ?? "docs";
        run.DocsRoot = Path.Join(run.Root, run.DocsPath);

        // A configured path that does not resolve disables the checks that read it and
        // leaves the run reporting PASS, so each message names the check that goes quiet.
        var configuredPaths = new[]
        {
            ("paths.docs", "docs", "the page scan finds no record to check"),
            ("paths.glossary", "glossary", "the vocabulary and mannered-term checks resolve nothing"),
        };

        foreach (var (field, key, consequence) in configuredPaths)
        {
            if (paths is null || !paths.TryGetPropertyValue(key, out var value))
            {
                continue;
            }

            var text = JsonRead.AsString(value);

            if (text is null || text.Trim().Length == 0)
            {
                run.Err($"litespec.project.json: {field} '{JsonValues.Text(value)}' is not a path");
                continue;
            }

            if (!File.Exists(Path.Join(run.Root, text)) && !Directory.Exists(Path.Join(run.Root, text)))
            {
                run.Err($"litespec.project.json: {field} '{text}' does not exist; {consequence}");
            }
        }

        // A selection records the criterion that was met and the date it is next
        // reviewed, so a selection that costs nothing to keep still expires.
        var declared = (run.Project as JsonObject)?["selectedExtensions"] is JsonArray array ? array.ToList() : new List<JsonNode?>();

        foreach (var entry in declared)
        {
            run.Selections.Add(NormalizeSelection(entry));
        }
    }

    /// <summary>
    ///     Normalizes one extension selection entry.
    /// </summary>
    /// <param name="entry">The selection entry as recorded.</param>
    /// <returns>The normalized selection.</returns>
    static Selection NormalizeSelection(JsonNode? entry)
    {
        if (entry is JsonValue && JsonRead.AsString(entry) is string id)
        {
            return new Selection(id, id, null, null);
        }

        if (entry is JsonObject container)
        {
            var selectedId = JsonRead.PropString(container, "id");

            return new Selection(selectedId, JsonRead.PropText(container, "id"), JsonRead.PropString(container, "reviewBy"), JsonRead.PropString(container, "releaseStage"));
        }

        return new Selection(null, "undefined", null, null);
    }
}
