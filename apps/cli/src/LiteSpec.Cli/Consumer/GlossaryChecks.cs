using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Consumer;

/// <summary>
///     The glossary check of the consumer validator: the glossary file against
///     its schema.
/// </summary>
/// <follows>litespec/core/language#record-the-project-vocabulary-as-data</follows>
static class GlossaryChecks
{
    /// <summary>
    ///     Checks the glossary file against the glossary schema and summarizes the outcome.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The glossary reader must report an unreadable glossary file as a finding and continue the run instead of crashing on any input.")]
    public static void CheckGlossary(ConsumerRun run)
    {
        var glossaryFile = run.GlossaryFile;
        var glossaryRelative = PathText.Relative(run.Root, glossaryFile);

        // The project record check already reported a configured glossary path that
        // does not resolve, so a missing file stays quiet here. A consumer without
        // a glossary keeps no shared terms.
        if (!File.Exists(glossaryFile))
        {
            run.GlossarySummary = "Glossary: no glossary found";
            return;
        }

        JsonNode? record = null;

        try
        {
            record = RecordFile.NodeOf(YamlText.Parse(run.ReadText(glossaryFile)));
        }
        catch (Exception cause)
        {
            run.Err(new Finding("glossary-invalid", $"not valid YAML ({cause.Message})", glossaryFile, 1, null));
        }

        if (JsonValues.IsTruthy(record))
        {
            run.Schema(
                "glossary-invalid",
                glossaryFile,
                glossaryRelative,
                JsonSchema.Problems(run.GlossarySchemaFile, record, glossaryRelative, run.ReadSchema));
        }

        var terms = (record as JsonObject)?["terms"] is JsonArray array ? array.Count : 0;
        run.GlossarySummary = $"Glossary: {terms} term(s)";

    }
}
