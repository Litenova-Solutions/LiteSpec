using Record = LiteSpec.Model.Record;
using System.Text.Json;
using LiteSpec.Model;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Builds project models by hand for check tests.</summary>
static class TestModel
{
    /// <summary>A config rooted at the folder, reading its declaration when one exists.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The loaded consumer config.</returns>
    public static ConsumerConfig Config(string root) => ConsumerConfig.Load(root);

    /// <summary>A record of the type and id, with the JSON fields.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The record at line 1 of the file.</returns>
    public static Record Record(string type, string id, string file, string json) =>
        Record(type, id, file, 1, json);

    /// <summary>A record of the type and id at the file and line, with the JSON fields.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="line">The line the record starts on.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <param name="explanationFile">The explanation file path, for a page record with one beside it.</param>
    /// <param name="explanation">The explanation file's full text, for a page record with one beside it.</param>
    /// <param name="isEntry">Whether the record is an entry on its page rather than the page itself.</param>
    /// <returns>The record at the given position.</returns>
    public static Record Record(string type, string id, string file, int line, string json, string? explanationFile = null, string? explanation = null, bool isEntry = false)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = file,
            Line = line,
            ExplanationFile = explanationFile,
            Explanation = explanation,
            IsEntry = isEntry,
            Raw = raw,
        };
    }

    /// <summary>A model holding the records, with empty code, screens and tests.</summary>
    /// <param name="config">The consumer config the model is built for.</param>
    /// <param name="records">The records the model holds.</param>
    /// <returns>The project model holding the records.</returns>
    public static ProjectModel Model(ConsumerConfig config, params Record[] records)
    {
        var model = new ProjectModel { Config = config };
        model.Records.AddRange(records);
        return model;
    }

    /// <summary>A page body of the file.</summary>
    /// <param name="file">The page file path.</param>
    /// <param name="body">The page body text.</param>
    /// <returns>The page body at line 1 with no carrying page record.</returns>
    public static PageBody Body(string file, string body) =>
        new PageBody(file, body, 1, null);

    /// <summary>The codes of the findings the check reports.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <returns>The finding codes in report order.</returns>
    public static List<string> Codes(ProjectModel model) =>
        Checker.Check(model).Findings.Select(finding => finding.Code).ToList();

    static string? StringField(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
