using System.Globalization;
using System.Text.RegularExpressions;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>Reads prose and dates the checks judge: code spans in backticks, ISO durations and dates, and line offsets.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class ProseText
{
    /// <summary>Text in backticks, which holds code rather than prose.</summary>
    internal static readonly Regex BacktickSpan = new("`[^`]*`", RegexOptions.Compiled);

    /// <summary>An ISO 8601 duration, as the failure policy delays and retention use.</summary>
    internal static readonly Regex IsoDuration = new(
        @"^P(?=.*\d)(?:\d+Y)?(?:\d+M)?(?:\d+W)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?$",
        RegexOptions.CultureInvariant);

    /// <summary>Parses an exact ISO calendar date, independent of the current culture.</summary>
    /// <param name="text">The date text in year-month-day form.</param>
    /// <param name="date">The parsed date, or the default date when the text is no exact ISO date.</param>
    /// <returns>True when the text is an exact ISO date; otherwise false.</returns>
    internal static bool IsoDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>The body of one H2 section of an explanation, without its heading.</summary>
    /// <param name="text">The explanation file text.</param>
    /// <param name="heading">The section heading text.</param>
    /// <returns>The lines of the section up to the next H2, or null when the section is absent.</returns>
    internal static string? SectionOf(string text, string heading)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var start = Array.FindIndex(lines, line => line.Trim() == "## " + heading);

        if (start < 0)
        {
            return null;
        }

        var end = Array.FindIndex(lines, start + 1, line => line.StartsWith("## ", StringComparison.Ordinal));

        return string.Join('\n', lines[(start + 1)..(end < 0 ? lines.Length : end)]);
    }

    /// <summary>Every line's start offset, for offset-to-line reads.</summary>
    /// <param name="text">The text whose line starts are indexed.</param>
    /// <returns>The offset of each line start, beginning with zero.</returns>
    internal static int[] LineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                starts.Add(index + 1);
            }
        }

        return [.. starts];
    }

    /// <summary>The 1-based line holding the offset.</summary>
    /// <param name="starts">The line start offsets produced for the same text.</param>
    /// <param name="offset">The character offset whose line is wanted.</param>
    /// <returns>The one-based line number that holds the offset.</returns>
    internal static int LineOf(int[] starts, int offset)
    {
        var found = Array.BinarySearch(starts, offset);

        return found >= 0 ? found + 1 : ~found;
    }
}
