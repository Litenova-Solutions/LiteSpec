using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Whether the idempotency declaration in one definition file replays the first result.
///     The replay check reads this, so the declaration and the use case record agree on what a repeat brings.
/// </summary>
/// <follows>litespec/core/use-cases#state-what-a-replay-does</follows>
/// <follows>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</follows>
public static class IdempotencyReplays
{
    /// <summary>The declaration type holding the keyed factory.</summary>
    private const string DeclarationTypeName = "IdempotencyDeclaration";

    /// <summary>The factory method declaring a keyed message.</summary>
    private const string IdempotentMethodName = "Idempotent";

    /// <summary>The factory argument naming the replay position.</summary>
    private const string ReplayArgumentName = "replayResult";

    /// <summary>The declaration property naming the replay position.</summary>
    private const string ReplayPropertyName = "ReplayResult";

    /// <summary>
    ///     Reads whether the definition file replays the first result: true when it writes
    ///     <c>replayResult: true</c> or <c>ReplayResult = true</c>, false when it writes either
    ///     with false, and null when it declares no keyed idempotency. A keyed declaration without the flag
    ///     replays nothing, the pipeline default. An exemption, a <c>NotIdempotent</c> declaration
    ///     and a flag that is no literal state none. The first explicit position in
    ///     source order wins.
    /// </summary>
    /// <param name="root">The parsed definition file root.</param>
    /// <returns>True when the file replays the first result, false when it replays nothing, and null when it states no explicit position.</returns>
    /// <exception cref="ArgumentNullException">The root is null.</exception>
    public static bool? Of(SyntaxNode root)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        var keyed = false;
        var undecidable = false;
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation when IsIdempotentCall(invocation):
                    keyed = true;
                    if (ReplayArgumentOf(invocation) is bool argumentReplay)
                    {
                        return argumentReplay;
                    }

                    undecidable |= HasReplayArgument(invocation);

                    break;
                case AssignmentExpressionSyntax assignment when IsReplayAssignment(assignment):
                    if (LiteralOf(assignment.Right) is bool assignedReplay)
                    {
                        return assignedReplay;
                    }

                    undecidable = true;

                    break;
            }
        }

        // A keyed declaration that states no replay replays nothing: the pipeline's default.
        // A flag the reader cannot evaluate leaves the position unknown.
        if (undecidable)
        {
            return null;
        }

        return keyed ? false : null;
    }

    /// <summary>Whether the factory call passes a replayResult argument at all.</summary>
    /// <param name="invocation">The factory call to read.</param>
    /// <returns>True when the call names a replayResult argument; otherwise false.</returns>
    private static bool HasReplayArgument(InvocationExpressionSyntax invocation)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.NameColon?.Name.Identifier.ValueText == ReplayArgumentName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the invocation calls the keyed factory on the idempotency declaration.</summary>
    /// <param name="invocation">The invocation to test.</param>
    /// <returns>True when the invocation calls IdempotencyDeclaration.Idempotent; otherwise false.</returns>
    private static bool IsIdempotentCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member
            || member.Name.Identifier.ValueText != IdempotentMethodName)
        {
            return false;
        }

        return RightmostName(member.Expression) == DeclarationTypeName;
    }

    /// <summary>Reads the replay position the factory call states in its replayResult argument.</summary>
    /// <param name="invocation">The factory call to read.</param>
    /// <returns>The stated position, or null when the call states none.</returns>
    private static bool? ReplayArgumentOf(InvocationExpressionSyntax invocation)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.NameColon?.Name.Identifier.ValueText == ReplayArgumentName
                && LiteralOf(argument.Expression) is bool replay)
            {
                return replay;
            }
        }

        return null;
    }

    /// <summary>Whether the assignment writes the replay position: ReplayResult = ... in an initializer, a with-expression, or a plain assignment.</summary>
    /// <param name="assignment">The assignment to test.</param>
    /// <returns>True when the assignment writes ReplayResult; otherwise false.</returns>
    private static bool IsReplayAssignment(AssignmentExpressionSyntax assignment)
    {
        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            return false;
        }

        return assignment.Left switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText == ReplayPropertyName,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText == ReplayPropertyName,
            _ => false,
        };
    }

    /// <summary>Reads a boolean literal.</summary>
    /// <param name="expression">The expression to read.</param>
    /// <returns>The literal value, or null when the expression is no boolean literal.</returns>
    private static bool? LiteralOf(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal)
        {
            if (literal.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                return true;
            }

            if (literal.IsKind(SyntaxKind.FalseLiteralExpression))
            {
                return false;
            }
        }

        return null;
    }

    /// <summary>Reads the rightmost name of an expression as written.</summary>
    /// <param name="expression">The expression to read.</param>
    /// <returns>The rightmost name, or null when the expression has none.</returns>
    private static string? RightmostName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            _ => null,
        };
    }
}
