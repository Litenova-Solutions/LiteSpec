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
///     <c>Follows</c> attributes in the generated namespace: unqualified, or
///     qualified with <c>LiteSpec</c> or <c>global::LiteSpec</c>, with or without
///     the <c>Attribute</c> suffix. Any other qualifier names a domain attribute,
///     never a mark. Whitespace inside a mark is trimmed. A malformed mark is returned
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

    /// <summary>Reads the documentation marks on one declaration.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="marks">The marks to append to.</param>
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

    /// <summary>Reads the marks in one structured documentation comment.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="comment">The documentation comment.</param>
    /// <param name="marks">The marks to append to.</param>
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

    /// <summary>Reads one mark from a documentation element.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="verb">The mark verb.</param>
    /// <param name="element">The documentation element.</param>
    /// <returns>The mark the element carries.</returns>
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

    /// <summary>Reads the marks in the plain comment text of one declaration.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="marks">The marks to append to.</param>
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

    /// <summary>One opening mark tag in comment text: its offset and verb.</summary>
    private sealed class OpenTag
    {
        /// <summary>Initializes a new instance of the <see cref="OpenTag"/> class.</summary>
        /// <param name="position">The offset where the tag starts.</param>
        /// <param name="verb">The verb the tag names.</param>
        public OpenTag(int position, MarkVerb verb)
        {
            Position = position;
            Verb = verb;
        }

        /// <summary>Gets the offset where the tag starts.</summary>
        public int Position { get; }

        /// <summary>Gets the verb the tag names.</summary>
        public MarkVerb Verb { get; }
    }

    /// <summary>Finds the next opening mark tag in comment text.</summary>
    /// <param name="text">The comment text.</param>
    /// <param name="index">The offset to start from.</param>
    /// <returns>The opening tag, or null when the text holds none.</returns>
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

    /// <summary>Whether the value occurs in the text at the offset.</summary>
    /// <param name="text">The text to read.</param>
    /// <param name="index">The offset to test.</param>
    /// <param name="value">The value to match.</param>
    /// <returns>True when the value occurs at the offset.</returns>
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

    /// <summary>Whether the offset ends a tag name.</summary>
    /// <param name="text">The text to read.</param>
    /// <param name="index">The offset just past the tag name.</param>
    /// <returns>True when the offset is a tag boundary.</returns>
    private static bool IsTagBoundary(string text, int index) =>
        index >= text.Length || char.IsWhiteSpace(text[index]) || text[index] == '>' || text[index] == '/';

    /// <summary>One closing mark tag in comment text: where it starts and where it ends.</summary>
    private sealed class CloseTag
    {
        /// <summary>Initializes a new instance of the <see cref="CloseTag"/> class.</summary>
        /// <param name="start">The offset where the closing tag starts.</param>
        /// <param name="end">The offset just past the closing tag.</param>
        public CloseTag(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>Gets the offset where the closing tag starts.</summary>
        public int Start { get; }

        /// <summary>Gets the offset just past the closing tag.</summary>
        public int End { get; }
    }

    /// <summary>Finds the closing tag for the verb in comment text.</summary>
    /// <param name="text">The comment text.</param>
    /// <param name="index">The offset to start from.</param>
    /// <param name="verb">The verb the closing tag names.</param>
    /// <returns>The closing tag, or null when the text holds none.</returns>
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

    /// <summary>Tag content with comment markers stripped: its text and where it starts.</summary>
    private sealed class CleanedContent
    {
        /// <summary>Initializes a new instance of the <see cref="CleanedContent"/> class.</summary>
        /// <param name="text">The cleaned text.</param>
        /// <param name="start">The offset where the text starts.</param>
        public CleanedContent(string text, int start)
        {
            Text = text;
            Start = start;
        }

        /// <summary>Gets the cleaned text.</summary>
        public string Text { get; }

        /// <summary>Gets the offset where the text starts.</summary>
        public int Start { get; }
    }

    /// <summary>Strips comment markers from tag content.</summary>
    /// <param name="text">The comment text.</param>
    /// <param name="start">The offset where the content starts.</param>
    /// <param name="end">The offset where the content ends.</param>
    /// <returns>The cleaned content and where it starts.</returns>
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

    /// <summary>Builds the empty mark a malformed tag in comment text reports.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="baseOffset">The offset where the declaration starts.</param>
    /// <param name="verb">The mark verb.</param>
    /// <param name="start">The offset where the tag starts.</param>
    /// <param name="length">The length the mark span takes.</param>
    /// <returns>The malformed mark, with an empty record identifier.</returns>
    private static Mark MalformedTextMark(
        MemberDeclarationSyntax member, int baseOffset, MarkVerb verb, int start, int length)
    {
        var span = new TextSpan(baseOffset + start, length);
        return new Mark(string.Empty, MarkStyle.Xml, verb, LineOf(member, span), span);
    }

    /// <summary>Reads the attribute marks on one declaration.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="marks">The marks to append to.</param>
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

                if (!SyntaxNames.IsGeneratedMarkName(attribute.Name))
                {
                    continue;
                }

                marks.Add(AttributeMark(member, verb, attribute));
            }
        }
    }

    /// <summary>Reads one mark from an attribute.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="verb">The mark verb.</param>
    /// <param name="attribute">The attribute.</param>
    /// <returns>The mark the attribute carries.</returns>
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

    /// <summary>Reads the span of a string literal's content, without its quotes.</summary>
    /// <param name="literal">The string literal.</param>
    /// <returns>The content span.</returns>
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

    /// <summary>Reads the 1-based line of a span in the declaration's tree.</summary>
    /// <param name="member">The type or method declaration.</param>
    /// <param name="span">The span to locate.</param>
    /// <returns>The 1-based line where the span starts.</returns>
    private static int LineOf(MemberDeclarationSyntax member, TextSpan span) =>
        member.SyntaxTree.GetLineSpan(span).StartLinePosition.Line + 1;
}
