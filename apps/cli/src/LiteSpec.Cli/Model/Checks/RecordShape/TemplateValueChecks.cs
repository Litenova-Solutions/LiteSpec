using System.Text.Json.Nodes;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The template value check: a record that <c>litespec new</c> scaffolded keeps no text or number its template copied.</summary>
/// <follows>litespec/core/authoring#copy-each-template-from-the-example</follows>
internal static class TemplateValueChecks
{
    /// <summary>The keys that name the record's kind and identity and so never count as a copied value.</summary>
    private static readonly HashSet<string> StructuralKeys = new(["type", "schemaVersion"], StringComparer.Ordinal);

    /// <summary>Reports template-value-unchanged.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (!RecordFiles.IsRecordFile(record.File))
            {
                continue;
            }

            string text;

            try
            {
                text = SourceTree.Read(record.File);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (RecordScaffold.TemplateOf(text) is not { } templateFile)
            {
                continue;
            }

            var templatePath = LiteSpecHome.PathTo($"templates/consumer/{templateFile}");
            if (!File.Exists(templatePath)
                || !RecordFile.TryParse(text, out _, out var tree, out _)
                || RecordFile.NodeOf(tree) is not JsonObject scaffold
                || !RecordFile.TryParse(File.ReadAllText(templatePath), out _, out var templateTree, out _)
                || RecordFile.NodeOf(templateTree) is not JsonObject template)
            {
                continue;
            }

            var kept = new List<string>();
            Unchanged(scaffold, template, string.Empty, kept);
            if (kept.Count > 0)
            {
                findings.Add(new Finding(
                    "template-value-unchanged",
                    $"Record '{record.Id}' keeps the template values at {string.Join(", ", kept.Take(5))}",
                    record.File,
                    1,
                    SubjectOf(record)));
            }
        }
    }

    /// <summary>Collects the paths of the text and number values a record holds exactly as its template does.</summary>
    /// <param name="record">The record node.</param>
    /// <param name="template">The template node at the same path.</param>
    /// <param name="path">The dotted path of the node.</param>
    /// <param name="kept">The paths of the unchanged values.</param>
    private static void Unchanged(JsonNode? record, JsonNode? template, string path, List<string> kept)
    {
        switch (record)
        {
            case JsonObject node when template is JsonObject templateNode:
                foreach (var pair in node)
                {
                    var identity = path.Length == 0 && pair.Key == "title";
                    if (StructuralKeys.Contains(pair.Key) || identity || pair.Key == "id" && path.Length == 0)
                    {
                        continue;
                    }

                    if (templateNode.TryGetPropertyValue(pair.Key, out var child))
                    {
                        Unchanged(pair.Value, child, path.Length == 0 ? pair.Key : $"{path}.{pair.Key}", kept);
                    }
                }

                break;
            case JsonArray items when template is JsonArray templateItems:
                for (var index = 0; index < Math.Min(items.Count, templateItems.Count); index++)
                {
                    Unchanged(items[index], templateItems[index], $"{path}[{index}]", kept);
                }

                break;
            case JsonValue value when template is JsonValue templateValue:
                var json = value.ToJsonString();
                var copied = json.Length > 0 && (json[0] == '"' || char.IsDigit(json[0]) || json[0] == '-');
                if (copied && json == templateValue.ToJsonString())
                {
                    kept.Add(path);
                }

                break;
        }
    }
}
