using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     The aggregates of one compilation: which aggregate folder a file belongs to, and
///     whether a type plays an aggregate role. Under one project per module an aggregate
///     lives in <c>Domain/{Aggregates}/</c> below the module root, the plural folder holding
///     the singular root type; under one modules project it lives there below its module
///     folder. A file directly under a module <c>Domain/</c> folder belongs to no aggregate.
///     Roles read by name, in any namespace:
///     a root derives from <c>AggregateRoot</c> with one type argument, a child entity from
///     <c>Entity</c> with one type argument, an event implements <c>IDomainEvent</c>, and an
///     exception derives from <c>System.Exception</c>.
/// </summary>
/// <follows>litespec/core/backend-domain#reach-an-aggregate-only-through-its-root</follows>
internal static class Aggregates
{
    /// <summary>Folder names that never name an aggregate.</summary>
    private static readonly HashSet<string> KnownFolders = new(StringComparer.Ordinal)
    {
        "Domain",
        "Application",
        "Infrastructure",
        "WebApi",
        "Contracts",
        "Composition",
        "Notifications",
        "ExternalSystems",
        "ReadModels",
        "Events",
        "Exceptions",
        "Persistence",
        "Modules",
    };

    /// <summary>
    ///     Returns the aggregate folder holding the file, or null when no aggregate does.
    ///     A <c>Domain/</c> folder directly under a module holds one aggregate per child
    ///     folder; a <c>Domain/</c> folder anywhere else holds none.
    /// </summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="modules">The module folders, as forward-slash directory paths.</param>
    /// <returns>The aggregate folder as a forward-slash directory path, or null.</returns>
    public static string? AggregateOf(string? filePath, IReadOnlyCollection<string> modules)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var segments = filePath!.Replace('\\', '/').Split('/');
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (!segments[index].Equals("Domain", StringComparison.Ordinal))
            {
                continue;
            }

            var parent = string.Join("/", segments, 0, index);
            if (!IsModule(parent, modules))
            {
                continue;
            }

            if (index + 2 < segments.Length && !KnownFolders.Contains(segments[index + 1]))
            {
                return parent + "/Domain/" + segments[index + 1];
            }

            return null;
        }

        return null;
    }

    /// <summary>Returns true when the type declares a file under the aggregate folder; otherwise false.</summary>
    /// <param name="candidate">The type under inspection.</param>
    /// <param name="aggregate">The aggregate folder as a forward-slash directory path.</param>
    /// <returns>True when a declaring file sits under the aggregate folder.</returns>
    public static bool DeclaredIn(INamedTypeSymbol candidate, string aggregate)
    {
        foreach (var reference in candidate.DeclaringSyntaxReferences)
        {
            var path = reference.SyntaxTree.FilePath;
            if (!string.IsNullOrEmpty(path)
                && path.Replace('\\', '/').StartsWith(aggregate + "/", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type derives from the generic aggregate root base; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is an aggregate root.</returns>
    public static bool IsRoot(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (definition.Name == "AggregateRoot" && definition.Arity == 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type derives from the generic entity base; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a child entity.</returns>
    public static bool IsEntity(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (definition.Name == "Entity" && definition.Arity == 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type implements the domain event contract; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a domain event.</returns>
    public static bool IsDomainEvent(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IDomainEvent")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type derives from <see cref="System.Exception" />; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is an exception.</returns>
    public static bool IsException(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "Exception"
                && current.ContainingNamespace is not null
                && current.ContainingNamespace.ToDisplayString() == "System")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the folder is a module of the compilation; otherwise false.</summary>
    /// <param name="folder">The folder as a forward-slash directory path.</param>
    /// <param name="modules">The module folders, as forward-slash directory paths.</param>
    /// <returns>True when the folder is a module.</returns>
    private static bool IsModule(string folder, IReadOnlyCollection<string> modules)
    {
        foreach (var module in modules)
        {
            if (folder.Equals(module, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
