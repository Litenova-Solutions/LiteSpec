using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The public instance properties one type declares: its public property members plus the
///     positional parameters of a record, which the compiler turns into public properties. The
///     contract checks read this, so the message a handler takes meets the input its use case
///     declares, field by field, and the result it returns meets the declared output.
/// </summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
public static class MessageProperties
{
    /// <summary>One public instance property: its name with its type as written.</summary>
    public sealed class MessageProperty
    {
        /// <summary>Initializes a new instance of the <see cref="MessageProperty" /> class.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="type">The property type as written.</param>
        public MessageProperty(string name, string type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>Gets the property name.</summary>
        public string Name { get; }

        /// <summary>Gets the property type as written.</summary>
        public string Type { get; }
    }

    /// <summary>
    ///     Reads the public instance properties the type declares, in source order: the positional
    ///     parameters first, then the public property members. A static member, a non-public member
    ///     and an explicit interface implementation declare no property of the message. Members an
    ///     inherited base type declares are never read here, so a platform base adds no property.
    /// </summary>
    /// <param name="declared">The type declaration to read.</param>
    /// <returns>The declared public instance properties, in source order, without duplicates.</returns>
    /// <exception cref="ArgumentNullException">The declaration is null.</exception>
    public static IReadOnlyList<MessageProperty> Of(TypeDeclarationSyntax declared)
    {
        if (declared is null)
        {
            throw new ArgumentNullException(nameof(declared));
        }

        var found = new List<MessageProperty>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Only a record turns its positional parameters into public properties; a class
        // primary constructor captures its parameters instead.
        if (declared is RecordDeclarationSyntax record && record.ParameterList is not null)
        {
            foreach (var parameter in record.ParameterList.Parameters)
            {
                if (parameter.Type is null || parameter.Identifier.IsMissing)
                {
                    continue;
                }

                Add(found, seen, parameter.Identifier.ValueText, parameter.Type.ToString());
            }
        }

        foreach (var property in declared.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.ExplicitInterfaceSpecifier is not null
                || property.Identifier.IsMissing
                || !IsPublic(property)
                || IsStatic(property))
            {
                continue;
            }

            Add(found, seen, property.Identifier.ValueText, property.Type.ToString());
        }

        return found;
    }

    /// <summary>Adds one property when no property with its name was added before.</summary>
    /// <param name="found">The properties read so far, appended in place.</param>
    /// <param name="seen">The property names read so far.</param>
    /// <param name="name">The property name.</param>
    /// <param name="type">The property type as written.</param>
    private static void Add(List<MessageProperty> found, HashSet<string> seen, string name, string type)
    {
        if (seen.Add(name))
        {
            found.Add(new MessageProperty(name, type));
        }
    }

    /// <summary>Whether the property carries a public modifier.</summary>
    /// <param name="property">The property to test.</param>
    /// <returns>True when the property carries a public modifier.</returns>
    private static bool IsPublic(PropertyDeclarationSyntax property)
    {
        foreach (var modifier in property.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PublicKeyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the property carries a static modifier.</summary>
    /// <param name="property">The property to test.</param>
    /// <returns>True when the property carries a static modifier.</returns>
    private static bool IsStatic(PropertyDeclarationSyntax property)
    {
        foreach (var modifier in property.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.StaticKeyword))
            {
                return true;
            }
        }

        return false;
    }
}
