using System;
using System.Collections.Generic;
using System.Text;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Reads every mark on one type or method declaration, in source order:
///     documentation marks first, then attribute marks. Documentation marks come
///     from the <c>implements</c>, <c>proves</c>, and <c>follows</c> tags in the leading trivia,
///     parsed from the trivia structure when the tree was parsed with
///     documentation, and scanned from the comment text when it was not.
///     Attribute marks come from the <c>Implements</c>, <c>Proves</c>, and
///     <c>Follows</c> attributes, with or without a namespace qualifier or the
///     <c>Attribute</c> suffix. Whitespace inside a mark is trimmed. A malformed mark is returned
///     with an empty record identifier so hosts report it as unresolved rather
///     than ignoring it.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class MarkReader
{
    /// <summary>Reads every mark on one type or method declaration, in source order.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <returns>The marks in source order: documentation marks, then attribute marks.</returns>
    /// <exception cref="ArgumentNullException">The member is null.</exception>
    public static IReadOnlyList<Mark> Read(MemberDeclarationSyntax member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        var marks = new List<Mark>();
        ReadDocumentationMarks(member, marks);
        ReadAttributeMarks(member, marks);
        return marks;
    }

    private static void ReadDocumentationMarks(MemberDeclarationSyntax member, List<Mark> marks)
    {
        var comments = new List<DocumentationCommentTriviaSyntax>();
        foreach (var item in member.GetLeadingTrivia())
        {
            if (item.HasStructure && item.GetStructure() is DocumentationCommentTriviaSyntax comment)
            {
                comments.Add(comment);
            }
        }

        if (comments.Count > 0)
        {
            foreach (var comment in comments)
            {
                ReadStructuredComment(member, comment, marks);
            }
        }
        else
        {
            ReadCommentText(member, marks);
        }
    }

    private static void ReadStructuredComment(
        MemberDeclarationSyntax member, DocumentationCommentTriviaSyntax comment, List<Mark> marks)
    {
        foreach (var node in comment.ChildNodes())
        {
            if (node is XmlElementSyntax element)
            {
                if (MarkVerbNames.TryParseTag(element.StartTag?.Name?.ToString(), out var verb))
                {
                    marks.Add(XmlMark(member, verb, element));
                }
            }
            else if (node is XmlEmptyElementSyntax empty)
            {
                if (MarkVerbNames.TryParseTag(empty.Name.ToString(), out var verb))
                {
                    marks.Add(new Mark(string.Empty, MarkStyle.Xml, verb, LineOf(member, empty.Span), empty.Span));
                }
            }
        }
    }

    private static Mark XmlMark(MemberDeclarationSyntax member, MarkVerb verb, XmlElementSyntax element)
    {
        if (element.EndTag is null || element.EndTag.IsMissing)
        {
            return new Mark(string.Empty, MarkStyle.Xml, verb, LineOf(member, element.Span), element.Span);
        }

        var text = new StringBuilder();
        var start = -1;
        var end = -1;
        foreach (var node in element.Content)
        {
            foreach (var token in node.DescendantTokens())
            {
                if (!token.IsKind(SyntaxKind.XmlTextLiteralToken)
                    && !token.IsKind(SyntaxKind.XmlEntityLiteralToken))
                {
                    continue;
                }

                var raw = token.Text;
                for (var i = 0; i < raw.Length; i++)
                {
                    text.Append(raw[i]);
                    if (!char.IsWhiteSpace(raw[i]))
                    {
                        if (start < 0)
                        {
                            start = token.Span.Start + i;
                        }

                        end = token.Span.Start + i + 1;
                    }
                }
            }
        }

        if (start < 0)
        {
            return new Mark(string.Empty, MarkStyle.Xml, verb, LineOf(member, element.Span), element.Span);
        }

        var span = TextSpan.FromBounds(start, end);
        return new Mark(text.ToString().Trim(), MarkStyle.Xml, verb, LineOf(member, span), span);
    }

    private static void ReadCommentText(MemberDeclarationSyntax member, List<Mark> marks)
    {
        var text = member.GetLeadingTrivia().ToString();
        if (text.Length == 0)
        {
            return;
        }

        var baseOffset = member.FullSpan.Start;
        var index = 0;
        while (index < text.Length)
        {
            var open = FindOpenTag(text, index);
            if (open is null)
            {
                return;
            }

            var tagEnd = text.IndexOf('>', open.Position);
            if (tagEnd < 0)
            {
                marks.Add(MalformedTextMark(member, baseOffset, open.Verb, open.Position, text.Length - open.Position));
                return;
            }

            var head = text.Substring(open.Position + 1, tagEnd - open.Position - 1);
            if (head.IndexOf('<') >= 0)
            {
                index = open.Position + 1;
                continue;
            }

            if (head.EndsWith("/", StringComparison.Ordinal))
            {
                marks.Add(MalformedTextMark(member, baseOffset, open.Verb, open.Position, tagEnd - open.Position + 1));
                index = tagEnd + 1;
                continue;
            }

            var close = FindCloseTag(text, tagEnd + 1, open.Verb);
            if (close is null)
            {
                marks.Add(MalformedTextMark(member, baseOffset, open.Verb, open.Position, tagEnd - open.Position + 1));
                index = tagEnd + 1;
                continue;
            }

            var cleaned = CleanTagContent(text, tagEnd + 1, close.Start);
            if (cleaned.Text.Length == 0)
            {
                marks.Add(MalformedTextMark(member, baseOffset, open.Verb, open.Position, close.End - open.Position));
            }
            else
            {
                var span = new TextSpan(baseOffset + cleaned.Start, cleaned.Text.Length);
                marks.Add(new Mark(cleaned.Text, MarkStyle.Xml, open.Verb, LineOf(member, span), span));
            }

            index = close.End;
        }
    }

    private sealed class OpenTag
    {
        public OpenTag(int position, MarkVerb verb)
        {
            Position = position;
            Verb = verb;
        }

        public int Position { get; }

        public MarkVerb Verb { get; }
    }

    private static OpenTag? FindOpenTag(string text, int index)
    {
        while (index < text.Length)
        {
            var next = text.IndexOf('<', index);
            if (next < 0 || next + 1 >= text.Length)
            {
                return null;
            }

            foreach (var verb in new[] { MarkVerb.Implements, MarkVerb.Proves, MarkVerb.Follows })
            {
                var name = verb.ToTagName();
                if (StartsAt(text, next + 1, name) && IsTagBoundary(text, next + 1 + name.Length))
                {
                    return new OpenTag(next, verb);
                }
            }

            index = next + 1;
        }

        return null;
    }

    private static bool StartsAt(string text, int index, string value)
    {
        if (index + value.Length > text.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (text[index + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTagBoundary(string text, int index) =>
        index >= text.Length || char.IsWhiteSpace(text[index]) || text[index] == '>' || text[index] == '/';

    private sealed class CloseTag
    {
        public CloseTag(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }

        public int End { get; }
    }

    private static CloseTag? FindCloseTag(string text, int index, MarkVerb verb)
    {
        var name = verb.ToTagName();
        while (index < text.Length)
        {
            var next = text.IndexOf("</", index, StringComparison.Ordinal);
            if (next < 0 || !StartsAt(text, next + 2, name))
            {
                return null;
            }

            var end = next + 2 + name.Length;
            while (end < text.Length && char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            if (end < text.Length && text[end] == '>')
            {
                return new CloseTag(next, end + 1);
            }

            index = next + 2;
        }

        return null;
    }

    private sealed class CleanedContent
    {
        public CleanedContent(string text, int start)
        {
            Text = text;
            Start = start;
        }

        public string Text { get; }

        public int Start { get; }
    }

    private static CleanedContent CleanTagContent(string text, int start, int end)
    {
        var cleaned = new StringBuilder();
        var sourceIndex = new List<int>();
        var i = start;
        while (i < end)
        {
            if (text[i] == '\r' || text[i] == '\n')
            {
                var j = i;
                if (j < end && text[j] == '\r')
                {
                    j++;
                }

                if (j < end && text[j] == '\n')
                {
                    j++;
                }

                while (j < end && (text[j] == ' ' || text[j] == '\t'))
                {
                    j++;
                }

                if (StartsAt(text, j, "///"))
                {
                    i = j + 3;
                    continue;
                }

                if (j < end && text[j] == '*' && !(j + 1 < end && text[j + 1] == '/'))
                {
                    i = j + 1;
                    continue;
                }
            }

            cleaned.Append(text[i]);
            sourceIndex.Add(i);
            i++;
        }

        var first = 0;
        while (first < cleaned.Length && char.IsWhiteSpace(cleaned[first]))
        {
            first++;
        }

        var last = cleaned.Length;
        while (last > first && char.IsWhiteSpace(cleaned[last - 1]))
        {
            last--;
        }

        if (last == first)
        {
            return new CleanedContent(string.Empty, start);
        }

        return new CleanedContent(
            cleaned.ToString(first, last - first), sourceIndex[first]);
    }

    private static Mark MalformedTextMark(
        MemberDeclarationSyntax member, int baseOffset, MarkVerb verb, int start, int length)
    {
        var span = new TextSpan(baseOffset + start, length);
        return new Mark(string.Empty, MarkStyle.Xml, verb, LineOf(member, span), span);
    }

    private static void ReadAttributeMarks(MemberDeclarationSyntax member, List<Mark> marks)
    {
        foreach (var list in member.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                var simple = SyntaxNames.SimpleName(attribute.Name);
                if (simple is null || !MarkVerbNames.TryParseAttributeName(simple, out var verb))
                {
                    continue;
                }

                marks.Add(AttributeMark(member, verb, attribute));
            }
        }
    }

    private static Mark AttributeMark(MemberDeclarationSyntax member, MarkVerb verb, AttributeSyntax attribute)
    {
        var fallback = new Mark(string.Empty, MarkStyle.Attribute, verb, LineOf(member, attribute.Span), attribute.Span);
        var arguments = attribute.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count == 0)
        {
            return fallback;
        }

        if (arguments.Value[0].Expression is not LiteralExpressionSyntax literal
            || literal.Token.Value is not string value)
        {
            return fallback;
        }

        if (value.Trim().Length == 0)
        {
            return fallback;
        }

        var span = LiteralContentSpan(literal);
        return new Mark(value.Trim(), MarkStyle.Attribute, verb, LineOf(member, span), span);
    }

    private static TextSpan LiteralContentSpan(LiteralExpressionSyntax literal)
    {
        var token = literal.Token;
        var span = token.Span;
        var text = token.Text;
        var start = 0;
        if (start < text.Length && text[start] == '@')
        {
            start++;
        }

        var quotes = 0;
        while (start + quotes < text.Length && text[start + quotes] == '"')
        {
            quotes++;
        }

        if (quotes == 0)
        {
            return span;
        }

        var end = text.Length;
        if (end >= 2 && (text.EndsWith("u8", StringComparison.Ordinal) || text.EndsWith("U8", StringComparison.Ordinal)))
        {
            end -= 2;
        }

        var trailing = 0;
        while (end - trailing - 1 >= start + quotes && text[end - trailing - 1] == '"')
        {
            trailing++;
        }

        if (trailing != quotes)
        {
            return span;
        }

        start += quotes;
        end -= quotes;
        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return end <= start ? span : new TextSpan(span.Start + start, end - start);
    }

    private static int LineOf(MemberDeclarationSyntax member, TextSpan span) =>
        member.SyntaxTree.GetLineSpan(span).StartLinePosition.Line + 1;
}
