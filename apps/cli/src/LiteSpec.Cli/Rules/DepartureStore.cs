using System.Globalization;
using System.Text.Json;
using LiteSpec.Model;

namespace LiteSpec.Rules;

/// <summary>
///     The departure exceptions: exception records of kind <c>departure</c> whose
///     covers names the provision they replace, with the subject records or paths
///     they replace it on. A live departure shows in place of its provision.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class DepartureStore
{
    /// <summary>One live departure exception.</summary>
    /// <param name="Id">The exception identifier.</param>
    /// <param name="Title">The exception title.</param>
    /// <param name="Covers">The provision identifier the departure replaces.</param>
    /// <param name="Subject">The record identifiers the departure covers.</param>
    /// <param name="Paths">The path globs the departure covers.</param>
    /// <param name="Reason">Why the departure stands.</param>
    /// <param name="Consequences">What the departure changes.</param>
    /// <param name="ExpiresOn">The expiry date text.</param>
    internal sealed record Departure(
        string Id,
        string Title,
        string Covers,
        IReadOnlyList<string> Subject,
        IReadOnlyList<string> Paths,
        string Reason,
        string Consequences,
        string ExpiresOn);

    /// <summary>Loads every live departure under the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="today">Today's date: a departure past it no longer replaces.</param>
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

                if (!relative.StartsWith("exceptions/", StringComparison.Ordinal)
                    && !relative.Contains("/exceptions/", StringComparison.Ordinal))
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

    /// <summary>Whether the departure covers the target.</summary>
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

    /// <summary>Reads one exception file.</summary>
    /// <param name="file">The exception file.</param>
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

        if (root.ValueKind != JsonValueKind.Object
            || Text(root, "type") != "exception"
            || Text(root, "kind") != "departure")
        {
            return null;
        }

        var id = Text(root, "id");
        var covers = Text(root, "covers");

        if (id.Length == 0 || covers.Length == 0)
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
