using System.Collections.Generic;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Analyzers.Records;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     What every traceability rule needs for one compilation: its layer, the
///     consumer's records, the cross-file kind names, and the event id of every
///     event type marked in source.
/// </summary>
internal sealed class TraceabilityContext
{
    /// <summary>Creates the context for one compilation.</summary>
    /// <param name="layer">The layer the compilation belongs to.</param>
    /// <param name="store">The consumer's records.</param>
    /// <param name="kinds">The cross-file kind names.</param>
    /// <param name="eventIds">The event id by simple event type name.</param>
    /// <param name="project">The assembly name, for project checks.</param>
    public TraceabilityContext(
        Layer layer, RecordStore store, KindContext kinds, IReadOnlyDictionary<string, string> eventIds, string project)
    {
        Layer = layer;
        Store = store;
        Kinds = kinds;
        EventIds = eventIds;
        Project = project;
    }

    /// <summary>The layer the compilation belongs to.</summary>
    public Layer Layer { get; }

    /// <summary>The consumer's records.</summary>
    public RecordStore Store { get; }

    /// <summary>The cross-file kind names.</summary>
    public KindContext Kinds { get; }

    /// <summary>The event id by simple event type name.</summary>
    public IReadOnlyDictionary<string, string> EventIds { get; }

    /// <summary>The assembly name, for project checks.</summary>
    public string Project { get; }

    /// <summary>Whether the compilation is a test project.</summary>
    public bool OnTest => Layer == Layer.Test;

    /// <summary>
    ///     Builds the context for one compilation: walks every source type once
    ///     for the aggregate roots, domain exceptions, marked ports and event ids.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="layer">The layer the compilation belongs to.</param>
    /// <param name="store">The consumer's records.</param>
    /// <returns>The context.</returns>
    public static TraceabilityContext Create(Compilation compilation, Layer layer, RecordStore store)
    {
        var roots = new HashSet<string>(System.StringComparer.Ordinal);
        var exceptions = new HashSet<string>(System.StringComparer.Ordinal);
        var ports = new HashSet<string>(System.StringComparer.Ordinal);
        var events = new Dictionary<string, string>(System.StringComparer.Ordinal);

        foreach (var type in SourceTypes(compilation))
        {
            if (DerivesFrom(type, "AggregateRoot"))
            {
                roots.Add(type.Name);
            }

            if (DerivesFrom(type, "DomainException"))
            {
                exceptions.Add(type.Name);
            }

            foreach (var contract in type.AllInterfaces)
            {
                if (IsExternalSystemPort(contract))
                {
                    ports.Add(contract.Name);
                }
            }

            if (IsEventType(type) && !events.ContainsKey(type.Name) && EventIdOf(type) is string id)
            {
                events.Add(type.Name, id);
            }
        }

        return new TraceabilityContext(
            layer, store, new KindContext(roots, exceptions, ports), events, compilation.AssemblyName ?? string.Empty);
    }

    private static IEnumerable<INamedTypeSymbol> SourceTypes(Compilation compilation)
    {
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(compilation.Assembly.GlobalNamespace);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (var member in current.GetMembers())
            {
                if (member is INamespaceSymbol space)
                {
                    stack.Push(space);
                }
                else if (member is INamedTypeSymbol type && IsSource(type))
                {
                    yield return type;
                    stack.Push(type);
                }
            }
        }
    }

    private static bool IsSource(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree.GetRoot().Language == LanguageNames.CSharp)
            {
                return true;
            }
        }

        return false;
    }

    private static bool DerivesFrom(INamedTypeSymbol type, string baseName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == baseName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEventType(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return false;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IDomainEvent")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the interface carries an external-system mark, in source or in metadata.</summary>
    private static bool IsExternalSystemPort(INamedTypeSymbol contract)
    {
        foreach (var attribute in contract.GetAttributes())
        {
            if (MarkAttributeValue(attribute) is string value
                && value.StartsWith("external-system/", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var reference in contract.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax member)
            {
                foreach (var mark in MarkReader.Read(member))
                {
                    if (mark.RecordId.StartsWith("external-system/", System.StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string? MarkAttributeValue(AttributeData attribute)
    {
        if (!LiteSpec.Model.MarkVerbNames.TryParseAttributeName(attribute.AttributeClass?.Name, out _))
        {
            return null;
        }

        if (attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string positional)
        {
            return positional.Trim();
        }

        return null;
    }

    /// <summary>The first event mark on the type, or null when it carries none.</summary>
    private static string? EventIdOf(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax member)
            {
                foreach (var mark in MarkReader.Read(member))
                {
                    if (mark.RecordId.StartsWith("event/", System.StringComparison.Ordinal))
                    {
                        return mark.RecordId;
                    }
                }
            }
        }

        return null;
    }
}
