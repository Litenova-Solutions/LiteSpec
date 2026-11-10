using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     Partitions findings from departures, shared by every check host.
/// </summary>
/// <follows>litespec/core/agent#apply-guidance-precedence</follows>
public static class Suppression
{
    /// <summary>
    ///     Splits findings into reported and covered. A departure holds the findings it covers on the
    ///     subjects and paths it names. It covers a finding code it names, and every code the provision
    ///     or page it names governs through <paramref name="codesOfProvision" />, except the codes the
    ///     model needs in order to load. A departure without an expiry date holds until it is changed;
    ///     one with an expiry date holds until that day. A departure naming a use case also covers
    ///     findings about the scenarios and results it holds. A departure finding is never held.
    /// </summary>
    /// <param name="findings">Every finding, in report order.</param>
    /// <param name="records">Every record.</param>
    /// <param name="root">The project root.</param>
    /// <param name="today">Today's date.</param>
    /// <param name="useCaseOfHome">The id of the use case a home names, when one holds it.</param>
    /// <param name="codesOfProvision">The finding codes a provision governs, by provision id; a departure covers nothing without it.</param>
    /// <returns>The reported findings and the covered ones, each in report order.</returns>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Covered) Partition(
        IReadOnlyList<Finding> findings,
        IReadOnlyList<IRecordFacts> records,
        string root,
        DateTime today,
        Func<string, string?> useCaseOfHome,
        Func<string, IReadOnlyList<string>>? codesOfProvision = null)
    {
        // A departure holds the findings it covers on the subjects and paths it names, while
        // it has no expiry date or its expiry date has not passed.
        var live = records
            .Where(record => record.Type == "departure" && Live(record, today))
            .Select(departure => new LiveDeparture(departure, codesOfProvision))
            .ToList();
        var kept = new List<Finding>();
        var covered = new List<Finding>();

        foreach (var finding in findings)
        {
            if (finding.Code is "departure-expired" or "departure-invalid")
            {
                kept.Add(finding);
                continue;
            }

            // A departure naming a use case also covers findings about the scenarios and results it holds.
            var subjects = new List<string>();

            if (finding.Subject is not null)
            {
                subjects.Add(finding.Subject);

                if ((finding.Subject.StartsWith("scenario/", StringComparison.Ordinal) || finding.Subject.StartsWith("result/", StringComparison.Ordinal))
                    && Identifiers.TrySplit(finding.Subject, out _, out var home, out _)
                    && home is not null
                    && useCaseOfHome(home) is string holder)
                {
                    subjects.Add(holder);
                }
            }

            var relative = RootRelative(root, finding.Path);
            var isCovered = live.Any(departure =>
                departure.Codes.Contains(finding.Code)
                && (subjects.Any(subject => departure.Subjects.Contains(subject))
                    || departure.Paths.Any(pattern => GlobMatches(pattern, relative))));

            (isCovered ? covered : kept).Add(finding);
        }

        return (kept, covered);
    }

    /// <summary>Whether a departure is live on the day: it has no expiry date, or its expiry date has not passed.</summary>
    /// <param name="departure">The departure record.</param>
    /// <param name="today">Today's date.</param>
    /// <returns>True when the departure holds findings today.</returns>
    private static bool Live(IRecordFacts departure, DateTime today) =>
        departure.Field("expiresOn") is not string expiresOn
        || (IsoDate(expiresOn, out var expires) && today <= expires);

    /// <summary>A live departure, with the finding codes, subjects and paths it holds.</summary>
    private sealed class LiveDeparture
    {
        /// <summary>Initializes a new instance of the <see cref="LiveDeparture"/> class.</summary>
        /// <param name="departure">The departure record.</param>
        /// <param name="codesOfProvision">The finding codes a provision or page governs, by identifier.</param>
        public LiveDeparture(IRecordFacts departure, Func<string, IReadOnlyList<string>>? codesOfProvision)
        {
            Subjects = new HashSet<string>(departure.Ids("subject"), StringComparer.Ordinal);
            Paths = departure.Ids("paths");
            Codes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var covered in departure.Ids("covers"))
            {
                if (FindingCodes.IsKnown(covered))
                {
                    Codes.Add(covered);
                }

                if (codesOfProvision is not null)
                {
                    foreach (var governed in codesOfProvision(covered))
                    {
                        Codes.Add(governed);
                    }
                }
            }

            Codes.ExceptWith(FindingCodes.Load);
        }

        /// <summary>Gets the subjects the departure holds.</summary>
        public HashSet<string> Subjects { get; }

        /// <summary>Gets the path patterns the departure holds.</summary>
        public IReadOnlyList<string> Paths { get; }

        /// <summary>Gets the finding codes the departure holds.</summary>
        public HashSet<string> Codes { get; }
    }

    /// <summary>
    ///     Whether the glob matches the forward-slash path from the project root:
    ///     <c>**</c> spans folders, <c>*</c> and <c>?</c> stay inside one segment,
    ///     and a trailing slash holds the whole folder.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="path">The forward-slash path from the project root.</param>
    /// <returns>True when the glob matches the path.</returns>
    private static bool GlobMatches(string pattern, string path)
    {
        var glob = pattern.Replace('\\', '/');

        if (glob.StartsWith("/", StringComparison.Ordinal))
        {
            glob = glob.Substring(1);
        }

        if (glob.EndsWith("/", StringComparison.Ordinal))
        {
            glob += "**";
        }

        return GlobSegments(
            glob.Split(['/'], StringSplitOptions.None),
            0,
            path.Split(['/'], StringSplitOptions.None),
            0);
    }

    /// <summary>Whether the trailing pattern segments match the trailing path segments.</summary>
    /// <param name="pattern">The glob segments.</param>
    /// <param name="nextPattern">The next glob segment to match.</param>
    /// <param name="path">The path segments.</param>
    /// <param name="nextPath">The next path segment to match.</param>
    /// <returns>True when the trailing segments match.</returns>
    private static bool GlobSegments(string[] pattern, int nextPattern, string[] path, int nextPath)
    {
        while (nextPattern < pattern.Length && nextPath <= path.Length)
        {
            if (string.Equals(pattern[nextPattern], "**", StringComparison.Ordinal))
            {
                for (var skip = nextPath; skip <= path.Length; skip++)
                {
                    if (GlobSegments(pattern, nextPattern + 1, path, skip))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (nextPath == path.Length || !GlobSegment(pattern[nextPattern], path[nextPath]))
            {
                return false;
            }

            nextPattern++;
            nextPath++;
        }

        while (nextPattern < pattern.Length && string.Equals(pattern[nextPattern], "**", StringComparison.Ordinal))
        {
            nextPattern++;
        }

        return nextPattern == pattern.Length && nextPath == path.Length;
    }

    /// <summary>Whether the segment pattern matches the path segment: <c>*</c> spans, <c>?</c> takes one.</summary>
    /// <param name="pattern">The segment pattern.</param>
    /// <param name="text">The path segment.</param>
    /// <returns>True when the pattern matches the segment.</returns>
    private static bool GlobSegment(string pattern, string text)
    {
        var nextPattern = 0;
        var nextText = 0;
        var star = -1;
        var mark = 0;

        while (nextText < text.Length)
        {
            if (nextPattern < pattern.Length
                && (pattern[nextPattern] == '?' || pattern[nextPattern] == text[nextText]))
            {
                nextPattern++;
                nextText++;
            }
            else if (nextPattern < pattern.Length && pattern[nextPattern] == '*')
            {
                star = nextPattern;
                mark = nextText;
                nextPattern++;
            }
            else if (star >= 0)
            {
                mark++;
                nextText = mark;
                nextPattern = star + 1;
            }
            else
            {
                return false;
            }
        }

        while (nextPattern < pattern.Length && pattern[nextPattern] == '*')
        {
            nextPattern++;
        }

        return nextPattern == pattern.Length;
    }

    /// <summary>A finding's file as a forward-slash path from the project root.</summary>
    /// <param name="root">The project root.</param>
    /// <param name="path">The finding's file path.</param>
    /// <returns>The forward-slash path from the project root.</returns>
    private static string RootRelative(string root, string path)
    {
        var relative = Path.IsPathRooted(path) ? RelativeTo(root, path) : path;

        return relative.Replace('\\', '/');
    }

    /// <summary>
    ///     The path from one folder to another, with forward slashes: dot when both name
    ///     the same folder, the path itself when they share no root.
    /// </summary>
    /// <param name="root">The folder to read from.</param>
    /// <param name="path">The path to read.</param>
    /// <returns>The path from the root folder, with forward slashes.</returns>
    private static string RelativeTo(string root, string path)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var from = root.Replace('\\', '/').TrimEnd('/');
        var to = path.Replace('\\', '/');

        if (!string.Equals(RootName(from), RootName(to), comparison))
        {
            return path;
        }

        var fromSegments = from.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var toSegments = to.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var common = 0;

        while (common < fromSegments.Length
            && common < toSegments.Length
            && string.Equals(fromSegments[common], toSegments[common], comparison))
        {
            common++;
        }

        var parts = new List<string>();

        for (var index = common; index < fromSegments.Length; index++)
        {
            parts.Add("..");
        }

        for (var index = common; index < toSegments.Length; index++)
        {
            parts.Add(toSegments[index]);
        }

        return parts.Count == 0 ? "." : string.Join("/", parts);
    }

    /// <summary>The root two paths must share: a drive, a UNC share, or the filesystem root.</summary>
    /// <param name="value">The path to read.</param>
    /// <returns>The root the path starts from.</returns>
    private static string RootName(string value)
    {
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            var segments = value.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

            return segments.Length >= 2
                ? "//" + segments[0] + "/" + segments[1]
                : value;
        }

        if (value.Length >= 2 && value[1] == ':')
        {
            return value.Substring(0, 2);
        }

        return value.StartsWith("/", StringComparison.Ordinal) ? "/" : "";
    }

    // Record dates are ISO 8601, so they parse the same under every culture.
    /// <summary>Tries to read an ISO date.</summary>
    /// <param name="text">The date text.</param>
    /// <param name="date">The date read, when one reads.</param>
    /// <returns>True when the text is an ISO date.</returns>
    private static bool IsoDate(string text, out DateTime date) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
