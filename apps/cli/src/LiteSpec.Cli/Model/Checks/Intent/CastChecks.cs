using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>
///     The cast checks: a person's address sits on a reserved test domain, every cast entry is
///     cited by a record or a seeds mark, and every seeds mark names a cast entry.
/// </summary>
/// <follows>litespec/quality/development#seed-only-development-and-test-environments</follows>
/// <follows>litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed</follows>
internal static class CastChecks
{
    /// <summary>The domains RFC 2606 reserves for documentation and examples.</summary>
    private static readonly HashSet<string> ReservedDomains = new(StringComparer.Ordinal)
    {
        "example.com", "example.net", "example.org",
    };

    /// <summary>The top-level suffixes RFC 2606 and RFC 6761 reserve for testing, which every name under them keeps.</summary>
    private static readonly string[] ReservedSuffixes = [".example", ".test", ".invalid", ".localhost"];

    /// <summary>Runs every cast check.</summary>
    /// <param name="model">The loaded project model that holds every record and seeds mark.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckIdentities(model, findings);
        CheckEntriesUsed(model, findings);
        CheckSeedMarks(model, findings);
    }

    /// <summary>Whether an email domain is one the reserved test domains cover.</summary>
    /// <param name="domain">The domain after the at sign.</param>
    /// <returns>True for example.com, example.net, example.org, and any name under .example, .test, .invalid or .localhost.</returns>
    internal static bool IsReservedDomain(string domain)
    {
        var lower = domain.ToLowerInvariant();

        return ReservedDomains.Contains(lower) || ReservedSuffixes.Any(suffix => lower.EndsWith(suffix, StringComparison.Ordinal));
    }

    /// <summary>Reports each cast person whose email sits on a domain that no reserved test domain covers.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIdentities(ProjectModel model, List<Finding> findings)
    {
        foreach (var person in model.Records.Where(record => record.Type == RecordTypes.Person && !record.Derived))
        {
            if (person.Field(RecordFields.Email) is not { } email || email.LastIndexOf('@') < 0)
            {
                continue;
            }

            var domain = email[(email.LastIndexOf('@') + 1)..];

            if (!IsReservedDomain(domain))
            {
                findings.Add(new Finding(
                    "cast-identity-unreserved",
                    $"Person '{person.Id}' has the email domain '{domain}', which is no reserved test domain.",
                    person.File, person.Line, SubjectOf(person)));
            }
        }
    }

    /// <summary>Reports each cast entry that no record field or seeds mark cites.</summary>
    /// <param name="model">The loaded project model that holds every record and seeds mark.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEntriesUsed(ProjectModel model, List<Finding> findings)
    {
        var entries = model.Records
            .Where(record => RecordTypes.CastTypes.Contains(record.Type) && record.Type != RecordTypes.Cast && !record.Derived)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var sources = Sources(model);
        var citers = CitersById(sources);
        var seeded = new HashSet<string>(model.Code.Seeds.Select(mark => mark.RecordId), StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (seeded.Contains(entry.Id) || CitedBy(entry, citers) || NamedBy(entry, sources))
            {
                continue;
            }

            findings.Add(new Finding(
                "cast-entry-unused",
                $"Cast entry '{entry.Id}' is cited by no record or code mark.",
                entry.File, entry.Line, SubjectOf(entry)));
        }
    }

    /// <summary>
    ///     Whether another source cites the entry by its identifier. A record's own text never cites it,
    ///     so a title that repeats the identifier proves no use.
    /// </summary>
    /// <param name="entry">The cast entry under test.</param>
    /// <param name="citers">The owners citing each identifier, from <see cref="CitersById" />.</param>
    /// <returns>True when a source other than the entry's own cites its identifier; otherwise false.</returns>
    private static bool CitedBy(Record entry, Dictionary<string, HashSet<string>> citers) =>
        citers.TryGetValue(entry.Id, out var owners) && owners.Any(owner => owner != entry.Id);

    /// <summary>
    ///     Reads every citation in the sources once: each cited identifier with the owners whose text cites it,
    ///     so each cast entry looks its citers up instead of scanning every text again.
    /// </summary>
    /// <param name="sources">The texts each owner wrote, as owner id and text.</param>
    /// <returns>The owners citing each identifier.</returns>
    private static Dictionary<string, HashSet<string>> CitersById(IReadOnlyList<(string Owner, string Text)> sources)
    {
        var citers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (owner, text) in sources)
        {
            foreach (Match match in Identifiers.Citation.Matches(text))
            {
                var id = Identifiers.TrimCitation(match.Value);

                if (!citers.TryGetValue(id, out var owners))
                {
                    owners = new HashSet<string>(StringComparer.Ordinal);
                    citers[id] = owners;
                }

                owners.Add(owner);
            }
        }

        return citers;
    }

    /// <summary>
    ///     Whether a person or party is named by its exact name in a source written by another record:
    ///     a word match, so <c>Ava</c> does not match inside <c>Avalon</c>.
    /// </summary>
    /// <param name="entry">The cast entry under test.</param>
    /// <param name="sources">The texts each owner wrote, as owner id and text.</param>
    /// <returns>True when a source other than the entry's own names it; otherwise false.</returns>
    private static bool NamedBy(Record entry, IReadOnlyList<(string Owner, string Text)> sources)
    {
        if (entry.Type is not (RecordTypes.Person or RecordTypes.Party) || entry.Field(RecordFields.Name) is not { Length: > 0 } name)
        {
            return false;
        }

        var word = new Regex($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(name)}(?![\p{{L}}\p{{N}}])", RegexOptions.CultureInvariant);

        // The substring test is cheap and rules out almost every text before the word match runs.
        return sources.Any(source => source.Owner != entry.Id
            && source.Text.Contains(name, StringComparison.Ordinal)
            && word.IsMatch(source.Text));
    }

    /// <summary>
    ///     Every text a record writes, each with its owner: a record's field values
    ///     except its identity. The cast's own record does not count.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The owner and text of each source.</returns>
    private static List<(string Owner, string Text)> Sources(ProjectModel model)
    {
        var sources = new List<(string Owner, string Text)>();

        foreach (var record in model.Records)
        {
            if (record.Raw.ValueKind != JsonValueKind.Object || record.Type == RecordTypes.Cast)
            {
                continue;
            }

            var texts = new List<string>();

            foreach (var property in record.Raw.EnumerateObject())
            {
                if (property.Name is RecordFields.Id or RecordFields.Type)
                {
                    continue;
                }

                CollectStrings(property.Value, texts);
            }

            sources.AddRange(texts.Select(text => (record.Id, text)));
        }

        return sources;
    }

    /// <summary>Reports each seeds mark that names no cast entry.</summary>
    /// <param name="model">The loaded project model that holds every cast entry and seeds mark.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSeedMarks(ProjectModel model, List<Finding> findings)
    {
        foreach (var mark in model.Code.Seeds)
        {
            if (!model.ById.TryGetValue(mark.RecordId, out var record) || !RecordTypes.CastTypes.Contains(record.Type) || record.Type == RecordTypes.Cast)
            {
                findings.Add(new Finding(
                    "mark-unknown",
                    $"Code seeds '{mark.RecordId}', which matches no party, person, occasion or figure in the cast.",
                    mark.File, mark.Line, mark.RecordId.Length > 0 ? mark.RecordId : null));
            }
        }
    }

    /// <summary>Collects every string value under a JSON node, in document order.</summary>
    /// <param name="node">The node to read.</param>
    /// <param name="texts">The list that receives each string.</param>
    private static void CollectStrings(JsonElement node, List<string> texts)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.String when node.GetString() is { } text:
                texts.Add(text);
                break;
            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                {
                    CollectStrings(item, texts);
                }

                break;
            case JsonValueKind.Object:
                foreach (var property in node.EnumerateObject())
                {
                    CollectStrings(property.Value, texts);
                }

                break;
        }
    }
}
