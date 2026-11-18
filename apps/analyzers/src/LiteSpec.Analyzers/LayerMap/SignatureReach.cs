using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     The types a contract hands to the class that implements it: every type the member signatures of
///     the interfaces name, followed transitively through the member signatures of each source type
///     reached, with the nested types of each. An adapter reads and builds whatever its port names, and a
///     repository builds whatever its Domain interface returns, so both rules allow this set.
/// </summary>
/// <follows>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</follows>
internal static class SignatureReach
{
    /// <summary>Returns the types the interfaces reach through their member signatures.</summary>
    /// <param name="interfaces">The interfaces whose signatures open the set.</param>
    /// <param name="compilation">The compilation whose source types open further types.</param>
    /// <returns>The reached types by original definition.</returns>
    public static HashSet<INamedTypeSymbol> Of(IEnumerable<INamedTypeSymbol> interfaces, Compilation compilation)
    {
        var direct = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var face in interfaces)
        {
            AddMemberSignatureTypes(face, direct);
            foreach (var inherited in face.AllInterfaces)
            {
                AddMemberSignatureTypes(inherited, direct);
            }
        }

        // Only source types open further types, so library generics such as Task or List open nothing.
        var reached = new HashSet<INamedTypeSymbol>(direct, SymbolEqualityComparer.Default);
        var pending = new Queue<INamedTypeSymbol>(direct);
        while (pending.Count > 0)
        {
            var named = pending.Dequeue();
            if (CodeMap.SourceFileOf(named, compilation) is null)
            {
                continue;
            }

            var next = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            AddMemberSignatureTypes(named, next);
            foreach (var type in next)
            {
                if (reached.Add(type))
                {
                    pending.Enqueue(type);
                }
            }
        }

        return reached;
    }

    /// <summary>Adds every type the members of the type name in their signatures, and its nested types.</summary>
    /// <param name="type">The type whose members are read.</param>
    /// <param name="into">The named types collected so far, by original definition.</param>
    private static void AddMemberSignatureTypes(INamedTypeSymbol type, HashSet<INamedTypeSymbol> into)
    {
        foreach (var member in type.GetMembers())
        {
            switch (member)
            {
                // A nested type is part of its container: the cases of a closed set come with the set.
                case INamedTypeSymbol nested:
                    into.Add(nested.OriginalDefinition);
                    break;
                case IMethodSymbol method when method.AssociatedSymbol is null:
                    AddSignatureTypes(method.ReturnType, into);
                    foreach (var parameter in method.Parameters)
                    {
                        AddSignatureTypes(parameter.Type, into);
                    }

                    break;
                case IPropertySymbol property:
                    AddSignatureTypes(property.Type, into);
                    foreach (var parameter in property.Parameters)
                    {
                        AddSignatureTypes(parameter.Type, into);
                    }

                    break;
                case IFieldSymbol field:
                    AddSignatureTypes(field.Type, into);
                    break;
                case IEventSymbol @event:
                    AddSignatureTypes(@event.Type, into);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>Adds the named types one type use states, unwrapping arrays, pointers and type arguments.</summary>
    /// <param name="type">The type as the signature states it, or null when the signature states none.</param>
    /// <param name="into">The named types collected so far, by original definition.</param>
    private static void AddSignatureTypes(ITypeSymbol? type, HashSet<INamedTypeSymbol> into)
    {
        switch (type)
        {
            case null:
            case ITypeParameterSymbol:
                break;
            case IArrayTypeSymbol array:
                AddSignatureTypes(array.ElementType, into);
                break;
            case IPointerTypeSymbol pointer:
                AddSignatureTypes(pointer.PointedAtType, into);
                break;
            case IFunctionPointerTypeSymbol pointer:
                AddSignatureTypes(pointer.Signature.ReturnType, into);
                foreach (var parameter in pointer.Signature.Parameters)
                {
                    AddSignatureTypes(parameter.Type, into);
                }

                break;
            // A tuple is a named type whose type arguments are its element types.
            // Always walk the type arguments: Task<A> and Task<List<B>> share one generic definition but name
            // different types. A type argument tree is finite, so the walk ends.
            case INamedTypeSymbol named:
                into.Add(named.OriginalDefinition);
                foreach (var argument in named.TypeArguments)
                {
                    AddSignatureTypes(argument, into);
                }

                break;
            default:
                break;
        }
    }
}
