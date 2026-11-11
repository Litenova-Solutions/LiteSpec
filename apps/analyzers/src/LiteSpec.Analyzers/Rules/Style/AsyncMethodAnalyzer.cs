using System;
using System.Collections.Immutable;
using System.Threading;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Style;

/// <summary>LS3001: an asynchronous method ends in Async.</summary>
/// <follows>litespec/workspace/naming#name-asynchronous-operations-completely</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS3001);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>Registers the analysis actions for the async method analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var types = new WellKnownTypes(context.Compilation, scope);
        context.RegisterSymbolAction(types.AnalyzeMethod, SymbolKind.Method);
        context.RegisterOperationAction(types.AnalyzeLocalFunction, OperationKind.LocalFunction);
    }

    /// <summary>Holds the well known types data the async method analyzer code works with.</summary>
    private sealed class WellKnownTypes
    {
        /// <summary>Stores the task used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _task;
        /// <summary>Stores the generic task used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _genericTask;
        /// <summary>Stores the value task used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _valueTask;
        /// <summary>Stores the generic value task used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _genericValueTask;
        /// <summary>Stores the scope used by the well known types code.</summary>
        private readonly FileScope _scope;

        /// <summary>Initializes a new instance of the <see cref="WellKnownTypes"/> class.</summary>
        /// <param name="compilation">The compilation whose types and references the check reads.</param>
        /// <param name="scope">Whether the rule applies to each file.</param>
        public WellKnownTypes(Compilation compilation, FileScope scope)
        {
            _scope = scope;
            _task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            _genericTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            _valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            _genericValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        }

        /// <summary>Analyzes the method.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        public void AnalyzeMethod(SymbolAnalysisContext context)
        {
            if (!_scope.AppliesTo(context.Symbol))
            {
                return;
            }

            var method = (IMethodSymbol)context.Symbol;
            if (method.MethodKind != MethodKind.Ordinary
                || method.IsImplicitlyDeclared
                || method.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            Analyze(method, context.ReportDiagnostic, context.CancellationToken);
        }

        /// <summary>Analyzes the local function.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        public void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            if (!_scope.AppliesTo(context.Operation.Syntax?.SyntaxTree))
            {
                return;
            }

            var symbol = ((ILocalFunctionOperation)context.Operation).Symbol;
            if (symbol is null || symbol.IsImplicitlyDeclared || symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            Analyze(symbol, context.ReportDiagnostic, context.CancellationToken);
        }

        /// <summary>Analyzes the void.</summary>
        /// <param name="method">The method under inspection.</param>
        /// <param name="report">The action that reports the diagnostic to the host.</param>
        /// <param name="cancellationToken">The token that cancels the operation when the host aborts it.</param>
        private void Analyze(IMethodSymbol method, Action<Diagnostic> report, CancellationToken cancellationToken)
        {
            if (ReturnsTaskWithoutSuffix(method))
            {
                report(Diagnostic.Create(
                    Descriptors.LS3001, method.Locations[0], method.Name, ReturnTypeText(method, cancellationToken)));
            }
        }

        /// <summary>Returns true when the returns-task-without-suffix condition holds; otherwise false.</summary>
        /// <param name="method">The method under inspection.</param>
        /// <returns>True when the returns-task-without-suffix condition holds; otherwise false.</returns>
        private bool ReturnsTaskWithoutSuffix(IMethodSymbol method)
        {
            if (method.ReturnType is not INamedTypeSymbol returns || !IsTaskType(returns))
            {
                return false;
            }

            if (method.Name.EndsWith("Async", StringComparison.Ordinal))
            {
                return false;
            }

            return !HasFixedSignature(method);
        }

        /// <summary>Returns true when the method has fixed signature; otherwise false.</summary>
        /// <param name="method">The method under inspection.</param>
        /// <returns>True when the method has fixed signature; otherwise false.</returns>
        private static bool HasFixedSignature(IMethodSymbol method)
        {
            if (method.IsOverride || method.ExplicitInterfaceImplementations.Length != 0)
            {
                return true;
            }

            if (method.MethodKind == MethodKind.Ordinary && method.IsStatic
                && method.Name is "Main" or "<Main>$")
            {
                return true;
            }

            return ImplementsOutsideInterfaceMember(method);
        }

        /// <summary>Returns true when the implements-outside-interface-member condition holds; otherwise false.</summary>
        /// <param name="method">The method under inspection.</param>
        /// <returns>True when the implements-outside-interface-member condition holds; otherwise false.</returns>
        private static bool ImplementsOutsideInterfaceMember(IMethodSymbol method)
        {
            var containing = method.ContainingType;
            if (containing is null || method.MethodKind != MethodKind.Ordinary)
            {
                return false;
            }

            foreach (var face in containing.AllInterfaces)
            {
                foreach (var member in face.GetMembers(method.Name))
                {
                    if (member is IMethodSymbol interfaceMethod
                        && interfaceMethod.DeclaringSyntaxReferences.Length == 0
                        && SymbolEqualityComparer.Default.Equals(
                            containing.FindImplementationForInterfaceMember(interfaceMethod), method))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Returns true when the returns is task type; otherwise false.</summary>
        /// <param name="returns">The return type under inspection.</param>
        /// <returns>True when the returns is task type; otherwise false.</returns>
        private bool IsTaskType(INamedTypeSymbol returns)
        {
            var definition = returns.OriginalDefinition;
            return IsDefinition(definition, _task)
                || IsDefinition(definition, _genericTask)
                || IsDefinition(definition, _valueTask)
                || IsDefinition(definition, _genericValueTask);
        }

        /// <summary>Returns true when the candidate is definition; otherwise false.</summary>
        /// <param name="candidate">The type declaration that may carry the mark.</param>
        /// <param name="known">The known supplied for the check, or null when the compilation does not provide it.</param>
        /// <returns>True when the candidate is definition; otherwise false.</returns>
        private static bool IsDefinition(INamedTypeSymbol candidate, INamedTypeSymbol? known) =>
            known is not null && SymbolEqualityComparer.Default.Equals(candidate, known);

        /// <summary>Returns the type text.</summary>
        /// <param name="method">The method under inspection.</param>
        /// <param name="cancellationToken">The token that cancels the operation when the host aborts it.</param>
        /// <returns>The type text as text.</returns>
        private static string ReturnTypeText(IMethodSymbol method, CancellationToken cancellationToken)
        {
            var syntax = method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            if (syntax is MethodDeclarationSyntax declaration)
            {
                return declaration.ReturnType.ToString();
            }

            if (syntax is LocalFunctionStatementSyntax local)
            {
                return local.ReturnType.ToString();
            }

            return method.ReturnType.ToDisplayString();
        }
    }
}
