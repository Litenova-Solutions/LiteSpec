using System.Text.Json;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The Decided group of <c>litespec rules</c> on a code path: each decision the file
///     follows with its choice and reopen condition, then each departure covering a provision
///     in the result with its reason and, for a gap, its expiry date.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class RulesDecided
{
    /// <summary>One followed decision with the fields the Decided group prints.</summary>
    /// <param name="Id">The decision identifier.</param>
    /// <param name="Choice">The chosen option, or empty when the record names none.</param>
    /// <param name="ReopenedWhen">What would reopen the choice, or empty when the record names none.</param>
    internal sealed record DecidedDecision(string Id, string Choice, string ReopenedWhen);

    /// <summary>Loads the decisions the target marks, in marked order.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="target">The rules target with its marked identifiers.</param>
    /// <returns>The fields of each marked decision found, in marked order.</returns>
    public static IReadOnlyList<DecidedDecision> LoadDecisions(ConsumerConfig config, PathContext target)
    {
        var wanted = target.Marked
            .Where(marked => marked.StartsWith("decision/", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (wanted.Count == 0)
        {
            return [];
        }

        var found = new Dictionary<string, DecidedDecision>(StringComparer.Ordinal);

        foreach (var root in config.RecordRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.OrderBy(name => name, StringComparer.Ordinal))
            {
                string text;

                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (!RecordFile.TryParse(text, out var parsed, out _, out _)
                    || parsed.ValueKind != JsonValueKind.Object
                    || !string.Equals(Text(parsed, "type"), "decision", StringComparison.Ordinal))
                {
                    continue;
                }

                var id = Text(parsed, "id");

                if (id.Length > 0 && wanted.Contains(id, StringComparer.Ordinal) && !found.ContainsKey(id))
                {
                    found[id] = new DecidedDecision(id, Text(parsed, "choice"), Text(parsed, "reopenedWhen"));
                }

                if (found.Count == wanted.Count)
                {
                    break;
                }
            }

            if (found.Count == wanted.Count)
            {
                break;
            }
        }

        return wanted.Where(found.ContainsKey).Select(id => found[id]).ToList();
    }

    /// <summary>Prints the Decided group: each followed decision, then each covering departure.</summary>
    /// <param name="decisions">The followed decisions in marked order.</param>
    /// <param name="results">The retrieved provisions with their covering departures.</param>
    public static void Print(IReadOnlyList<DecidedDecision> decisions, IReadOnlyList<ProvisionMatch> results)
    {
        var departures = new List<DepartureFiles.Departure>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var match in results)
        {
            if (match.Departure is { } departure && seen.Add(departure.Id))
            {
                departures.Add(departure);
            }
        }

        departures.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));

        if (decisions.Count == 0 && departures.Count == 0)
        {
            return;
        }

        CliHost.Out.WriteLine("Decided:");

        foreach (var decision in decisions)
        {
            var display = DecisionDisplay(decision);
            CliHost.Out.WriteLine(display.Length > 0 ? $"- {decision.Id}: {display}" : $"- {decision.Id}");
        }

        foreach (var departure in departures)
        {
            CliHost.Out.WriteLine(departure.ExpiresOn.Length > 0
                ? $"- {departure.Id}: {departure.Reason}; expires {departure.ExpiresOn}"
                : $"- {departure.Id}: {departure.Reason}");
        }
    }

    /// <summary>The display of one followed decision: its choice and reopen condition.</summary>
    /// <param name="decision">The followed decision.</param>
    /// <returns>The choice and reopen condition the record states, or empty when it states neither.</returns>
    static string DecisionDisplay(DecidedDecision decision)
    {
        var parts = new List<string>();

        if (decision.Choice.Length > 0)
        {
            parts.Add($"choice {decision.Choice}");
        }

        if (decision.ReopenedWhen.Length > 0)
        {
            parts.Add($"reopened when {decision.ReopenedWhen}");
        }

        return string.Join("; ", parts);
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
