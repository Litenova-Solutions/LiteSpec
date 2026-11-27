using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2036: production code outside Domain reads time from the pipeline timestamp or an injected clock, never from the static clock.</summary>
/// <follows>litespec/core/backend-application#read-time-from-the-scoped-clock</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProductionClockAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2036);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test && layer != ArchitectureLayer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the production clock analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var types = new WellKnownTypes(context.Compilation, scope);
        context.RegisterOperationAction(types.AnalyzeProperty, OperationKind.PropertyReference);
    }

    /// <summary>Holds the well known types data the production clock analyzer code works with.</summary>
    private sealed class WellKnownTypes
    {
        /// <summary>Holds the date time used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _dateTime;
        /// <summary>Holds the date time offset used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _dateTimeOffset;
        /// <summary>Holds the scope used by the well known types code.</summary>
        private readonly FileScope _scope;

        /// <summary>Initializes a new instance of the <see cref="WellKnownTypes"/> class.</summary>
        /// <param name="compilation">The compilation whose types and references the check reads.</param>
        /// <param name="scope">Whether the rule applies to each file.</param>
        public WellKnownTypes(Compilation compilation, FileScope scope)
        {
            _scope = scope;
            _dateTime = compilation.GetTypeByMetadataName("System.DateTime");
            _dateTimeOffset = compilation.GetTypeByMetadataName("System.DateTimeOffset");
        }

        /// <summary>Analyzes the property.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        public void AnalyzeProperty(OperationAnalysisContext context)
        {
            if (!_scope.AppliesTo(context.Operation.Syntax?.SyntaxTree))
            {
                return;
            }

            var reference = (IPropertyReferenceOperation)context.Operation;
            if (!IsClockRead(reference.Property))
            {
                return;
            }

            if (IsInsideNameof(reference.Syntax))
            {
                return;
            }

            if (HasShortcutTag(EnclosingType(context.ContainingSymbol)))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2036, MemberLocation(reference.Syntax), reference.Syntax.ToString()));
        }

        /// <summary>Returns true when the property is a static clock read; otherwise false.</summary>
        /// <param name="property">The property declaration under inspection.</param>
        /// <returns>True for the Now, UtcNow and Today reads of DateTime and DateTimeOffset; otherwise false.</returns>
        private bool IsClockRead(IPropertySymbol property)
        {
            var containing = property.ContainingType;
            var name = property.Name;
            if (IsType(containing, _dateTime))
            {
                return name == "Now" || name == "UtcNow" || name == "Today";
            }

            if (IsType(containing, _dateTimeOffset))
            {
                return name == "Now" || name == "UtcNow";
            }

            return false;
        }

        /// <summary>Returns true when the candidate is type; otherwise false.</summary>
        /// <param name="candidate">The candidate supplied for the check, or null when the compilation does not provide it.</param>
        /// <param name="known">The known supplied for the check, or null when the compilation does not provide it.</param>
        /// <returns>True when the candidate is type; otherwise false.</returns>
        private static bool IsType(ISymbol? candidate, INamedTypeSymbol? known) =>
            known is not null && SymbolEqualityComparer.Default.Equals(candidate, known);

        /// <summary>Returns the named type enclosing the symbol, or null when there is none.</summary>
        /// <param name="symbol">The symbol under inspection.</param>
        /// <returns>The enclosing named type, or null.</returns>
        private static INamedTypeSymbol? EnclosingType(ISymbol? symbol) => symbol switch
        {
            IMethodSymbol method => method.ContainingType,
            IFieldSymbol field => field.ContainingType,
            IPropertySymbol property => property.ContainingType,
            INamedTypeSymbol type => type,
            _ => null,
        };

        /// <summary>Returns true when the type or any type containing it carries a development-shortcut tag; otherwise false.</summary>
        /// <param name="type">The type under inspection, or null when there is none.</param>
        /// <returns>True when a development-shortcut tag sits above the type; otherwise false.</returns>
        private static bool HasShortcutTag(INamedTypeSymbol? type)
        {
            for (var current = type; current is not null; current = current.ContainingType)
            {
                foreach (var reference in current.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax() is TypeDeclarationSyntax part && IsTagged(part))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Returns true when the declaration carries a development-shortcut tag; otherwise false.</summary>
        /// <param name="part">One declaration of the type.</param>
        /// <returns>True when the leading documentation names a development shortcut; otherwise false.</returns>
        private static bool IsTagged(TypeDeclarationSyntax part)
        {
            foreach (var trivia in part.GetLeadingTrivia())
            {
                if (IsDocumentation(trivia) && trivia.ToString().IndexOf("development-shortcut", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns true when the trivia is an XML documentation comment, parsed or as plain text.</summary>
        /// <param name="trivia">The trivia under inspection.</param>
        /// <returns>True when the trivia is a documentation comment; otherwise false.</returns>
        private static bool IsDocumentation(SyntaxTrivia trivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                return true;
            }

            // Without a documentation file the compiler parses "///" as an ordinary single-line comment.
            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                return false;
            }

            var text = trivia.ToString();
            return text.StartsWith("///", StringComparison.Ordinal) && !text.StartsWith("////", StringComparison.Ordinal);
        }

        /// <summary>Returns true when the syntax is inside nameof; otherwise false.</summary>
        /// <param name="syntax">The syntax node under inspection.</param>
        /// <returns>True when the syntax is inside nameof; otherwise false.</returns>
        private static bool IsInsideNameof(SyntaxNode syntax)
        {
            for (var node = syntax.Parent; node is not null; node = node.Parent)
            {
                if (node is InvocationExpressionSyntax invocation
                    && invocation.Expression is IdentifierNameSyntax name
                    && name.Identifier.ValueText == "nameof")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the location.</summary>
        /// <param name="syntax">The syntax node under inspection.</param>
        /// <returns>The location.</returns>
        private static Location MemberLocation(SyntaxNode syntax) =>
            syntax is MemberAccessExpressionSyntax access ? access.Name.GetLocation() : syntax.GetLocation();
    }
}
