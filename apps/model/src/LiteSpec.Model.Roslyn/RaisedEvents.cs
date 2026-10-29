using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The event types one method raises through <c>RaiseDomainEvent</c>. The CLI
///     reader, the kind reader and the act-event analyzer share this, so all
///     three agree on what a method raises.
/// </summary>
/// <follows>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</follows>
public static class RaisedEvents
{
    private const string RaiseMethodName = "RaiseDomainEvent";

    /// <summary>
    ///     Reads the simple names of the event types the method raises: each
    ///     <c>RaiseDomainEvent(new X(...))</c> in its body, or in a private method
    ///     of its type it calls. A call through any other name, without a
    ///     constructed event, or through a second helper raises nothing here.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <param name="owner">The type declaring the method and its helpers.</param>
    /// <returns>The raised event type names, in source order, without duplicates.</returns>
    /// <exception cref="ArgumentNullException">The method or owner is null.</exception>
    public static IReadOnlyList<string> Of(MethodDeclarationSyntax method, TypeDeclarationSyntax owner)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (owner is null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        var raised = new List<string>();
        Collect(method, raised);

        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (InvocationName(invocation) is string name
                && name != RaiseMethodName
                && FindPrivateMethod(owner, name) is MethodDeclarationSyntax helper)
            {
                Collect(helper, raised);
            }
        }

        return raised;
    }

    private static void Collect(MethodDeclarationSyntax method, List<string> raised)
    {
        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (InvocationName(invocation) != RaiseMethodName
                || invocation.ArgumentList.Arguments.Count == 0
                || invocation.ArgumentList.Arguments[0].Expression is not ObjectCreationExpressionSyntax created)
            {
                continue;
            }

            if (SimpleName(created.Type) is string name && !raised.Contains(name))
            {
                raised.Add(name);
            }
        }
    }

    private static MethodDeclarationSyntax? FindPrivateMethod(TypeDeclarationSyntax parent, string name)
    {
        foreach (var candidate in parent.Members.OfType<MethodDeclarationSyntax>())
        {
            if (candidate.Identifier.ValueText == name && IsPrivate(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsPrivate(MethodDeclarationSyntax method)
    {
        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PublicKeyword)
                || modifier.IsKind(SyntaxKind.InternalKeyword)
                || modifier.IsKind(SyntaxKind.ProtectedKeyword))
            {
                return false;
            }
        }

        return true;
    }

    private static string? InvocationName(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText;
            case MemberAccessExpressionSyntax member:
                return member.Name.Identifier.ValueText;
            case MemberBindingExpressionSyntax binding:
                return binding.Name.Identifier.ValueText;
            default:
                return null;
        }
    }

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
