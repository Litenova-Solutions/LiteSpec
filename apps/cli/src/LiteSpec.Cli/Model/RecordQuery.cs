using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

/// <summary>
///     Prints one record with its links in both directions, and lists records by text or by type.
/// </summary>
/// <follows>litespec/core/agent#select-task-context</follows>
static class RecordQuery
{
    /// <summary>Queries one record id, or returns null when no record holds it.</summary>
    /// <param name="model">The built project model to query.</param>
    /// <param name="id">The record id to print.</param>
    /// <returns>The record as indented JSON with its links in both directions, or null when no record holds the id.</returns>
    public static string? Query(ProjectModel model, string id)
    {
        if (!model.ById.TryGetValue(id, out var record))
        {
            return null;
        }

        var key = record.Id;

        var incoming = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var other in model.Records)
        {
            if (other.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in other.Raw.EnumerateObject())
            {
                if (RecordTypes.LinkTargets.ContainsKey(property.Name)
                    || RecordTypes.NestedLinkFields.Contains(property.Name)
                    || property.Name is RecordFields.Refusal or RecordFields.Provision)
                {
                    var targets = ModelBuilder.TargetsOf(property.Value);

                    // A flow step or an external-system operation links through
                    // its carrying record, so its base id links back too.
                    if (targets.Any(target => string.Equals(target, key, StringComparison.Ordinal)
                        || string.Equals(Identifiers.BaseId(target), key, StringComparison.Ordinal)))
                    {
                        if (!incoming.TryGetValue(other.Id, out var fields))
                        {
                            fields = [];
                            incoming[other.Id] = fields;
                        }

                        fields.Add(property.Name);
                    }
                }
            }
        }

        JsonObject? computed = null;

        if (model.Completion.TryGetValue(key, out var completion))
        {
            if (record.Type == RecordTypes.UseCase)
            {
                computed = new JsonObject
                {
                    ["completion"] = CompletionJson(completion),
                    ["implements"] = CliJson.Strings(model.Implements.Where(link => link.To == key).Select(link => link.Path)),
                    ["calls"] = CliJson.Strings(model.Calls.Where(link => link.To == key).Select(link => link.Screen)),
                    ["proves"] = ProvesJson(model.Proves.Where(link => ScenarioOf(model, link.Claim) == ScenarioName(key))),
                };
            }
            else
            {
                computed = new JsonObject
                {
                    ["completion"] = CompletionJson(completion),
                    ["proves"] = ProvesJson(model.Proves.Where(link => link.Claim == key)),
                };
            }
        }
        else if (record.Type == RecordTypes.Scenario && model.ProofStatuses.TryGetValue(key, out var proof))
        {
            computed = new JsonObject
            {
                ["proof"] = ProofJson(proof),
                ["proves"] = ProvesJson(model.Proves.Where(link => link.Claim == key)),
            };
        }

        var query = new JsonObject
        {
            ["type"] = record.Type,
            ["id"] = record.Id,
            ["folder"] = record.Folder,
            ["module"] = record.Module,
            ["title"] = record.Title,
            ["file"] = PathText.Relative(model.Config.Root, record.File),
            ["line"] = record.Line,
            ["fields"] = JsonNode.Parse(record.Raw.GetRawText()),
            ["links"] = CliJson.StringLists(ModelBuilder.ResolvedLinks(model, record)),
            ["linkedBy"] = CliJson.StringLists(incoming),
            ["computed"] = computed,
        };

        return CliJson.Serialize(query);
    }

    /// <summary>
    ///     Finds the written records whose identifier, title, summary or statement holds the text,
    ///     case-insensitive. An exact identifier match comes first, then other identifier matches,
    ///     then title, summary and statement matches; each group orders by identifier.
    /// </summary>
    /// <param name="model">The built project model to search.</param>
    /// <param name="text">The text to find in the records.</param>
    /// <returns>The matching records in print order.</returns>
    public static IReadOnlyList<Record> Search(ProjectModel model, string text) =>
        model.Records
            .Where(record => !record.Derived && record.Id.Length > 0 && Rank(record, text) >= 0)
            .OrderBy(record => Rank(record, text))
            .ThenBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>Lists the written records of the type, optionally only those of one module, by identifier.</summary>
    /// <param name="model">The built project model to list from.</param>
    /// <param name="type">The record type to list, such as <c>use-case</c>.</param>
    /// <param name="module">The module to keep, or null for every module.</param>
    /// <returns>The records of the type in identifier order.</returns>
    public static IReadOnlyList<Record> List(ProjectModel model, string type, string? module) =>
        model.Records
            .Where(record => !record.Derived
                && record.Id.Length > 0
                && record.Type.Equals(type, StringComparison.Ordinal)
                && (module is null || module.Equals(record.Module, StringComparison.Ordinal)))
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>One record's line for search and list output: its identifier, title and file path.</summary>
    /// <param name="model">The built project model holding the consumer root.</param>
    /// <param name="record">The record to print.</param>
    /// <returns>The printed line for the record.</returns>
    public static string Line(ProjectModel model, Record record) =>
        $"{record.Id} - {record.Title} ({PathText.Relative(model.Config.Root, record.File)})";

    /// <summary>The search rank of a record: 0 for an exact identifier, 1 to 4 down the fields, -1 for no match.</summary>
    /// <param name="record">The record to rank.</param>
    /// <param name="text">The searched text.</param>
    /// <returns>The rank, or -1 when no searched field holds the text.</returns>
    static int Rank(Record record, string text)
    {
        if (record.Id.Equals(text, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (record.Id.Contains(text, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (record.Title.Contains(text, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (record.Field(RecordFields.Summary) is { } summary
            && summary.Contains(text, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (record.Field(RecordFields.Statement) is { } statement
            && statement.Contains(text, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return -1;
    }

    /// <summary>One record's computed completion as its JSON object.</summary>
    /// <param name="completion">The completion to project.</param>
    /// <returns>The JSON object for the completion.</returns>
    static JsonObject CompletionJson(Completion completion)
    {
        var counts = new JsonObject();

        foreach (var (label, count) in completion.Counts)
        {
            counts[label] = new JsonObject { ["done"] = count.Done, ["of"] = count.Of };
        }

        var parkedBy = new JsonArray();

        foreach (var parked in completion.ParkedBy)
        {
            parkedBy.Add((JsonNode)new JsonObject
            {
                ["id"] = parked.Id,
                ["waiver"] = parked.Waiver,
                ["expiresOn"] = parked.ExpiresOn,
            });
        }

        return new JsonObject
        {
            ["stage"] = completion.Stage,
            ["proof"] = completion.Proof,
            ["heldBy"] = CliJson.Strings(completion.HeldBy),
            ["parkedBy"] = parkedBy,
            ["inScope"] = completion.InScope,
            ["parked"] = completion.Parked,
            ["total"] = completion.Total,
            ["counts"] = counts,
        };
    }

    /// <summary>One scenario's computed proof status as its JSON object.</summary>
    /// <param name="proof">The proof status to project.</param>
    /// <returns>The JSON object for the proof status.</returns>
    static JsonObject ProofJson(ProofStatus proof)
    {
        var json = new JsonObject
        {
            ["commit"] = proof.Commit,
            ["lastPassedAt"] = proof.LastPassedAt,
            ["status"] = proof.Status,
            ["commitsBehind"] = proof.CommitsBehind,
        };

        if (proof.StaleReason is not null)
        {
            json["staleReason"] = proof.StaleReason;
        }

        return json;
    }

    /// <summary>The proof links of a record as a JSON array, in model order.</summary>
    /// <param name="links">The proof links to project.</param>
    /// <returns>The JSON array of proof links.</returns>
    static JsonArray ProvesJson(IEnumerable<ProvesLink> links) =>
        new([.. links.Select(link => (JsonNode?)new JsonObject
        {
            ["scenario"] = link.Scenario,
            ["claim"] = link.Claim,
            ["confirmed"] = link.Confirmed,
            ["passing"] = link.Passing,
        })]);

    /// <summary>The home name a scenario id belongs to.</summary>
    /// <param name="model">The built project model, kept for symmetry with the other query helpers.</param>
    /// <param name="scenarioId">The scenario id to split.</param>
    /// <returns>The home segment of the scenario id, or null when the id does not split.</returns>
    private static string? ScenarioOf(ProjectModel model, string scenarioId) =>
        Identifiers.TrySplit(scenarioId, out _, out var home, out _) ? home : null;

    /// <summary>The name segment of a use-case id, which its scenarios carry as their home.</summary>
    /// <param name="useCaseId">The use-case id to split.</param>
    /// <returns>The name segment of the use-case id, or null when the id does not split.</returns>
    private static string? ScenarioName(string useCaseId) =>
        Identifiers.TrySplit(useCaseId, out _, out _, out var name) ? name : null;
}
