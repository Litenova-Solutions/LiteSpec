using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Analyzers.Records;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     What every traceability rule needs for one compilation: its layer, the
///     consumer's records, the cross-file kind names, the event id of every
///     event type marked in source, and the known-gap and deviation sets built
///     once so reporting a diagnostic never rescans every record.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class TraceabilityContext
{
    private static readonly ConditionalWeakTable<Compilation, TraceabilityContext> Cache = new();

    private readonly HashSet<string> heldFiles;
    private readonly HashSet<string> heldSubjects;
    private readonly List<Gap> gaps;

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

        var today = DateTime.UtcNow.Date;
        heldFiles = new HashSet<string>(StringComparer.Ordinal);
        heldSubjects = new HashSet<string>(StringComparer.Ordinal);
        gaps = new List<Gap>();

        foreach (var record in store.Index.Records)
        {
            if (record.Type == "deviation"
                && record.Status == "approved"
                && record.Field("reviewBy") is string reviewBy
                && IsoDate(reviewBy, out var review)
                && today <= review)
            {
                foreach (var path in record.Ids("code"))
                {
                    heldFiles.Add(path.Replace('\\', '/'));
                }

                foreach (var subject in record.Ids("subject"))
                {
                    heldSubjects.Add(subject);
                }
            }
            else if (record.Type == "known-gap"
                && record.Status != "retired"
                && record.Field("check") is string check
                && check.Length > 0
                && record.Field("endsBy") is string endsBy
                && IsoDate(endsBy, out var ends)
                && today <= ends)
            {
                gaps.Add(new Gap(check, new HashSet<string>(record.Ids("subject"), StringComparer.Ordinal)));
            }
        }
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
    ///     Whether an open known gap or an in-date approved deviation covers the
    ///     finding, as <see cref="Suppression.Partition" /> decides for the CLI but
    ///     from the sets built once in the constructor.
    /// </summary>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding is known and stays silent.</returns>
    public bool IsKnown(Finding finding)
    {
        if (finding.Code != "known-gap-expired" && heldFiles.Contains(RootRelative(Store.Root, finding.Path)))
        {
            return true;
        }

        if (finding.Code == "known-gap-expired" || finding.Subject is null)
        {
            return false;
        }

        var subjects = new List<string> { finding.Subject };

        if ((finding.Subject.StartsWith("scenario/", StringComparison.Ordinal) || finding.Subject.StartsWith("result/", StringComparison.Ordinal))
            && Identifiers.TrySplit(finding.Subject, out _, out var home, out _)
            && home is not null
            && Store.UseCaseOfHome(home) is string holder)
        {
            subjects.Add(holder);
        }

        foreach (var subject in subjects)
        {
            if (heldSubjects.Contains(subject))
            {
                return true;
            }
        }

        foreach (var gap in gaps)
        {
            if (gap.Check == finding.Code && gap.Subjects.Overlaps(subjects))
            {
                return true;
            }
        }

        return false;
    }

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
        var roots = new HashSet<string>(StringComparer.Ordinal);
        var exceptions = new HashSet<string>(StringComparer.Ordinal);
        var ports = new HashSet<string>(StringComparer.Ordinal);
        var events = new Dictionary<string, string>(StringComparer.Ordinal);

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

    /// <summary>Reads the cached context for the compilation, when one was built.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="context">The cached context, or null.</param>
    /// <returns>True when the compilation already has a context.</returns>
    public static bool TryGet(Compilation compilation, out TraceabilityContext? context) =>
        Cache.TryGetValue(compilation, out context);

    /// <summary>
    ///     Reads the cached context for the compilation or builds and caches it:
    ///     every traceability analyzer reuses the one walk over the source types.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="layer">The layer the compilation belongs to.</param>
    /// <param name="store">The consumer's records.</param>
    /// <returns>The compilation's context.</returns>
    public static TraceabilityContext GetOrCreate(Compilation compilation, Layer layer, RecordStore store)
    {
        if (Cache.TryGetValue(compilation, out var cached)
            && cached.Layer == layer
            && ReferenceEquals(cached.Store, store))
        {
            return cached;
        }

        var created = Create(compilation, layer, store);
        var result = Cache.GetValue(compilation, _ => created);

        if (result.Layer == layer && ReferenceEquals(result.Store, store))
        {
            return result;
        }

        return created;
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
                && value.StartsWith("external-system/", StringComparison.Ordinal))
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
        if (!MarkVerbNames.TryParseAttributeName(attribute.AttributeClass?.Name, out _))
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
                    if (mark.RecordId.StartsWith("event/", StringComparison.Ordinal))
                    {
                        return mark.RecordId;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>A finding's file as a forward-slash path from the project root.</summary>
    private static string RootRelative(string root, string path)
    {
        var relative = Path.IsPathRooted(path) ? RelativeTo(root, path) : path;

        return relative.Replace('\\', '/');
    }

    /// <summary>
    ///     The path from one folder to another, with forward slashes: dot when both name
    ///     the same folder, the path itself when they share no root.
    /// </summary>
    private static string RelativeTo(string root, string path)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var from = root.Replace('\\', '/').TrimEnd('/');
        var to = path.Replace('\\', '/');

        if (!string.Equals(RootName(from), RootName(to), comparison))
        {
            return path;
        }

        var fromSegments = from.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var toSegments = to.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var common = 0;

        while (common < fromSegments.Length
            && common < toSegments.Length
            && string.Equals(fromSegments[common], toSegments[common], comparison))
        {
            common++;
        }

        var parts = new List<string>();

        for (var index = common; index < fromSegments.Length; index++)
        {
            parts.Add("..");
        }

        for (var index = common; index < toSegments.Length; index++)
        {
            parts.Add(toSegments[index]);
        }

        return parts.Count == 0 ? "." : string.Join("/", parts);
    }

    /// <summary>The root two paths must share: a drive, a UNC share, or the filesystem root.</summary>
    private static string RootName(string value)
    {
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            var segments = value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            return segments.Length >= 2
                ? "//" + segments[0] + "/" + segments[1]
                : value;
        }

        if (value.Length >= 2 && value[1] == ':')
        {
            return value.Substring(0, 2);
        }

        return value.StartsWith("/", StringComparison.Ordinal) ? "/" : "";
    }

    private static bool IsoDate(string text, out DateTime date) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private sealed class Gap
    {
        public Gap(string check, HashSet<string> subjects)
        {
            Check = check;
            Subjects = subjects;
        }

        public string Check { get; }

        public HashSet<string> Subjects { get; }
    }
}
