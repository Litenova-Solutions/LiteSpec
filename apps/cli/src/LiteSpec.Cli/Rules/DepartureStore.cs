using System.Globalization;
using System.Text.Json;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The departures: departure records under <c>.litespec/governance/departures/</c> whose
///     covers names the provisions, pages or finding codes they replace, with the subject records
///     or paths they replace them on. A live departure shows in place of the provisions it covers.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class DepartureStore
{
    /// <summary>One live departure.</summary>
    /// <param name="Id">The departure identifier.</param>
    /// <param name="Title">The departure title.</param>
    /// <param name="Covers">The provision identifiers, page identifiers and finding codes the departure names.</param>
    /// <param name="Subject">The record identifiers the departure covers.</param>
    /// <param name="Paths">The path globs the departure covers.</param>
    /// <param name="Reason">Why the departure stands.</param>
    /// <param name="Consequences">What the departure changes.</param>
    /// <param name="ExpiresOn">The expiry date text, or empty for a lasting departure.</param>
    internal sealed record Departure(
        string Id,
        string Title,
        IReadOnlyList<string> Covers,
        IReadOnlyList<string> Subject,
        IReadOnlyList<string> Paths,
        string Reason,
        string Consequences,
        string ExpiresOn);

    /// <summary>Loads every live departure under the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="today">Today's date: a departure past its expiry date no longer replaces.</param>
    /// <returns>The live departures in identifier order.</returns>
    public static IReadOnlyList<Departure> Load(ConsumerConfig config, DateOnly today)
    {
        var departures = new List<Departure>();

        foreach (var root in config.RecordRoots)
        {
            var folder = Path.Combine(root, "governance");

            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(folder, "*.yaml", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
            {
                var relative = PathText.Relative(folder, file);

                if (!relative.StartsWith("departures/", StringComparison.Ordinal)
                    && !relative.Contains("/departures/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Read(file, today) is { } departure)
                {
                    departures.Add(departure);
                }
            }
        }

        departures.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return departures;
    }

    /// <summary>Whether the departure names the provision: its identifier, its page, or a finding code the provision's checking line names.</summary>
    /// <param name="departure">The departure.</param>
    /// <param name="entry">The provision.</param>
    /// <returns>True when a covers item names the provision, its page or one of its checked codes.</returns>
    public static bool Names(Departure departure, ProvisionEntry entry)
    {
        var page = entry.Id.Split('#')[0];

        return departure.Covers.Any(cover =>
            string.Equals(cover, entry.Id, StringComparison.Ordinal)
            || string.Equals(cover, page, StringComparison.Ordinal)
            || entry.CheckedCodes.Contains(cover, StringComparer.Ordinal));
    }

    /// <summary>Whether the departure covers the target on its subject records or paths.</summary>
    /// <param name="departure">The departure.</param>
    /// <param name="target">The rules target.</param>
    /// <param name="candidates">The target's path spellings for glob matching.</param>
    /// <returns>True when a subject names the target or a marked record, or a path matches.</returns>
    public static bool Covers(Departure departure, PathContext target, IReadOnlyList<string> candidates)
    {
        foreach (var subject in departure.Subject)
        {
            if (target.IsRecord && string.Equals(subject, target.Display, StringComparison.Ordinal))
            {
                return true;
            }

            if (target.Marked.Any(marked => string.Equals(marked, subject, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return departure.Paths.Any(glob => candidates.Any(candidate => Glob.Matches(glob, candidate)));
    }

    /// <summary>Reads one departure file.</summary>
    /// <param name="file">The departure file.</param>
    /// <param name="today">Today's date.</param>
    /// <returns>The departure, or null when the file holds no live departure.</returns>
    static Departure? Read(string file, DateOnly today)
    {
        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (!RecordFile.TryParse(text, out var root, out _, out _))
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object || Text(root, "type") != "departure")
        {
            return null;
        }

        var id = Text(root, "id");
        var covers = Items(root, "covers");

        if (id.Length == 0 || covers.Count == 0)
        {
            return null;
        }

        var expiresOn = Text(root, "expiresOn");

        if (DateOnly.TryParseExact(expiresOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry) && expiry < today)
        {
            return null;
        }

        return new Departure(
            id,
            Text(root, "title"),
            covers,
            Items(root, "subject"),
            Items(root, "paths"),
            Text(root, "reason"),
            Text(root, "consequences"),
            expiresOn);
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>One string array property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string entries.</returns>
    static List<string> Items(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.String)
            .Select(entry => entry.GetString() ?? "")
            .Where(entry => entry.Length > 0)
            .ToList();
    }
}
