using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Consumer;

/// <summary>
///     The project record checks of the consumer validator: the record against
///     its schema, the documentation root, and the extension selections.
/// </summary>
/// <follows>litespec/core/model#declare-structured-specification-metadata</follows>
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
        var projectFile = ConsumerRoot.ProjectFile(run.Root);

        // The project record is validated through the shared schema module, so the
        // schema is the one statement of its shape. A hand table beside it would need
        // the same edit twice and drift on the third.
        run.Schema("project-invalid", projectFile, ConsumerRoot.ProjectFileDisplay, JsonSchema.Problems(run.ProjectSchemaFile, run.Project, ConsumerRoot.ProjectFileDisplay, run.ReadSchema));

        // A placeholder left from the template, such as __PROJECT__, reads as a real value to
        // every later check, so the run names each one instead of passing over it.
        foreach (var placeholder in Init.InitCommand.Placeholders(run.Project).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            run.Err(new Finding(
                "project-invalid",
                $"template placeholder '{placeholder}' is not filled [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                projectFile,
                1,
                null));
        }

        var paths = (run.Project as JsonObject)?["paths"] as JsonObject;

        // The record root is configuration, not a constant. A hard-coded folder
        // scans nothing in a consumer that keeps its pages elsewhere, and a scan of
        // nothing reports PASS. '.litespec' stays the default because that is the layout the
        // standards describe.
        run.DocsPath = JsonValues.PropString(paths, "docs") ?? ".litespec";
        run.DocsRoot = Path.Join(run.Root, run.DocsPath);

        // A configured path that does not resolve disables the checks that read it and
        // leaves the run reporting PASS, so each message names the check that goes quiet.
        var configuredPaths = new[]
        {
            ("paths.docs", "docs", "the page scan finds no record to check"),
            ("paths.glossary", "glossary", "the glossary check resolves nothing"),
        };

        foreach (var (field, key, consequence) in configuredPaths)
        {
            if (paths is null || !paths.TryGetPropertyValue(key, out var value))
            {
                continue;
            }

            var text = JsonValues.AsString(value);

            if (text is null || text.Trim().Length == 0)
            {
                run.Err(new Finding("project-path-invalid", $"{field} '{JsonValues.Text(value)}' is not a path", projectFile, 1, null));
                continue;
            }

            if (!File.Exists(Path.Join(run.Root, text)) && !Directory.Exists(Path.Join(run.Root, text)))
            {
                run.Err(new Finding("project-path-invalid", $"{field} '{text}' does not exist; {consequence}", projectFile, 1, null));
            }
        }

        // A selection records the criterion that was met, so a selection that costs
        // nothing to keep still states why it is there.
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
        if (entry is JsonValue && JsonValues.AsString(entry) is string id)
        {
            return new Selection(id, id);
        }

        if (entry is JsonObject container)
        {
            return new Selection(JsonValues.PropString(container, "id"), JsonValues.PropText(container, "id"));
        }

        return new Selection(null, "undefined");
    }
}
