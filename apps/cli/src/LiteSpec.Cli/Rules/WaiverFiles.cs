using System.Globalization;
using System.Text.Json;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The waivers: waiver records under <c>.litespec/governance/waivers/</c> whose
///     covers names the provisions, pages or finding codes they replace, with the subject records
///     or paths they replace them on. A live waiver shows in place of the provisions it covers.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class WaiverFiles
{
    /// <summary>One live waiver.</summary>
    /// <param name="Id">The waiver identifier.</param>
    /// <param name="Title">The waiver title.</param>
    /// <param name="Covers">The provision identifiers, page identifiers and finding codes the waiver names.</param>
    /// <param name="Subject">The record identifiers the waiver covers.</param>
    /// <param name="Paths">The path globs the waiver covers.</param>
    /// <param name="Reason">Why the waiver stands.</param>
    /// <param name="Consequences">What the waiver changes.</param>
    /// <param name="ExpiresOn">The expiry date text, or empty for a lasting waiver.</param>
    internal sealed record Waiver(
        string Id,
        string Title,
        IReadOnlyList<string> Covers,
        IReadOnlyList<string> Subject,
        IReadOnlyList<string> Paths,
        string Reason,
        string Consequences,
        string ExpiresOn);

    /// <summary>Loads every live waiver under the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="today">Today's date: a waiver past its expiry date no longer replaces.</param>
    /// <returns>The live waivers in identifier order.</returns>
    public static IReadOnlyList<Waiver> Load(ConsumerConfig config, DateOnly today)
    {
        var waivers = new List<Waiver>();

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

                if (!relative.StartsWith("waivers/", StringComparison.Ordinal)
                    && !relative.Contains("/waivers/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Read(file, today) is { } waiver)
                {
                    waivers.Add(waiver);
                }
            }
        }

        waivers.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return waivers;
    }

    /// <summary>Whether the waiver names the provision: its identifier, its page, or a finding code the provision's checking line names.</summary>
    /// <param name="waiver">The waiver.</param>
    /// <param name="entry">The provision.</param>
    /// <returns>True when a covers item names the provision, its page or one of its checked codes.</returns>
    public static bool Names(Waiver waiver, ProvisionEntry entry)
    {
        var page = entry.Id.Split('#')[0];

        return waiver.Covers.Any(cover =>
            string.Equals(cover, entry.Id, StringComparison.Ordinal)
            || string.Equals(cover, page, StringComparison.Ordinal)
            || entry.CheckedCodes.Contains(cover, StringComparer.Ordinal));
    }

    /// <summary>Whether the waiver covers the target on its subject records or paths.</summary>
    /// <param name="waiver">The waiver.</param>
    /// <param name="target">The rules target.</param>
    /// <param name="candidates">The target's path spellings for glob matching.</param>
    /// <returns>True when a subject names the target or a marked record, or a path matches.</returns>
    public static bool Covers(Waiver waiver, PathContext target, IReadOnlyList<string> candidates)
    {
        foreach (var subject in waiver.Subject)
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

        return waiver.Paths.Any(glob => candidates.Any(candidate => Glob.Matches(glob, candidate)));
    }

    /// <summary>Reads one waiver file.</summary>
    /// <param name="file">The waiver file.</param>
    /// <param name="today">Today's date.</param>
    /// <returns>The waiver, or null when the file holds no live waiver.</returns>
    static Waiver? Read(string file, DateOnly today)
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

        if (root.ValueKind != JsonValueKind.Object || Text(root, "type") != "waiver")
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

        return new Waiver(
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
