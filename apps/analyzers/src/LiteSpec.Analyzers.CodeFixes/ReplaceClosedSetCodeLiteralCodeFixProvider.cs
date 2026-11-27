using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Replaces the closed-set Code literal LS2041 reports with the typed case it names:
///     outside a queryable expression the comparison branches on the one sealed case
///     returning the literal, and inside an expression tree the literal reads the case
///     CodeValue constant, adding the constant and pointing Code at it when the case
///     declares none. The fix stays silent when no case or several cases return the
///     literal. Fix All applies every fix of its own kind in scope.
/// </summary>
/// <follows>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReplaceClosedSetCodeLiteralCodeFixProvider))]
[Shared]
public sealed class ReplaceClosedSetCodeLiteralCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS2041");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var rewrite = await Rewrite.ReadAsync(
            context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false);

        if (rewrite is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                rewrite.Title,
                createChangedSolution: token => Rewrite.ApplyAsync(
                    context.Document, diagnostic.Location.SourceSpan, token),
                equivalenceKey: rewrite.EquivalenceKey),
            diagnostic);
    }

    /// <summary>
    ///     The rewrite one LS2041 diagnostic takes: the titles the fix offers and the
    ///     nodes the transform reads. Read once to decide and again to apply, so Fix All
    ///     sees the fixes that ran before it.
    /// </summary>
    private sealed class Rewrite
    {
        /// <summary>The parse options the rewrite parses its replacement with.</summary>
        private static readonly CSharpParseOptions LatestOptions = new CSharpParseOptions(LanguageVersion.Latest);

        /// <summary>The document holding the comparison being fixed.</summary>
        private readonly Document _document;

        /// <summary>The syntax root the rewrite reads.</summary>
        private readonly SyntaxNode _root;

        /// <summary>The literal the diagnostic covers.</summary>
        private readonly LiteralExpressionSyntax _literal;

        /// <summary>The literal value the diagnostic covers.</summary>
        private readonly string _value;

        /// <summary>The equality the literal takes part in.</summary>
        private readonly BinaryExpressionSyntax _comparison;

        /// <summary>The expression the Code read runs on, or null when the fix keeps the comparison.</summary>
        private readonly ExpressionSyntax? _instance;

        /// <summary>The name of the one sealed case returning the literal.</summary>
        private readonly string _caseName;

        /// <summary>Holds true when the comparison is a not-equals comparison.</summary>
        private readonly bool _negated;

        /// <summary>Holds true when the comparison sits in an expression tree.</summary>
        private readonly bool _queryable;

        /// <summary>Holds true when the fix adds the CodeValue constant to the case.</summary>
        private readonly bool _addsCodeValue;

        /// <summary>The case declaration gaining the CodeValue constant, or null when the fix adds none.</summary>
        private readonly TypeDeclarationSyntax? _caseDeclaration;

        /// <summary>The document holding the case declaration, or null when the fix adds none.</summary>
        private readonly DocumentId? _caseDocumentId;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Rewrite"/> class.
        /// </summary>
        /// <param name="document">The document holding the comparison being fixed.</param>
        /// <param name="root">The syntax root the rewrite reads.</param>
        /// <param name="literal">The literal the diagnostic covers.</param>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <param name="comparison">The equality the literal takes part in.</param>
        /// <param name="instance">The expression the Code read runs on, or null when the fix keeps the comparison.</param>
        /// <param name="caseName">The name of the one sealed case returning the literal.</param>
        /// <param name="negated">True when the comparison is a not-equals comparison.</param>
        /// <param name="queryable">True when the comparison sits in an expression tree.</param>
        /// <param name="addsCodeValue">True when the fix adds the CodeValue constant to the case.</param>
        /// <param name="caseDeclaration">The case declaration gaining the CodeValue constant, or null when the fix adds none.</param>
        /// <param name="caseDocumentId">The document holding the case declaration, or null when the fix adds none.</param>
        /// <param name="title">The title of the fix action.</param>
        /// <param name="equivalenceKey">The equivalence key grouping the fix under Fix All.</param>
        private Rewrite(
            Document document,
            SyntaxNode root,
            LiteralExpressionSyntax literal,
            string value,
            BinaryExpressionSyntax comparison,
            ExpressionSyntax? instance,
            string caseName,
            bool negated,
            bool queryable,
            bool addsCodeValue,
            TypeDeclarationSyntax? caseDeclaration,
            DocumentId? caseDocumentId,
            string title,
            string equivalenceKey)
        {
            _document = document;
            _root = root;
            _literal = literal;
            _value = value;
            _comparison = comparison;
            _instance = instance;
            _caseName = caseName;
            _negated = negated;
            _queryable = queryable;
            _addsCodeValue = addsCodeValue;
            _caseDeclaration = caseDeclaration;
            _caseDocumentId = caseDocumentId;
            Title = title;
            EquivalenceKey = equivalenceKey;
        }

        /// <summary>Gets the title of the fix action.</summary>
        public string Title { get; }

        /// <summary>Gets the equivalence key grouping the fix under Fix All.</summary>
        public string EquivalenceKey { get; }

        /// <summary>Reads the rewrite for the diagnostic span, or null when the fix offers none.</summary>
        /// <param name="document">The document holding the diagnostic.</param>
        /// <param name="span">The source span the diagnostic covers.</param>
        /// <param name="token">The token that cancels the operation when the host aborts it.</param>
        /// <returns>A task whose result is the rewrite, or null when the fix offers none.</returns>
        public static async Task<Rewrite?> ReadAsync(
            Document document, Microsoft.CodeAnalysis.Text.TextSpan span, System.Threading.CancellationToken token)
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);

            if (root is null
                || model is null
                || LiteralAt(root, span) is not LiteralExpressionSyntax literal
                || literal.Token.ValueText is not string value
                || ComparisonOf(literal) is not BinaryExpressionSyntax comparison
                || ComparedCode(comparison, literal) is not ExpressionSyntax code
                || model.GetSymbolInfo(code, token).Symbol is not IPropertySymbol property
                || !IsClosedSetCode(property)
                || code is not MemberAccessExpressionSyntax access
                || MatchingCase(model.Compilation, property.ContainingType, value) is not INamedTypeSymbol match)
            {
                return null;
            }

            if (IsInExpressionTree(model, comparison))
            {
                return ForQueryable(document, root, literal, value, comparison, match);
            }

            return new Rewrite(
                document,
                root,
                literal,
                value,
                comparison,
                access.Expression,
                match.Name,
                comparison.IsKind(SyntaxKind.NotEqualsExpression),
                queryable: false,
                addsCodeValue: false,
                caseDeclaration: null,
                caseDocumentId: null,
                "Branch on '" + match.Name + "'",
                "BranchOnClosedSetCase");
        }

        /// <summary>Applies the rewrite the span resolves to in the live solution.</summary>
        /// <param name="document">The document holding the diagnostic.</param>
        /// <param name="span">The source span the diagnostic covers.</param>
        /// <param name="token">The token that cancels the operation when the host aborts it.</param>
        /// <returns>A task whose result is the solution.</returns>
        public static async Task<Solution> ApplyAsync(
            Document document, Microsoft.CodeAnalysis.Text.TextSpan span, System.Threading.CancellationToken token)
        {
            var rewrite = await ReadAsync(document, span, token).ConfigureAwait(false);

            if (rewrite is null)
            {
                return document.Project.Solution;
            }

            return await rewrite.TransformAsync(token).ConfigureAwait(false);
        }

        /// <summary>Reads the queryable rewrite for the literal, or null when the fix offers none.</summary>
        /// <param name="document">The document holding the diagnostic.</param>
        /// <param name="root">The syntax root the rewrite reads.</param>
        /// <param name="literal">The literal the diagnostic covers.</param>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <param name="comparison">The equality the literal takes part in.</param>
        /// <param name="match">The one sealed case returning the literal.</param>
        /// <returns>The rewrite, or null when the fix offers none.</returns>
        private static Rewrite? ForQueryable(
            Document document,
            SyntaxNode root,
            LiteralExpressionSyntax literal,
            string value,
            BinaryExpressionSyntax comparison,
            INamedTypeSymbol match)
        {
            foreach (var member in match.GetMembers("CodeValue"))
            {
                if (member is not IFieldSymbol field
                    || !field.IsConst
                    || field.Type.SpecialType != SpecialType.System_String
                    || !SymbolEqualityComparer.Default.Equals(field.ContainingType, match))
                {
                    continue;
                }

                if (field.ConstantValue is not string current
                    || !current.Equals(value, System.StringComparison.Ordinal))
                {
                    return null;
                }

                return new Rewrite(
                    document,
                    root,
                    literal,
                    value,
                    comparison,
                    instance: null,
                    match.Name,
                    negated: false,
                    queryable: true,
                    addsCodeValue: false,
                    caseDeclaration: null,
                    caseDocumentId: null,
                    "Compare '" + match.Name + ".CodeValue'",
                    "CompareClosedSetCodeValue");
            }

            foreach (var reference in match.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is not TypeDeclarationSyntax declaration
                    || CodeProperty(declaration, value) is null
                    || document.Project.Solution.GetDocumentId(reference.SyntaxTree) is not DocumentId caseDocumentId)
                {
                    continue;
                }

                return new Rewrite(
                    document,
                    root,
                    literal,
                    value,
                    comparison,
                    instance: null,
                    match.Name,
                    negated: false,
                    queryable: true,
                    addsCodeValue: true,
                    declaration,
                    caseDocumentId,
                    "Compare '" + match.Name + ".CodeValue'",
                    "CompareClosedSetCodeValue");
            }

            return null;
        }

        /// <summary>Returns the solution with the rewrite applied.</summary>
        /// <param name="token">The token that cancels the operation when the host aborts it.</param>
        /// <returns>A task whose result is the solution.</returns>
        private Task<Solution> TransformAsync(System.Threading.CancellationToken token)
        {
            if (!_queryable)
            {
                return Task.FromResult(TransformBranch());
            }

            return TransformQueryableAsync(token);
        }

        /// <summary>Returns the solution with the comparison branching on the sealed case.</summary>
        /// <returns>The solution.</returns>
        private Solution TransformBranch()
        {
            var instance = _instance;

            if (instance is null)
            {
                return _document.Project.Solution;
            }

            var text = instance.WithoutTrivia().ToString() + (_negated ? " is not " : " is ") + _caseName;
            var replacement = SyntaxFactory.ParseExpression(text, options: LatestOptions)
                .WithLeadingTrivia(_comparison.GetLeadingTrivia())
                .WithTrailingTrivia(_comparison.GetTrailingTrivia());

            return _document.WithSyntaxRoot(_root.ReplaceNode(_comparison, replacement)).Project.Solution;
        }

        /// <summary>Returns the solution with the literal reading the CodeValue constant.</summary>
        /// <param name="token">The token that cancels the operation when the host aborts it.</param>
        /// <returns>A task whose result is the solution.</returns>
        private async Task<Solution> TransformQueryableAsync(System.Threading.CancellationToken token)
        {
            var access = SyntaxFactory.ParseExpression(_caseName + ".CodeValue", options: LatestOptions)
                .WithLeadingTrivia(_literal.GetLeadingTrivia())
                .WithTrailingTrivia(_literal.GetTrailingTrivia());
            var comparisonRoot = _root.ReplaceNode(_literal, access);
            var caseDeclaration = _caseDeclaration;
            var caseDocumentId = _caseDocumentId;

            if (!_addsCodeValue
                || caseDeclaration is null
                || caseDocumentId is null
                || CodeProperty(caseDeclaration, _value) is not PropertyDeclarationSyntax code)
            {
                return _document.WithSyntaxRoot(comparisonRoot).Project.Solution;
            }

            var fixedCode = (PropertyDeclarationSyntax)code.ReplaceNodes(
                CodeLiterals(code, _value),
                (original, _) => SyntaxFactory.IdentifierName("CodeValue")
                    .WithLeadingTrivia(original.GetLeadingTrivia())
                    .WithTrailingTrivia(original.GetTrailingTrivia()));
            var members = caseDeclaration.Members;
            var updated = caseDeclaration.WithMembers(
                members.Replace(code, fixedCode).Insert(members.IndexOf(code) + 1, CaseValueField(_value, code)));

            if (caseDocumentId.Equals(_document.Id))
            {
                var replacements = new Dictionary<SyntaxNode, SyntaxNode>
                {
                    [_literal] = access,
                    [caseDeclaration] = updated,
                };

                return _document.WithSyntaxRoot(
                    _root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original])).Project.Solution;
            }

            var solution = _document.WithSyntaxRoot(comparisonRoot).Project.Solution;

            if (solution.GetDocument(caseDocumentId) is not Document caseDocument
                || await caseDocument.GetSyntaxRootAsync(token).ConfigureAwait(false) is not SyntaxNode caseRoot)
            {
                return solution;
            }

            return solution.WithDocumentSyntaxRoot(caseDocumentId, caseRoot.ReplaceNode(caseDeclaration, updated));
        }

        /// <summary>The string literal the diagnostic covers, or null when the span names none.</summary>
        /// <param name="root">The syntax root of the tree being searched.</param>
        /// <param name="span">The source span the diagnostic covers.</param>
        /// <returns>The literal, or null when none applies.</returns>
        private static LiteralExpressionSyntax? LiteralAt(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            var literal = root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<LiteralExpressionSyntax>();

            if (literal is null || literal.Span != span || !literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return null;
            }

            return literal;
        }

        /// <summary>The equality the literal takes part in, or null when the literal sits outside == and !=.</summary>
        /// <param name="literal">The literal the diagnostic covers.</param>
        /// <returns>The equality, or null when none applies.</returns>
        private static BinaryExpressionSyntax? ComparisonOf(LiteralExpressionSyntax literal)
        {
            var comparison = literal.FirstAncestorOrSelf<BinaryExpressionSyntax>();

            if (comparison is not null
                && !comparison.IsKind(SyntaxKind.EqualsExpression)
                && !comparison.IsKind(SyntaxKind.NotEqualsExpression))
            {
                return null;
            }

            return comparison;
        }

        /// <summary>The side of the comparison reading Code, or null when neither side reads Code alone.</summary>
        /// <param name="comparison">The equality under inspection.</param>
        /// <param name="literal">The literal the diagnostic covers.</param>
        /// <returns>The Code expression, or null when the literal spans both sides.</returns>
        private static ExpressionSyntax? ComparedCode(BinaryExpressionSyntax comparison, LiteralExpressionSyntax literal)
        {
            var leftHoldsLiteral = comparison.Left.DescendantNodesAndSelf().Contains(literal);
            var rightHoldsLiteral = comparison.Right.DescendantNodesAndSelf().Contains(literal);

            if (leftHoldsLiteral && !rightHoldsLiteral)
            {
                return comparison.Right;
            }

            if (rightHoldsLiteral && !leftHoldsLiteral)
            {
                return comparison.Left;
            }

            return null;
        }

        /// <summary>Returns true when the property is a Code of a closed set; otherwise false.</summary>
        /// <param name="property">The property under inspection.</param>
        /// <returns>True when the property is a string Code declared on an abstract type.</returns>
        private static bool IsClosedSetCode(IPropertySymbol property) =>
            property.Name.Equals("Code", System.StringComparison.Ordinal)
            && property.Type.SpecialType == SpecialType.System_String
            && property.ContainingType is { IsAbstract: true };

        /// <summary>The one sealed case returning the literal from Code or a CodeValue constant, or null.</summary>
        /// <param name="compilation">The compilation holding the closed set.</param>
        /// <param name="closedSet">The abstract type declaring Code.</param>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <returns>The case, or null when no case or several cases return the literal.</returns>
        private static INamedTypeSymbol? MatchingCase(Compilation compilation, INamedTypeSymbol closedSet, string value)
        {
            INamedTypeSymbol? match = null;
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(compilation.GlobalNamespace);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                foreach (var member in current.GetMembers())
                {
                    if (member is INamespaceSymbol space)
                    {
                        stack.Push(space);
                    }
                    else if (member is INamedTypeSymbol type)
                    {
                        stack.Push(type);

                        if (type.IsSealed && DerivesFrom(type, closedSet) && ReturnsLiteral(type, value))
                        {
                            if (match is not null)
                            {
                                return null;
                            }

                            match = type;
                        }
                    }
                }
            }

            return match;
        }

        /// <summary>Returns true when the type derives from the closed set; otherwise false.</summary>
        /// <param name="type">The type under inspection.</param>
        /// <param name="closedSet">The abstract type declaring Code.</param>
        /// <returns>True when a base type is the closed set.</returns>
        private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol closedSet)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, closedSet))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns true when the case returns the literal from Code or a CodeValue constant; otherwise false.</summary>
        /// <param name="type">The sealed case under inspection.</param>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <returns>True when the case names the literal.</returns>
        private static bool ReturnsLiteral(INamedTypeSymbol type, string value)
        {
            foreach (var member in type.GetMembers("Code"))
            {
                if (member is not IPropertySymbol property
                    || property.Type.SpecialType != SpecialType.System_String
                    || !SymbolEqualityComparer.Default.Equals(property.ContainingType, type))
                {
                    continue;
                }

                foreach (var reference in property.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax() is not PropertyDeclarationSyntax declaration)
                    {
                        continue;
                    }

                    if (CodeLiterals(declaration, value).Count > 0)
                    {
                        return true;
                    }
                }
            }

            foreach (var member in type.GetMembers("CodeValue"))
            {
                if (member is IFieldSymbol field
                    && field.IsConst
                    && field.Type.SpecialType == SpecialType.System_String
                    && SymbolEqualityComparer.Default.Equals(field.ContainingType, type)
                    && field.ConstantValue is string current
                    && current.Equals(value, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns true when the comparison sits in a lambda the compiler converts to an expression tree; otherwise false.</summary>
        /// <param name="model">The semantic model of the document holding the comparison.</param>
        /// <param name="comparison">The equality under inspection.</param>
        /// <returns>True when an enclosing lambda converts to an expression tree.</returns>
        private static bool IsInExpressionTree(SemanticModel model, SyntaxNode comparison)
        {
            foreach (var ancestor in comparison.Ancestors())
            {
                if (ancestor is not LambdaExpressionSyntax && ancestor is not AnonymousMethodExpressionSyntax)
                {
                    continue;
                }

                if (model.GetTypeInfo(ancestor).ConvertedType is not INamedTypeSymbol converted)
                {
                    continue;
                }

                var space = converted.ContainingNamespace?.ToDisplayString();

                if (converted.Name.Equals("Expression", System.StringComparison.Ordinal)
                    && space is not null
                    && space.Equals("System.Linq.Expressions", System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The Code property returning the literal, or null.</summary>
        /// <param name="declaration">The case declaration under inspection.</param>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <returns>The property, or null when none returns the literal.</returns>
        private static PropertyDeclarationSyntax? CodeProperty(TypeDeclarationSyntax declaration, string value)
        {
            foreach (var member in declaration.Members)
            {
                if (member is PropertyDeclarationSyntax property
                    && property.Identifier.ValueText.Equals("Code", System.StringComparison.Ordinal)
                    && CodeLiterals(property, value).Count > 0)
                {
                    return property;
                }
            }

            return null;
        }

        /// <summary>The string literals the node holds with the value.</summary>
        /// <param name="node">The node under inspection.</param>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <returns>The literals.</returns>
        private static List<LiteralExpressionSyntax> CodeLiterals(SyntaxNode node, string value)
        {
            var literals = new List<LiteralExpressionSyntax>();

            foreach (var literal in node.DescendantNodes().OfType<LiteralExpressionSyntax>())
            {
                if (literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && literal.Token.ValueText is string current
                    && current.Equals(value, System.StringComparison.Ordinal))
                {
                    literals.Add(literal);
                }
            }

            return literals;
        }

        /// <summary>The CodeValue constant declaring the literal, set off by the blank line the house style keeps.</summary>
        /// <param name="value">The literal value the diagnostic covers.</param>
        /// <param name="code">The Code property the constant follows.</param>
        /// <returns>The constant.</returns>
        private static FieldDeclarationSyntax CaseValueField(string value, PropertyDeclarationSyntax code)
        {
            var end = "\n";
            var indent = string.Empty;

            foreach (var trivia in code.GetLeadingTrivia())
            {
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    end = trivia.ToString();
                }
                else if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    indent = trivia.ToString();
                }
            }

            var declarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("CodeValue"))
                .WithInitializer(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.Token(SyntaxKind.EqualsToken)
                        .WithLeadingTrivia(SyntaxFactory.Space)
                        .WithTrailingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value))));
            var field = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.StringKeyword).WithTrailingTrivia(SyntaxFactory.Space)),
                        SyntaxFactory.SingletonSeparatedList(declarator)))
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.Token(SyntaxKind.ConstKeyword).WithTrailingTrivia(SyntaxFactory.Space)))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            var leading = new List<SyntaxTrivia> { SyntaxFactory.EndOfLine(end) };

            if (indent.Length > 0)
            {
                leading.Add(SyntaxFactory.Whitespace(indent));
            }

            return field
                .WithLeadingTrivia(SyntaxFactory.TriviaList(leading))
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(end)));
        }
    }
}
