using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Whether one method changes the state of its type. The kind reader shares
///     this, so a public aggregate-root method that assigns state without raising
///     an event is still recognized and still needs its use-case mark.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class StateChanges
{
    /// <summary>
    ///     Whether the method assigns the state of its owner: an assignment or an
    ///     increment to a field or property of the owner, in its body or in a private
    ///     method of its owner it calls. A write through <c>this</c> always counts;
    ///     a bare name counts only when the owner declares it and no parameter or
    ///     local shadows it. An assignment to a local changes no state.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <param name="owner">The type declaring the method and its helpers.</param>
    /// <returns>Whether the method assigns the state of its owner.</returns>
    /// <exception cref="ArgumentNullException">The method or owner is null.</exception>
    public static bool Changes(MethodDeclarationSyntax method, TypeDeclarationSyntax owner)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (owner is null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        var fields = OwnerFields(owner);

        if (Assigns(method, fields))
        {
            return true;
        }

        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (InvocationName(invocation) is string name
                && FindPrivateMethod(owner, name) is MethodDeclarationSyntax helper
                && Assigns(helper, fields))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the method assigns a state field directly.</summary>
    /// <param name="method">The method to read.</param>
    /// <param name="fields">The owner's field and property names.</param>
    /// <returns>True when the method assigns the state of its owner.</returns>
    private static bool Assigns(MethodDeclarationSyntax method, HashSet<string> fields)
    {
        var shadowed = ShadowedNames(method);

        foreach (var assignment in method.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (WritesState(assignment.Left, fields, shadowed))
            {
                return true;
            }
        }

        foreach (var increment in method.DescendantNodes().OfType<PrefixUnaryExpressionSyntax>())
        {
            if (increment.IsKind(SyntaxKind.PreIncrementExpression)
                || increment.IsKind(SyntaxKind.PreDecrementExpression))
            {
                if (WritesState(increment.Operand, fields, shadowed))
                {
                    return true;
                }
            }
        }

        foreach (var increment in method.DescendantNodes().OfType<PostfixUnaryExpressionSyntax>())
        {
            if (WritesState(increment.Operand, fields, shadowed))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether an assignment target writes owner state.</summary>
    /// <param name="target">The assignment target.</param>
    /// <param name="fields">The owner's field and property names.</param>
    /// <param name="shadowed">The parameter and local names that shadow owner members.</param>
    /// <returns>True when the target writes the state of the owner.</returns>
    private static bool WritesState(ExpressionSyntax target, HashSet<string> fields, HashSet<string> shadowed)
    {
        switch (target)
        {
            case IdentifierNameSyntax plain:
                return fields.Contains(plain.Identifier.ValueText) && !shadowed.Contains(plain.Identifier.ValueText);
            case MemberAccessExpressionSyntax member when member.Expression is ThisExpressionSyntax:
                return true;
            case ElementAccessExpressionSyntax element when element.Expression is ThisExpressionSyntax:
                return true;
            case ElementAccessExpressionSyntax element when element.Expression is IdentifierNameSyntax plain:
                return fields.Contains(plain.Identifier.ValueText) && !shadowed.Contains(plain.Identifier.ValueText);
            default:
                return false;
        }
    }

    /// <summary>Reads the field and property names a type declares.</summary>
    /// <param name="owner">The type to read.</param>
    /// <returns>The owner's field and property names.</returns>
    private static HashSet<string> OwnerFields(TypeDeclarationSyntax owner)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in owner.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                fields.Add(variable.Identifier.ValueText);
            }
        }

        foreach (var field in owner.Members.OfType<EventFieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                fields.Add(variable.Identifier.ValueText);
            }
        }

        foreach (var property in owner.Members.OfType<PropertyDeclarationSyntax>())
        {
            fields.Add(property.Identifier.ValueText);
        }

        return fields;
    }

    /// <summary>Reads the parameter and local names that shadow owner members.</summary>
    /// <param name="method">The method to read.</param>
    /// <returns>The names declared inside the method.</returns>
    private static HashSet<string> ShadowedNames(MethodDeclarationSyntax method)
    {
        var shadowed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in method.ParameterList.Parameters)
        {
            if (parameter.Identifier.ValueText.Length > 0)
            {
                shadowed.Add(parameter.Identifier.ValueText);
            }
        }

        foreach (var variable in method.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            shadowed.Add(variable.Identifier.ValueText);
        }

        foreach (var loop in method.DescendantNodes().OfType<ForEachStatementSyntax>())
        {
            shadowed.Add(loop.Identifier.ValueText);
        }

        return shadowed;
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
}
