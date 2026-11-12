using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static LiteSpec.Model.Roslyn.BaseTypeNames;
using static LiteSpec.Model.Roslyn.GeneratedCode;
using static LiteSpec.Model.Roslyn.MarkReader;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Recognizes the LiteSpec code kind of a type or method from the code,
///     never from the mark, except for ports, domain services, and job hosts,
///     which are recognized by their own mark, for adapters, recognized
///     through the marked port they implement, and for a public aggregate-root
///     method that neither raises nor assigns state, recognized once it carries a mark.
///     The syntax overloads read one declaration; the symbol overloads walk base types and
///     interfaces semantically. Both agree on the same code
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
    /// <summary>The record id prefix a port mark names.</summary>
    private const string ExternalSystemPrefix = "external-system/";

    /// <summary>The record id prefix a domain service mark names.</summary>
    private const string RulePrefix = "rule/";

    /// <summary>The record id prefix a setting mark names.</summary>
    private const string SettingPrefix = "setting/";

    /// <summary>The record id prefix a job mark names.</summary>
    private const string JobPrefix = "job/";

    /// <summary>The attribute names that mark a test method.</summary>
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
    /// <param name="containingLayer">
    ///     The layer holding the type, such as <c>Domain</c> or <c>Application</c>, or null when unknown.
    ///     A known layer matches beside the project, so a type in the one product project reads by its layer.
    /// </param>
    /// <returns>The kind, or null when the type is of no markable kind.</returns>
    /// <exception cref="ArgumentNullException">The type or context is null.</exception>
    public static CodeKind? KindOf(TypeDeclarationSyntax type, KindContext context, string? containingProject = null, string? containingLayer = null)
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

        if (IsJob(type, containingProject, containingLayer))
        {
            return CodeKind.Job;
        }

        if (IsPort(type, containingProject, containingLayer))
        {
            return CodeKind.Port;
        }

        if (IsAdapter(type, context))
        {
            return CodeKind.Adapter;
        }

        if (IsEndpoint(type, containingProject, containingLayer))
        {
            return CodeKind.Endpoint;
        }

        if (IsDomainService(type, containingProject, containingLayer))
        {
            return CodeKind.DomainService;
        }

        if (TransitiveKindOf(type, context) is CodeKind transitive)
        {
            return transitive;
        }

        return IsTechnical(type, context, containingProject, containingLayer) ? CodeKind.Technical : (CodeKind?)null;
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
    /// <param name="containingLayer">
    ///     The layer holding the type, such as <c>Domain</c> or <c>Application</c>, or null when unknown.
    ///     A known layer matches beside the project, so a type in the one product project reads by its layer.
    /// </param>
    /// <returns>The kind, or null when the type is of no markable kind.</returns>
    /// <exception cref="ArgumentNullException">The type or context is null.</exception>
    public static CodeKind? KindOf(INamedTypeSymbol type, KindContext context, string? containingProject = null, string? containingLayer = null)
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

        if (IsJob(type, containingProject, containingLayer))
        {
            return CodeKind.Job;
        }

        if (IsPort(type, containingProject, containingLayer))
        {
            return CodeKind.Port;
        }

        if (IsAdapter(type, context))
        {
            return CodeKind.Adapter;
        }

        if (IsEndpoint(type, containingProject, containingLayer))
        {
            return CodeKind.Endpoint;
        }

        if (IsDomainService(type, containingProject, containingLayer))
        {
            return CodeKind.DomainService;
        }

        return IsTechnical(type, containingProject, containingLayer) ? CodeKind.Technical : (CodeKind?)null;
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
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the declaration is another part of a partial type.</returns>
    private static bool IsOtherPart(TypeDeclarationSyntax type) =>
        type.BaseList is null
        && type.Parent is not TypeDeclarationSyntax
        && type.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

    /// <summary>Whether the declaration is an aggregate root, from its bases or the collected root names.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the type is an aggregate root.</returns>
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

    // A symbol sees its whole base chain across every partial part, so it is known by its bases
    // only; its own name would mistake a nested type sharing a root's simple name for the root.
    /// <summary>Whether the symbol is an aggregate root, from its base chain.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the type is an aggregate root.</returns>
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

    /// <summary>Whether a base type is the generic aggregate root base.</summary>
    /// <param name="type">The base type as written.</param>
    /// <returns>True when the base is the aggregate root base.</returns>
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

    /// <summary>Whether the declaration is a domain event.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the type is a domain event.</returns>
    private static bool IsDomainEvent(TypeDeclarationSyntax type) =>
        !(type is InterfaceDeclarationSyntax) && HasBaseName(type, "IDomainEvent");

    /// <summary>Whether the symbol is a domain event.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>True when the type is a domain event.</returns>
    private static bool IsDomainEvent(INamedTypeSymbol type) =>
        type.TypeKind != TypeKind.Interface && Implements(type, "IDomainEvent");

    /// <summary>Whether the declaration is a rule exception.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the type is a rule exception.</returns>
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

    /// <summary>Whether the symbol is a rule exception.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the type is a rule exception.</returns>
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

    /// <summary>Whether the declaration is a command or query handler.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the type is a handler.</returns>
    private static bool IsHandler(TypeDeclarationSyntax type) =>
        IsClassLike(type) && (HasBaseName(type, "ICommandHandler") || HasBaseName(type, "IQueryHandler"));

    /// <summary>Whether the symbol is a command or query handler.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>True when the type is a handler.</returns>
    private static bool IsHandler(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && (Implements(type, "ICommandHandler") || Implements(type, "IQueryHandler"));

    /// <summary>Whether the declaration is an event handler.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the type is an event handler.</returns>
    private static bool IsEventHandler(TypeDeclarationSyntax type) =>
        IsClassLike(type) && (HasBaseName(type, "IEventHandler") || HasBaseName(type, "IDomainEventReaction"));

    /// <summary>Whether the symbol is an event handler.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>True when the type is an event handler.</returns>
    private static bool IsEventHandler(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && (Implements(type, "IEventHandler") || Implements(type, "IDomainEventReaction"));

    /// <summary>Whether the declaration is a job host.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a job host.</returns>
    private static bool IsJob(TypeDeclarationSyntax type, string? containingProject, string? containingLayer) =>
        IsClassLike(type) && (
            ((HasBaseName(type, "BackgroundService") || HasBaseName(type, "IHostedService"))
                && HasJobName(type.Identifier.ValueText))
            || (ProjectMatches(containingProject, containingLayer, "Application", "Application.Abstractions", "Infrastructure", "Worker")
                && HasMark(type, JobPrefix)));

    /// <summary>Whether the symbol is a job host.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a job host.</returns>
    private static bool IsJob(INamedTypeSymbol type, string? containingProject, string? containingLayer)
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

        return ProjectMatches(containingProject, containingLayer, "Application", "Application.Abstractions", "Infrastructure", "Worker")
            && HasMark(type, JobPrefix);
    }

    /// <summary>Whether the name is a job name: it ends in Job or Worker.</summary>
    /// <param name="name">The type name.</param>
    /// <returns>True when the name ends in Job or Worker.</returns>
    private static bool HasJobName(string name) =>
        name.EndsWith("Job", StringComparison.Ordinal) || name.EndsWith("Worker", StringComparison.Ordinal);

    /// <summary>Whether the declaration is a port: a marked interface in an application project.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a port.</returns>
    private static bool IsPort(TypeDeclarationSyntax type, string? containingProject, string? containingLayer) =>
        type is InterfaceDeclarationSyntax
        && ProjectMatches(containingProject, containingLayer, "Application", "Application.Abstractions")
        && HasExternalSystemMark(type);

    /// <summary>Whether the symbol is a port: a marked interface in an application project.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a port.</returns>
    private static bool IsPort(INamedTypeSymbol type, string? containingProject, string? containingLayer) =>
        type.TypeKind == TypeKind.Interface
        && ProjectMatches(containingProject, containingLayer, "Application", "Application.Abstractions")
        && HasExternalSystemMark(type);

    /// <summary>Whether the declaration is a domain service: a marked class in the domain project.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a domain service.</returns>
    private static bool IsDomainService(TypeDeclarationSyntax type, string? containingProject, string? containingLayer) =>
        type is ClassDeclarationSyntax
        && ProjectMatches(containingProject, containingLayer, "Domain")
        && HasMark(type, RulePrefix);

    /// <summary>Whether the symbol is a domain service: a marked class in the domain project.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a domain service.</returns>
    private static bool IsDomainService(INamedTypeSymbol type, string? containingProject, string? containingLayer) =>
        type.TypeKind == TypeKind.Class
        && ProjectMatches(containingProject, containingLayer, "Domain")
        && HasMark(type, RulePrefix);

    /// <summary>Whether the declaration carries an external-system mark.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the type carries an external-system mark.</returns>
    private static bool HasExternalSystemMark(TypeDeclarationSyntax type) => HasMark(type, ExternalSystemPrefix);

    /// <summary>Whether the symbol carries an external-system mark.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>True when the type carries an external-system mark.</returns>
    private static bool HasExternalSystemMark(INamedTypeSymbol type) => HasMark(type, ExternalSystemPrefix);

    /// <summary>Whether the declaration is an adapter: a class implementing a marked port.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the type is an adapter.</returns>
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

    /// <summary>Whether the symbol is an adapter: a class implementing a marked port.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the type is an adapter.</returns>
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

    /// <summary>Whether the declaration is an endpoint.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is an endpoint.</returns>
    private static bool IsEndpoint(TypeDeclarationSyntax type, string? containingProject, string? containingLayer) =>
        IsClassLike(type)
        && (HasBaseName(type, "IEndpoint")
            || (type.Identifier.ValueText.EndsWith("Endpoint", StringComparison.Ordinal)
                && ProjectMatches(containingProject, containingLayer, "WebApi")));

    /// <summary>Whether the symbol is an endpoint.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is an endpoint.</returns>
    private static bool IsEndpoint(INamedTypeSymbol type, string? containingProject, string? containingLayer) =>
        type.TypeKind == TypeKind.Class
        && (Implements(type, "IEndpoint")
            || (type.Name.EndsWith("Endpoint", StringComparison.Ordinal)
                && ProjectMatches(containingProject, containingLayer, "WebApi")));

    /// <summary>
    ///     Whether the type declaration is a technical class: a top-level class in an
    ///     outer-layer project that declares an ordinary method, excluding generated
    ///     code, migrations, and model snapshots. A null project leaves the class
    ///     unrecognized: without a project name no layer can be confirmed. A partial
    ///     part reads through the context names the host collected, so partial parts
    ///     count as one class.
    /// </summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a technical class.</returns>
    private static bool IsTechnical(TypeDeclarationSyntax type, KindContext context, string? containingProject, string? containingLayer)
    {
        if (type is not ClassDeclarationSyntax
            || type.Parent is TypeDeclarationSyntax
            || containingProject is null
            || !ProjectMatches(containingProject, containingLayer, "Application", "Application.Abstractions", "Infrastructure", "WebApi", "Worker"))
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
    /// <param name="type">The type symbol.</param>
    /// <param name="containingProject">The short project name holding the type, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <returns>True when the type is a technical class.</returns>
    private static bool IsTechnical(INamedTypeSymbol type, string? containingProject, string? containingLayer)
    {
        if (type.TypeKind != TypeKind.Class
            || type.IsRecord
            || type.ContainingType is not null
            || containingProject is null
            || !ProjectMatches(containingProject, containingLayer, "Application", "Application.Abstractions", "Infrastructure", "WebApi", "Worker"))
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
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the declaration holds an ordinary method directly.</returns>
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

    /// <summary>Whether the method carries a test attribute.</summary>
    /// <param name="method">The method declaration.</param>
    /// <returns>True when the method is a test method.</returns>
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

    /// <summary>Whether the method symbol carries a test attribute.</summary>
    /// <param name="method">The method symbol.</param>
    /// <returns>True when the method is a test method.</returns>
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

    /// <summary>Whether the method is an aggregate act: a public method of an aggregate root that raises, assigns, or carries a mark.</summary>
    /// <param name="method">The method declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the method is an aggregate method.</returns>
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

        // A method that assigns state without raising an event is still an act:
        // the assignment is what recognizes it, and the mark it carries names its use case.
        // A method that neither raises nor assigns is recognized once it carries a mark.
        return RaisesEvent(method, parent) || StateChanges.Changes(method, parent) || MarkReader.Read(method).Count > 0;
    }

    /// <summary>Whether the method symbol is an aggregate act.</summary>
    /// <param name="method">The method symbol.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>True when the method is an aggregate method.</returns>
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
                && (RaisesEvent(declaration, parent) || StateChanges.Changes(declaration, parent) || MarkReader.Read(declaration).Count > 0))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the method raises a domain event.</summary>
    /// <param name="method">The method declaration.</param>
    /// <param name="parent">The type declaring the method.</param>
    /// <returns>True when the method raises an event.</returns>
    private static bool RaisesEvent(MethodDeclarationSyntax method, TypeDeclarationSyntax parent) =>
        RaisedEvents.Of(method, parent).Count != 0;

    /// <summary>
    ///     The kind a type carries through a base declared in another file: a handler, event
    ///     handler, job or domain event whose contract the host's cross-file closure names.
    ///     Null when the type carries no such contract or fails the kind's shape.
    /// </summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <returns>The transitive kind, or null when the type carries none.</returns>
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

    /// <summary>Whether the containing project or layer is one of the named projects.</summary>
    /// <param name="containingProject">The short project name, or null when unknown.</param>
    /// <param name="containingLayer">The layer holding the type, or null when unknown.</param>
    /// <param name="shortNames">The project names that match.</param>
    /// <returns>True when the project or the layer matches; a null project matches every name.</returns>
    private static bool ProjectMatches(string? containingProject, string? containingLayer, params string[] shortNames)
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

            if (containingLayer is not null && LayerMatches(containingLayer, shortName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the layer answers for the project name: the application layer and the contracts part answer for each other.</summary>
    /// <param name="layer">The layer holding the type.</param>
    /// <param name="shortName">The project name to match.</param>
    /// <returns>True when the layer answers for the project name.</returns>
    private static bool LayerMatches(string layer, string shortName) =>
        shortName is "Application" or "Application.Abstractions"
            ? layer is "Application" or "ApplicationAbstractions"
            : layer == shortName;
}
