using System.Globalization;
using System.Text.RegularExpressions;
using LiteSpec.Maintain.Validate.Pages;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Validate;

/// <summary>How the validators read page text: lines, fence-stripped lines, line numbers, provision citations and heading verbs, and non-ASCII characters with their ASCII wording.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class PageText
{
    /// <summary>The heading that routes a reader at the documentation root.</summary>
    public const string RoutingHeading = "Which Page Do You Want";

    // Every provision identifier is litespec/<page>#<heading-slug>, computed from
    // the owning page path and the heading. The grammar lives in Provisions, so the
    // validator, the generator, and the parsed heading cannot report three different
    // formats.
    /// <summary>Gets the provision identifier source pattern with forward slashes.</summary>
    public static string IdPattern => Provisions.IdSource.Replace("\\/", "/", StringComparison.Ordinal);

    /// <summary>Gets the pattern that finds a provision citation in prose.</summary>
    public static readonly Regex CitationPattern = new($"\\b(?:{IdPattern})\\b", RegexOptions.Compiled);

    /// <summary>Gets the action verbs a provision heading may start with.</summary>
    public static readonly HashSet<string> ActionVerbs = new(
        ("Accept Activate Add Observe Seed Advance Describe Order Prefetch Reuse Sanitize Share Take Align Anchor Announce Answer Apply Approve Ask Assert Assign Attach Audit " +
        "Authenticate Author Authorize Avoid Await Back Bind Bound Build Call Canonicalize Centralize Check Cite Claim Classify " +
        "Co-locate Collect Commit Compare Complete Compose Compute Configure Confirm Connect Control Copy Cover Create " +
        "Declare Default Defer Define Delay Deliver Depart Deploy Deprecate Derive Diff Discard Dispatch Distinguish Document " +
        "Drive Emit End Enforce Escalate Escape Evolve Exclude Exercise Explain Expose Express Finish Follow Format Gate Generate " +
        "Give Govern Grant Group Handle Hide Hold Identify Illustrate Implement Inject Inspect Isolate Keep Lease Limit List Load " +
        "Localize Locate Make Map Mark Match Measure Meet Minimize Mirror Model Move Name Note Operate Organize Own " +
        "Parameterize Parse Pass Persist Pin Place Plan Point Prefer Preserve Prevent Process Project Promote Protect " +
        "Prove Provide Publish Purge Query Raise Reach Read Reconnect Record Recover Reference Reflect Refuse Regenerate " +
        "Register Reject Reload Release Remove Render Replay Replace Report Represent Require Renew Requeue Resolve " +
        "Restrict Retain Retire Retry Return Revalidate Review Rotate Route Run Scan Scope Select Separate Serialize Set Shape " +
        "Show Signal Simulate Specify Split Stage Start State Stop Store Subscribe Supply Support Tabulate Tag Test Tolerate Trace " +
        "Track Translate Treat Trigger Update Use Validate Verify Version Wait Write " +
        "Admit Comment Grade Open Perform Put Advise Carry Close Confine Draw Enter File Flag Freeze Guard Honor Host Issue Justify Leave Pause Qualify Request Restore Resume Rewrite Sample Scaffold Schedule Search Send Serve Trust").Split(' '),
        StringComparer.Ordinal);

    // Naming the codepoint tells an author which character to hunt for. Naming the
    // ASCII form tells them what to type instead, which is the action the rule
    // actually asks for. These are the characters an editor substitution or a paste
    // from a word processor introduces; anything else reports its codepoint alone.
    // The keys are codepoints rather than characters, because this file is itself
    // under the ASCII rule.
    /// <summary>Gets the ASCII replacement wording for each common non-ASCII codepoint, keyed by codepoint.</summary>
    private static readonly Dictionary<string, string> AsciiReplacements = new(StringComparer.Ordinal)
    {
        ["00A0"] = "a plain space",
        ["00B7"] = "a hyphen",
        ["00D7"] = "the letter x",
        ["2013"] = "a hyphen",
        ["2014"] = "a hyphen",
        ["2018"] = "a straight apostrophe",
        ["2019"] = "a straight apostrophe",
        ["201C"] = "a straight quotation mark",
        ["201D"] = "a straight quotation mark",
        ["2022"] = "a Markdown list marker",
        ["2026"] = "three full stops",
        ["2192"] = "the word to",
    };

    /// <summary>The compiled pattern that splits text into lines.</summary>
    private static readonly Regex NewlineRegex = new(@"\r?\n", RegexOptions.Compiled);

    /// <summary>Splits text into lines on a carriage-return or line-feed boundary.</summary>
    /// <param name="text">The text to split.</param>
    /// <returns>The lines of the text.</returns>
    public static string[] SplitLines(string text) => NewlineRegex.Split(text);

    /// <summary>Counts the 1-based line number of a character offset in text.</summary>
    /// <param name="text">The text to scan.</param>
    /// <param name="offset">The character offset.</param>
    /// <returns>The 1-based line number.</returns>
    public static int LineNumber(string text, int offset)
    {
        var line = 1;
        for (var index = 0; index < offset && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>Finds the offset of the first non-ASCII character in text, or -1 when there is none.</summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>The first non-ASCII offset, or -1.</returns>
    public static int NonAsciiOffset(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] > 0x7F)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Builds the message that names a non-ASCII character and its ASCII replacement.</summary>
    /// <param name="character">The non-ASCII character.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <returns>The diagnostic message.</returns>
    public static string NonAsciiMessage(char character, string prefix)
    {
        var codepoint = ((int)character).ToString("X", CultureInfo.InvariantCulture).PadLeft(4, '0');
        return AsciiReplacements.TryGetValue(codepoint, out var replacement)
            ? $"{prefix}U+{codepoint}; write {replacement}"
            : $"{prefix}U+{codepoint}";
    }

    // A fenced block is literal content, so the measures do not reach inside one.
    // The line is replaced rather than dropped, which keeps every reported line
    // number equal to the line number in the file.
    /// <summary>Builds the fence-stripped lines of a page.</summary>
    /// <param name="reader">The parsed page.</param>
    /// <returns>The fence-stripped lines.</returns>
    public static List<PageLine> CleanLines(PageReader reader)
    {
        var result = new List<PageLine>(reader.Lines.Length);
        for (var index = 0; index < reader.Lines.Length; index++)
        {
            var number = index + 1;
            var fenced = reader.FencedLines.Contains(number);
            var generated = reader.GeneratedLines.Contains(number);
            result.Add(new PageLine(fenced || generated ? "" : reader.Lines[index], number, fenced));
        }

        return result;
    }
}
