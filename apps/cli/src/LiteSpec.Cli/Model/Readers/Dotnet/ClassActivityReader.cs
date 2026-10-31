using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     What a class does with what it holds: the collaborators it builds, the
///     repository reads it makes inside a loop, and the filters it applies in memory.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class ClassActivityReader
{
    /// <summary>The LINQ operators that filter, order, page or count in memory.</summary>
    private static readonly HashSet<string> MemoryFilterOperators = new(StringComparer.Ordinal)
    {
        "Where", "OrderBy", "Skip", "Take", "Count", "Any", "SingleOrDefault",
    };

    /// <summary>
    ///     The collaborators a class builds with <c>new</c> whose own constructor takes
    ///     a repository, in line order.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    /// <returns>The collaborators the class builds, in line order.</returns>
    public static List<CodeCollaborator> CollaboratorsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        HashSet<string> holders)
    {
        var found = new List<CodeCollaborator>();
        var seen = new HashSet<(string Type, int Line)>();

        if (holders.Count == 0)
        {
            return found;
        }

        foreach (var creation in declared.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var type = DotnetNames.SimpleName(DotnetNames.SimpleTypeName(creation.Type));
            var line = tree.GetLineSpan(creation.Span).StartLinePosition.Line + 1;

            if (holders.Contains(type) && seen.Add((type, line)))
            {
                found.Add(new CodeCollaborator(type, file, line));
            }
        }

        found.Sort((left, right) =>
        {
            var line = left.Line.CompareTo(right.Line);

            return line != 0 ? line : string.Compare(left.Type, right.Type, StringComparison.Ordinal);
        });

        return found;
    }

    /// <summary>
    ///     The repository reads a class makes inside a <c>for</c>, <c>foreach</c> or
    ///     <c>while</c> body: an awaited read on a held repository, in line order.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <returns>The repository reads the class makes inside a loop body, in line order.</returns>
    public static List<CodeLoopRead> LoopReadsOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new List<CodeLoopRead>();
        var receivers = ClassDetailsReader.ReceiverTypes(declared);

        if (receivers.Count == 0)
        {
            return found;
        }

        foreach (var loop in declared.DescendantNodes().OfType<StatementSyntax>())
        {
            if (loop is not (ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax))
            {
                continue;
            }

            foreach (var invocation in loop.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax member
                    || member.Expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax })
                    || invocation.Parent is not AwaitExpressionSyntax)
                {
                    continue;
                }

                var receiver = member.Expression switch
                {
                    IdentifierNameSyntax plain => plain.Identifier.ValueText,
                    MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
                    _ => null,
                };

                var method = member.Name.Identifier.ValueText;

                if (receiver is not null
                    && receivers.TryGetValue(receiver, out var type)
                    && DotnetNames.KindOf(type).EndsWith("Repository", StringComparison.Ordinal)
                    && method.EndsWith("Async", StringComparison.Ordinal)
                    && (method.StartsWith("Get", StringComparison.Ordinal)
                        || method.StartsWith("Find", StringComparison.Ordinal)
                        || method.StartsWith("List", StringComparison.Ordinal)
                        || method.StartsWith("Load", StringComparison.Ordinal)
                        || method.StartsWith("Count", StringComparison.Ordinal)
                        || method.StartsWith("Exists", StringComparison.Ordinal)))
                {
                    found.Add(new CodeLoopRead(
                        type,
                        method,
                        file,
                        tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
                }
            }
        }

        found.Sort((left, right) =>
        {
            var line = left.Line.CompareTo(right.Line);

            return line != 0 ? line : string.Compare(left.Method, right.Method, StringComparison.Ordinal);
        });

        return found;
    }

    /// <summary>
    ///     The LINQ filters a query handler applies to the result of a repository list
    ///     call, chained or through a local, one per line.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <returns>The LINQ filters the class applies to a repository list, one per line.</returns>
    public static List<CodeMemoryFilter> MemoryFiltersOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new List<CodeMemoryFilter>();
        var seen = new HashSet<int>();
        var receivers = ClassDetailsReader.ReceiverTypes(declared);

        if (receivers.Count == 0)
        {
            return found;
        }

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member
                || !MemoryFilterOperators.Contains(member.Name.Identifier.ValueText))
            {
                continue;
            }

            if (!FiltersRepositoryList(invocation, member.Expression, receivers))
            {
                continue;
            }

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;

            if (seen.Add(line))
            {
                found.Add(new CodeMemoryFilter(file, line));
            }
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>
    ///     Whether a LINQ call filters the result of a repository list call: the call
    ///     chain bottoms out at one, or the filtered local holds one's awaited result.
    /// </summary>
    /// <param name="invocation">The LINQ invocation.</param>
    /// <param name="filtered">The expression the LINQ call filters.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <returns>True when a LINQ call filters the result of a repository list call.</returns>
    public static bool FiltersRepositoryList(
        InvocationExpressionSyntax invocation,
        ExpressionSyntax filtered,
        Dictionary<string, string> receivers)
    {
        var current = filtered;

        while (current is InvocationExpressionSyntax inner
            && inner.Expression is MemberAccessExpressionSyntax innerMember)
        {
            if (IsRepositoryListCall(innerMember, receivers))
            {
                return true;
            }

            current = innerMember.Expression;
        }

        return current is IdentifierNameSyntax local
            && LocalHoldsRepositoryList(invocation, local.Identifier.ValueText, receivers);
    }

    /// <summary>Whether the call lists through a held repository: <c>List*Async</c> on one.</summary>
    /// <param name="member">The member access of the call.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <returns>True when the call lists through a held repository.</returns>
    public static bool IsRepositoryListCall(MemberAccessExpressionSyntax member, Dictionary<string, string> receivers)
    {
        var receiver = member.Expression switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
            _ => null,
        };

        return receiver is not null
            && receivers.TryGetValue(receiver, out var type)
            && DotnetNames.KindOf(type).EndsWith("Repository", StringComparison.Ordinal)
            && member.Name.Identifier.ValueText.StartsWith("List", StringComparison.Ordinal)
            && member.Name.Identifier.ValueText.EndsWith("Async", StringComparison.Ordinal);
    }

    /// <summary>Whether the local holds the awaited result of a repository list call.</summary>
    /// <param name="invocation">The LINQ invocation.</param>
    /// <param name="name">The local's name.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <returns>True when the local holds the awaited result of a repository list call.</returns>
    public static bool LocalHoldsRepositoryList(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers)
    {
        var scope = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

        if (scope is null)
        {
            return false;
        }

        foreach (var declarator in scope.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText == name
                && declarator.Initializer?.Value is AwaitExpressionSyntax awaited
                && awaited.Expression is InvocationExpressionSyntax list
                && list.Expression is MemberAccessExpressionSyntax member
                && IsRepositoryListCall(member, receivers))
            {
                return true;
            }
        }

        return false;
    }
}
