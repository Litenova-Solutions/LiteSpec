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

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the runtime value analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state) =>
        context.RegisterSymbolAction(c => Analyze(c, state), SymbolKind.NamedType);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
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

    /// <summary>Analyzes the event.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="type">The type under inspection.</param>
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

    /// <summary>Analyzes the exception.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="type">The type under inspection.</param>
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
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="type">The type under inspection.</param>
    /// <param name="recordType">The record type the mark must resolve to.</param>
    /// <returns>The mark as text, or null when none applies.</returns>
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
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="ruleId">The rule identifier whose refusal code the value must match.</param>
    /// <returns>The code as text, or null when none applies.</returns>
    private static string? ExpectedCode(TraceabilityContext state, string ruleId)
    {
        var rule = state.Store.Index.ById(ruleId);

        return rule is null ? null : RefusalCode.Of(rule, state.Store.Index.ById);
    }

    /// <summary>Every hand-written string literal value of the named member declared on the type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    /// <returns>The values, in the order this method builds them, or an empty collection when none apply.</returns>
    private static System.Collections.Generic.List<WrittenValue> WrittenValues(
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

    /// <summary>Reads the field.</summary>
    /// <param name="field">The field declaration that carries the hand-written value.</param>
    /// <param name="memberName">The name of the member that carries the hand-written value.</param>
    /// <param name="values">The values collected so far, in the order they were found.</param>
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
    /// <param name="property">The property declaration under inspection.</param>
    /// <returns>The literal as text, or null when none applies.</returns>
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

    /// <summary>Holds the written value data the runtime value analyzer code works with.</summary>
    private sealed class WrittenValue
    {
        /// <summary>Initializes a new instance of the <see cref="WrittenValue"/> class.</summary>
        /// <param name="value">The value to convert or test.</param>
        /// <param name="location">The source location the diagnostic reports at.</param>
        public WrittenValue(string value, Location location)
        {
            Value = value;
            Location = location;
        }

        /// <summary>Gets the value for the written value.</summary>
        public string Value { get; }

        /// <summary>Gets the location for the written value.</summary>
        public Location Location { get; }
    }
}
