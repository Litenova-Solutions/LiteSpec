using System.Text;

namespace LiteSpec.Patterns;

/// <summary>Parses the text of one <c>wireframe</c> block.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class WireframeParser
{
    /// <summary>The separator between inline children.</summary>
    const string InlineSeparator = " · ";

    /// <summary>Parses the block text and checks it against the rules of the language.</summary>
    /// <param name="text">The block text.</param>
    /// <returns>The parsed wireframe, with every error found.</returns>
    public static Wireframe Parse(string text)
    {
        var errors = new List<WireframeError>();
        var roots = new List<WireframeNode>();
        var stack = new List<(int Depth, WireframeNode Node)>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var number = index + 1;
            var raw = lines[index].TrimEnd();
            var content = raw.TrimStart(' ');

            if (content.Length == 0 || content.StartsWith('#'))
            {
                continue;
            }

            if (content.StartsWith('\t') || raw.Contains('\t'))
            {
                errors.Add(new(number, "indent with two spaces, not tabs"));
                continue;
            }

            var indent = raw.Length - content.Length;

            if (indent % 2 != 0)
            {
                errors.Add(new(number, "indent by a multiple of two spaces"));
                continue;
            }

            var depth = indent / 2;
            var node = ParseLine(content, number, errors);

            if (node is null)
            {
                continue;
            }

            while (stack.Count > 0 && stack[^1].Depth >= depth)
            {
                stack.RemoveAt(stack.Count - 1);
            }

            if (stack.Count == 0)
            {
                if (depth != 0)
                {
                    errors.Add(new(number, "the first part of a block starts at column one"));
                }

                roots.Add(node);
            }
            else if (depth > stack[^1].Depth + 1)
            {
                errors.Add(new(number, "indent one level (two spaces) deeper than the parent"));
                continue;
            }
            else
            {
                if (stack[^1].Node.InlineChildren)
                {
                    errors.Add(new(number, $"'{stack[^1].Node.Part}' already has inline children, so it takes no indented ones"));
                }

                stack[^1].Node.Children.Add(node);
            }

            stack.Add((depth, node));
        }

        if (roots.Count != 1 || roots[0].Part != "page")
        {
            errors.Add(new(roots.Count > 1 ? roots[1].Line : 1, "a wireframe has exactly one 'page' at the top"));
        }

        foreach (var root in roots)
        {
            Check(root, null, errors);
        }

        if (roots.Count == 1 && roots[0].Part == "page")
        {
            CheckPage(roots[0], errors);
        }

        errors.Sort((left, right) => left.Line.CompareTo(right.Line));
        return new Wireframe(errors.Count == 0 && roots.Count == 1 ? roots[0] : null, errors);
    }

    /// <summary>Parses one line into a node with its inline children.</summary>
    /// <param name="content">The line text, without its indent.</param>
    /// <param name="number">The 1-based line inside the block.</param>
    /// <param name="errors">The errors to append to.</param>
    /// <returns>The node, or null when the line holds no part.</returns>
    static WireframeNode? ParseLine(string content, int number, List<WireframeError> errors)
    {
        var tokens = Tokenize(content, number, errors);

        if (tokens is null)
        {
            return null;
        }

        var inlineAt = tokens.FindIndex(token => token.Kind == TokenKind.Colon);
        var head = inlineAt < 0 ? tokens : tokens[..inlineAt];
        var node = Build(head, number, errors);

        if (node is null || inlineAt < 0)
        {
            return node;
        }

        node.InlineChildren = true;
        var rest = tokens[(inlineAt + 1)..];
        var group = new List<Token>();

        foreach (var token in rest.Append(new Token(TokenKind.Separator, "")))
        {
            if (token.Kind != TokenKind.Separator)
            {
                group.Add(token);
                continue;
            }

            if (group.Count == 0)
            {
                errors.Add(new(number, "an inline list needs a part between each ' · '"));
                continue;
            }

            var child = Build(group, number, errors);

            if (child is not null)
            {
                node.Children.Add(child);
            }

            group = [];
        }

        return node;
    }

    /// <summary>Builds one node from its head tokens.</summary>
    /// <param name="tokens">The tokens before any inline separator.</param>
    /// <param name="number">The 1-based line inside the block.</param>
    /// <param name="errors">The errors to append to.</param>
    /// <returns>The node, or null when the tokens hold no known part.</returns>
    static WireframeNode? Build(List<Token> tokens, int number, List<WireframeError> errors)
    {
        if (tokens.Count == 0 || tokens[0].Kind != TokenKind.Word)
        {
            errors.Add(new(number, "a line starts with a part name"));
            return null;
        }

        var part = tokens[0].Text;

        if (!WireframeParts.All.ContainsKey(part))
        {
            errors.Add(new(number, $"unknown part '{part}'"));
            return null;
        }

        var node = new WireframeNode { Part = part, Line = number };

        for (var index = 1; index < tokens.Count; index++)
        {
            var token = tokens[index];

            switch (token.Kind)
            {
                case TokenKind.Word:
                    node.Modifiers.Add(token.Text);
                    break;
                case TokenKind.String:
                    node.Strings.Add(token.Text);
                    break;
                case TokenKind.Count:
                    node.Count = int.Parse(token.Text, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case TokenKind.Ratio:
                    node.Ratio = token.Text.Split(':').Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
                    break;
                case TokenKind.Equals:
                    if (index + 1 < tokens.Count && tokens[index + 1].Kind == TokenKind.String)
                    {
                        node.Value = tokens[++index].Text;
                    }
                    else
                    {
                        errors.Add(new(number, "'=' is followed by a quoted value"));
                    }

                    break;
                default:
                    errors.Add(new(number, $"unexpected '{token.Text}'"));
                    break;
            }
        }

        return node;
    }

    /// <summary>Checks one node and its children against the part rules.</summary>
    /// <param name="node">The node to check.</param>
    /// <param name="parent">The parent node, or null at the top.</param>
    /// <param name="errors">The errors to append to.</param>
    static void Check(WireframeNode node, WireframeNode? parent, List<WireframeError> errors)
    {
        var spec = WireframeParts.All[node.Part];

        foreach (var modifier in node.Modifiers.Where(modifier => !spec.Modifiers.Contains(modifier)))
        {
            errors.Add(new(node.Line, spec.Modifiers.Length == 0
                ? $"'{node.Part}' takes no modifier, so '{modifier}' is not allowed"
                : $"'{node.Part}' takes {string.Join(", ", spec.Modifiers)}, not '{modifier}'"));
        }

        if (WireframeParts.IsExclusiveModifierPart(node.Part) && node.Modifiers.Count > 1)
        {
            errors.Add(new(node.Line, $"'{node.Part}' takes one of {string.Join(", ", spec.Modifiers)}"));
        }

        if (node.Part == "action" && node.Modifiers.Count(modifier => modifier is "primary" or "secondary" or "quiet") > 1)
        {
            errors.Add(new(node.Line, "an action is one of primary, secondary or quiet"));
        }

        if (node.Strings.Count < spec.MinStrings || node.Strings.Count > spec.MaxStrings)
        {
            errors.Add(new(node.Line, spec.MinStrings == spec.MaxStrings
                ? $"'{node.Part}' takes {Quoted(spec.MinStrings)}"
                : $"'{node.Part}' takes {spec.MinStrings} to {spec.MaxStrings} quoted strings"));
        }

        if (node.Value is not null && node.Part != "field")
        {
            errors.Add(new(node.Line, "only a field takes a value after '='"));
        }

        if (spec.Number == "count" && node.Count is null)
        {
            errors.Add(new(node.Line, $"'{node.Part}' needs a count, such as '{node.Part} 3'"));
        }

        if (spec.Number != "count" && node.Count is not null)
        {
            errors.Add(new(node.Line, $"'{node.Part}' takes no count"));
        }

        if (spec.Number == "ratio" && node.Ratio is null)
        {
            errors.Add(new(node.Line, "a split needs a ratio, such as 'split 2:1'"));
        }

        if (spec.Number != "ratio" && node.Ratio is not null)
        {
            errors.Add(new(node.Line, $"'{node.Part}' takes no ratio"));
        }

        if (node.Count is < 1 or > 50)
        {
            errors.Add(new(node.Line, "a count is between 1 and 50"));
        }

        CheckChildren(node, spec, errors);

        if (WireframeParts.Overlays.Contains(node.Part) && parent?.Part != "page")
        {
            errors.Add(new(node.Line, $"a {node.Part} is a child of 'page'"));
        }

        if (node.Part == "page" && parent is not null)
        {
            errors.Add(new(node.Line, "'page' is only the top part"));
        }

        if (node.Part is "header" or "footer" or "toolbar" || WireframeParts.Overlays.Contains(node.Part))
        {
            var primaries = node.Descendants().Count(child => child.Part == "action" && child.Has("primary"));

            if (primaries > 1)
            {
                errors.Add(new(node.Line, $"a {node.Part} holds at most one primary action, and this one holds {primaries}"));
            }
        }

        foreach (var child in node.Children)
        {
            Check(child, node, errors);
        }
    }

    /// <summary>Checks a node's children against what its part accepts.</summary>
    /// <param name="node">The node to check.</param>
    /// <param name="spec">What the node's part accepts.</param>
    /// <param name="errors">The errors to append to.</param>
    static void CheckChildren(WireframeNode node, WireframeParts.Spec spec, List<WireframeError> errors)
    {
        var count = node.Children.Count;

        switch (spec.Children)
        {
            case "any":
                break;
            case "none" when count > 0:
                errors.Add(new(node.Line, $"'{node.Part}' takes no children"));
                break;
            case "one" when count != 1:
                errors.Add(new(node.Line, $"'{node.Part}' has exactly one child, the template for each entry"));
                break;
            case "none" or "one":
                break;
            default:
                var allowed = spec.Children.Split(',');

                foreach (var child in node.Children.Where(child => !allowed.Contains(child.Part)))
                {
                    errors.Add(new(child.Line, $"'{node.Part}' holds only {string.Join(", ", allowed)}, not '{child.Part}'"));
                }

                if (node.Part == "table" && count > 1)
                {
                    errors.Add(new(node.Line, "a table has at most one 'item', its first row"));
                }

                break;
        }

        if (node.Part == "split" && node.Ratio is not null && node.Ratio.Count != count)
        {
            errors.Add(new(node.Line, $"split {string.Join(':', node.Ratio)} has {node.Ratio.Count} parts, so it holds {node.Ratio.Count} children, not {count}"));
        }

        if (node.Part == "split" && node.Ratio is { Count: < 2 })
        {
            errors.Add(new(node.Line, "a split ratio has at least two parts"));
        }

        if (node.Part == "row" && count < 2)
        {
            errors.Add(new(node.Line, "a row holds at least two children"));
        }
    }

    /// <summary>Checks the page node's children: overlays come last.</summary>
    /// <param name="page">The page node.</param>
    /// <param name="errors">The errors to append to.</param>
    static void CheckPage(WireframeNode page, List<WireframeError> errors)
    {
        var seenOverlay = false;

        foreach (var child in page.Children)
        {
            var overlay = WireframeParts.Overlays.Contains(child.Part);

            if (seenOverlay && !overlay)
            {
                errors.Add(new(child.Line, "an overlay comes after the page content, so nothing follows it but another overlay"));
            }

            seenOverlay |= overlay;
        }
    }

    /// <summary>Writes a quoted-string count as words.</summary>
    /// <param name="count">The count to write.</param>
    /// <returns>The count as words.</returns>
    static string Quoted(int count) => count switch
    {
        0 => "no quoted string",
        1 => "one quoted string",
        _ => $"{count} quoted strings",
    };

    /// <summary>The kind of one token in a wireframe line.</summary>
    enum TokenKind
    {
        /// <summary>A part or modifier word.</summary>
        Word,
        /// <summary>A quoted string.</summary>
        String,
        /// <summary>A bare integer count.</summary>
        Count,
        /// <summary>A width ratio.</summary>
        Ratio,
        /// <summary>An equals sign.</summary>
        Equals,
        /// <summary>A colon before inline children.</summary>
        Colon,
        /// <summary>The inline separator.</summary>
        Separator,
    }

    /// <summary>One token in a wireframe line.</summary>
    /// <param name="Kind">The token kind.</param>
    /// <param name="Text">The token text.</param>
    sealed record Token(TokenKind Kind, string Text);

    /// <summary>Splits one line into tokens.</summary>
    /// <param name="content">The line text, without its indent.</param>
    /// <param name="number">The 1-based line inside the block.</param>
    /// <param name="errors">The errors to append to.</param>
    /// <returns>The tokens, or null when a quoted string is not closed.</returns>
    static List<Token>? Tokenize(string content, int number, List<WireframeError> errors)
    {
        var tokens = new List<Token>();
        var index = 0;

        while (index < content.Length)
        {
            var current = content[index];

            if (current == ' ')
            {
                if (string.CompareOrdinal(content, index, InlineSeparator, 0, InlineSeparator.Length) == 0)
                {
                    tokens.Add(new(TokenKind.Separator, "·"));
                    index += InlineSeparator.Length;
                    continue;
                }

                index++;
                continue;
            }

            if (current == '"')
            {
                var text = new StringBuilder();
                index++;
                var closed = false;

                while (index < content.Length)
                {
                    if (content[index] == '\\' && index + 1 < content.Length && content[index + 1] is '"' or '\\')
                    {
                        text.Append(content[index + 1]);
                        index += 2;
                        continue;
                    }

                    if (content[index] == '"')
                    {
                        closed = true;
                        index++;
                        break;
                    }

                    text.Append(content[index]);
                    index++;
                }

                if (!closed)
                {
                    errors.Add(new(number, "a quoted string is not closed"));
                    return null;
                }

                tokens.Add(new(TokenKind.String, text.ToString()));
                continue;
            }

            if (current == '=')
            {
                tokens.Add(new(TokenKind.Equals, "="));
                index++;
                continue;
            }

            var start = index;

            while (index < content.Length && content[index] is not (' ' or '"' or '='))
            {
                index++;
            }

            var word = content[start..index];

            if (word.EndsWith(':'))
            {
                if (word.Length > 1)
                {
                    tokens.Add(Classify(word[..^1], number, errors));
                }

                tokens.Add(new(TokenKind.Colon, ":"));
                continue;
            }

            tokens.Add(Classify(word, number, errors));
        }

        return tokens;
    }

    /// <summary>Classifies one bare word as a count, a ratio, or a word.</summary>
    /// <param name="word">The bare word.</param>
    /// <param name="number">The 1-based line inside the block.</param>
    /// <param name="errors">The errors to append to.</param>
    /// <returns>The token for the word.</returns>
    static Token Classify(string word, int number, List<WireframeError> errors)
    {
        if (word.All(char.IsAsciiDigit))
        {
            return new(TokenKind.Count, word);
        }

        if (word.Contains(':') && word.Split(':').All(part => part.Length > 0 && part.All(char.IsAsciiDigit)))
        {
            return new(TokenKind.Ratio, word);
        }

        if (!word.All(character => char.IsAsciiLetterLower(character) || character == '-'))
        {
            errors.Add(new(number, $"'{word}' is not a part, modifier, count or ratio; copy goes in double quotes"));
        }

        return new(TokenKind.Word, word);
    }
}
