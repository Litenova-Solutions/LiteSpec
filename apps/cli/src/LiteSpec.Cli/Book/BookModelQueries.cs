using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Book;

/// <summary>
///     The relationship readings over the index: one scenario's proof state, the
///     children and use case of a result, and the first calculation example.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookModelQueries
{
    /// <summary>One scenario's proof state from its proves links, while the model holds no stored state for it.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="scenarioId">The scenario id.</param>
    /// <returns>The proof state, its enriched links and whether it is claimed.</returns>
    public static ScenarioProofResult ScenarioProof(BookIndex index, string scenarioId)
    {
        var links = index.ProvesByClaim.TryGetValue(scenarioId, out var found) ? found : [];
        var enriched = links.Select(link => new ProofLink(
            link["scenario"],
            BookRender.IsTrue(link["confirmed"]),
            BookRender.IsTrue(link["indirect"]),
            link["passing"] is JsonValue scalar && scalar.TryGetValue<bool>(out var passing) ? passing : (bool?)null,
            BookRender.StrProp(link, "scenario") is { } scenario ? index.TestsByName.GetValueOrDefault(scenario) : null)).ToList();
        var confirmed = enriched.Where(link => link.Confirmed).ToList();
        var state = "unproved";
        if (!BookModel.HasTestRun(index))
        {
            state = "norun";
        }
        else if (confirmed.Count > 0)
        {
            if (confirmed.Any(link => link.Indirect))
            {
                state = "proved-indirect";
            }
            else if (confirmed.Any(link => link.Passing == false))
            {
                state = "proved-failing";
            }
            else if (confirmed.All(link => link.Passing == true))
            {
                state = "proved-passing";
            }
            else
            {
                state = "awaiting-run";
            }
        }

        return new ScenarioProofResult(state, enriched, links.Count > 0 && confirmed.Count == 0);
    }

    /// <summary>The results and scenarios that belong to one use case.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="useCaseId">The use-case id.</param>
    /// <returns>The use case's results and scenarios.</returns>
    public static UseCaseKids UseCaseChildren(BookIndex index, string useCaseId)
    {
        var name = BookModel.SplitId(useCaseId).Name;
        return new UseCaseKids(
            index.ResultsOf.TryGetValue(name, out var results) ? results : [],
            index.ScenariosOf.TryGetValue(name, out var scenarios) ? scenarios : []);
    }

    /// <summary>The use case a result or scenario belongs to, matched by home name.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="childId">The result or scenario id.</param>
    /// <returns>The owning use case, or null.</returns>
    public static JsonObject? UseCaseOf(BookIndex index, string childId)
    {
        if (BookIndexBuilder.DerivedHolder(childId) is { } holder)
        {
            return (index.ByType.TryGetValue("use-case", out var derived) ? derived : [])
                .FirstOrDefault(useCase => BookModel.SplitId(BookRender.StrProp(useCase, "id") ?? "").Name == holder);
        }

        var home = BookModel.SplitId(childId).Home;
        if (home is null)
        {
            return null;
        }

        return (index.ByType.TryGetValue("use-case", out var cases) ? cases : [])
            .FirstOrDefault(useCase => BookModel.SplitId(BookRender.StrProp(useCase, "id") ?? "").Name == home);
    }

    /// <summary>The first examples table proving a calculation rule, with its scenario, or null.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="rule">The rule record.</param>
    /// <returns>The scenario and its examples, or null.</returns>
    public static (JsonObject Scenario, JsonNode Examples)? CalculationExample(BookIndex index, JsonObject rule)
    {
        foreach (var governed in LinksOf(rule, "governs"))
        {
            if (!index.ById.TryGetValue(governed, out var target) || BookRender.StrProp(target, "type") != "use-case")
            {
                continue;
            }

            var children = UseCaseChildren(index, governed);
            foreach (var scenario in children.Scenarios)
            {
                var examples = BookRender.Fields(scenario)?["examples"];
                if (examples is JsonArray rows && rows.Count > 0)
                {
                    return (scenario, examples);
                }
            }
        }

        return null;
    }

    /// <summary>The target ids of one link field.</summary>
    /// <param name="record">The record.</param>
    /// <param name="field">The link field.</param>
    /// <returns>The target ids.</returns>
    public static IReadOnlyList<string> LinksOf(JsonObject record, string field)
    {
        var links = BookRender.Links(record)?[field] as JsonArray ?? [];
        return links
            .Where(target => target is JsonValue scalar && scalar.TryGetValue<string>(out _))
            .Select(target => BookRender.Str(target))
            .Where(id => id.Length > 0)
            .ToList();
    }
}
