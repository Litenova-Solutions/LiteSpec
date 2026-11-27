using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The outbox decision each domain event type carries in composition code. The CLI
///     reader collects these, so the outbox decision check can hold them to the records.
/// </summary>
/// <follows>litespec/core/outbox#decide-every-domain-event-outcome</follows>
public static class OutboxDecisions
{
    /// <summary>The method name that drops a domain event from the outbox.</summary>
    private const string IgnoreMethodName = "IgnoreDomainEvent";

    /// <summary>The method name that stages the events a domain event translates.</summary>
    private const string TranslateMethodName = "TranslateDomainEvent";

    /// <summary>
    ///     Reads the outbox decision of each domain event type the file decides: each
    ///     <c>IgnoreDomainEvent&lt;T&gt;()</c> and <c>TranslateDomainEvent&lt;T&gt;(...)</c> call, on any
    ///     receiver or none, with the decided type taken from the first type argument.
    ///     A call through any other name, or without a type argument, decides nothing here.
    /// </summary>
    /// <param name="tree">The parsed file to read.</param>
    /// <returns>The stated decisions, in source order.</returns>
    /// <exception cref="ArgumentNullException">The tree is null.</exception>
    public static IReadOnlyList<OutboxDecision> Of(SyntaxTree tree)
    {
        if (tree is null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        var found = new List<OutboxDecision>();

        foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (DecisionOf(invocation) is (string name, bool ignored))
            {
                found.Add(new OutboxDecision(
                    name,
                    ignored,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
        }

        return found;
    }

    /// <summary>Reads the decision one invocation states, or null when it states none.</summary>
    /// <param name="invocation">The invocation to read.</param>
    /// <returns>The decided event type name with whether it is ignored, or null when the invocation is no outbox decision.</returns>
    private static (string EventType, bool Ignored)? DecisionOf(InvocationExpressionSyntax invocation)
    {
        GenericNameSyntax? called;
        bool ignored;

        switch (invocation.Expression)
        {
            case GenericNameSyntax generic when generic.Identifier.ValueText == IgnoreMethodName:
                called = generic;
                ignored = true;
                break;
            case GenericNameSyntax generic when generic.Identifier.ValueText == TranslateMethodName:
                called = generic;
                ignored = false;
                break;
            case MemberAccessExpressionSyntax member when member.Name is GenericNameSyntax generic
                && generic.Identifier.ValueText == IgnoreMethodName:
                called = generic;
                ignored = true;
                break;
            case MemberAccessExpressionSyntax member when member.Name is GenericNameSyntax generic
                && generic.Identifier.ValueText == TranslateMethodName:
                called = generic;
                ignored = false;
                break;
            default:
                return null;
        }

        var arguments = called.TypeArgumentList.Arguments;

        if (arguments.Count == 0)
        {
            return null;
        }

        return SimpleName(arguments[0]) is string name ? (name, ignored) : null;
    }

    /// <summary>Reads the simple name of a type as written.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>The simple name, or null when the type has none.</returns>
    private static string? SimpleName(TypeSyntax type)
    {
        switch (type)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText;
            case QualifiedNameSyntax qualified:
                return qualified.Right.Identifier.ValueText;
            case AliasQualifiedNameSyntax alias:
                return alias.Name.Identifier.ValueText;
            default:
                return null;
        }
    }
}
