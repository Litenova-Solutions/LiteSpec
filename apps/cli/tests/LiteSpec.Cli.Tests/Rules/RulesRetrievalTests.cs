using System.Text.Json;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>
///     The retrieval evaluation: each case names a real path or record id with the
///     provisions a reviewer agrees apply. Every agreed provision must be returned,
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

        var returned = result.Output
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..(line.IndexOf(':') > 2 ? line.IndexOf(':') : line.Length)].Trim())
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var missing = agreed.Where(id => !returned.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToList();
        var agreedCount = returned.Count(returnedId => agreed.Contains(returnedId));
        var precision = returned.Count == 0 ? 1.0 : (double)agreedCount / returned.Count;

        Assert.True(
            missing.Count == 0,
            $"Case '{name}' misses {missing.Count} agreed provisions: {string.Join(", ", missing.Take(10))}.");
        Assert.True(
            precision >= 0.8,
            $"Case '{name}' precision is {precision:0.00}: {agreedCount} of {returned.Count} returned provisions are agreed.");
    }

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
