using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Deletes the hand-written runtime value LS5005 reports as drifting, and
///     the hand-written value equal to the record on the type LS5006 reports, so
///     the generator writes the value from the record. Fix All deletes every
///     reported value in scope.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DeleteHandWrittenValueCodeFixProvider))]
[Shared]
public sealed class DeleteHandWrittenValueCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS5005", "LS5006");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var span = await ElementSpanAsync(context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);
        var fix = await TraceabilityFix.ReadAsync(context.Document, span, context.CancellationToken).ConfigureAwait(false);

        if (fix?.Kind is not CodeKind kind || MemberName(kind) is not string memberName)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        if (diagnostic.Id == "LS5005")
        {
            RegisterDriftFix(context, diagnostic, fix, root, memberName);
        }
        else
        {
            RegisterEqualFix(context, diagnostic, fix, memberName);
        }
    }

    /// <summary>
    ///     The span of the marked type a diagnostic belongs to: LS5005 sits on the hand-written
    ///     member inside the type, LS5006 on the type itself.
    /// </summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the span.</returns>
    private static async Task<Microsoft.CodeAnalysis.Text.TextSpan> ElementSpanAsync(
        Document document, Diagnostic diagnostic, System.Threading.CancellationToken token)
    {
        var span = diagnostic.Location.SourceSpan;

        if (diagnostic.Id != "LS5005"
            || await document.GetSyntaxRootAsync(token).ConfigureAwait(false) is not SyntaxNode root)
        {
            return span;
        }

        var type = root.FindToken(span.Start).Parent?.FirstAncestorOrSelf<TypeDeclarationSyntax>();

        return type is null ? span : type.Identifier.Span;
    }

    /// <summary>Registers the drift fix.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="fix">The fix context read for the diagnostic: the records, the marked member and its kind.</param>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    private static void RegisterDriftFix(
        CodeFixContext context, Diagnostic diagnostic, TraceabilityFix fix, SyntaxNode root, string memberName)
    {
        if (ValueMember(root, diagnostic.Location.SourceSpan) is not MemberDeclarationSyntax member)
        {
            return;
        }

        var expected = ExpectedValue(fix, memberName);
        var title = expected is null
            ? "Delete hand-written '" + memberName + "'"
            : "Delete hand-written '" + memberName + "' (the record states '" + expected + "')";

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                createChangedSolution: token => DeleteAsync(context.Document, diagnostic, token),
                equivalenceKey: "DeleteHandWrittenValue"),
            diagnostic);
    }

    /// <summary>Registers the equal fix.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="fix">The fix context read for the diagnostic: the records, the marked member and its kind.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    private static void RegisterEqualFix(
        CodeFixContext context,
        Diagnostic diagnostic,
        TraceabilityFix fix,
        string memberName)
    {
        if (fix.Member is not TypeDeclarationSyntax type || ExpectedValue(fix, memberName) is not string expected)
        {
            return;
        }

        foreach (var child in type.Members)
        {
            if (MemberLiteral(child, memberName) is string value
                && string.Equals(value, expected, System.StringComparison.Ordinal))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Delete hand-written '" + memberName + "' equal to the record",
                        createChangedSolution: token => DeleteEqualAsync(context.Document, diagnostic, memberName, token),
                        equivalenceKey: "DeleteHandWrittenValue"),
                    diagnostic);
                return;
            }
        }
    }

    /// <summary>Returns the solution.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the solution.</returns>
    private static async Task<Solution> DeleteAsync(
        Document document, Diagnostic diagnostic, System.Threading.CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var text = await document.GetTextAsync(token).ConfigureAwait(false);

        if (root is null
            || text is null
            || ValueMember(root, diagnostic.Location.SourceSpan) is not MemberDeclarationSyntax member)
        {
            return document.Project.Solution;
        }

        return document.WithText(MarkEdits.DeleteMember(text, member, diagnostic.Location.SourceSpan)).Project.Solution;
    }

    /// <summary>Returns the equal.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the equal.</returns>
    private static async Task<Solution> DeleteEqualAsync(
        Document document, Diagnostic diagnostic, string memberName, System.Threading.CancellationToken token)
    {
        var fix = await TraceabilityFix.ReadAsync(document, diagnostic.Location.SourceSpan, token).ConfigureAwait(false);
        var text = await document.GetTextAsync(token).ConfigureAwait(false);

        if (fix?.Member is not TypeDeclarationSyntax type
            || text is null
            || ExpectedValue(fix, memberName) is not string expected)
        {
            return document.Project.Solution;
        }

        foreach (var child in type.Members)
        {
            if (MemberLiteral(child, memberName) is string value
                && string.Equals(value, expected, System.StringComparison.Ordinal))
            {
                return document.WithText(MarkEdits.DeleteMember(text, child, child.Span)).Project.Solution;
            }
        }

        return document.Project.Solution;
    }

    /// <summary>The field or property declaration holding the span, or null.</summary>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <returns>The member, or null when none applies.</returns>
    private static MemberDeclarationSyntax? ValueMember(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var node = root.FindNode(span, getInnermostNodeForTie: true);

        while (node is not null && node is not FieldDeclarationSyntax && node is not PropertyDeclarationSyntax)
        {
            node = node.Parent;
        }

        return node as MemberDeclarationSyntax;
    }

    /// <summary>The runtime value member of the kind: Classification on events, Code on rule exceptions.</summary>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <returns>The name as text, or null when none applies.</returns>
    private static string? MemberName(CodeKind kind)
    {
        if (kind == CodeKind.DomainEvent)
        {
            return "Classification";
        }

        if (kind == CodeKind.RuleException)
        {
            return "Code";
        }

        return null;
    }

    /// <summary>
    ///     The value the record states: the event id, or the rule's refusal
    ///     result code. Null when no mark resolves to the expected record.
    /// </summary>
    /// <param name="fix">The fix context read for the diagnostic: the records, the marked member and its kind.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    /// <returns>The value as text, or null when none applies.</returns>
    private static string? ExpectedValue(TraceabilityFix fix, string memberName)
    {
        var recordType = memberName == "Classification" ? "event" : "rule";

        foreach (var mark in MarkReader.Read(fix.Member))
        {
            if (mark.HasRecordId
                && fix.Store.Index.ById(mark.RecordId) is IRecordFacts record
                && record.Type == recordType)
            {
                return recordType == "event" ? mark.RecordId : RefusalCode(fix, mark.RecordId);
            }
        }

        return null;
    }

    /// <summary>Returns the code.</summary>
    /// <param name="fix">The fix context read for the diagnostic: the records, the marked member and its kind.</param>
    /// <param name="ruleId">The rule identifier whose refusal code the value must match.</param>
    /// <returns>The code as text, or null when none applies.</returns>
    private static string? RefusalCode(TraceabilityFix fix, string ruleId)
    {
        var rule = fix.Store.Index.ById(ruleId);

        if (rule?.Field("refusal") is not string refusal || refusal.Length == 0)
        {
            return null;
        }

        if (refusal.StartsWith("result/", System.StringComparison.Ordinal))
        {
            var result = fix.Store.Index.ById(refusal);

            return result?.Field("code") is string code && code.Length > 0 ? code : null;
        }

        return refusal;
    }

    /// <summary>The string literal the member declares under the name, or null.</summary>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    /// <returns>The literal as text, or null when none applies.</returns>
    private static string? MemberLiteral(MemberDeclarationSyntax member, string memberName)
    {
        if (member is FieldDeclarationSyntax field)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Identifier.ValueText == memberName
                    && variable.Initializer?.Value is LiteralExpressionSyntax literal)
                {
                    return literal.Token.ValueText;
                }
            }

            return null;
        }

        if (member is PropertyDeclarationSyntax property && property.Identifier.ValueText == memberName)
        {
            if (property.ExpressionBody?.Expression is LiteralExpressionSyntax arrow)
            {
                return arrow.Token.ValueText;
            }

            if (property.AccessorList is not null)
            {
                foreach (var accessor in property.AccessorList.Accessors)
                {
                    if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        continue;
                    }

                    if (accessor.ExpressionBody?.Expression is LiteralExpressionSyntax getArrow)
                    {
                        return getArrow.Token.ValueText;
                    }

                    if (accessor.Body is BlockSyntax body
                        && body.Statements.Count == 1
                        && body.Statements[0] is ReturnStatementSyntax returned
                        && returned.Expression is LiteralExpressionSyntax returnedLiteral)
                    {
                        return returnedLiteral.Token.ValueText;
                    }
                }
            }
        }

        return null;
    }
}
