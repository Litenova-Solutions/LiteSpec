using System.Text.Json;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>
///     The retrieval evaluation: each case names a real path or record id with the
///     provisions a reviewer agrees apply. Every agreed provision must be returned,
///     collapsed into its record line, or left out by its applicability condition,
///     and at least 0.8 of the returned provisions must be agreed.
/// </summary>
public sealed class RulesRetrievalTests
{
    /// <summary>Every evaluation case name.</summary>
    /// <returns>One row per case, holding the case name.</returns>
    public static IEnumerable<object[]> CaseNames() =>
        Cases().Targets.Keys.Order(StringComparer.Ordinal).Select(name => new object[] { name }).ToList();

    /// <summary>The agreed provisions are all returned and precision meets 0.8.</summary>
    /// <param name="name">The evaluation case name.</param>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void RulesRetrievalMeetsPrecisionTarget(string name)
    {
        var evaluation = Cases();
        var agreed = evaluation.Agreed[name];
        var target = evaluation.Targets[name];
        var root = evaluation.Roots[name];
        var result = TestCli.Run("rules", "--root", root, target);

        Assert.Equal(0, result.Status);

        var lines = result.Output.Split('\n').Select(line => line.Trim()).ToList();
        var returned = lines
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..(line.IndexOf(':') > 2 ? line.IndexOf(':') : line.Length)].Trim())
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var collapsed = lines
            .Where(line => line.Contains(" provisions about the record;", StringComparison.Ordinal) && line.Contains(':'))
            .Select(line => line[..line.IndexOf(':')].Trim())
            .Where(id => id.Length > 0 && id.Contains('/'))
            .ToHashSet(StringComparer.Ordinal);
        var fields = ProvisionFields();
        var missing = agreed
            .Where(id => !returned.Contains(id))
            .Where(id => !CoveredByCollapse(id, collapsed, fields))
            .Where(id => !HasWhen(id, fields))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var agreedCount = returned.Count(returnedId => agreed.Contains(returnedId));
        var precision = returned.Count == 0 ? 1.0 : (double)agreedCount / returned.Count;

        Assert.True(
            missing.Count == 0,
            $"Case '{name}' misses {missing.Count} agreed provisions: {string.Join(", ", missing.Take(10))}.");
        Assert.True(
            precision >= 0.8,
            $"Case '{name}' precision is {precision:0.00}: {agreedCount} of {returned.Count} returned provisions are agreed.");
    }

    /// <summary>Whether a collapsed record line covers the agreed provision through its artifact.</summary>
    /// <param name="id">The agreed provision identifier.</param>
    /// <param name="collapsed">The collapsed record identifiers.</param>
    /// <param name="fields">The provision artifacts and conditions by identifier.</param>
    /// <returns>True when a collapsed record holds the provision's artifact.</returns>
    static bool CoveredByCollapse(string id, HashSet<string> collapsed, Dictionary<string, (string Artifact, string When)> fields)
    {
        if (!fields.TryGetValue(id, out var found) || found.Artifact.Length == 0 || collapsed.Count == 0)
        {
            return false;
        }

        var artifacts = found.Artifact
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Trim())
            .Select(value => value.StartsWith("record:", StringComparison.OrdinalIgnoreCase) ? value["record:".Length..].Trim() : value)
            .Select(value => value.ToLowerInvariant())
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var record in collapsed)
        {
            var slash = record.IndexOf('/');

            if (slash <= 0)
            {
                continue;
            }

            var type = record[..slash].ToLowerInvariant();
            string[] candidates = type == "event" ? [type, "domain-event"] : [type];

            if (candidates.Any(candidate => artifacts.Contains(candidate)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the provision carries an applicability condition, so a code path may leave it out.</summary>
    /// <param name="id">The provision identifier.</param>
    /// <param name="fields">The provision artifacts and conditions by identifier.</param>
    /// <returns>True when the provision states a condition.</returns>
    static bool HasWhen(string id, Dictionary<string, (string Artifact, string When)> fields) =>
        fields.TryGetValue(id, out var found) && found.When.Length > 0;

    /// <summary>The provision artifacts and conditions by identifier, from the carried provisions file.</summary>
    /// <returns>The artifact and condition of each provision.</returns>
    static Dictionary<string, (string Artifact, string When)> ProvisionFields()
    {
        var file = Path.Combine(TestCli.Repository, "docs", "reference", "provisions.json");
        var fields = new Dictionary<string, (string Artifact, string When)>(StringComparer.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var root = document.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray()
            : root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("provisions", out var list)
                && list.ValueKind == JsonValueKind.Array
                    ? list.EnumerateArray()
                    : Enumerable.Empty<JsonElement>();

        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = Field(item, "id");

            if (id.Length > 0 && !fields.ContainsKey(id))
            {
                fields[id] = (Field(item, "artifact"), Field(item, "when"));
            }
        }

        return fields;
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Field(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>One loaded evaluation: agreed sets, targets and roots by case name.</summary>
    sealed record Evaluation(
        Dictionary<string, HashSet<string>> Agreed,
        Dictionary<string, string> Targets,
        Dictionary<string, string> Roots);

    /// <summary>Loads the evaluation sets and cases from the rules-eval fixtures.</summary>
    /// <returns>The loaded evaluation.</returns>
    static Evaluation Cases()
    {
        var sets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var file in new[] { "scopes-small.json", "scopes-records.json", "scopes-frontend.json", "scopes-artifacts.json" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(TestCli.Fixture($"rules-eval/{file}")));

            foreach (var set in document.RootElement.GetProperty("sets").EnumerateObject())
            {
                sets[set.Name] = set.Value.EnumerateArray()
                    .Select(entry => entry.GetString() ?? "")
                    .Where(id => id.Length > 0)
                    .ToHashSet(StringComparer.Ordinal);
            }
        }

        var agreed = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var targets = new Dictionary<string, string>(StringComparer.Ordinal);
        var roots = new Dictionary<string, string>(StringComparer.Ordinal);

        using var cases = JsonDocument.Parse(File.ReadAllText(TestCli.Fixture("rules-eval/cases.json")));

        foreach (var entry in cases.RootElement.GetProperty("cases").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!;
            var union = new HashSet<string>(StringComparer.Ordinal);

            foreach (var set in entry.GetProperty("sets").EnumerateArray())
            {
                union.UnionWith(sets[set.GetString()!]);
            }

            if (entry.TryGetProperty("adds", out var adds))
            {
                foreach (var id in adds.EnumerateArray())
                {
                    union.Add(id.GetString()!);
                }
            }

            if (entry.TryGetProperty("removes", out var removes))
            {
                foreach (var id in removes.EnumerateArray())
                {
                    union.Remove(id.GetString()!);
                }
            }

            agreed[name] = union;
            targets[name] = entry.GetProperty("target").GetString()!;
            roots[name] = entry.GetProperty("root").GetString()! switch
            {
                "claimwick" => Path.Combine(TestCli.Repository, "examples", "claimwick"),
                "rules-basic" => TestCli.Fixture("rules-basic"),
                "rules-layered" => TestCli.Fixture("rules-layered"),
                var other => throw new InvalidOperationException($"Unknown evaluation root '{other}'."),
            };
        }

        return new Evaluation(agreed, targets, roots);
    }
}
