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
    /// <summary>The method name that raises a domain event on an aggregate.</summary>
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

    /// <summary>Collects the event types one method raises directly.</summary>
    /// <param name="method">The method to read.</param>
    /// <param name="raised">The raised event type names to append to.</param>
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

    /// <summary>Finds a private method of the type by name.</summary>
    /// <param name="parent">The type declaring the method.</param>
    /// <param name="name">The method name.</param>
    /// <returns>The method, or null when the type declares no private method with the name.</returns>
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

    /// <summary>Whether the method is private: no public, internal, or protected modifier.</summary>
    /// <param name="method">The method to test.</param>
    /// <returns>True when the method is private.</returns>
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

    /// <summary>Reads the simple name an invocation calls.</summary>
    /// <param name="invocation">The invocation to read.</param>
    /// <returns>The called name, or null when the invocation calls no simple name.</returns>
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
