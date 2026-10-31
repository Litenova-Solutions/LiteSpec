using System.Globalization;
using System.Text.RegularExpressions;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The page contract: page classes, section order, and the per-page parse.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class PageValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PageValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public PageValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    /// <summary>A page contract: its required and optional H2 sections.</summary>
    private sealed record PageContract(List<string> Required, List<string> Optional);

    // A page's location names its class. Four areas hold pages a reader follows or
    // looks up rather than pages that own provisions, and each one answers a
    // question at a single layer. (litespec/core/authoring#state-one-layer-per-page)

    // The words that stay lowercase inside a Title Case heading, unless first or
    // last: articles, coordinating conjunctions, and prepositions of four letters
    // or fewer. Every other word starts with a capital.
    /// <summary>Gets the words that stay lowercase inside a Title Case heading.</summary>
    private static readonly HashSet<string> TitleSmallWords = new(
        ["a", "an", "the", "and", "but", "for", "nor", "or", "so", "yet",
         "as", "at", "by", "from", "in", "into", "of", "off", "on", "onto",
         "over", "per", "to", "up", "upon", "via", "with"],
        StringComparer.Ordinal);

    /// <summary>Names the class of a page from its repository-relative path, or null when it holds no page.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <returns>The page class, or null.</returns>
    public static string? PageClass(string relative)
    {
        if (relative == Provisions.IndexPath)
        {
            return "index";
        }

        if (relative == "docs/reference/glossary.md")
        {
            return "glossary";
        }

        if (relative.EndsWith("/README.md", StringComparison.Ordinal) || relative == "README.md")
        {
            return "index";
        }

        if (!relative.StartsWith("docs/", StringComparison.Ordinal) || !relative.EndsWith(".md", StringComparison.Ordinal))
        {
            return null;
        }

        // Any page inside an ext/ folder is an extension page, whether it extends one
        // stack or every stack.
        if (relative.Contains("/ext/", StringComparison.Ordinal))
        {
            return "extension";
        }

        // LiteSpec keeps its own research and decisions as consumer-shaped records.
        // They inform the provisions and settle nothing themselves.
        // (litespec/core/maintaining#record-the-reasoning-behind-each-release)
        if (relative.StartsWith("docs/intent/research/", StringComparison.Ordinal))
        {
            return "research";
        }

        if (relative.StartsWith("docs/intent/decisions/", StringComparison.Ordinal)
            || relative.StartsWith("docs/implementation/decisions/", StringComparison.Ordinal))
        {
            return "decision";
        }

        var segments = relative["docs/".Length..^".md".Length].Split('/');
        if (segments[0] == "tools" || segments[0] == "maintain")
        {
            return "command";
        }

        if (segments[0] == "reference")
        {
            return "reference";
        }

        if (segments[0] == "tutorial")
        {
            return "tutorial";
        }

        if (segments[0] == "guide")
        {
            return "how-to";
        }

        // The analyzers area mirrors the command area: one reference page per rule
        // under rules/, and the procedures for using the package beside them.
        if (segments[0] == "analyzers")
        {
            return segments.Length > 2 && segments[1] == "rules" ? "reference" : "how-to";
        }

        // An explanation page builds understanding. Its sections are free, it owns no
        // provisions, and no selection applies it. The example area follows one
        // product through LiteSpec the same way.
        if (segments[0] == "learn" || segments[0] == "example")
        {
            return "explanation";
        }

        if (Patterns.PatternPage.IsPatternPage(relative))
        {
            return "pattern";
        }

        if ((segments[0] == "backend" || segments[0] == "frontend") && segments[^1] == "stack")
        {
            return "stack";
        }

        return "topic";
    }

    /// <summary>Returns the section contract for a page class, or null when the class has none.</summary>
    /// <param name="kind">The page class.</param>
    /// <returns>The section contract, or null.</returns>
    private static PageContract? ExpectedSections(string? kind)
    {
        if (kind == "topic")
        {
            // A topic page carries no per-stack sections. Shared pages stay neutral
            // across stacks, and each stack page carries its own bindings.
            // (litespec/core/authoring#place-each-stack-binding-on-its-stack-page)
            return new PageContract(["Intent", "Standards", "Conventions"], ["Concepts", "Reference Example"]);
        }

        // A pattern page offers drawn options for recurring interface questions; its
        // Standards hold the rules every option keeps.
        if (kind == "pattern")
        {
            return new PageContract(["Intent", "Patterns"], ["Concepts", "Standards", "Conventions"]);
        }

        if (kind == "stack")
        {
            return new PageContract(["Intent", "Standards", "Conventions"], ["Concepts"]);
        }

        if (kind == "extension")
        {
            return new PageContract(["Intent", "Activation", "Baseline Relationship", "Standards", "Conventions", "Dependencies"], []);
        }

        if (kind == "how-to")
        {
            return new PageContract(["Purpose", "Procedure", "Check"], ["Prerequisites"]);
        }

        // A tutorial closes on what the reader now has. Check asks whether a
        // procedure worked, which is a different question asked by a different reader.
        if (kind == "tutorial")
        {
            return new PageContract(["Purpose", "Prerequisites", "Lesson", "What You Built"], []);
        }

        if (kind == "reference")
        {
            return new PageContract(["Intent", "Reference"], ["Notes"]);
        }

        // Underneath is required rather than optional, so a command page that hides its
        // mechanism fails rather than passing in silence. A command with nothing
        // separately runnable writes 'None.' (litespec/core/authoring#name-the-escape-from-every-abstraction)
        if (kind == "command")
        {
            return new PageContract(["Name", "Synopsis", "Description", "Arguments", "Options", "Exit Codes", "Examples", "Underneath"], []);
        }

        // The record contracts mirror the consumer table exactly, so an invented
        // section fails here as it does in a consumer.
        if (kind == "research")
        {
            return new PageContract(["Answer"], ["Notes on Sources"]);
        }

        if (kind == "decision")
        {
            return new PageContract(["Context", "Decision", "Consequences"], ["Evidence with Confidence"]);
        }

        return null;
    }

    /// <summary>Checks that a page holds its required sections in contract order.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="kind">The page class.</param>
    /// <param name="sections">The page H2 sections.</param>
    private void CheckSectionOrder(string relative, string? kind, List<PageSection> sections)
    {
        var add = Context.Add;
        var contract = ExpectedSections(kind);
        if (contract is null)
        {
            return;
        }

        var names = sections.Select(section => section.Name).ToList();
        foreach (var required in contract.Required)
        {
            if (!names.Contains(required))
            {
                add(relative, 1, "PAGE_MISSING_SECTION", $"missing required H2 '{required}'");
            }
        }

        var allowed = new HashSet<string>(contract.Required.Concat(contract.Optional), StringComparer.Ordinal);
        foreach (var section in sections)
        {
            if (!allowed.Contains(section.Name))
            {
                add(relative, section.Line, "PAGE_UNKNOWN_SECTION", $"H2 '{section.Name}' is not valid for {kind} pages");
            }
        }

        List<string> canonical;
        if (kind == "topic")
        {
            canonical = ["Intent", "Concepts", "Standards", "Conventions", "Reference Example"];
        }
        else if (kind == "pattern")
        {
            canonical = ["Intent", "Concepts", "Standards", "Conventions", "Patterns"];
        }
        else if (kind == "stack")
        {
            canonical = ["Intent", "Concepts", "Standards", "Conventions"];
        }
        else if (kind == "how-to")
        {
            canonical = ["Purpose", "Prerequisites", "Procedure", "Check"];
        }
        else if (kind == "reference")
        {
            canonical = ["Intent", "Reference", "Notes"];
        }
        else if (kind == "research")
        {
            canonical = ["Answer", "Notes on Sources"];
        }
        else if (kind == "decision")
        {
            canonical = ["Context", "Decision", "Consequences", "Evidence with Confidence"];
        }
        else
        {
            canonical = contract.Required;
        }

        var previous = -1;
        foreach (var section in sections)
        {
            var position = canonical.IndexOf(section.Name);
            if (position < previous)
            {
                add(relative, section.Line, "PAGE_SECTION_ORDER", $"H2 '{section.Name}' is out of order");
                break;
            }

            previous = position;
        }
    }

    /// <summary>Returns the trimmed body of one H2 section of a page.</summary>
    /// <param name="reader">The parsed page.</param>
    /// <param name="sectionName">The section name.</param>
    /// <returns>The trimmed section body.</returns>
    internal static string SectionBody(PageReader reader, string sectionName)
    {
        var headings = reader.Headings.Where(heading => heading.Level == 2).ToList();
        var start = headings.FindIndex(heading => PageReader.StripAnchorSuffix(heading.Text).Trim() == sectionName);
        if (start < 0)
        {
            return "";
        }

        var startLine = headings[start].Line;
        var endLine = start + 1 < headings.Count ? headings[start + 1].Line : reader.Lines.Length + 1;
        return string.Join('\n', reader.Lines[startLine..(endLine - 1)]).Trim();
    }

    /// <summary>Reports whether a heading uses Title Case.</summary>
    /// <param name="value">The heading text.</param>
    /// <returns>True when the heading uses Title Case.</returns>
    private static bool TitleCase(string value)
    {
        // Code spans and explicit anchors keep their own case; the rule judges
        // the words around them.
        value = PageReader.StripAnchorSuffix(value).Trim();
        var matches = TitleTokenPattern().Matches(value).Cast<Match>().ToList();
        return matches.Select((match, index) => (token: match.Value, index, start: match.Index)).All(item =>
        {
            if (TitleAtypicalPattern().IsMatch(item.token) || TitleCamelPattern().IsMatch(item.token) || item.token.Contains('.'))
            {
                return true;
            }

            // The word after a colon starts a new phrase and is capitalized like a first word.
            var afterColon = value[..item.start].TrimEnd().EndsWith(':');

            if (item.index > 0 && item.index < matches.Count - 1 && !afterColon && TitleSmallWords.Contains(item.token.ToLowerInvariant()))
            {
                return string.Equals(item.token, item.token.ToLowerInvariant(), StringComparison.Ordinal);
            }

            return TitleCapitalPattern().IsMatch(item.token);
        });
    }

    // Every heading below the title uses Title Case. The page contract owns
    // its H2 literals, and the root routing heading is a fixed literal too,
    // so those stay untouched here and change with the contract instead. The
    // generated provision index is tool output rather than authored headings.
    /// <summary>Checks that every heading below the title uses Title Case.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="kind">The page class.</param>
    /// <param name="reader">The parsed page.</param>
    private void CheckHeadingCase(string relative, string? kind, PageReader reader)
    {
        var add = Context.Add;
        if (relative == Provisions.IndexPath)
        {
            return;
        }

        var contract = ExpectedSections(kind);
        var contractNames = contract is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(contract.Required.Concat(contract.Optional), StringComparer.Ordinal);
        foreach (var heading in reader.Headings)
        {
            if (heading.Level < 2 || heading.Level > 6)
            {
                continue;
            }

            var title = PageReader.StripAnchorSuffix(heading.Text).Trim();
            if (heading.Level == 2 && (title == RoutingHeading || contractNames.Contains(title)))
            {
                continue;
            }

            if (title.Length > 0 && !TitleCase(heading.DisplayText))
            {
                add(relative, heading.Line, "PAGE_HEADING_CASE", $"H{heading.Level} '{title}' must use Title Case");
            }
        }
    }

    // A required section with nothing to say holds exactly `None.`. These are
    // the placeholder phrasings that mean the same thing in other words.
    /// <summary>Reports whether a section body is a placeholder variant of an empty section.</summary>
    /// <param name="body">The section body.</param>
    /// <returns>True when the body is a placeholder variant.</returns>
    private static bool EmptySectionVariant(string body)
    {
        var text = body.Trim();
        if (text is "None" or "N/A" or "n/a")
        {
            return true;
        }

        if (text.StartsWith("None.", StringComparison.Ordinal) && text != "None.")
        {
            return true;
        }

        return AddsNoContentPattern().IsMatch(text);
    }

    /// <summary>Checks the index Intent rule and the glossary order and definition rules.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="kind">The page class.</param>
    /// <param name="reader">The parsed page.</param>
    /// <param name="sections">The page H2 sections.</param>
    private void CheckIndexAndGlossary(string relative, string? kind, PageReader reader, List<PageSection> sections)
    {
        var add = Context.Add;
        if (kind == "index" && (sections.Count == 0 || sections[0].Name != "Intent"))
        {
            add(relative, sections.Count > 0 ? sections[0].Line : 1, "INDEX_INTENT", "an index must start with an Intent H2");
        }

        if (kind == "explanation" && (sections.Count == 0 || sections[0].Name != "Intent"))
        {
            add(relative, sections.Count > 0 ? sections[0].Line : 1, "PAGE_MISSING_SECTION", "an explanation page starts with an Intent H2");
        }

        if (kind != "glossary")
        {
            return;
        }

        var terms = sections.Select(section => section.Name).ToList();
        var sorted = terms
            .OrderBy(term => term, StringComparer.Create(CultureInfo.GetCultureInfo("en"), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace))
            .ToList();
        if (terms.Where((term, index) => term != sorted[index]).Any())
        {
            add(relative, 1, "GLOSSARY_ORDER", "glossary terms are not alphabetical");
        }

        var lines = reader.Lines;
        var headingLines = reader.Headings.Select(heading => heading.Line).ToHashSet();
        for (var index = 0; index < sections.Count; index++)
        {
            var start = sections[index].Line;
            var end = index + 1 < sections.Count ? sections[index + 1].Line - 1 : lines.Length;
            var firstParagraph = new List<string>();
            for (var cursor = start; cursor < end; cursor++)
            {
                var line = lines[cursor];
                if (line.Trim().Length == 0)
                {
                    if (firstParagraph.Count > 0)
                    {
                        break;
                    }

                    continue;
                }

                if (headingLines.Contains(cursor + 1))
                {
                    break;
                }

                firstParagraph.Add(line);
            }

            if (Prose.Sentences(PageReader.VisibleText(string.Join(' ', firstParagraph))).Count != 1)
            {
                add(relative, sections[index].Line, "GLOSSARY_DEFINITION", $"term '{sections[index].Name}' must start with one definition sentence");
            }
        }

        for (var index = 0; index < sections.Count; index++)
        {
            var start = sections[index].Line;
            var end = index + 1 < sections.Count ? sections[index + 1].Line - 1 : lines.Length;
            var group = new List<string>();
            foreach (var heading in reader.Headings)
            {
                if (heading.Level == 3 && heading.Line > start && heading.Line <= end)
                {
                    group.Add(heading.Text.Trim());
                }
            }

            var ordered = group
                .OrderBy(term => term, StringComparer.Create(CultureInfo.GetCultureInfo("en"), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace))
                .ToList();
            if (group.Where((term, position) => term != ordered[position]).Any())
            {
                add(relative, sections[index].Line, "GLOSSARY_ORDER", $"glossary terms under '{sections[index].Name}' are not alphabetical");
            }
        }
    }

    /// <summary>Parses one page and runs the per-page checks over it.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="reader">The parsed page.</param>
    /// <param name="virtual">Whether the page is a template parsed under a virtual path.</param>
    /// <returns>The blocks collected from the page.</returns>
    internal ParsedBlocks ParsePage(string relative, PageReader reader, bool @virtual = false)
    {
        var add = Context.Add;
        var globalIds = Context.GlobalIds;
        var kind = PageClass(relative);
        var clean = CleanLines(reader);
        var sections = new List<PageSection>();
        var currentH2 = "";
        var h1s = reader.Headings.Where(heading => heading.Level == 1).ToList();
        var provisions = new List<Provision>();

        foreach (var heading in reader.Headings)
        {
            if (heading.Level == 2)
            {
                currentH2 = PageReader.StripAnchorSuffix(heading.Text).Trim();
                sections.Add(new PageSection(currentH2, heading.Line));

                continue;
            }

            if (heading.Level != 3)
            {
                continue;
            }

            var pattern = currentH2 == "Patterns" && kind == "pattern";
            if (currentH2 != "Standards" && currentH2 != "Conventions" && !pattern)
            {
                continue;
            }

            if (!Provisions.IsProvisionPage(relative))
            {
                continue;
            }

            // The ID line under the heading is the provision's identity. A heading
            // may change without changing it, so the heading carries no ID text
            // of its own and the page path still names the owning page.
            var title = heading.Text.Trim();
            if (title.Contains("litespec/", StringComparison.Ordinal))
            {
                add(relative, heading.Line, "ID_IN_HEADING", $"provision heading '{title}' carries identifier text; the ID line under the heading states the identity");
            }

            var nextLine = reader.Headings
                .Where(candidate => (candidate.Level == 2 || candidate.Level == 3) && candidate.Line > heading.Line)
                .Select(candidate => candidate.Line)
                .Append(int.MaxValue)
                .Min();
            var body = clean.Where(item => item.Number > heading.Line && item.Number < nextLine).ToList();
            var page = Provisions.PagePath(relative);
            var derived = $"litespec/{page}#{Provisions.HeadingSlug(title)}";
            var id = derived;
            var firstContent = body.FirstOrDefault(item => item.Line.Trim().Length > 0);
            if (firstContent is not null
                && PageReader.TryParseIdLine(firstContent.Line, out var stated)
                && Provisions.Parse(stated)?.Page == page)
            {
                id = stated;
            }

            var firstWord = FirstWordPattern().Match(title);
            // A pattern heading is the pattern's name, so its ID survives a change of default.
            if (!pattern && (!firstWord.Success || !ActionVerbs.Contains(firstWord.Value)))
            {
                add(relative, heading.Line, "HEADING_ACTION", $"provision heading '{title}' must start with an action verb");
            }

            if (globalIds.TryGetValue(id, out var first))
            {
                add(relative, heading.Line, "ID_DUPLICATE", $"provision ID '{id}' also appears in {first}");
            }
            else
            {
                globalIds[id] = $"{relative}:{heading.Line}";
            }

            provisions.Add(new Provision(id, title, currentH2, heading.Line, body));
        }

        // A heading with no body and no lower-level heading beneath it groups
        // nothing. It becomes a dead entry in any generated table of contents.
        var levels = reader.Headings.ToDictionary(heading => heading.Line, heading => heading.Level);
        foreach (var heading in reader.Headings)
        {
            if (heading.Level < 2 || heading.Level > 4)
            {
                continue;
            }

            var hasBody = false;
            for (var line = heading.Line + 1; line <= reader.Lines.Length; line++)
            {
                if (levels.TryGetValue(line, out var nextLevel))
                {
                    if (nextLevel > heading.Level)
                    {
                        hasBody = true;
                    }

                    break;
                }

                if (reader.Lines[line - 1].Trim().Length > 0 || reader.FencedLines.Contains(line) || reader.GeneratedLines.Contains(line))
                {
                    hasBody = true;
                    break;
                }
            }

            if (!hasBody)
            {
                add(relative, heading.Line, "HEADING_EMPTY_BODY", $"heading '{heading.Text}' has no body and no nested heading");
            }
        }

        // An index navigates. A numbered procedure inside one duplicates a canonical
        // provision without citing it. (litespec/core/authoring#use-the-declared-page-contract)
        if (kind == "index" && reader.OrderedStepLines.Count > 0)
        {
            add(relative, reader.OrderedStepLines[0], "INDEX_CONTAINS_PROCEDURE", "an index must not contain a numbered procedure");
        }

        if (kind is not null && kind != "index" && h1s.Count != 1)
        {
            add(relative, h1s.Count > 1 ? h1s[1].Line : 1, "PAGE_TITLE_COUNT", $"expected one H1, found {h1s.Count}");
        }

        if (kind is not null && kind != "index" && h1s.Count == 1 && !TitleCase(h1s[0].DisplayText))
        {
            add(relative, h1s[0].Line, "PAGE_TITLE_CASE", $"H1 '{h1s[0].Text}' must use Title Case");
        }

        CheckSectionOrder(relative, kind, sections);
        CheckIndexAndGlossary(relative, kind, reader, sections);
        CheckHeadingCase(relative, kind, reader);
        var contract = ExpectedSections(kind);
        if (contract is not null)
        {
            var allowed = new HashSet<string>(contract.Required.Concat(contract.Optional), StringComparer.Ordinal);
            foreach (var section in sections)
            {
                if (!allowed.Contains(section.Name))
                {
                    continue;
                }

                var body = SectionBody(reader, section.Name);
                if (body.Length == 0)
                {
                    add(relative, section.Line, "PAGE_EMPTY_SECTION", $"H2 '{section.Name}' must contain content or 'None.'");
                }
                else if (EmptySectionVariant(body))
                {
                    add(relative, section.Line, "PAGE_EMPTY_SECTION_TEXT", $"H2 '{section.Name}' must contain exactly 'None.' when it has nothing to say");
                }
            }
        }

        if (kind == "topic" && sections.Any(section => section.Name == "Reference Example"))
        {
            var example = SectionBody(reader, "Reference Example");
            var exampleHeading = reader.Headings.FirstOrDefault(heading => heading.Level == 2 && PageReader.StripAnchorSuffix(heading.Text).Trim() == "Reference Example");
            var exampleEnd = reader.Headings
                .Where(candidate => candidate.Level == 2 && exampleHeading is not null && candidate.Line > exampleHeading.Line)
                .Select(candidate => candidate.Line)
                .Append(reader.Lines.Length + 1)
                .Min();
            var firstLine = "";
            if (exampleHeading is not null)
            {
                for (var line = exampleHeading.Line + 1; line < exampleEnd; line++)
                {
                    if (reader.Lines[line - 1].Trim().Length > 0 && !reader.FencedLines.Contains(line) && !reader.GeneratedLines.Contains(line))
                    {
                        firstLine = reader.Lines[line - 1].Trim();
                        break;
                    }
                }
            }

            var ids = CitationPattern.Matches(example);
            if (!firstLine.StartsWith("This informative example demonstrates ", StringComparison.Ordinal) || ids.Count == 0)
            {
                var section = sections.FirstOrDefault(item => item.Name == "Reference Example");
                add(relative, section?.Line ?? 1, "REFERENCE_EXAMPLE_DECLARATION", "Reference Example must begin with an informative declaration and provision IDs");
            }
        }

        var rules = provisions.Where(provision => provision.Section != "Patterns").ToList();
        Context.Provisions.CheckProvisionBodies(relative, reader, rules);
        Context.Provisions.CheckProvisionClaims(relative, rules);
        Context.Provisions.CheckNormativeLocation(relative, clean, provisions);
        Context.Provisions.CheckInlineVerification(relative, reader, provisions);

        if (kind == "pattern")
        {
            var group = Path.GetFileNameWithoutExtension(relative);
            foreach (var (line, message) in Patterns.PatternPage.Parse(group, reader.Raw).Check())
            {
                add(relative, line, "PATTERN_INVALID", message);
            }
        }

        return new ParsedBlocks(kind, provisions);
    }

    /// <summary>Gets the pattern that matches a section that adds no content.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^This [A-Za-z]+ adds no [A-Za-z]+\.$")]
    private static partial Regex AddsNoContentPattern();

    /// <summary>Gets the pattern that matches one heading token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"[A-Za-z0-9][A-Za-z0-9'./+-]*")]
    private static partial Regex TitleTokenPattern();

    // A token holding a digit is a proper name such as arc42 or C4 and keeps its own case.
    /// <summary>Gets the pattern that matches an atypical heading token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^(?:v?\d|[A-Z0-9.]+$|[A-Za-z]+\d)")]
    private static partial Regex TitleAtypicalPattern();

    /// <summary>Gets the pattern that matches a camel-cased heading token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"[a-z][A-Z]|[A-Z][a-z]+[A-Z]")]
    private static partial Regex TitleCamelPattern();

    /// <summary>Gets the pattern that matches a capitalized heading token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^[A-Z]")]
    private static partial Regex TitleCapitalPattern();

    /// <summary>Gets the pattern that matches the first word of a heading.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^[A-Za-z]+(?:-[A-Za-z]+)?")]
    private static partial Regex FirstWordPattern();
}
