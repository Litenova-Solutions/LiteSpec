using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static LiteSpec.Authoring.ManifestValidator;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The value-style pass: the mechanical half of the value-style table over LiteSpec's own templates and examples.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class ValueStyleValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueStyleValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public ValueStyleValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    // The value style states one written form per value: a period ends a
    // statement, a question mark ends a question, and a clause never repeats
    // the word it completes. Placeholders skip every rule: angle brackets and
    // dunder names teach the shape, not the style. A definition meaning is
    // checked on term and setting records, where the value-style table owns
    // it; result, event, permission, and field meanings follow the text-kind
    // table instead.
    // (litespec/core/language#write-each-value-in-the-value-style)
    /// <summary>Checks the value style of the shipped templates and examples.</summary>
    internal void CheckValueStyle()
    {
        var root = Context.Root;
        var add = Context.Add;
        var files = new List<string>();
        var templates = Path.Combine(root, "templates", "consumer");
        if (Directory.Exists(templates))
        {
            files.AddRange(Directory.GetFileSystemEntries(templates)
                .Where(file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.Ordinal));
        }

        var examples = Path.Combine(root, "examples");
        if (Directory.Exists(examples))
        {
            files.AddRange(Walk(examples, file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var file in files)
        {
            var relative = Slash(Path.GetRelativePath(root, file));
            var raw = ReadText(file);
            if (raw.Split('\n')[0].TrimEnd('\r') != "---")
            {
                continue;
            }

            if (!Context.Records.TryParseRecordFrontmatter(raw, out var frontmatter, out _))
            {
                continue;
            }

            if (frontmatter is JsonObject obj)
            {
                CheckValueStyleNode(obj, false, JsonString(obj["type"]), relative);
            }
        }
    }

    /// <summary>Checks the value style of one frontmatter node.</summary>
    /// <param name="node">The frontmatter node.</param>
    /// <param name="inSteps">Whether the node sits inside a steps list.</param>
    /// <param name="enclosingType">The enclosing record type.</param>
    /// <param name="relative">The repository-relative path.</param>
    private void CheckValueStyleNode(JsonNode? node, bool inSteps, string? enclosingType, string relative)
    {
        var add = Context.Add;
        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                CheckValueStyleNode(item, inSteps, enclosingType, relative);
            }

            return;
        }

        if (node is not JsonObject obj)
        {
            return;
        }

        var type = JsonString(obj["type"]) ?? enclosingType;
        foreach (var (field, child) in obj)
        {
            if (child is JsonValue && JsonString(child) is string text)
            {
                CheckValueField(field, text, inSteps, type, relative);
            }
            else
            {
                CheckValueStyleNode(child, field == "steps", type, relative);
            }
        }
    }

    /// <summary>Checks the value style of one text field.</summary>
    /// <param name="field">The field name.</param>
    /// <param name="value">The field value.</param>
    /// <param name="inSteps">Whether the field sits inside a steps list.</param>
    /// <param name="enclosingType">The enclosing record type.</param>
    /// <param name="relative">The repository-relative path.</param>
    private void CheckValueField(string field, string value, bool inSteps, string? enclosingType, string relative)
    {
        var add = Context.Add;
        if (RepositoryValidator.IsTemplatePlaceholder(value))
        {
            return;
        }

        var text = value.Trim();
        if (text.Length == 0)
        {
            return;
        }

        if (field == "statement")
        {
            if (inSteps)
            {
                // A step starts with its actor: `The` or an actor title, which
                // reads as a capitalized word.
                if (!char.IsUpper(text[0]))
                {
                    add(relative, 1, "VALUE_STYLE", "step `statement` must start with `The` or an actor title");
                }

                if (!text.EndsWith('.'))
                {
                    add(relative, 1, "VALUE_STYLE", "step `statement` must end in a period");
                }

                return;
            }

            if (!text.EndsWith('.'))
            {
                add(relative, 1, "VALUE_STYLE", "`statement` must end in a period");
            }

            if (enclosingType is "requirement" or "rule" && LowerModalPattern().Match(BacktickSpanPattern().Replace(text, " ")) is { Success: true } modal)
            {
                add(relative, 1, "VALUE_STYLE", $"`statement` must not hold the modal word `{modal.Groups[1].Value}`");
            }

            return;
        }

        if (field == "meaning" && enclosingType is "term" or "setting")
        {
            if (!text.StartsWith("A ", StringComparison.Ordinal)
                && !text.StartsWith("An ", StringComparison.Ordinal)
                && !text.StartsWith("The ", StringComparison.Ordinal))
            {
                add(relative, 1, "VALUE_STYLE", "`meaning` must start with A, An or The");
            }

            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 25)
            {
                add(relative, 1, "VALUE_STYLE", $"`meaning` holds {words.Length} words; at most 25");
            }

            return;
        }

        if (field == "rationale" || field == "reason" || (field.Length > 7 && field.EndsWith("Because", StringComparison.Ordinal)))
        {
            if (StartsWithWord(text, "because"))
            {
                add(relative, 1, "VALUE_STYLE", $"`{field}` must not start with `because`");
            }

            return;
        }

        if (field == "condition" || (field.Length > 4 && field.EndsWith("When", StringComparison.Ordinal)))
        {
            if (StartsWithWord(text, "when") || StartsWithWord(text, "if"))
            {
                add(relative, 1, "VALUE_STYLE", $"`{field}` must not start with `when` or `if`");
            }

            return;
        }

        if (field == "failure")
        {
            if (RefusalCodePattern().IsMatch(text))
            {
                add(relative, 1, "VALUE_STYLE", "`failure` must not hold a refusal code");
            }

            return;
        }

        if (field == "question")
        {
            if (!text.EndsWith('?'))
            {
                add(relative, 1, "VALUE_STYLE", "`question` must end in `?`");
            }

            return;
        }

        if (field == "title")
        {
            // A title opens with a capital letter and carries no final period.
            // Small words inside keep their own case, as the shipped example
            // titles do.
            if (char.IsLower(text[0]))
            {
                add(relative, 1, "VALUE_STYLE", "`title` must start with a capital letter");
            }

            if (text.EndsWith('.'))
            {
                add(relative, 1, "VALUE_STYLE", "`title` must carry no final period");
            }

            return;
        }
    }

    /// <summary>Reports whether text starts with a whole word.</summary>
    /// <param name="text">The text to check.</param>
    /// <param name="word">The word to match.</param>
    /// <returns>True when the text starts with the word.</returns>
    private bool StartsWithWord(string text, string word) =>
        text.StartsWith(word, StringComparison.OrdinalIgnoreCase)
        && (text.Length == word.Length || !char.IsLetterOrDigit(text[word.Length]));

    // A modal verb as a whole word in lower case; a hyphen counts as part of
    // the word, so a modal inside an identifier is a name, not prose. Text in
    // backticks holds code rather than prose.
    /// <summary>Gets the pattern that matches a lowercase modal verb.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"(?<![\w-])(should|must|shall|may|might|can|could|would|will|ought)(?![\w-])")]
    private static partial Regex LowerModalPattern();

    /// <summary>Gets the pattern that matches a backtick span.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("`[^`]*`")]
    private static partial Regex BacktickSpanPattern();

    // The refusal-code shape the record schema fixes, read as a substring: two
    // lowercase segments of at least two characters joined by a dot, so short
    // forms such as e.g. and version numbers never match.
    /// <summary>Gets the pattern that matches a refusal code.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"[a-z0-9][a-z0-9-]+\.[a-z0-9][a-z0-9-]+")]
    private static partial Regex RefusalCodePattern();
}
