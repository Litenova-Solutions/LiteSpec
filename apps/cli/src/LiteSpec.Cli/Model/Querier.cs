using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     Prints one record with its links in both directions.
/// </summary>
/// <follows>litespec/core/agent#select-task-context</follows>
static class Querier
{
    /// <summary>Queries one id or qualified identifier, or null when no record holds it.</summary>
    /// <param name="model">The built project model to query.</param>
    /// <param name="id">The record id or qualified identifier to print.</param>
    /// <returns>The record as indented JSON with its links in both directions, or null when no record holds the id.</returns>
    public static string? Query(ProjectModel model, string id)
    {
        if (!model.ById.TryGetValue(id, out var record)
            && !model.ByQualified.TryGetValue(id, out record))
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

                    if (targets.Contains(key, StringComparer.Ordinal))
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

        var query = new JsonObject
        {
            ["type"] = record.Type,
            ["id"] = record.Id,
            ["qualifiedId"] = record.QualifiedId,
            ["title"] = record.Title,
            ["file"] = model.Config.Relative(record.File),
            ["line"] = record.Line,
            ["fields"] = JsonNode.Parse(record.Raw.GetRawText()),
            ["links"] = CliJson.StringLists(ModelBuilder.ResolvedLinks(model, record)),
            ["linkedBy"] = CliJson.StringLists(incoming),
            ["computed"] = computed,
        };

        return CliJson.Serialize(query);
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

        return new JsonObject
        {
            ["stage"] = completion.Stage,
            ["proof"] = completion.Proof,
            ["heldBy"] = CliJson.Strings(completion.HeldBy),
            ["counts"] = counts,
        };
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
