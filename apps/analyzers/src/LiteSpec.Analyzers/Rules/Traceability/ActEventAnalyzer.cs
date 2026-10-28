using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5007: an aggregate method marked with a use case raises an event the use
///     case does not list in <c>raises</c>. LS5008: a use case lists an event in
///     <c>raises</c> that no method marked with it raises.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActEventAnalyzer : TraceabilityAnalyzer
{
    private const string RaiseMethodName = "RaiseDomainEvent";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5007, Descriptors.LS5008);

    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state)
    {
        var collected = new List<MarkedAct>();
        var collectedLock = new object();

        context.RegisterSymbolAction(
            c => Collect(c, state, collected, collectedLock), SymbolKind.Method);
        context.RegisterCompilationEndAction(c => Analyze(c, state, collected));
    }

    private static void Collect(
        SymbolAnalysisContext context,
        TraceabilityContext state,
        List<MarkedAct> collected,
        object collectedLock)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        if (KindReader.KindOf(method.ContainingType, state.Kinds, state.Project) != CodeKind.AggregateRoot)
        {
            return;
        }

        var useCases = UseCaseMarks(state, method);

        if (useCases.Count == 0)
        {
            return;
        }

        var raised = RaisedEvents(state, method);

        lock (collectedLock)
        {
            collected.Add(new MarkedAct(method, useCases, raised));
        }
    }

    /// <summary>Every use-case mark on the method that resolves to a use-case record.</summary>
    private static IReadOnlyList<string> UseCaseMarks(TraceabilityContext state, IMethodSymbol method)
    {
        var useCases = new List<string>();

        foreach (var mark in MarksOf(method))
        {
            if (mark.HasRecordId
                && mark.RecordId.StartsWith("use-case/", System.StringComparison.Ordinal)
                && state.Store.Index.ById(mark.RecordId) is IRecordFacts record
                && record.Type == "use-case"
                && !useCases.Contains(mark.RecordId))
            {
                useCases.Add(mark.RecordId);
            }
        }

        return useCases;
    }

    /// <summary>
    ///     The event ids the method raises: each <c>RaiseDomainEvent(new X(...))</c>
    ///     in its body or in a private method of its type it calls, resolved
    ///     through the event type's <c>event/</c> mark.
    /// </summary>
    private static IReadOnlyList<string> RaisedEvents(TraceabilityContext state, IMethodSymbol method)
    {
        var raised = new List<string>();

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MethodDeclarationSyntax declaration
                && declaration.Parent is TypeDeclarationSyntax parent)
            {
                CollectRaised(state, declaration, raised);
                CollectHelperRaised(state, declaration, parent, raised);
            }
        }

        return raised;
    }

    private static void CollectRaised(
        TraceabilityContext state, MethodDeclarationSyntax method, List<string> raised)
    {
        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (InvocationName(invocation) != RaiseMethodName
                || invocation.ArgumentList.Arguments.Count == 0
                || invocation.ArgumentList.Arguments[0].Expression is not ObjectCreationExpressionSyntax created)
            {
                continue;
            }

            if (SimpleName(created.Type) is string name
                && state.EventIds.TryGetValue(name, out var id)
                && !raised.Contains(id))
            {
                raised.Add(id);
            }
        }
    }

    private static void CollectHelperRaised(
        TraceabilityContext state, MethodDeclarationSyntax method, TypeDeclarationSyntax parent, List<string> raised)
    {
        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (InvocationName(invocation) is string name
                && name != RaiseMethodName
                && FindPrivateMethod(parent, name) is MethodDeclarationSyntax helper)
            {
                CollectRaised(state, helper, raised);
            }
        }
    }

    private static MethodDeclarationSyntax? FindPrivateMethod(TypeDeclarationSyntax parent, string name)
    {
        foreach (var candidate in parent.Members.OfType<MethodDeclarationSyntax>())
        {
            if (candidate.Identifier.ValueText == name && IsPrivate(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsPrivate(MethodDeclarationSyntax method)
    {
        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PublicKeyword)
                || modifier.IsKind(SyntaxKind.InternalKeyword)
                || modifier.IsKind(SyntaxKind.ProtectedKeyword))
            {
                return false;
            }
        }

        return true;
    }

    private static string? InvocationName(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText;
            case MemberAccessExpressionSyntax member:
                return member.Name.Identifier.ValueText;
            case MemberBindingExpressionSyntax binding:
                return binding.Name.Identifier.ValueText;
            default:
                return null;
        }
    }

    private static string? SimpleName(TypeSyntax type)
    {
        switch (type)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText;
            case QualifiedNameSyntax qualified:
                return qualified.Right.Identifier.ValueText;
            case AliasQualifiedNameSyntax alias:
                return alias.Name.Identifier.ValueText;
            default:
                return null;
        }
    }

    private static void Analyze(
        CompilationAnalysisContext context, TraceabilityContext state, List<MarkedAct> collected)
    {
        var raisedByUseCase = new Dictionary<string, HashSet<string>>(System.StringComparer.Ordinal);

        foreach (var act in collected)
        {
            foreach (var useCase in act.UseCases)
            {
                if (!raisedByUseCase.TryGetValue(useCase, out var raised))
                {
                    raised = new HashSet<string>(System.StringComparer.Ordinal);
                    raisedByUseCase.Add(useCase, raised);
                }

                foreach (var id in act.Raised)
                {
                    raised.Add(id);
                }
            }
        }

        foreach (var act in collected)
        {
            foreach (var useCase in act.UseCases)
            {
                var declared = Raises(state, useCase);
                var raised = raisedByUseCase.TryGetValue(useCase, out var union)
                    ? union
                    : new HashSet<string>(System.StringComparer.Ordinal);

                foreach (var id in act.Raised)
                {
                    if (!declared.Contains(id))
                    {
                        TraceabilityReporter.Report(
                            state,
                            context.ReportDiagnostic,
                            Descriptors.LS5007,
                            "act-event-undeclared",
                            useCase,
                            act.Method.Locations[0],
                            act.Method.Name,
                            useCase,
                            id);
                    }
                }

                foreach (var id in declared)
                {
                    if (!raised.Contains(id))
                    {
                        TraceabilityReporter.Report(
                            state,
                            context.ReportDiagnostic,
                            Descriptors.LS5008,
                            "act-event-unraised",
                            useCase,
                            act.Method.Locations[0],
                            act.Method.Name,
                            useCase,
                            id);
                    }
                }
            }
        }
    }

    private static HashSet<string> Raises(TraceabilityContext state, string useCase)
    {
        var declared = new HashSet<string>(System.StringComparer.Ordinal);
        var record = state.Store.Index.ById(useCase);

        if (record is not null)
        {
            foreach (var id in record.Ids("raises"))
            {
                declared.Add(id);
            }
        }

        return declared;
    }

    private sealed class MarkedAct
    {
        public MarkedAct(IMethodSymbol method, IReadOnlyList<string> useCases, IReadOnlyList<string> raised)
        {
            Method = method;
            UseCases = useCases;
            Raised = raised;
        }

        public IMethodSymbol Method { get; }

        public IReadOnlyList<string> UseCases { get; }

        public IReadOnlyList<string> Raised { get; }
    }
}
