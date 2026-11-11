using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;

using static LiteSpec.Analyzers.Generation.Sources.MarkAttributeWriter;
using static LiteSpec.Analyzers.Generation.Sources.ValuePartWriter;
using static LiteSpec.Analyzers.Generation.Sources.RegistryWriter;
using static LiteSpec.Analyzers.Generation.Sources.PermissionWriter;
using static LiteSpec.Analyzers.Generation.Sources.RoleWriter;

namespace LiteSpec.Analyzers.Generation.Sources;

/// <summary>Writes the generator's sources from the records, the marked types and the host: it decides which sources a project gets and hands each to its writer. Every value is written only where no member of that name is declared in source, and only where its record is known.</summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class GeneratedSourceWriter
{
    /// <summary>Writes every source the inputs call for, or nothing without a usable project file, with no records and no marks, or in an opted-out project.</summary>
    /// <param name="context">The source production context.</param>
    /// <param name="records">The record facts.</param>
    /// <param name="types">The marked types.</param>
    /// <param name="host">The compilation facts.</param>
    public static void Execute(
        SourceProductionContext context,
        RecordModel records,
        ImmutableArray<MarkedType> types,
        HostModel host)
    {
        // The shared kernel reads layer None with no application project and no host,
        // so it leaves through the same gate as an opted-out project and generates nothing.
        if (host.Layer == ArchitectureLayer.None && !host.ApplicationProject)
        {
            return;
        }

        if (!records.Valid)
        {
            return;
        }

        if (records.EventIds.IsEmpty
            && records.RuleCodes.IsEmpty
            && records.Permissions.IsEmpty
            && records.Roles.IsEmpty
            && records.Grants.IsEmpty
            && types.IsEmpty)
        {
            return;
        }

        var ordered = OrderedByFullName(types);

        if (records.Style == MarkStyle.Attribute)
        {
            context.AddSource("LiteSpecAttributes.g.cs", AttributesText(host.XunitV3));
        }

        var events = new HashSet<string>(records.EventIds, StringComparer.Ordinal);
        var codes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var rule in records.RuleCodes)
        {
            codes[rule.RuleId] = rule.Code;
        }

        var grants = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var grant in records.Grants)
        {
            grants[grant.UseCaseId] = grant.Grant;
        }

        foreach (var type in ordered)
        {
            if (type.Kind == CodeKind.DomainEvent
                && type.IsPartial
                && !type.HasClassification
                && FirstWithPrefix(type.MarkIds, "event/") is string eventId
                && events.Contains(eventId))
            {
                context.AddSource(HintName("LiteSpecEvent", type), EventText(type, eventId));
            }
            else if (type.Kind == CodeKind.RuleException
                && type.IsPartial
                && !type.HasCode
                && FirstWithPrefix(type.MarkIds, "rule/") is string ruleId
                && codes.TryGetValue(ruleId, out var code))
            {
                context.AddSource(HintName("LiteSpecException", type), ExceptionText(type, code));
            }
        }

        // A host beside the modules project sees the registry, the permissions and the
        // roles through that project, so it generates none of its own.
        if (!host.ModuleHost)
        {
            EmitRegistry(context, ordered, grants, host.Layer == ArchitectureLayer.WebApi || host.ApplicationProject);
        }
        else if (host.Layer == ArchitectureLayer.WebApi)
        {
            // The host still maps the operations of the endpoints it holds, which the modules
            // project cannot see.
            context.AddSource("LiteSpecHostOperations.g.cs", HostOperationsText(OperationUseCasesOf(ordered)));
        }

        if (!host.ModuleHost
            && (host.Layer == ArchitectureLayer.Domain
                || host.Layer == ArchitectureLayer.Application
                || host.Layer == ArchitectureLayer.Infrastructure
                || host.Layer == ArchitectureLayer.WebApi
                || host.ApplicationProject))
        {
            EmitPermissions(context, records);
            EmitRoles(context, records);
        }
    }

    /// <summary>Returns the by full name.</summary>
    /// <param name="types">The types collected so far, in discovery order.</param>
    /// <returns>The by full name, in the order this method builds them, or an empty collection when none apply.</returns>
    private static List<MarkedType> OrderedByFullName(ImmutableArray<MarkedType> types)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<MarkedType>();

        foreach (var type in types)
        {
            if (seen.Add(type.FullName))
            {
                ordered.Add(type);
            }
        }

        ordered.Sort(static (left, right) => string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));

        return ordered;
    }

    /// <summary>Returns the with prefix.</summary>
    /// <param name="ids">The identifiers collected so far, without duplicates.</param>
    /// <param name="prefix">The text written before each generated entry.</param>
    /// <returns>The with prefix as text, or null when none applies.</returns>
    internal static string? FirstWithPrefix(ImmutableArray<string> ids, string prefix)
    {
        foreach (var id in ids)
        {
            if (id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>Returns the name.</summary>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The name as text.</returns>
    private static string HintName(string kind, MarkedType type) =>
        kind + "." + type.FullName.Replace('`', '_') + ".g.cs";
}
