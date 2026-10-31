using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     Partitions findings from exceptions, shared by every check host.
/// </summary>
/// <follows>litespec/core/model#apply-guidance-precedence</follows>
public static class Suppression
{
    /// <summary>
    ///     Splits findings into reported and known. An approved exception inside its expiry date
    ///     holds the findings it covers on the subjects and paths it names: a gap names its
    ///     finding codes, and a departure names the provision whose codes it follows through
    ///     <paramref name="codesOfProvision" />. An exception naming a use case also covers
    ///     findings about the scenarios and results it holds. An expiry finding is never held.
    /// </summary>
    /// <param name="findings">Every finding, in report order.</param>
    /// <param name="records">Every record.</param>
    /// <param name="root">The project root.</param>
    /// <param name="today">Today's date.</param>
    /// <param name="useCaseOfHome">The id of the use case a home names, when one holds it.</param>
    /// <param name="codesOfProvision">The finding codes a provision governs, by provision id; a departure covers nothing without it.</param>
    /// <returns>The reported findings and the known ones, each in report order.</returns>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Partition(
        IReadOnlyList<Finding> findings,
        IReadOnlyList<IRecordFacts> records,
        string root,
        DateTime today,
        Func<string, string?> useCaseOfHome,
        Func<string, IReadOnlyList<string>>? codesOfProvision = null)
    {
        // An approved exception inside its expiry date holds the findings it covers on the
        // subjects and paths it names.
        var live = records
            .Where(record => record.Type == "exception"
                && record.Status == "approved"
                && record.Field("expiresOn") is string expiresOn
                && IsoDate(expiresOn, out var expires)
                && today <= expires)
            .Select(exception => new LiveException(exception, codesOfProvision))
            .ToList();
        var kept = new List<Finding>();
        var known = new List<Finding>();

        foreach (var finding in findings)
        {
            if (finding.Code == "exception-expired")
            {
                kept.Add(finding);
                continue;
            }

            // An exception naming a use case also covers findings about the scenarios and results it holds.
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
            var covered = live.Any(exception =>
                exception.Codes.Contains(finding.Code)
                && (subjects.Any(subject => exception.Subjects.Contains(subject))
                    || exception.Paths.Any(pattern => GlobMatches(pattern, relative))));

            (covered ? known : kept).Add(finding);
        }

        return (kept, known);
    }

    /// <summary>An approved exception inside its expiry date, with the codes, subjects and paths it holds.</summary>
    private sealed class LiveException
    {
        /// <summary>Initializes a new instance of the <see cref="LiveException"/> class.</summary>
        /// <param name="exception">The exception record.</param>
        /// <param name="codesOfProvision">The finding codes a provision governs, by provision id.</param>
        public LiveException(IRecordFacts exception, Func<string, IReadOnlyList<string>>? codesOfProvision)
        {
            Subjects = new HashSet<string>(exception.Ids("subject"), StringComparer.Ordinal);
            Paths = exception.Ids("paths");
            Codes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var covered in exception.Ids("covers"))
            {
                Codes.Add(covered);

                if (codesOfProvision is not null)
                {
                    foreach (var governed in codesOfProvision(covered))
                    {
                        Codes.Add(governed);
                    }
                }
            }
        }

        /// <summary>Gets the subjects the exception holds.</summary>
        public HashSet<string> Subjects { get; }

        /// <summary>Gets the path patterns the exception holds.</summary>
        public IReadOnlyList<string> Paths { get; }

        /// <summary>Gets the finding codes the exception holds.</summary>
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
