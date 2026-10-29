using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5005: a hand-written event <c>Classification</c> or exception <c>Code</c>
///     differs from what its record states.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RuntimeValueAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5005);

    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state) =>
        context.RegisterSymbolAction(c => Analyze(c, state), SymbolKind.NamedType);

    private static void Analyze(SymbolAnalysisContext context, TraceabilityContext state)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.IsImplicitlyDeclared || type.TypeKind == TypeKind.Error)
        {
            return;
        }

        var kind = KindReader.KindOf(type, state.Kinds, state.Project);

        if (kind == CodeKind.DomainEvent)
        {
            AnalyzeEvent(context, state, type);
        }
        else if (kind == CodeKind.RuleException)
        {
            AnalyzeException(context, state, type);
        }
    }

    private static void AnalyzeEvent(SymbolAnalysisContext context, TraceabilityContext state, INamedTypeSymbol type)
    {
        var mark = ResolvingMark(state, type, "event");

        if (mark is null)
        {
            return;
        }

        foreach (var written in WrittenValues(type, "Classification"))
        {
            if (!string.Equals(written.Value, mark, System.StringComparison.Ordinal))
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5005,
                    "runtime-value-drift",
                    mark,
                    written.Location,
                    type.Name,
                    "Classification",
                    written.Value,
                    mark);
            }
        }
    }

    private static void AnalyzeException(
        SymbolAnalysisContext context, TraceabilityContext state, INamedTypeSymbol type)
    {
        var mark = ResolvingMark(state, type, "rule");

        if (mark is null || ExpectedCode(state, mark) is not string expected)
        {
            return;
        }

        foreach (var written in WrittenValues(type, "Code"))
        {
            if (!string.Equals(written.Value, expected, System.StringComparison.Ordinal))
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5005,
                    "runtime-value-drift",
                    mark,
                    written.Location,
                    type.Name,
                    "Code",
                    written.Value,
                    expected);
            }
        }
    }

    /// <summary>The first mark resolving to a record of the expected type, or null.</summary>
    private static string? ResolvingMark(TraceabilityContext state, INamedTypeSymbol type, string recordType)
    {
        foreach (var member in Declarations(type))
        {
            foreach (var mark in MarkReader.Read(member))
            {
                if (mark.HasRecordId && state.Store.Index.ById(mark.RecordId) is IRecordFacts record
                    && record.Type == recordType)
                {
                    return mark.RecordId;
                }
            }
        }

        return null;
    }

    /// <summary>The refusal code of the rule through the shared rule, or null when it names none.</summary>
    private static string? ExpectedCode(TraceabilityContext state, string ruleId)
    {
        var rule = state.Store.Index.ById(ruleId);

        return rule is null ? null : RefusalCode.Of(rule, state.Store.Index.ById);
    }

    /// <summary>Every hand-written string literal value of the named member declared on the type.</summary>
    private static System.Collections.Generic.IReadOnlyList<WrittenValue> WrittenValues(
        INamedTypeSymbol type, string memberName)
    {
        var values = new System.Collections.Generic.List<WrittenValue>();

        foreach (var member in Declarations(type))
        {
            if (member is not TypeDeclarationSyntax declared)
            {
                continue;
            }

            foreach (var child in declared.Members)
            {
                if (child is FieldDeclarationSyntax field)
                {
                    ReadField(field, memberName, values);
                }
                else if (child is PropertyDeclarationSyntax property
                    && property.Identifier.ValueText == memberName
                    && PropertyLiteral(property) is string value)
                {
                    values.Add(new WrittenValue(value, property.Identifier.GetLocation()));
                }
            }
        }

        return values;
    }

    private static void ReadField(
        FieldDeclarationSyntax field, string memberName, System.Collections.Generic.List<WrittenValue> values)
    {
        foreach (var variable in field.Declaration.Variables)
        {
            if (variable.Identifier.ValueText == memberName
                && variable.Initializer?.Value is LiteralExpressionSyntax literal
                && literal.Token.ValueText is string value)
            {
                values.Add(new WrittenValue(value, variable.Identifier.GetLocation()));
            }
        }
    }

    /// <summary>The string literal the property returns, or null when it returns none.</summary>
    private static string? PropertyLiteral(PropertyDeclarationSyntax property)
    {
        if (property.ExpressionBody?.Expression is LiteralExpressionSyntax arrow)
        {
            return arrow.Token.ValueText;
        }

        if (property.AccessorList is not null)
        {
            foreach (var accessor in property.AccessorList.Accessors)
            {
                if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    continue;
                }

                if (accessor.ExpressionBody?.Expression is LiteralExpressionSyntax getArrow)
                {
                    return getArrow.Token.ValueText;
                }

                if (accessor.Body is BlockSyntax body
                    && body.Statements.Count == 1
                    && body.Statements[0] is ReturnStatementSyntax returned
                    && returned.Expression is LiteralExpressionSyntax returnedLiteral)
                {
                    return returnedLiteral.Token.ValueText;
                }
            }
        }

        return null;
    }

    private sealed class WrittenValue
    {
        public WrittenValue(string value, Location location)
        {
            Value = value;
            Location = location;
        }

        public string Value { get; }

        public Location Location { get; }
    }
}
