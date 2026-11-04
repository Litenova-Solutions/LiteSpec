using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     Prints one record with its links in both directions.
/// </summary>
/// <follows>litespec/core/agent#select-task-context</follows>
static class Querier
{
    /// <summary>The JSON options query output uses: indented, with LF newlines.</summary>
    private static readonly JsonSerializerOptions QueryOptions = new() { WriteIndented = true, NewLine = "\n" };

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

        object? computed = null;

        if (model.Completion.TryGetValue(key, out var completion))
        {
            if (record.Type == RecordTypes.UseCase)
            {
                computed = new
                {
                    completion = CompletionJson(completion),
                    implements = model.Implements.Where(link => link.To == key).Select(link => link.Path).ToList(),
                    calls = model.Calls.Where(link => link.To == key).Select(link => link.Screen).ToList(),
                    proves = model.Proves
                        .Where(link => ScenarioOf(model, link.Claim) == ScenarioName(key))
                        .Select(link => new { scenario = link.Scenario, claim = link.Claim, confirmed = link.Confirmed, passing = link.Passing })
                        .ToList(),
                };
            }
            else
            {
                computed = new
                {
                    completion = CompletionJson(completion),
                    proves = model.Proves
                        .Where(link => link.Claim == key)
                        .Select(link => new { scenario = link.Scenario, claim = link.Claim, confirmed = link.Confirmed, passing = link.Passing })
                        .ToList(),
                };
            }
        }

        return JsonSerializer.Serialize(
            new
            {
                type = record.Type,
                id = record.Id,
                qualifiedId = record.QualifiedId,
                title = record.Title,
                file = model.Config.Relative(record.File),
                line = record.Line,
                fields = JsonSerializer.Deserialize<JsonElement>(record.Raw.GetRawText()),
                links = ModelBuilder.ResolvedLinks(model, record),
                linkedBy = incoming,
                computed,
            },
            QueryOptions);
    }

    /// <summary>One record's computed completion as its JSON object.</summary>
    /// <param name="completion">The completion to project.</param>
    /// <returns>The JSON object for the completion.</returns>
    static object CompletionJson(Completion completion) => new
    {
        stage = completion.Stage,
        proof = completion.Proof,
        heldBy = completion.HeldBy,
        counts = completion.Counts.ToDictionary(
            entry => entry.Key,
            entry => new { done = entry.Value.Done, of = entry.Value.Of },
            StringComparer.Ordinal),
    };

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
