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
///     Moves the CancellationToken parameter LS3002 reports to the position the
///     analyzer accepts, last or just before a trailing params array, and
///     reorders the positional arguments at every call site in the solution.
///     Named arguments stay untouched. Fix All moves every reported token in scope.
/// </summary>
/// <follows>litespec/packs/dotnet/naming#put-the-cancellation-token-last</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveCancellationTokenLastCodeFixProvider))]
[Shared]
public sealed class MoveCancellationTokenLastCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS3002");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];

        if (await MovePlan.ReadAsync(
            context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false) is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Move CancellationToken parameter last",
                createChangedSolution: token => MoveAsync(context.Document, diagnostic, token),
                equivalenceKey: "MoveCancellationTokenLast"),
            diagnostic);
    }

    /// <summary>Returns the solution with the token moved.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the solution.</returns>
    private static async Task<Solution> MoveAsync(
        Document document, Diagnostic diagnostic, System.Threading.CancellationToken token)
    {
        var plan = await MovePlan.ReadAsync(document, diagnostic.Location.SourceSpan, token).ConfigureAwait(false);

        if (plan is null)
        {
            return document.Project.Solution;
        }

        return await plan.ApplyAsync(token).ConfigureAwait(false);
    }

    /// <summary>
    ///     The move LS3002 needs: the declaration, the token position, and the new
    ///     parameter order. Read from one solution snapshot, then applied to it.
    /// </summary>
    private sealed class MovePlan
    {
        /// <summary>Holds the solution the plan reads.</summary>
        private readonly Solution _solution;
        /// <summary>Holds the document holding the declaration being fixed.</summary>
        private readonly DocumentId _documentId;
        /// <summary>Holds the method or local function declaration being fixed.</summary>
        private readonly SyntaxNode _declaration;
        /// <summary>Holds the parameter list the token moves within.</summary>
        private readonly ParameterListSyntax _list;
        /// <summary>Holds the new parameter order: the old index now at each slot.</summary>
        private readonly List<int> _newOrder;
        /// <summary>Holds the new slot of each old parameter index.</summary>
        private readonly int[] _moved;
        /// <summary>Holds the declared name of each old parameter index.</summary>
        private readonly string[] _names;
        /// <summary>Holds the parameter count of the declaration.</summary>
        private readonly int _count;
        /// <summary>Holds true when the last parameter is a params array.</summary>
        private readonly bool _lastIsParams;
        /// <summary>Holds the declared method the call sites resolve to.</summary>
        private readonly IMethodSymbol _symbol;

        /// <summary>Initializes a new instance of the <see cref="MovePlan"/> class.</summary>
        /// <param name="solution">The solution the plan reads.</param>
        /// <param name="documentId">The document holding the declaration being fixed.</param>
        /// <param name="declaration">The method or local function declaration being fixed.</param>
        /// <param name="list">The parameter list the token moves within.</param>
        /// <param name="newOrder">The new parameter order: the old index now at each slot.</param>
        /// <param name="moved">The new slot of each old parameter index.</param>
        /// <param name="names">The declared name of each old parameter index.</param>
        /// <param name="count">The parameter count of the declaration.</param>
        /// <param name="lastIsParams">True when the last parameter is a params array.</param>
        /// <param name="symbol">The declared method the call sites resolve to.</param>
        private MovePlan(
            Solution solution,
            DocumentId documentId,
            SyntaxNode declaration,
            ParameterListSyntax list,
            List<int> newOrder,
            int[] moved,
            string[] names,
            int count,
            bool lastIsParams,
            IMethodSymbol symbol)
        {
            _solution = solution;
            _documentId = documentId;
            _declaration = declaration;
            _list = list;
            _newOrder = newOrder;
            _moved = moved;
            _names = names;
            _count = count;
            _lastIsParams = lastIsParams;
            _symbol = symbol;
        }

        /// <summary>
        ///     Reads the move for the diagnostic span: the token parameter, its target
        ///     slot, and the new order. Returns null when the token already sits where
        ///     the analyzer accepts it, or when the span names no plain method.
        /// </summary>
        /// <param name="document">The document holding the diagnostic.</param>
        /// <param name="span">The source span the diagnostic covers.</param>
        /// <param name="token">The token that cancels the operation when the host aborts it.</param>
        /// <returns>A task whose result is the plan, or null.</returns>
        public static async Task<MovePlan?> ReadAsync(
            Document document, Microsoft.CodeAnalysis.Text.TextSpan span, System.Threading.CancellationToken token)
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);

            if (root is null
                || model is null
                || ParameterAt(root, span) is not ParameterSyntax parameter
                || parameter.Parent is not ParameterListSyntax list
                || list.Parent is not SyntaxNode declaration)
            {
                return null;
            }

            IMethodSymbol? symbol = declaration switch
            {
                MethodDeclarationSyntax method => model.GetDeclaredSymbol(method, token) as IMethodSymbol,
                LocalFunctionStatementSyntax local => model.GetDeclaredSymbol(local, token) as IMethodSymbol,
                _ => null,
            };

            var parameters = list.Parameters;
            var count = parameters.Count;

            if (symbol is null || symbol.Parameters.Length != count || count < 2)
            {
                return null;
            }

            var tokenIndex = parameters.IndexOf(parameter);

            if (tokenIndex < 0
                || !string.Equals(
                    symbol.Parameters[tokenIndex].Name,
                    parameter.Identifier.ValueText,
                    System.StringComparison.Ordinal)
                || symbol.IsOverride
                || symbol.ExplicitInterfaceImplementations.Length != 0
                || parameters[tokenIndex].Modifiers.Any(SyntaxKind.ParamsKeyword))
            {
                return null;
            }

            var lastIsParams = parameters[count - 1].Modifiers.Any(SyntaxKind.ParamsKeyword);
            var target = lastIsParams && tokenIndex != count - 1 ? count - 2 : count - 1;

            if (tokenIndex == target)
            {
                return null;
            }

            var newOrder = new List<int>();

            for (var index = 0; index < count; index++)
            {
                if (index != tokenIndex)
                {
                    newOrder.Add(index);
                }
            }

            newOrder.Insert(target, tokenIndex);

            var moved = new int[count];

            for (var slot = 0; slot < count; slot++)
            {
                moved[newOrder[slot]] = slot;
            }

            var names = new string[count];

            for (var index = 0; index < count; index++)
            {
                names[index] = symbol.Parameters[index].Name;
            }

            return new MovePlan(
                document.Project.Solution,
                document.Id,
                declaration,
                list,
                newOrder,
                moved,
                names,
                count,
                lastIsParams,
                symbol);
        }

        /// <summary>Applies the move to the solution.</summary>
        /// <param name="token">The token that cancels the operation when the host aborts it.</param>
        /// <returns>A task whose result is the solution.</returns>
        public async Task<Solution> ApplyAsync(System.Threading.CancellationToken token)
        {
            var roots = new Dictionary<DocumentId, SyntaxNode>();
            var maps = new Dictionary<DocumentId, Dictionary<SyntaxNode, SyntaxNode>>();

            foreach (var project in _solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
                    var model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);

                    if (root is null || model is null)
                    {
                        continue;
                    }

                    roots[document.Id] = root;

                    foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (IsCallOf(model.GetSymbolInfo(call, token).Symbol, _symbol)
                            && RewriteCall(call) is InvocationExpressionSyntax rewritten)
                        {
                            Map(document.Id, call, rewritten, maps);
                        }
                    }
                }
            }

            if (!roots.ContainsKey(_documentId))
            {
                return _solution;
            }

            Map(_documentId, _declaration, NewDeclaration(), maps);

            var solution = _solution;

            foreach (var pair in maps)
            {
                if (!roots.TryGetValue(pair.Key, out var root))
                {
                    continue;
                }

                var replacements = pair.Value;
                solution = solution.WithDocumentSyntaxRoot(
                    pair.Key, root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]));
            }

            return solution;
        }

        /// <summary>The parameter holding the span, or null.</summary>
        /// <param name="root">The syntax root of the tree being searched.</param>
        /// <param name="span">The source span the diagnostic covers.</param>
        /// <returns>The parameter, or null when none applies.</returns>
        private static ParameterSyntax? ParameterAt(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            var node = root.FindNode(span, getInnermostNodeForTie: true);
            return node.FirstAncestorOrSelf<ParameterSyntax>();
        }

        /// <summary>Returns true when the candidate calls the target; otherwise false.</summary>
        /// <param name="candidate">The symbol the call site resolves to, or null when it resolves to none.</param>
        /// <param name="target">The declared method the fix moves.</param>
        /// <returns>True when the candidate calls the target; otherwise false.</returns>
        private static bool IsCallOf(ISymbol? candidate, IMethodSymbol target)
        {
            if (candidate is not IMethodSymbol method)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(method.OriginalDefinition, target.OriginalDefinition))
            {
                return true;
            }

            return method.ReducedFrom is IMethodSymbol reduced
                && !ReferenceEquals(reduced, method)
                && SymbolEqualityComparer.Default.Equals(reduced.OriginalDefinition, target.OriginalDefinition);
        }

        /// <summary>Records one syntax replacement in its document map.</summary>
        /// <param name="id">The document holding the replaced node.</param>
        /// <param name="original">The node the solution holds.</param>
        /// <param name="rewritten">The node taking its place.</param>
        /// <param name="maps">The replacements by document.</param>
        private static void Map(
            DocumentId id,
            SyntaxNode original,
            SyntaxNode rewritten,
            Dictionary<DocumentId, Dictionary<SyntaxNode, SyntaxNode>> maps)
        {
            if (!maps.TryGetValue(id, out var replacements))
            {
                replacements = new Dictionary<SyntaxNode, SyntaxNode>();
                maps[id] = replacements;
            }

            replacements[original] = rewritten;
        }

        /// <summary>Returns the declaration with the token moved.</summary>
        /// <returns>The declaration with the token moved.</returns>
        private SyntaxNode NewDeclaration()
        {
            var parameters = _list.Parameters;
            var reordered = new ParameterSyntax[_count];

            for (var slot = 0; slot < _count; slot++)
            {
                reordered[slot] = parameters[_newOrder[slot]]
                    .WithLeadingTrivia(parameters[slot].GetLeadingTrivia())
                    .WithTrailingTrivia(parameters[slot].GetTrailingTrivia());
            }

            var updated = _list.WithParameters(SyntaxFactory.SeparatedList(reordered, SeparatorsOf(parameters)));

            return _declaration.ReplaceNode(_list, updated);
        }

        /// <summary>
        ///     Rewrites one call site: the positional arguments follow their parameters
        ///     into the new order, and an argument left without a positional slot becomes
        ///     a named argument. Returns null when the call needs no change or the fix
        ///     cannot keep it compiling.
        /// </summary>
        /// <param name="call">The invocation expression under inspection.</param>
        /// <returns>The rewritten call, or null.</returns>
        private InvocationExpressionSyntax? RewriteCall(InvocationExpressionSyntax call)
        {
            var list = call.ArgumentList;
            var arguments = list.Arguments;

            if (arguments.Count == 0)
            {
                return null;
            }

            var namedStart = arguments.Count;

            for (var index = 0; index < arguments.Count; index++)
            {
                if (arguments[index].NameColon is not null)
                {
                    namedStart = index;
                    break;
                }
            }

            for (var index = namedStart; index < arguments.Count; index++)
            {
                if (arguments[index].NameColon is null)
                {
                    return null;
                }
            }

            var fixedCount = _lastIsParams ? _count - 1 : _count;

            if (namedStart > fixedCount && !_lastIsParams)
            {
                return null;
            }

            var kept = new List<ArgumentSyntax>();
            var keptFlags = new bool[fixedCount];
            var prefix = true;

            for (var slot = 0; slot < fixedCount; slot++)
            {
                var oldIndex = _newOrder[slot];

                if (prefix && oldIndex < namedStart)
                {
                    kept.Add(arguments[oldIndex]);
                    keptFlags[oldIndex] = true;
                }
                else
                {
                    prefix = false;
                }
            }

            var converted = new List<ArgumentSyntax>();

            for (var oldIndex = 0; oldIndex < fixedCount; oldIndex++)
            {
                if (oldIndex < namedStart && !keptFlags[oldIndex])
                {
                    converted.Add(Named(_names[oldIndex], arguments[oldIndex].Expression));
                }
            }

            var extra = new List<ArgumentSyntax>();

            for (var index = fixedCount; index < namedStart; index++)
            {
                extra.Add(arguments[index]);
            }

            if (extra.Count > 0 && (converted.Count > 0 || kept.Count != fixedCount))
            {
                return null;
            }

            if (converted.Count == 0 && extra.Count == 0 && kept.Count == namedStart)
            {
                var same = true;

                for (var index = 0; index < kept.Count; index++)
                {
                    if (!ReferenceEquals(kept[index], arguments[index]))
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                {
                    return null;
                }
            }

            for (var index = 0; index < kept.Count; index++)
            {
                kept[index] = kept[index]
                    .WithLeadingTrivia(arguments[index].GetLeadingTrivia())
                    .WithTrailingTrivia(arguments[index].GetTrailingTrivia());
            }

            var final = new List<ArgumentSyntax>(kept);
            final.AddRange(extra);
            final.AddRange(converted);

            for (var index = namedStart; index < arguments.Count; index++)
            {
                final.Add(arguments[index]);
            }

            if (kept.Count == 0 && final.Count > 0)
            {
                final[0] = final[0].WithLeadingTrivia(arguments[0].GetLeadingTrivia());
            }

            return call.WithArgumentList(list.WithArguments(SyntaxFactory.SeparatedList(final, SeparatorsOf(arguments))));
        }

        /// <summary>Names the expression for the parameter.</summary>
        /// <param name="name">The declared name of the parameter.</param>
        /// <param name="expression">The argument expression naming its parameter.</param>
        /// <returns>The named argument.</returns>
        private static ArgumentSyntax Named(string name, ExpressionSyntax expression) =>
            SyntaxFactory.Argument(
                SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))
                    .WithColonToken(SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space)),
                default,
                expression.WithLeadingTrivia()).WithLeadingTrivia();

        /// <summary>The separator tokens between the list entries, in order.</summary>
        /// <typeparam name="T">The syntax node kind the list holds.</typeparam>
        /// <param name="list">The separated list under inspection.</param>
        /// <returns>The separators in list order.</returns>
        private static List<SyntaxToken> SeparatorsOf<T>(SeparatedSyntaxList<T> list)
            where T : SyntaxNode
        {
            var separators = new List<SyntaxToken>();

            for (var index = 0; index < list.Count - 1; index++)
            {
                separators.Add(list.GetSeparator(index));
            }

            return separators;
        }
    }
}
