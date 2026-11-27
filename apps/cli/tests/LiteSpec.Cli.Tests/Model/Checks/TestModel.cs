using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Builds project models by hand for check tests.</summary>
static class TestModel
{
    /// <summary>A config rooted at the folder, reading its declaration when one exists.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The loaded consumer config.</returns>
    public static ConsumerConfig Config(string root) => ConsumerConfig.Load(root);

    /// <summary>A record of the type and id, with the JSON fields, read at line 1 of the file.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <returns>The record the file reader yields for the text.</returns>
    public static Record Record(string type, string id, string file, string json) =>
        Record(type, id, file, 1, json);

    /// <summary>Every record the file reader yields for one record file, read at line 1 of the file.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <returns>The records the file reader yields, the page first.</returns>
    public static IReadOnlyList<Record> Records(string type, string id, string file, string json) =>
        Records(type, id, file, 1, json);

    /// <summary>
    ///     A record of the type and id at the file and line, with the JSON fields. The fields become
    ///     YAML text and go through <see cref="RecordFiles.Scan"/>, the file reader the CLI reads
    ///     record files with, so the YAML parse, the line of every value and the raw form are the
    ///     production ones. A text the reader refuses fails the helper, not the check under test.
    ///     The explanation beside a record is the consumer's own rule, so it is passed in, not read.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="line">The line the record starts on, with its type on that line and its id on the next.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <param name="explanationFile">The explanation file path, for a page record with one beside it.</param>
    /// <param name="explanation">The explanation file's full text, for a page record with one beside it.</param>
    /// <param name="isEntry">Whether the check sees the record as an entry, which some checks leave out.</param>
    /// <returns>The record the file reader yields, at the given position.</returns>
    /// <exception cref="InvalidOperationException">The reader refused the text or did not yield exactly the one record.</exception>
    public static Record Record(string type, string id, string file, int line, string json, string? explanationFile = null, string? explanation = null, bool isEntry = false)
    {
        var records = Records(type, id, file, line, json, explanationFile, explanation, isEntry);

        if (records.Count != 1)
        {
            throw new InvalidOperationException(
                $"The record file reader did not yield one record for '{id}'.");
        }

        return records[0];
    }

    /// <summary>
    ///     Every record the file reader yields for one record file: the page, then its
    ///     entries, then each step of a flow. A flow test adds them all to its model,
    ///     so the steps resolve like the production reader yields them.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="line">The line the record starts on.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <param name="explanationFile">The explanation file path, for a page record with one beside it.</param>
    /// <param name="explanation">The explanation file's full text, for a page record with one beside it.</param>
    /// <param name="isEntry">Whether the check sees the record as an entry, which some checks leave out.</param>
    /// <returns>The records the file reader yields, the page first.</returns>
    /// <exception cref="InvalidOperationException">The reader refused the text.</exception>
    public static IReadOnlyList<Record> Records(string type, string id, string file, int line, string json, string? explanationFile = null, string? explanation = null, bool isEntry = false)
    {
        var fields = JsonText.Parse(json);
        var properties = new List<KeyValuePair<string, JsonText>>
        {
            new(RecordFields.Type, JsonText.Text(type)),
            new(RecordFields.Id, JsonText.Text(id)),
            new(RecordFields.Title, JsonText.Text(id)),
        };

        foreach (var property in fields.Properties)
        {
            if (property.Key == RecordFields.Title)
            {
                properties.RemoveAll(existing => existing.Key == RecordFields.Title);
            }

            properties.Add(property);
        }

        var body = YamlWriter.Write(JsonText.ObjectOf(properties)).TrimEnd('\n');
        var text = new string('\n', line - 1) + body;
        var scan = RecordFiles.Scan(file, text);

        if (scan.Problems.Count > 0 || scan.Records.Count == 0)
        {
            throw new InvalidOperationException(
                $"The record file reader did not yield a record for '{id}': "
                + string.Join("; ", scan.Problems.Select(problem => problem.Message)));
        }

        return scan.Records.Select((indexed, at) => new Record
        {
            Type = indexed.Type,
            Id = indexed.Id,
            Title = indexed.Title,
            File = indexed.File,
            Line = indexed.Line,
            ExplanationFile = at == 0 ? explanationFile : null,
            Explanation = at == 0 ? explanation : null,
            IsEntry = isEntry || indexed.IsEntry,
            PageId = indexed.PageId,
            Raw = RecordFile.ElementOf(indexed.Tree),
        }).ToList();
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
