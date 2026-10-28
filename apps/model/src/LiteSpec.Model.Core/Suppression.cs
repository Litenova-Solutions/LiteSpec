using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     Partitions findings from known gaps and deviations, shared by every check host.
/// </summary>
public static class Suppression
{
    /// <summary>
    ///     Splits findings into reported and known. An approved deviation inside its review date
    ///     holds the findings on the code files and the records it names. A gap naming a use case
    ///     also covers findings about the scenarios and results it holds.
    /// </summary>
    /// <param name="findings">Every finding, in report order.</param>
    /// <param name="records">Every record.</param>
    /// <param name="root">The project root.</param>
    /// <param name="today">Today's date.</param>
    /// <param name="useCaseOfHome">The id of the use case a home names, when one holds it.</param>
    /// <returns>The reported findings and the known ones, each in report order.</returns>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Partition(
        IReadOnlyList<Finding> findings,
        IReadOnlyList<IRecordFacts> records,
        string root,
        DateTime today,
        Func<string, string?> useCaseOfHome)
    {
        var gaps = records.Where(record => record.Type == "known-gap").ToList();

        // An approved deviation inside its review date holds the findings on the code files and the
        // records it names.
        var deviations = records
            .Where(record => record.Type == "deviation"
                && record.Status == "approved"
                && record.Field("reviewBy") is string reviewBy
                && IsoDate(reviewBy, out var review)
                && today <= review)
            .ToList();
        var heldFiles = new HashSet<string>(
            deviations
                .SelectMany(record => record.Ids("code"))
                .Select(path => path.Replace('\\', '/')),
            StringComparer.Ordinal);
        var heldSubjects = new HashSet<string>(
            deviations.SelectMany(record => record.Ids("subject")),
            StringComparer.Ordinal);
        var kept = new List<Finding>();
        var known = new List<Finding>();

        foreach (var finding in findings)
        {
            if (finding.Code != "known-gap-expired" && heldFiles.Contains(RootRelative(root, finding.Path)))
            {
                known.Add(finding);
                continue;
            }

            if (finding.Code == "known-gap-expired" || finding.Subject is null)
            {
                kept.Add(finding);
                continue;
            }

            // A gap naming a use case also covers findings about the scenarios and results it holds.
            var subjects = new List<string> { finding.Subject };

            if ((finding.Subject.StartsWith("scenario/", StringComparison.Ordinal) || finding.Subject.StartsWith("result/", StringComparison.Ordinal))
                && Identifiers.TrySplit(finding.Subject, out _, out var home, out _)
                && home is not null
                && useCaseOfHome(home) is string holder)
            {
                subjects.Add(holder);
            }

            var covered = subjects.Any(heldSubjects.Contains) || gaps.Any(gap =>
                gap.Field("check") == finding.Code
                && gap.Ids("subject").Any(subject => subjects.Contains(subject, StringComparer.Ordinal))
                && gap.Status != "retired"
                && gap.Field("endsBy") is string endsBy
                && IsoDate(endsBy, out var ends)
                && today <= ends);

            (covered ? known : kept).Add(finding);
        }

        return (kept, known);
    }

    /// <summary>A finding's file as a forward-slash path from the project root.</summary>
    private static string RootRelative(string root, string path)
    {
        var relative = Path.IsPathRooted(path) ? RelativeTo(root, path) : path;

        return relative.Replace('\\', '/');
    }

    /// <summary>
    ///     The path from one folder to another, with forward slashes: dot when both name
    ///     the same folder, the path itself when they share no root.
    /// </summary>
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

        var fromSegments = from.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var toSegments = to.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
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
    private static string RootName(string value)
    {
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            var segments = value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

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
    private static bool IsoDate(string text, out DateTime date) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
