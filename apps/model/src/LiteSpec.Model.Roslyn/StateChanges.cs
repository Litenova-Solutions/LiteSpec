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
}
