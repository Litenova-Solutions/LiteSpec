using System.Text.Json.Nodes;

namespace LiteSpec.Standards;

/// <summary>
///     Composes the effective record JSON schema for a consumer's selected stack packs and
///     optional extensions from <c>schemas/record.schema.yaml</c> and the modular fragments
///     under <c>schemas/fragments/</c>. Properties declared by unselected pack or extension
///     fragments are stripped so consumer records can only use fields belonging to Core and
///     their declared selections.
/// </summary>
/// <follows>litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema</follows>
public static class RecordSchemaComposer
{
    /// <summary>The relative folder holding modular pack and extension schema fragments.</summary>
    public const string FragmentsFolder = "schemas/fragments";

    /// <summary>
    ///     Composes the record schema for the given stack and extension selections.
    /// </summary>
    /// <param name="standardsRoot">The LiteSpec release root holding <c>schemas/</c>.</param>
    /// <param name="stacks">The stack pack identifiers the consumer selects.</param>
    /// <param name="extensions">The optional extension identifiers the consumer selects.</param>
    /// <returns>The composed schema node ready for evaluation.</returns>
    public static JsonObject Compose(
        string standardsRoot,
        IReadOnlyList<string> stacks,
        IReadOnlyList<string> extensions)
    {
        var basePath = Path.Combine(standardsRoot, "schemas", "record.schema.yaml");
        var root = (JsonObject)(RecordText.Read(basePath)?.DeepClone()
            ?? throw new InvalidOperationException($"Record schema '{basePath}' could not be read."));

        var selectedStacks = new HashSet<string>(stacks, StringComparer.Ordinal);
        var selectedExtensions = new HashSet<string>(extensions, StringComparer.Ordinal);

        ApplyFolder(root, Path.Combine(standardsRoot, "schemas", "fragments", "packs"), selectedStacks);
        ApplyFolder(root, Path.Combine(standardsRoot, "schemas", "fragments", "ext"), selectedExtensions);

        return root;
    }

    /// <summary>
    ///     Applies or strips every fragment in one fragment directory based on whether its stem is selected.
    /// </summary>
    /// <param name="schema">The mutable record schema object.</param>
    /// <param name="directory">The fragment directory, such as <c>schemas/fragments/ext</c>.</param>
    /// <param name="selected">The selected identifiers for that kind.</param>
    private static void ApplyFolder(JsonObject schema, string directory, HashSet<string> selected)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var defs = schema["$defs"] as JsonObject;
        if (defs is null)
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.schema.yaml").OrderBy(path => path, StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(file);
            var id = fileName[..^".schema.yaml".Length];
            if (selected.Contains(id))
            {
                MergeFragment(defs, file);
            }
            else
            {
                StripFragment(defs, file);
            }
        }
    }

    /// <summary>Merges fragment definitions into the schema when the pack or extension is selected.</summary>
    /// <param name="defs">The target <c>$defs</c> map.</param>
    /// <param name="fragmentFile">The fragment schema file path.</param>
    private static void MergeFragment(JsonObject defs, string fragmentFile)
    {
        if (RecordText.Read(fragmentFile) is not JsonObject fragment
            || fragment["$defs"] is not JsonObject fragmentDefs)
        {
            return;
        }

        foreach (var entry in fragmentDefs)
        {
            if (entry.Value is not null)
            {
                defs[entry.Key] = entry.Value.DeepClone();
            }
        }
    }

    /// <summary>Strips fragment-owned properties and definitions when the pack or extension is not selected.</summary>
    /// <param name="defs">The target <c>$defs</c> map.</param>
    /// <param name="fragmentFile">The fragment schema file path.</param>
    private static void StripFragment(JsonObject defs, string fragmentFile)
    {
        if (RecordText.Read(fragmentFile) is not JsonObject fragment)
        {
            return;
        }

        if (fragment["x-extends"] is JsonObject extendsMap)
        {
            foreach (var pair in extendsMap)
            {
                if (defs[pair.Key] is not JsonObject targetDef
                    || targetDef["properties"] is not JsonObject properties
                    || pair.Value is not JsonArray propNames)
                {
                    continue;
                }

                foreach (var propNode in propNames)
                {
                    if (propNode is JsonValue val && val.TryGetValue<string>(out var propName))
                    {
                        properties.Remove(propName);
                    }
                }
            }
        }

        if (fragment["$defs"] is JsonObject fragmentDefs)
        {
            foreach (var entry in fragmentDefs)
            {
                defs.Remove(entry.Key);
            }
        }
    }
}
