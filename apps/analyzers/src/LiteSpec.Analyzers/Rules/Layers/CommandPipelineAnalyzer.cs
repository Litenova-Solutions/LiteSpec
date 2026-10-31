using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2004: a command handler writes only through repositories.</summary>
/// <follows>litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandPipelineAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2004);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the command pipeline analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeDeclarator, OperationKind.VariableDeclarator);
    }

    /// <summary>Analyzes the field.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.IsImplicitlyDeclared || !IsCommandHandler(field.ContainingType))
        {
            return;
        }

        var type = Unwrap(field.Type);
        if (!IsSessionOrContext(type))
        {
            return;
        }

        foreach (var declaration in field.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is VariableDeclaratorSyntax declarator
                && declarator.Parent is VariableDeclarationSyntax list
                && IsFirst(list, declarator))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2004, list.Type.GetLocation(), field.ContainingType.Name, type.Name));
                return;
            }
        }
    }

    /// <summary>Analyzes the property.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (property.IsImplicitlyDeclared || !IsCommandHandler(property.ContainingType))
        {
            return;
        }

        var type = Unwrap(property.Type);
        if (!IsSessionOrContext(type))
        {
            return;
        }

        foreach (var declaration in property.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is BasePropertyDeclarationSyntax syntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2004, syntax.Type.GetLocation(), property.ContainingType.Name, type.Name));
                return;
            }
        }
    }

    /// <summary>Analyzes the parameter.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeParameter(SymbolAnalysisContext context)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.IsImplicitlyDeclared)
        {
            return;
        }

        if (!(parameter.ContainingSymbol is IMethodSymbol method) || method.MethodKind != MethodKind.Constructor)
        {
            return;
        }

        if (!IsCommandHandler(method.ContainingType))
        {
            return;
        }

        var type = Unwrap(parameter.Type);
        if (!IsSessionOrContext(type))
        {
            return;
        }

        foreach (var declaration in parameter.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is ParameterSyntax syntax && syntax.Type != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2004, syntax.Type.GetLocation(), method.ContainingType.Name, type.Name));
                return;
            }
        }
    }

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var handler = InnermostType(context.ContainingSymbol);
        if (handler == null || !IsCommandHandler(handler))
        {
            return;
        }

        var receiver = invocation.Instance;
        if (receiver?.Type != null)
        {
            var type = Unwrap(receiver.Type);
            if (IsSessionOrContext(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2004, MemberLocation(invocation), handler.Name, type.Name));
            }
        }
    }

    /// <summary>Analyzes the declarator.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeDeclarator(OperationAnalysisContext context)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        var handler = InnermostType(context.ContainingSymbol);
        if (handler == null || !IsCommandHandler(handler))
        {
            return;
        }

        var type = Unwrap(declarator.Symbol.Type);
        if (!IsSessionOrContext(type))
        {
            return;
        }

        if (declarator.Syntax is VariableDeclaratorSyntax node
            && node.Parent is VariableDeclarationSyntax list
            && IsFirst(list, node))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2004, list.Type.GetLocation(), handler.Name, type.Name));
        }
    }

    /// <summary>Returns true when the type is command handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is command handler; otherwise false.</returns>
    private static bool IsCommandHandler(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "ICommandHandler")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is session or context; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is session or context; otherwise false.</returns>
    private static bool IsSessionOrContext(ITypeSymbol type)
    {
        if (type.Name == "IDocumentSession" || type.Name == "DbContext")
        {
            return true;
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.Name == "DbContext")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the i type symbol.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The i type symbol.</returns>
    private static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }

    /// <summary>Returns the type.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The type, or null when none applies.</returns>
    private static INamedTypeSymbol? InnermostType(ISymbol symbol)
    {
        for (var current = symbol; current != null; current = current.ContainingSymbol)
        {
            if (current is INamedTypeSymbol type)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>Returns the location.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>The location.</returns>
    private static Location MemberLocation(IInvocationOperation invocation)
    {
        if (invocation.Syntax is InvocationExpressionSyntax syntax)
        {
            if (syntax.Expression is MemberAccessExpressionSyntax access)
            {
                return access.Name.GetLocation();
            }

            if (syntax.Expression is MemberBindingExpressionSyntax binding)
            {
                return binding.Name.GetLocation();
            }

            return syntax.Expression.GetLocation();
        }

        return invocation.Syntax.GetLocation();
    }

    /// <summary>Returns true when the list is first; otherwise false.</summary>
    /// <param name="list">The list that receives the collected values.</param>
    /// <param name="declarator">The variable declarator under inspection.</param>
    /// <returns>True when the list is first; otherwise false.</returns>
    private static bool IsFirst(VariableDeclarationSyntax list, VariableDeclaratorSyntax declarator) =>
        list.Variables.Count == 0 || list.Variables[0].SpanStart == declarator.SpanStart;
}
