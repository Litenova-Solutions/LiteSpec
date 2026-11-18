using LiteSpec.Cli.Model.Code;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>
///     Corrects the kinds the shared kind reader assigned from simple names, using
///     the namespace-qualified names of one read, and names a module-event contract
///     technical by its mark.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class ElementKindCorrection
{
    /// <summary>The record id prefix a module event mark names.</summary>
    private const string ModuleEventPrefix = "module-event/";

    /// <summary>The short project names of the outer layers that hold technical classes.</summary>
    static readonly string[] OuterLayerProjects = ["Application", "Infrastructure", "WebApi", "Worker"];

    /// <summary>
    ///     Corrects the kinds the shared kind reader assigned from simple names, using the
    ///     namespace-qualified names of this read: a kind that only a same-named type in
    ///     another namespace would give this type is dropped, an aggregate method of
    ///     a type that is no aggregate root loses its kind, and a module-event contract
    ///     with no kind reads technical.
    /// </summary>
    /// <param name="elements">The elements the shared kind reader read.</param>
    /// <param name="types">The type declarations by identifier span.</param>
    /// <param name="methods">The method declarations by identifier span.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <param name="containingProject">The short project name holding the file, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the file, or null when unknown.</param>
    /// <returns>The elements with their kinds corrected.</returns>
    public static IReadOnlyList<MarkableElement> CorrectElements(
        IReadOnlyList<MarkableElement> elements,
        IReadOnlyDictionary<TextSpan, TypeDeclarationSyntax> types,
        IReadOnlyDictionary<TextSpan, MethodDeclarationSyntax> methods,
        QualifiedKinds qualified,
        string? containingProject = null,
        string? containingLayer = null)
    {
        var corrected = new List<MarkableElement>(elements.Count);

        foreach (var element in elements)
        {
            CodeKind? kind = element.Kind;

            if (element.IsType && types.TryGetValue(element.Span, out var declared))
            {
                kind = CorrectKind(declared, element.Kind, qualified, containingProject, containingLayer);
            }
            else if (!element.IsType
                && element.Kind == CodeKind.AggregateMethod
                && methods.TryGetValue(element.Span, out var method)
                && method.Parent is TypeDeclarationSyntax parent
                && CorrectKind(parent, CodeKind.AggregateRoot, qualified, containingProject, containingLayer) != CodeKind.AggregateRoot)
            {
                kind = null;
            }
            else if (!element.IsType
                && element.Kind is null
                && methods.TryGetValue(element.Span, out var markedMethod)
                && markedMethod.Parent is TypeDeclarationSyntax markedParent
                && CorrectKind(markedParent, CodeKind.AggregateRoot, qualified, containingProject, containingLayer) == CodeKind.AggregateRoot
                && markedMethod.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
            {
                kind = CodeKind.AggregateMethod;
            }

            corrected.Add(kind == element.Kind
                ? element
                : new MarkableElement(element.Name, element.IsType, kind, element.Marks, element.Line, element.Span));
        }

        return corrected;
    }

    /// <summary>
    ///     The kind of one type from its namespace-qualified bases, following the shared
    ///     kind reader's order for the kinds a cross-file name can carry. Kinds that no
    ///     cross-file name carries keep the kind the shared reader assigned, except a
    ///     module-event contract with no kind, which reads technical by its mark.
    /// </summary>
    /// <param name="declared">The type declaration.</param>
    /// <param name="kind">The kind the shared reader assigned.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>The kind of the type from its namespace-qualified bases.</returns>
    public static CodeKind? CorrectKind(TypeDeclarationSyntax declared, CodeKind? kind, QualifiedKinds qualified, string? containingProject = null, string? containingLayer = null)
    {
        var qualifiedName = DotnetNames.QualifiedNameOf(declared);
        var resolved = qualified.ResolvedBases(declared);
        var simpleBases = resolved.Select(DotnetNames.SimpleName).ToHashSet(StringComparer.Ordinal);
        var otherPart = declared.BaseList is null
            && declared.Parent is not TypeDeclarationSyntax
            && declared.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

        if (qualified.MarkedAggregateRoots.Contains(qualifiedName)
            || resolved.Any(parent => DotnetNames.SimpleName(parent).StartsWith("AggregateRoot", StringComparison.Ordinal))
            || resolved.Any(parent => qualified.AggregateRoots.Contains(parent))
            || (otherPart && qualified.AggregateRoots.Contains(qualifiedName)))
        {
            return CodeKind.AggregateRoot;
        }

        if (declared is not InterfaceDeclarationSyntax && simpleBases.Contains("IDomainEvent"))
        {
            return CodeKind.DomainEvent;
        }

        if (!declared.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword))
            && ((otherPart && qualified.DomainExceptions.Contains(qualifiedName))
                || resolved.Any(parent =>
                    DotnetNames.SimpleName(parent) == "DomainException" || qualified.DomainExceptions.Contains(parent))))
        {
            return CodeKind.RuleException;
        }

        if (ElementReader.IsClassLike(declared)
            && (simpleBases.Contains("ICommandHandler") || simpleBases.Contains("IQueryHandler")))
        {
            return CodeKind.Handler;
        }

        if (ElementReader.IsClassLike(declared)
            && (simpleBases.Contains("IEventHandler") || simpleBases.Contains("IDomainEventReaction")))
        {
            return CodeKind.EventHandler;
        }

        if (kind == CodeKind.Job
            && ElementReader.IsClassLike(declared)
            && (((simpleBases.Contains("BackgroundService") || simpleBases.Contains("IHostedService"))
                    && ElementReader.HasJobName(declared.Identifier.ValueText))
                || MarkReader.Read(declared).Any(mark =>
                    mark.RecordId.StartsWith("job/", StringComparison.Ordinal))))
        {
            return CodeKind.Job;
        }

        if (kind is CodeKind.AggregateRoot or CodeKind.RuleException or CodeKind.Handler
            or CodeKind.EventHandler or CodeKind.DomainEvent or CodeKind.Job or CodeKind.Adapter)
        {
            if (declared is not InterfaceDeclarationSyntax
                && ((otherPart && qualified.DomainEvents.Contains(qualifiedName))
                    || resolved.Any(parent => qualified.DomainEvents.Contains(parent))))
            {
                return CodeKind.DomainEvent;
            }

            if (ElementReader.IsClassLike(declared)
                && ((otherPart
                        && (qualified.CommandHandlers.Contains(qualifiedName)
                            || qualified.QueryHandlers.Contains(qualifiedName)))
                    || resolved.Any(parent =>
                        qualified.CommandHandlers.Contains(parent) || qualified.QueryHandlers.Contains(parent))))
            {
                return CodeKind.Handler;
            }

            if (ElementReader.IsClassLike(declared)
                && ((otherPart && qualified.EventHandlers.Contains(qualifiedName))
                    || resolved.Any(parent => qualified.EventHandlers.Contains(parent))))
            {
                return CodeKind.EventHandler;
            }

            if (ElementReader.IsClassLike(declared)
                && ElementReader.HasJobName(declared.Identifier.ValueText)
                && ((otherPart && qualified.JobHosts.Contains(qualifiedName))
                    || resolved.Any(parent => qualified.JobHosts.Contains(parent))))
            {
                return CodeKind.Job;
            }

            if (kind == CodeKind.Adapter
                && ElementReader.IsClassLike(declared)
                && resolved.Any(parent => qualified.MarkedPorts.Contains(parent)))
            {
                return CodeKind.Adapter;
            }

            return null;
        }

        if (kind == CodeKind.Technical
            && otherPart
            && !qualified.TechnicalClasses.Contains(qualifiedName)
            && !declared.Members.OfType<MethodDeclarationSyntax>().Any()
            && !MarkReader.Read(declared).Any(mark =>
                mark.RecordId.StartsWith("setting/", StringComparison.Ordinal)
                || mark.RecordId.StartsWith(ModuleEventPrefix, StringComparison.Ordinal)))
        {
            return null;
        }

        // A module-event contract is technical by its mark, as an integration-event
        // contract is in the shared reader: a record, an interface or a class with
        // no method carries it, as the mark names the type's role.
        if (kind is null
            && declared.Parent is not TypeDeclarationSyntax
            && IsOuterLayer(containingProject, containingLayer)
            && MarkReader.Read(declared).Any(mark => mark.RecordId.StartsWith(ModuleEventPrefix, StringComparison.Ordinal)))
        {
            return CodeKind.Technical;
        }

        return kind;
    }

    /// <summary>Whether the containing project or layer is an outer layer that holds technical classes.</summary>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the project or the layer is an outer layer; a null project matches.</returns>
    private static bool IsOuterLayer(string? containingProject, string? containingLayer)
    {
        if (containingProject is null)
        {
            return true;
        }

        foreach (var shortName in OuterLayerProjects)
        {
            if (containingProject.Equals(shortName, StringComparison.Ordinal)
                || containingProject.EndsWith("." + shortName, StringComparison.Ordinal))
            {
                return true;
            }

            if (containingLayer is not null && containingLayer == shortName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Merges the partial parts of one aggregate root into one element: parts
    ///     count as one root and a mark on any part counts. The merged element
    ///     keeps the earliest part's file and line and carries every part's marks.
    ///     Parts group by class and folder, so two roots sharing a simple name in
    ///     different modules never merge.
    /// </summary>
    /// <param name="elements">The marked elements.</param>
    /// <returns>The elements with the partial parts of one aggregate root merged.</returns>
    public static List<MarkedElement> MergeAggregateRootPartials(List<MarkedElement> elements)
    {
        if (elements.All(element => element.Kind != CodeKind.AggregateRoot))
        {
            return elements;
        }

        var merged = new List<MarkedElement>();
        var parts = new Dictionary<string, List<MarkedElement>>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            if (element.Kind != CodeKind.AggregateRoot)
            {
                merged.Add(element);
                continue;
            }

            var key = element.Class + "\0" + (Path.GetDirectoryName(element.File) ?? "");

            if (!parts.TryGetValue(key, out var group))
            {
                group = [];
                parts[key] = group;
            }

            group.Add(element);
        }

        foreach (var group in parts.Values)
        {
            var first = group
                .OrderBy(part => part.File, StringComparer.Ordinal)
                .ThenBy(part => part.Line)
                .First();
            var marks = group
                .SelectMany(part => part.Marks)
                .OrderBy(mark => mark.File, StringComparer.Ordinal)
                .ThenBy(mark => mark.Line)
                .ToList();

            merged.Add(first with { Marks = marks });
        }

        return merged
            .OrderBy(element => element.File, StringComparer.Ordinal)
            .ThenBy(element => element.Line)
            .ThenBy(element => element.Class, StringComparer.Ordinal)
            .ThenBy(element => element.Member ?? "", StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    ///     Merges the partial parts of one technical class into one element: parts
    ///     count as one class and a mark on any part counts. The merged element
    ///     keeps the earliest part's file and line and carries every part's marks.
    /// </summary>
    /// <param name="elements">The marked elements.</param>
    /// <returns>The elements with the partial parts of one technical class merged.</returns>
    public static List<MarkedElement> MergeTechnicalPartials(List<MarkedElement> elements)
    {
        if (elements.All(element => element.Kind != CodeKind.Technical))
        {
            return elements;
        }

        var merged = new List<MarkedElement>();
        var parts = new Dictionary<string, List<MarkedElement>>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            if (element.Kind != CodeKind.Technical)
            {
                merged.Add(element);
                continue;
            }

            if (!parts.TryGetValue(element.Class, out var group))
            {
                group = [];
                parts[element.Class] = group;
            }

            group.Add(element);
        }

        foreach (var group in parts.Values)
        {
            var first = group
                .OrderBy(part => part.File, StringComparer.Ordinal)
                .ThenBy(part => part.Line)
                .First();
            var marks = group
                .SelectMany(part => part.Marks)
                .OrderBy(mark => mark.File, StringComparer.Ordinal)
                .ThenBy(mark => mark.Line)
                .ToList();

            merged.Add(first with { Marks = marks });
        }

        return merged
            .OrderBy(element => element.File, StringComparer.Ordinal)
            .ThenBy(element => element.Line)
            .ThenBy(element => element.Class, StringComparer.Ordinal)
            .ThenBy(element => element.Member ?? "", StringComparer.Ordinal)
            .ToList();
    }
}
