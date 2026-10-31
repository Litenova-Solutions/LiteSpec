using System;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Writes marks as text edits: inserting a mark line above a member in the
///     project's style, converting one mark between the styles, adding the
///     partial modifier, and deleting a hand-written member.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class MarkEdits
{
    /// <summary>
    ///     Inserts a mark line above the member: a documentation tag line, added
    ///     to the existing trivia block when there is one and alone otherwise
    ///     (never with an invented summary), or an attribute line.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="member">The member gaining the mark.</param>
    /// <param name="verb">The mark verb.</param>
    /// <param name="style">The project style to write.</param>
    /// <param name="recordId">The record identifier.</param>
    /// <returns>The text with the mark line inserted.</returns>
    public static SourceText InsertMark(
        SourceText text, MemberDeclarationSyntax member, MarkVerb verb, MarkStyle style, string recordId)
    {
        var line = text.Lines.GetLineFromPosition(member.SpanStart);
        var indent = IndentOf(line.ToString());
        var newline = NewlineOf(text);
        var mark = style == MarkStyle.Xml
            ? "/// <" + verb.ToTagName() + ">" + recordId + "</" + verb.ToTagName() + ">"
            : "[LiteSpec." + verb.ToAttributeName() + "(\"" + recordId + "\")]";

        return text.WithChanges(new TextChange(
            new TextSpan(member.SpanStart, 0), mark + newline + indent));
    }

    /// <summary>Replaces the span text, such as a mistyped record identifier, with the record id.</summary>
    /// <param name="text">The document text.</param>
    /// <param name="span">The span to replace.</param>
    /// <param name="recordId">The record identifier.</param>
    /// <returns>The text with the span replaced.</returns>
    public static SourceText Replace(SourceText text, TextSpan span, string recordId) =>
        text.WithChanges(new TextChange(span, recordId));

    /// <summary>
    ///     Converts the documentation-tag mark holding the span to an attribute:
    ///     the tag leaves its line and the attribute is inserted above the member.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="member">The member carrying the mark.</param>
    /// <param name="span">The mark span.</param>
    /// <param name="verb">The mark verb.</param>
    /// <param name="recordId">The record identifier.</param>
    /// <returns>The text with the mark converted.</returns>
    public static SourceText ConvertToAttribute(
        SourceText text, MemberDeclarationSyntax member, TextSpan span, MarkVerb verb, string recordId)
    {
        var inserted = InsertMark(text, member, verb, MarkStyle.Attribute, recordId);

        return RemoveXmlTag(inserted, span, verb);
    }

    /// <summary>
    ///     Converts the attribute mark holding the span to a documentation tag:
    ///     the tag is inserted above the member and the attribute leaves.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="root">The syntax root.</param>
    /// <param name="member">The member carrying the mark.</param>
    /// <param name="span">The mark span.</param>
    /// <param name="verb">The mark verb.</param>
    /// <param name="recordId">The record identifier.</param>
    /// <returns>The text with the mark converted.</returns>
    public static SourceText ConvertToXml(
        SourceText text,
        SyntaxNode root,
        MemberDeclarationSyntax member,
        TextSpan span,
        MarkVerb verb,
        string recordId)
    {
        var line = text.Lines.GetLineFromPosition(member.SpanStart);
        var indent = IndentOf(line.ToString());
        var newline = NewlineOf(text);
        var mark = "/// <" + verb.ToTagName() + ">" + recordId + "</" + verb.ToTagName() + ">";

        var removeSpan = RemoveAttributeSpan(text, root, span);

        if (removeSpan is null)
        {
            return text;
        }

        var removed = removeSpan.Value;
        var position = member.SpanStart;
        var insertion = mark + newline + indent;

        if (position > removed.Start && position < removed.End)
        {
            position = removed.Start;
            insertion = indent + mark + newline;
        }
        else if (position >= removed.End)
        {
            position -= removed.Length;
        }

        var withoutAttribute = text.WithChanges(new TextChange(removed, string.Empty));

        return withoutAttribute.WithChanges(new TextChange(new TextSpan(position, 0), insertion));
    }

    /// <summary>Adds the partial modifier before the type keyword.</summary>
    /// <param name="text">The document text.</param>
    /// <param name="type">The type declaration.</param>
    /// <returns>The text with the modifier added.</returns>
    public static SourceText AddPartial(SourceText text, TypeDeclarationSyntax type) =>
        text.WithChanges(new TextChange(new TextSpan(type.Keyword.SpanStart, 0), "partial "));

    /// <summary>
    ///     Deletes the field or property declaration: the whole lines it covers,
    ///     or just the one declarator when the field declares several.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="member">The field or property declaration.</param>
    /// <param name="span">The diagnostic span inside the member.</param>
    /// <returns>The text with the member deleted.</returns>
    public static SourceText DeleteMember(SourceText text, MemberDeclarationSyntax member, TextSpan span)
    {
        if (member is FieldDeclarationSyntax field && field.Declaration.Variables.Count > 1)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Span.Contains(span))
                {
                    return text.WithChanges(new TextChange(DeclaratorRemoval(field, variable), string.Empty));
                }
            }
        }

        var first = text.Lines.GetLineFromPosition(member.Span.Start);
        var last = text.Lines.GetLineFromPosition(member.Span.End);

        if (first.LineNumber == last.LineNumber && LineHoldsOtherCode(text, first, member.Span))
        {
            return text.WithChanges(new TextChange(member.Span, string.Empty));
        }

        return text.WithChanges(new TextChange(
            TextSpan.FromBounds(first.Start, last.EndIncludingLineBreak), string.Empty));
    }

    /// <summary>Returns the removal.</summary>
    /// <param name="field">The field declaration that carries the hand-written value.</param>
    /// <param name="variable">The variable declarator that carries the hand-written value.</param>
    /// <returns>The removal.</returns>
    private static TextSpan DeclaratorRemoval(FieldDeclarationSyntax field, VariableDeclaratorSyntax variable)
    {
        var variables = field.Declaration.Variables;
        var index = variables.IndexOf(variable);

        if (index > 0)
        {
            return TextSpan.FromBounds(variables[index - 1].Span.End, variable.Span.End);
        }

        return TextSpan.FromBounds(variable.Span.Start, variables[index + 1].Span.Start);
    }

    /// <summary>Returns true when the line-holds-other-code condition holds; otherwise false.</summary>
    /// <param name="text">The source text the edit applies to.</param>
    /// <param name="line">The source line under inspection.</param>
    /// <param name="keep">The span of the line that stays when the rest is removed.</param>
    /// <returns>True when the line-holds-other-code condition holds; otherwise false.</returns>
    private static bool LineHoldsOtherCode(SourceText text, TextLine line, TextSpan keep)
    {
        var content = line.ToString();

        var before = content.Substring(0, Math.Max(0, Math.Min(keep.Start - line.Start, content.Length)));
        var afterStart = Math.Max(0, Math.Min(keep.End - line.Start, content.Length));
        var after = content.Substring(afterStart);

        return before.Trim().Length > 0 || after.Trim().Length > 0;
    }

    /// <summary>Removes the documentation tag around the span: its line when the tag owns it, else the tag alone.</summary>
    /// <param name="text">The source text the edit applies to.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <param name="verb">The mark verb the edit writes, such as implements or follows.</param>
    /// <returns>The xml tag.</returns>
    private static SourceText RemoveXmlTag(SourceText text, TextSpan span, MarkVerb verb)
    {
        var whole = text.ToString();
        var tag = verb.ToTagName();
        var open = whole.LastIndexOf("<" + tag, span.Start, span.Start, StringComparison.Ordinal);
        var close = whole.IndexOf("</" + tag + ">", span.End, StringComparison.Ordinal);

        if (open < 0 || close < 0)
        {
            return text;
        }

        var end = close + tag.Length + 3;
        var first = text.Lines.GetLineFromPosition(open);
        var last = text.Lines.GetLineFromPosition(end);

        if (first.LineNumber == last.LineNumber)
        {
            var content = first.ToString();
            var head = open - first.Start;
            var tail = end - first.Start;
            var remainder = content.Substring(0, head) + content.Substring(tail);

            if (remainder.Trim() is "" or "///")
            {
                return text.WithChanges(new TextChange(
                    TextSpan.FromBounds(first.Start, first.EndIncludingLineBreak), string.Empty));
            }

            return text.WithChanges(new TextChange(first.Span, remainder));
        }

        return text.WithChanges(new TextChange(TextSpan.FromBounds(open, end), string.Empty));
    }

    /// <summary>
    ///     The span removing the attribute holding the mark: its whole line when
    ///     the list owns it, else the attribute with one adjacent comma or space.
    /// </summary>
    /// <param name="text">The source text the edit applies to.</param>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <returns>The attribute span, or null when none applies.</returns>
    private static TextSpan? RemoveAttributeSpan(
        SourceText text, SyntaxNode root, TextSpan span)
    {
        var node = root.FindNode(span, getInnermostNodeForTie: true);

        while (node is not null && node is not AttributeSyntax)
        {
            node = node.Parent;
        }

        if (node is not AttributeSyntax attribute || attribute.Parent is not AttributeListSyntax list)
        {
            return null;
        }

        if (list.Attributes.Count > 1)
        {
            var index = list.Attributes.IndexOf(attribute);

            if (index > 0)
            {
                var previous = list.Attributes[index - 1];

                return TextSpan.FromBounds(previous.Span.End, attribute.Span.End);
            }

            var next = list.Attributes[index + 1];

            return TextSpan.FromBounds(attribute.Span.Start, next.Span.Start);
        }

        var first = text.Lines.GetLineFromPosition(list.Span.Start);
        var last = text.Lines.GetLineFromPosition(list.Span.End);

        if (first.LineNumber == last.LineNumber)
        {
            var content = first.ToString();
            var head = list.Span.Start - first.Start;
            var tail = Math.Min(list.Span.End - first.Start, content.Length);

            if (head >= 0 && tail >= head
                && (content.Substring(0, head) + content.Substring(tail)).Trim().Length == 0)
            {
                return TextSpan.FromBounds(first.Start, first.EndIncludingLineBreak);
            }

            return list.Span;
        }

        return list.Span;
    }

    /// <summary>Returns the indent of the line.</summary>
    /// <param name="line">The source line under inspection.</param>
    /// <returns>The indent of the line.</returns>
    private static string IndentOf(string line)
    {
        var length = 0;

        while (length < line.Length && char.IsWhiteSpace(line[length]))
        {
            length++;
        }

        return line.Substring(0, length);
    }

    /// <summary>Returns the newline of the text.</summary>
    /// <param name="text">The source text the edit applies to.</param>
    /// <returns>The newline of the text.</returns>
    private static string NewlineOf(SourceText text) =>
        text.ToString().IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
}
