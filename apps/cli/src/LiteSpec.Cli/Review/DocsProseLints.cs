namespace LiteSpec.Cli.Review;

/// <summary>
///     The prose lints of <c>litespec docs review</c>: paragraph length, banned
///     LiteSpec-ese and promotional phrases, and raw refusal codes shown outside
///     backticks. The lints read text only, so they call no model and no network.
/// </summary>
/// <follows>litespec/core/writing#write-human-docs-for-the-cold-reader</follows>
static class DocsProseLints
{
    /// <summary>How many sentences a Markdown paragraph may hold before the lint flags it.</summary>
    const int MaxSentencesPerParagraph = 4;

    /// <summary>How many characters a record value may hold before the lint flags it.</summary>
    const int MaxRecordValueLength = 240;

    /// <summary>Stilted LiteSpec-ese phrases the Style Card replaces with plain words.</summary>
    static readonly string[] LitespecEse =
    [
        "gains a home",
        "refuses other",
        "names no ",
        "homes of records",
        "holds the values",
        "unlock the power of",
        "seamlessly",
        "instantly",
        "effortlessly",
        "effortless",
        "streamlined",
        "rapidly evolving",
        "dive in",
        "facilitation of",
        "visibility into",
        "robust and comprehensive",
        "alignment between",
    ];

    /// <summary>Promotional phrases that state praise instead of observable behavior.</summary>
    static readonly string[] Promotional =
    [
        "cutting-edge",
        "next-generation",
        "world-class",
        "blazing",
        "state-of-the-art",
        "best-in-class",
        "game-changer",
        "powerful",
        "leverage",
        "synergy",
        "robust",
        "comprehensive",
    ];

    /// <summary>Refusal code endings: a hyphenated token ending here names a refusal code.</summary>
    static readonly HashSet<string> RefusalEndings = new(StringComparer.OrdinalIgnoreCase)
    {
        "missing",
        "invalid",
        "failed",
        "duplicate",
        "refused",
        "denied",
        "expired",
        "blocked",
        "conflict",
    };

    /// <summary>Lints one changed file with the prose lints for its extension.</summary>
    /// <param name="absolutePath">The absolute file path.</param>
    /// <param name="relativePath">The file path relative to the repository root, used in findings.</param>
    /// <returns>The findings in line order, or a single file-unreadable finding.</returns>
    public static IReadOnlyList<ProseLint> LintFile(string absolutePath, string relativePath)
    {
        string text;

        try
        {
            text = File.ReadAllText(absolutePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [new ProseLint(relativePath, 1, "file-unreadable", "The file cannot be read, so the reviewer reads it by hand.")];
        }

        var extension = Path.GetExtension(absolutePath);

        if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            return LintMarkdown(text, relativePath);
        }

        return LintRecord(text, relativePath);
    }

    /// <summary>Lints Markdown prose: paragraph length per paragraph, phrases and refusal codes per line.</summary>
    /// <param name="text">The file text.</param>
    /// <param name="relativePath">The file path relative to the repository root, used in findings.</param>
    /// <returns>The findings in line order.</returns>
    static List<ProseLint> LintMarkdown(string text, string relativePath)
    {
        var findings = new List<ProseLint>();
        var lines = text.Split('\n');
        var inFence = false;
        var paragraphStart = 0;
        var paragraphSentences = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                findings.AddRange(FlushParagraph(relativePath, paragraphStart, paragraphSentences));
                paragraphStart = 0;
                paragraphSentences = 0;
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            if (IsNonProseLine(trimmed))
            {
                findings.AddRange(FlushParagraph(relativePath, paragraphStart, paragraphSentences));
                paragraphStart = 0;
                paragraphSentences = 0;

                if (trimmed.Length > 0)
                {
                    findings.AddRange(LintLine(line, relativePath, index + 1));
                }

                continue;
            }

            if (trimmed.Length == 0)
            {
                findings.AddRange(FlushParagraph(relativePath, paragraphStart, paragraphSentences));
                paragraphStart = 0;
                paragraphSentences = 0;
                continue;
            }

            if (paragraphStart == 0)
            {
                paragraphStart = index + 1;
            }

            paragraphSentences += CountSentences(trimmed);
            findings.AddRange(LintLine(line, relativePath, index + 1));
        }

        findings.AddRange(FlushParagraph(relativePath, paragraphStart, paragraphSentences));

        return findings;
    }

    /// <summary>Lints a record file: each value after the key carries the phrase and refusal code lints, and a long value carries the paragraph length lint.</summary>
    /// <param name="text">The file text.</param>
    /// <param name="relativePath">The file path relative to the repository root, used in findings.</param>
    /// <returns>The findings in line order.</returns>
    static List<ProseLint> LintRecord(string text, string relativePath)
    {
        var findings = new List<ProseLint>();
        var lines = text.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var value = RecordValue(trimmed);

            if (value.Length == 0)
            {
                continue;
            }

            if (value.Length > MaxRecordValueLength)
            {
                findings.Add(new ProseLint(
                    relativePath,
                    index + 1,
                    "paragraph-too-long",
                    $"The value holds {value.Length} characters (over {MaxRecordValueLength}): split it into shorter sentences."));
            }

            findings.AddRange(LintLine(value, relativePath, index + 1));
        }

        return findings;
    }

    /// <summary>The value half of a record line: the text after the key colon, or after the list dash.</summary>
    /// <param name="trimmed">The trimmed record line.</param>
    /// <returns>The value text, or an empty string when the line holds no prose.</returns>
    static string RecordValue(string trimmed)
    {
        var body = trimmed.StartsWith("- ", StringComparison.Ordinal) ? trimmed[2..].Trim() : trimmed;
        var colon = body.IndexOf(':');

        if (colon < 0)
        {
            return body;
        }

        return body[(colon + 1)..].Trim().Trim('\'', '"');
    }

    /// <summary>Whether a Markdown line never belongs to a paragraph: a heading, table row, quote, list marker, or fenced block edge.</summary>
    /// <param name="trimmed">The trimmed line.</param>
    /// <returns>True when the line never belongs to a paragraph.</returns>
    static bool IsNonProseLine(string trimmed) =>
        trimmed.StartsWith('#')
        || trimmed.StartsWith('|')
        || trimmed.StartsWith('>')
        || trimmed.StartsWith("- [")
        || trimmed.StartsWith("* [")
        || (trimmed.StartsWith("- ") && trimmed.Length < 60);

    /// <summary>The paragraph finding for a paragraph that holds too many sentences, or nothing when it fits.</summary>
    /// <param name="relativePath">The file path relative to the repository root, used in findings.</param>
    /// <param name="startLine">The one-based line the paragraph starts on, or zero when no paragraph is open.</param>
    /// <param name="sentences">How many sentences the paragraph holds.</param>
    /// <returns>The paragraph-too-long finding, or an empty list.</returns>
    static IReadOnlyList<ProseLint> FlushParagraph(string relativePath, int startLine, int sentences)
    {
        if (startLine == 0 || sentences <= MaxSentencesPerParagraph)
        {
            return [];
        }

        return [new ProseLint(
            relativePath,
            startLine,
            "paragraph-too-long",
            $"The paragraph holds {sentences} sentences (over {MaxSentencesPerParagraph}): keep one idea in each paragraph.")];
    }

    /// <summary>Lints one line for banned phrases and raw refusal codes.</summary>
    /// <param name="line">The line text.</param>
    /// <param name="relativePath">The file path relative to the repository root, used in findings.</param>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <returns>The findings on the line, at most one per lint code.</returns>
    static List<ProseLint> LintLine(string line, string relativePath, int lineNumber)
    {
        var findings = new List<ProseLint>();

        foreach (var phrase in LitespecEse)
        {
            if (line.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new ProseLint(
                    relativePath,
                    lineNumber,
                    "litespec-ese",
                    $"The line holds LiteSpec-ese '{phrase}': use plain domain words."));
                break;
            }
        }

        if (findings.Count == 0)
        {
            foreach (var phrase in Promotional)
            {
                if (line.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new ProseLint(
                        relativePath,
                        lineNumber,
                        "promotional-language",
                        $"The line holds promotional language '{phrase}': state the observable behavior instead."));
                    break;
                }
            }
        }

        var raw = FirstRawRefusalCode(RemoveBacktickSpans(line));

        if (raw is not null)
        {
            findings.Add(new ProseLint(
                relativePath,
                lineNumber,
                "refusal-code-raw",
                $"The line shows refusal code '{raw}' outside backticks: wrap the code in backticks."));
        }

        return findings;
    }

    /// <summary>The line with every backtick span removed, so codes in backticks never count as raw.</summary>
    /// <param name="line">The line text.</param>
    /// <returns>The line with backtick spans removed.</returns>
    static string RemoveBacktickSpans(string line)
    {
        var kept = new System.Text.StringBuilder(line.Length);
        var inCode = false;

        foreach (var glyph in line)
        {
            if (glyph == '`')
            {
                inCode = !inCode;
                continue;
            }

            if (!inCode)
            {
                kept.Append(glyph);
            }
        }

        return kept.ToString();
    }

    /// <summary>The first hyphenated token on the line that ends like a refusal code, or null.</summary>
    /// <param name="line">The line text with backtick spans removed.</param>
    /// <returns>The raw refusal code, or null when the line shows none.</returns>
    static string? FirstRawRefusalCode(string line)
    {
        foreach (var token in line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var word = token.Trim(',', '.', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']');

            if (!word.Contains('-', StringComparison.Ordinal) || word.StartsWith('-'))
            {
                continue;
            }

            var dash = word.LastIndexOf('-');
            var ending = word[(dash + 1)..];

            if (!RefusalEndings.Contains(ending))
            {
                continue;
            }

            var valid = true;

            foreach (var glyph in word)
            {
                if (!char.IsAsciiLetterOrDigit(glyph) && glyph != '-')
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                return word;
            }
        }

        return null;
    }

    /// <summary>How many sentences the text holds: one per period, exclamation, or question mark that ends a word.</summary>
    /// <param name="text">The text to count.</param>
    /// <returns>The sentence count.</returns>
    static int CountSentences(string text)
    {
        var count = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is '.' or '!' or '?'
                && (index + 1 >= text.Length || char.IsWhiteSpace(text[index + 1])))
            {
                count++;
            }
        }

        return count;
    }
}
