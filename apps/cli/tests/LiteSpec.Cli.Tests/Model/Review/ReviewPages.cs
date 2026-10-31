using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Edits fixture record files for the review and batch approval tests.</summary>
static class ReviewPages
{
    static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, NewLine = "\n" };

    /// <summary>Joins the root with forward-slash page parts.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="parts">The path segments under the root.</param>
    /// <returns>The full page path.</returns>
    public static string Page(string root, params string[] parts) =>
        Path.Combine([root, .. parts]);

    /// <summary>Reads the record file into its fields object.</summary>
    /// <param name="path">The record file path.</param>
    /// <returns>The record fields object.</returns>
    public static JsonObject Read(string path)
    {
        if (!RecordFile.TryParse(File.ReadAllText(path), out _, out var tree, out var error))
        {
            throw new InvalidOperationException($"Record file '{path}' could not be read: {error}");
        }

        return (JsonObject)JsonNode.Parse(tree.ToJson())!;
    }

    /// <summary>Writes the record fields back in canonical form.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="front">The record fields object to write.</param>
    public static void Write(string path, JsonObject front) =>
        File.WriteAllText(path, RecordFile.Write(front));

    /// <summary>Patches the page-level record fields object.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="mutate">The change to apply to the fields.</param>
    public static void PatchPage(string path, Action<JsonObject> mutate)
    {
        var front = Read(path);

        mutate(front);
        Write(path, front);
    }

    /// <summary>Patches the entry carrying the id: the page itself or one records entry.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="id">The record id of the entry to patch.</param>
    /// <param name="mutate">The change to apply to the entry.</param>
    public static void PatchEntry(string path, string id, Action<JsonObject> mutate)
    {
        var front = Read(path);

        if (front["id"]?.GetValue<string>() == id)
        {
            mutate(front);
        }
        else
        {
            var entry = front["records"]?.AsArray()
                .OfType<JsonObject>()
                .First(entry => entry["id"]?.GetValue<string>() == id);

            mutate(entry!);
        }

        Write(path, front);
    }

    /// <summary>Appends an entry to the page records array.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="entry">The record entry to append.</param>
    public static void AppendEntry(string path, JsonObject entry)
    {
        PatchPage(path, front =>
        {
            if (front["records"] is not JsonArray records)
            {
                records = [];
                front["records"] = records;
            }

            records.Add(entry);
        });
    }

    /// <summary>Turns the page into a draft decided by the owner on the date.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="decider">The owner who decides the draft.</param>
    /// <param name="decideBy">The date the draft is decided by, in ISO form.</param>
    public static void DraftPage(string path, string decider = "lending-team", string decideBy = "2027-01-01") =>
        PatchPage(path, front => Draft(front, decider, decideBy));

    /// <summary>Turns the entry carrying the id into a draft.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="id">The record id of the entry to draft.</param>
    /// <param name="decider">The owner who decides the draft.</param>
    /// <param name="decideBy">The date the draft is decided by, in ISO form.</param>
    public static void DraftEntry(string path, string id, string decider = "lending-team", string decideBy = "2027-01-01") =>
        PatchEntry(path, id, front => Draft(front, decider, decideBy));

    /// <summary>Marks the fields a draft: status, decider, decide-by, and no approval data.</summary>
    /// <param name="front">The fields object to mark.</param>
    /// <param name="decider">The owner who decides the draft.</param>
    /// <param name="decideBy">The date the draft is decided by, in ISO form.</param>
    static void Draft(JsonObject front, string decider, string decideBy)
    {
        front["status"] = "draft";
        front["decider"] = decider;
        front["decideBy"] = decideBy;
        front.Remove("approvedBy");
        front.Remove("approvedOn");
        front.Remove("approvedHash");
    }

    /// <summary>Adds one risk value to the page, which makes it tier 1.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="risk">The risk value to add.</param>
    public static void AddPageRisk(string path, string risk = "money") =>
        PatchPage(path, front => front["risk"] = new JsonArray(risk));

    /// <summary>Adds one risk value to the entry carrying the id.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="id">The record id of the entry to mark.</param>
    /// <param name="risk">The risk value to add.</param>
    public static void AddEntryRisk(string path, string id, string risk = "money") =>
        PatchEntry(path, id, front => front["risk"] = new JsonArray(risk));

    /// <summary>Adds one open question to the page.</summary>
    /// <param name="path">The record file path.</param>
    /// <param name="question">The question text to add.</param>
    public static void AddPageQuestion(
        string path, string question = "Who approves a split across cost centers?") =>
        PatchPage(path, front => front["questions"] = new JsonArray(
            new JsonObject
            {
                ["question"] = question,
                ["decider"] = "lending-team",
                ["decideBy"] = "2027-06-01",
                ["ballot"] = "split-approval",
                ["recommendation"] = "Approve the split as written.",
            }));

    /// <summary>Sets the project's review sample share.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="share">The sample share to write, as a fraction.</param>
    public static void SetSampleShare(string root, double share)
    {
        var path = Path.Combine(root, "litespec.project.json");
        var project = (JsonObject)JsonNode.Parse(File.ReadAllText(path))!;

        if (project["review"] is not JsonObject review)
        {
            review = [];
            project["review"] = review;
        }

        review["sampleShare"] = share;
        File.WriteAllText(path, project.ToJsonString(Indented) + "\n");
    }

    /// <summary>Parses standard output as JSON.</summary>
    /// <param name="result">The completed command run.</param>
    /// <returns>The parsed JSON output.</returns>
    public static JsonNode OutputJson(Support.CliResult result) =>
        JsonNode.Parse(result.Stdout)!;

    /// <summary>The semantic hash of one fields object, as the builder reads it. A page of an article type also hashes its explanation file beside the record file.</summary>
    /// <param name="type">The record type of the fields.</param>
    /// <param name="id">The record id of the fields.</param>
    /// <param name="front">The fields object to hash.</param>
    /// <param name="recordPath">The record file path, so an article page hashes its explanation file; null for fields alone.</param>
    /// <returns>The semantic hash text.</returns>
    public static string SemanticHash(string type, string id, JsonObject front, string? recordPath = null)
    {
        using var document = JsonDocument.Parse(front.ToJsonString());

        string? explanation = null;

        if (recordPath is not null)
        {
            var explanationPath = Path.ChangeExtension(recordPath, ".md");

            if (File.Exists(explanationPath))
            {
                explanation = File.ReadAllText(explanationPath);
            }
        }

        return Checker.SemanticHash(new LiteSpec.Model.Record
        {
            Type = type,
            Id = id,
            Raw = document.RootElement.Clone(),
            Explanation = explanation,
        });
    }
}
