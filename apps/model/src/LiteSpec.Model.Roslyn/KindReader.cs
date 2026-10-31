using System;
using System.Collections.Generic;
using System.Linq;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Recognizes the LiteSpec code kind of a type or method from the code,
///     never from the mark, except for ports, domain services, and job hosts,
///     which are recognized by their own mark, for adapters, recognized
///     through the marked port they implement, and for aggregate methods
///     that raise nothing, recognized once they carry a mark. The syntax overloads read
///     one declaration; the symbol overloads walk base types and interfaces
///     semantically. Both agree on the same code
///     except where syntax cannot see across files: transitive handler and job
///     bases resolve only from symbols, and transitive aggregate roots, domain
///     exceptions, and marked ports resolve from syntax only through the
///     <see cref="KindContext" /> names the host collected. When several kinds
///     match one declaration, the first in the design table order wins; a test
///     attribute wins over an aggregate method body. A technical class is last:
///     a top-level class in an outer-layer project that takes no other kind and
///     declares an ordinary method, excluding generated code, migrations, and
///     nested types. FrontendModule is a frontend kind and is never recognized
///     from C#.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class KindReader
{
    private const string ExternalSystemPrefix = "external-system/";

    private const string RulePrefix = "rule/";

    private const string SettingPrefix = "setting/";

    private const string JobPrefix = "job/";

    private static readonly HashSet<string> TestAttributeNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Fact",
        "Theory",
        "Scenario",
        "Test",
        "TestCase",
        "TestCaseSource",
        "TestMethod",
        "DataTestMethod",
    };

    /// <summary>Recognizes the kind of a type declaration from syntax.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <param name="containingProject">
    ///     The short project name holding the type, such as <c>Application</c> or
    ///     <c>Shop.WebApi</c>, or null when unknown. A null project skips the
    ///     project check, so a marked interface reads as a port, a marked class reads
    ///     as a job, and a type named <c>*Endpoint</c> reads as an endpoint.
    /// </param>
    /// <returns>The kind, or null when the type is of no markable kind.</returns>
    /// <exception cref="ArgumentNullException">The type or context is null.</exception>
    public static CodeKind? KindOf(TypeDeclarationSyntax type, KindContext context, string? containingProject = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (IsAggregateRoot(type, context))
        {
            return CodeKind.AggregateRoot;
        }

        if (IsDomainEvent(type))
        {
            return CodeKind.DomainEvent;
        }

        if (IsRuleException(type, context))
        {
            return CodeKind.RuleException;
        }

        if (IsHandler(type))
        {
            return CodeKind.Handler;
        }

        if (IsEventHandler(type))
        {
            return CodeKind.EventHandler;
        }

        if (IsJob(type, containingProject))
        {
            return CodeKind.Job;
        }

        if (IsPort(type, containingProject))
        {
            return CodeKind.Port;
        }

        if (IsAdapter(type, context))
        {
            return CodeKind.Adapter;
        }

        if (IsEndpoint(type, containingProject))
        {
            return CodeKind.Endpoint;
        }

        if (IsDomainService(type, containingProject))
        {
            return CodeKind.DomainService;
        }

        if (TransitiveKindOf(type, context) is CodeKind transitive)
        {
            return transitive;
        }

        return IsTechnical(type, context, containingProject) ? CodeKind.Technical : (CodeKind?)null;
    }

    /// <summary>Recognizes the kind of a method declaration from syntax.</summary>
    /// <param name="method">The method declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>The kind, or null when the method is of no markable kind.</returns>
    /// <exception cref="ArgumentNullException">The method or context is null.</exception>
    public static CodeKind? KindOf(MethodDeclarationSyntax method, KindContext context)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (IsTestMethod(method))
        {
            return CodeKind.Test;
        }

        if (IsAggregateMethod(method, context))
        {
            return CodeKind.AggregateMethod;
        }

        return null;
    }

    /// <summary>Recognizes the kind of a type symbol, walking base types and interfaces.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <param name="containingProject">
    ///     The short project name holding the type, such as <c>Application</c> or
    ///     <c>Shop.WebApi</c>, or null when unknown. A null project skips the
    ///     project check, so a marked interface reads as a port, a marked class reads
    ///     as a job, and a type named <c>*Endpoint</c> reads as an endpoint.
    /// </param>
    /// <returns>The kind, or null when the type is of no markable kind.</returns>
    /// <exception cref="ArgumentNullException">The type or context is null.</exception>
    public static CodeKind? KindOf(INamedTypeSymbol type, KindContext context, string? containingProject = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (IsAggregateRoot(type, context))
        {
            return CodeKind.AggregateRoot;
        }

        if (IsDomainEvent(type))
        {
            return CodeKind.DomainEvent;
        }

        if (IsRuleException(type, context))
        {
            return CodeKind.RuleException;
        }

        if (IsHandler(type))
        {
            return CodeKind.Handler;
        }

        if (IsEventHandler(type))
        {
            return CodeKind.EventHandler;
        }

        if (IsJob(type, containingProject))
        {
            return CodeKind.Job;
        }

        if (IsPort(type, containingProject))
        {
            return CodeKind.Port;
        }

        if (IsAdapter(type, context))
        {
            return CodeKind.Adapter;
        }

        if (IsEndpoint(type, containingProject))
        {
            return CodeKind.Endpoint;
        }

        if (IsDomainService(type, containingProject))
        {
            return CodeKind.DomainService;
        }

        return IsTechnical(type, containingProject) ? CodeKind.Technical : (CodeKind?)null;
    }

    /// <summary>Recognizes the kind of a method symbol.</summary>
    /// <param name="method">The method symbol.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>The kind, or null when the method is of no markable kind.</returns>
    /// <exception cref="ArgumentNullException">The method or context is null.</exception>
    public static CodeKind? KindOf(IMethodSymbol method, KindContext context)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (IsTestMethod(method))
        {
            return CodeKind.Test;
        }

        if (IsAggregateMethod(method, context))
        {
            return CodeKind.AggregateMethod;
        }

        return null;
    }

    /// <summary>
    ///     Whether a declaration is another part of a partial type declared elsewhere: a top-level
    ///     partial declaration with no base list. Only such a part takes its kind from its name;
    ///     any other type is known by its bases, so a nested type sharing a root's simple name is
    ///     never mistaken for the root.
    /// </summary>
    private static bool IsOtherPart(TypeDeclarationSyntax type) =>
        type.BaseList is null
        && type.Parent is not TypeDeclarationSyntax
        && type.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

    private static bool IsAggregateRoot(TypeDeclarationSyntax type, KindContext context)
    {
        if (IsOtherPart(type) && context.AggregateRoots.Contains(type.Identifier.ValueText))
        {
            return true;
        }

        foreach (var baseType in BaseTypes(type))
        {
            if (IsAggregateRootBase(baseType) || context.AggregateRoots.Contains(SyntaxNames.SimpleName(baseType)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAggregateRootBase(TypeSyntax type)
    {
        switch (type)
        {
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText == "AggregateRoot"
                    && generic.TypeArgumentList.Arguments.Count == 1;
            case QualifiedNameSyntax qualified:
                return IsAggregateRootBase(qualified.Right);
            case AliasQualifiedNameSyntax alias:
                return IsAggregateRootBase(alias.Name);
            default:
                return false;
        }
    }

    // A symbol sees its whole base chain across every partial part, so it is known by its bases
    // only; its own name would mistake a nested type sharing a root's simple name for the root.
    private static bool IsAggregateRoot(INamedTypeSymbol type, KindContext context)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (definition.Name == "AggregateRoot" && definition.Arity == 1)
            {
                return true;
            }

            if (context.AggregateRoots.Contains(current.Name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDomainEvent(TypeDeclarationSyntax type) =>
        !(type is InterfaceDeclarationSyntax) && HasBaseName(type, "IDomainEvent");

    private static bool IsDomainEvent(INamedTypeSymbol type) =>
        type.TypeKind != TypeKind.Interface && Implements(type, "IDomainEvent");

    private static bool IsRuleException(TypeDeclarationSyntax type, KindContext context)
    {
        if (type.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword)))
        {
            return false;
        }

        if (IsOtherPart(type) && context.DomainExceptions.Contains(type.Identifier.ValueText))
        {
            return true;
        }

        foreach (var baseType in BaseTypes(type))
        {
            var name = SyntaxNames.SimpleName(baseType);
            if (name == "DomainException" || context.DomainExceptions.Contains(name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRuleException(INamedTypeSymbol type, KindContext context)
    {
        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
        {
            return false;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "DomainException" || context.DomainExceptions.Contains(current.Name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHandler(TypeDeclarationSyntax type) =>
        IsClassLike(type) && (HasBaseName(type, "ICommandHandler") || HasBaseName(type, "IQueryHandler"));

    private static bool IsHandler(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && (Implements(type, "ICommandHandler") || Implements(type, "IQueryHandler"));

    private static bool IsEventHandler(TypeDeclarationSyntax type) =>
        IsClassLike(type) && (HasBaseName(type, "IEventHandler") || HasBaseName(type, "IDomainEventReaction"));

    private static bool IsEventHandler(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && (Implements(type, "IEventHandler") || Implements(type, "IDomainEventReaction"));

    private static bool IsJob(TypeDeclarationSyntax type, string? containingProject) =>
        IsClassLike(type) && (
            ((HasBaseName(type, "BackgroundService") || HasBaseName(type, "IHostedService"))
                && HasJobName(type.Identifier.ValueText))
            || (ProjectMatches(containingProject, "Application", "Application.Abstractions", "Infrastructure", "Worker")
                && HasMark(type, JobPrefix)));

    private static bool IsJob(INamedTypeSymbol type, string? containingProject)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (HasJobName(type.Name) && current.Name == "BackgroundService")
            {
                return true;
            }
        }

        if (HasJobName(type.Name) && Implements(type, "IHostedService"))
        {
            return true;
        }

        return ProjectMatches(containingProject, "Application", "Application.Abstractions", "Infrastructure", "Worker")
            && HasMark(type, JobPrefix);
    }

    private static bool HasJobName(string name) =>
        name.EndsWith("Job", StringComparison.Ordinal) || name.EndsWith("Worker", StringComparison.Ordinal);

    private static bool IsPort(TypeDeclarationSyntax type, string? containingProject) =>
        type is InterfaceDeclarationSyntax
        && ProjectMatches(containingProject, "Application", "Application.Abstractions")
        && HasExternalSystemMark(type);

    private static bool IsPort(INamedTypeSymbol type, string? containingProject) =>
        type.TypeKind == TypeKind.Interface
        && ProjectMatches(containingProject, "Application", "Application.Abstractions")
        && HasExternalSystemMark(type);

    private static bool IsDomainService(TypeDeclarationSyntax type, string? containingProject) =>
        type is ClassDeclarationSyntax
        && ProjectMatches(containingProject, "Domain")
        && HasMark(type, RulePrefix);

    private static bool IsDomainService(INamedTypeSymbol type, string? containingProject) =>
        type.TypeKind == TypeKind.Class
        && ProjectMatches(containingProject, "Domain")
        && HasMark(type, RulePrefix);

    private static bool HasExternalSystemMark(TypeDeclarationSyntax type) => HasMark(type, ExternalSystemPrefix);

    private static bool HasExternalSystemMark(INamedTypeSymbol type) => HasMark(type, ExternalSystemPrefix);

    private static bool HasMark(TypeDeclarationSyntax type, string prefix)
    {
        foreach (var mark in MarkReader.Read(type))
        {
            if (mark.RecordId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMark(INamedTypeSymbol type, string prefix)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (MarkAttributeValue(attribute) is string value
                && value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MemberDeclarationSyntax member)
            {
                foreach (var mark in MarkReader.Read(member))
                {
                    if (mark.RecordId.StartsWith(prefix, StringComparison.Ordinal))
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

        foreach (var named in attribute.NamedArguments)
        {
            if (named.Value.Value is string namedValue)
            {
                return namedValue.Trim();
            }
        }

        return null;
    }

    private static bool IsAdapter(TypeDeclarationSyntax type, KindContext context)
    {
        if (!IsClassLike(type))
        {
            return false;
        }

        foreach (var baseType in BaseTypes(type))
        {
            if (context.MarkedPorts.Contains(SyntaxNames.SimpleName(baseType)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAdapter(INamedTypeSymbol type, KindContext context)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (context.MarkedPorts.Contains(contract.Name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEndpoint(TypeDeclarationSyntax type, string? containingProject) =>
        IsClassLike(type)
        && (HasBaseName(type, "IEndpoint")
            || (type.Identifier.ValueText.EndsWith("Endpoint", StringComparison.Ordinal)
                && ProjectMatches(containingProject, "WebApi")));

    private static bool IsEndpoint(INamedTypeSymbol type, string? containingProject) =>
        type.TypeKind == TypeKind.Class
        && (Implements(type, "IEndpoint")
            || (type.Name.EndsWith("Endpoint", StringComparison.Ordinal)
                && ProjectMatches(containingProject, "WebApi")));

    /// <summary>
    ///     Whether the type declaration is a technical class: a top-level class in an
    ///     outer-layer project that declares an ordinary method, excluding generated
    ///     code, migrations, and model snapshots. A null project leaves the class
    ///     unrecognized: without a project name no layer can be confirmed. A partial
    ///     part reads through the context names the host collected, so partial parts
    ///     count as one class.
    /// </summary>
    private static bool IsTechnical(TypeDeclarationSyntax type, KindContext context, string? containingProject)
    {
        if (type is not ClassDeclarationSyntax
            || type.Parent is TypeDeclarationSyntax
            || containingProject is null
            || !ProjectMatches(containingProject, "Application", "Application.Abstractions", "Infrastructure", "WebApi", "Worker"))
        {
            return false;
        }

        if (IsGeneratedFile(type.SyntaxTree) || HasGeneratedCodeAttribute(type) || IsMigration(type))
        {
            return false;
        }

        // An options class binds a keyed setting with properties alone, so its setting mark makes it technical.
        return DeclaresOrdinaryMethod(type)
            || HasMark(type, SettingPrefix)
            || (IsOtherPart(type) && context.TechnicalClasses.Contains(type.Identifier.ValueText));
    }

    /// <summary>
    ///     Whether the type symbol is a technical class: a top-level non-record class
    ///     in an outer-layer project that declares an ordinary method, excluding
    ///     generated code, migrations, and model snapshots. Partial parts merge in
    ///     the symbol, so no context names are needed.
    /// </summary>
    private static bool IsTechnical(INamedTypeSymbol type, string? containingProject)
    {
        if (type.TypeKind != TypeKind.Class
            || type.IsRecord
            || type.ContainingType is not null
            || containingProject is null
            || !ProjectMatches(containingProject, "Application", "Application.Abstractions", "Infrastructure", "WebApi", "Worker"))
        {
            return false;
        }

        foreach (var attribute in type.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;

            if (name is not null && SyntaxNames.StripAttributeSuffix(name) == "GeneratedCode")
            {
                return false;
            }
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name is "Migration" or "ModelSnapshot")
            {
                return false;
            }
        }

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (IsGeneratedFile(reference.SyntaxTree))
            {
                return false;
            }
        }

        if (HasMark(type, SettingPrefix))
        {
            return true;
        }

        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method
                && method.MethodKind == MethodKind.Ordinary
                && !method.IsImplicitlyDeclared)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the declaration holds an ordinary method directly: any method declaration.</summary>
    private static bool DeclaresOrdinaryMethod(TypeDeclarationSyntax type)
    {
        foreach (var member in type.Members)
        {
            if (member is MethodDeclarationSyntax)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the declaration carries a <c>GeneratedCode</c> attribute.</summary>
    private static bool HasGeneratedCodeAttribute(TypeDeclarationSyntax type)
    {
        foreach (var list in type.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                var simple = SyntaxNames.SimpleName(attribute.Name);

                if (simple is not null && SyntaxNames.StripAttributeSuffix(simple) == "GeneratedCode")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Whether the declaration derives from a type named <c>Migration</c> or <c>ModelSnapshot</c>.</summary>
    private static bool IsMigration(TypeDeclarationSyntax type)
    {
        foreach (var baseType in BaseTypes(type))
        {
            if (SyntaxNames.SimpleName(baseType) is "Migration" or "ModelSnapshot")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Whether the compiler treats the file as generated: a generated file name
    ///     or an <c>auto-generated</c> marker on one of the first two lines.
    /// </summary>
    private static bool IsGeneratedFile(SyntaxTree tree)
    {
        var path = tree.FilePath.Replace('\\', '/');
        var slash = path.LastIndexOf('/');
        var file = slash < 0 ? path : path.Substring(slash + 1);

        if (file.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var text = tree.GetText();

        if (text.Lines.Count == 0)
        {
            return false;
        }

        if (text.Lines[0].ToString().Contains("<auto-generated", StringComparison.Ordinal))
        {
            return true;
        }

        return text.Lines.Count > 1
            && text.Lines[1].ToString().Contains("<auto-generated", StringComparison.Ordinal);
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method)
    {
        foreach (var list in method.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                var simple = SyntaxNames.SimpleName(attribute.Name);
                if (simple is not null && TestAttributeNames.Contains(SyntaxNames.StripAttributeSuffix(simple)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsTestMethod(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;
            if (name is not null && TestAttributeNames.Contains(SyntaxNames.StripAttributeSuffix(name)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAggregateMethod(MethodDeclarationSyntax method, KindContext context)
    {
        if (!method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
        {
            return false;
        }

        if (method.Parent is not TypeDeclarationSyntax parent || !IsAggregateRoot(parent, context))
        {
            return false;
        }

        // A state-changing method that raises nothing carries no event to recognize
        // it by, so the mark is what makes it an act.
        return RaisesEvent(method, parent) || MarkReader.Read(method).Count > 0;
    }

    private static bool IsAggregateMethod(IMethodSymbol method, KindContext context)
    {
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (!IsAggregateRoot(method.ContainingType, context))
        {
            return false;
        }

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MethodDeclarationSyntax declaration
                && declaration.Parent is TypeDeclarationSyntax parent
                && (RaisesEvent(declaration, parent) || MarkReader.Read(declaration).Count > 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RaisesEvent(MethodDeclarationSyntax method, TypeDeclarationSyntax parent) =>
        RaisedEvents.Of(method, parent).Count != 0;

    /// <summary>
    ///     The kind a type carries through a base declared in another file: a handler, event
    ///     handler, job or domain event whose contract the host's cross-file closure names.
    ///     Null when the type carries no such contract or fails the kind's shape.
    /// </summary>
    private static CodeKind? TransitiveKindOf(TypeDeclarationSyntax type, KindContext context)
    {
        var name = type.Identifier.ValueText;

        if (type is not InterfaceDeclarationSyntax
            && ((IsOtherPart(type) && context.DomainEvents.Contains(name)) || BaseCarries(type, context.DomainEvents)))
        {
            return CodeKind.DomainEvent;
        }

        if (IsClassLike(type)
            && ((IsOtherPart(type) && (context.CommandHandlers.Contains(name) || context.QueryHandlers.Contains(name)))
                || BaseCarries(type, context.CommandHandlers)
                || BaseCarries(type, context.QueryHandlers)))
        {
            return CodeKind.Handler;
        }

        if (IsClassLike(type)
            && ((IsOtherPart(type) && context.EventHandlers.Contains(name)) || BaseCarries(type, context.EventHandlers)))
        {
            return CodeKind.EventHandler;
        }

        if (IsClassLike(type)
            && HasJobName(name)
            && ((IsOtherPart(type) && context.JobHosts.Contains(name)) || BaseCarries(type, context.JobHosts)))
        {
            return CodeKind.Job;
        }

        return null;
    }

    private static bool BaseCarries(TypeDeclarationSyntax type, IReadOnlyCollection<string> known)
    {
        if (type.BaseList is null || known.Count == 0)
        {
            return false;
        }

        foreach (var baseType in type.BaseList.Types)
        {
            if (known.Contains(SyntaxNames.SimpleName(baseType.Type)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsClassLike(TypeDeclarationSyntax type)
    {
        switch (type)
        {
            case ClassDeclarationSyntax:
                return true;
            case RecordDeclarationSyntax record:
                return !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword);
            default:
                return false;
        }
    }

    private static IEnumerable<TypeSyntax> BaseTypes(TypeDeclarationSyntax type)
    {
        if (type.BaseList is null)
        {
            yield break;
        }

        foreach (var item in type.BaseList.Types)
        {
            yield return item.Type;
        }
    }

    private static bool HasBaseName(TypeDeclarationSyntax type, string name)
    {
        foreach (var baseType in BaseTypes(type))
        {
            if (SyntaxNames.SimpleName(baseType) == name)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Implements(INamedTypeSymbol type, string name)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ProjectMatches(string? containingProject, params string[] shortNames)
    {
        if (containingProject is null)
        {
            return true;
        }

        foreach (var shortName in shortNames)
        {
            if (containingProject.Equals(shortName, StringComparison.Ordinal)
                || containingProject.EndsWith("." + shortName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
