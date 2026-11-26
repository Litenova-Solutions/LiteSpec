using System.Globalization;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using LiteSpec.Standards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Screens;

/// <summary>
///     Reads the route entries of an Android frontend: every destination activity or
///     fragment class, with the screen mark the entry carries and the generated-client
///     operations it calls. A call is an <c>*Async</c> invocation on a member typed or
///     named like the generated client, reported without the <c>Async</c> suffix.
/// </summary>
/// <follows>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</follows>
static partial class DotnetScreenReader
{
    /// <summary>Reads every route entry of the frontend, in file, line and address order.</summary>
    /// <param name="frontend">The frontend to read.</param>
    /// <returns>One route fact per Android destination.</returns>
    public static IReadOnlyList<RouteFact> Read(FrontendConfig frontend) => ReadWithProblems(frontend).Routes;

    /// <summary>Reads every route entry of the frontend, with one problem per file that could not be read or parsed.</summary>
    /// <param name="frontend">The frontend to read.</param>
    /// <returns>The route facts and the reader problems.</returns>
    public static DotnetScreenRead ReadWithProblems(FrontendConfig frontend)
    {
        var problems = new List<Finding>();
        var reported = new HashSet<string>(StringComparer.Ordinal);

        var routes = frontend.Platform.Equals("android", StringComparison.Ordinal)
            ? ReadAndroid(frontend, problems, reported)
            : new List<RouteFact>();

        problems.Sort((left, right) =>
        {
            var file = string.Compare(left.Path, right.Path, StringComparison.Ordinal);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });

        return new DotnetScreenRead(routes, problems);
    }

    /// <summary>Adds one <c>reader-missing</c> problem for the file, when none was added for it yet.</summary>
    /// <param name="problems">The problems collected so far, added to in place.</param>
    /// <param name="reported">The files already reported, so each file reports one problem.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <param name="message">The problem message to record for the file.</param>
    private static void AddProblem(List<Finding> problems, HashSet<string> reported, string file, string message)
    {
        if (!reported.Add(file))
        {
            return;
        }

        problems.Add(new Finding("reader-missing", message, file, 1, null)
        {
            Reader = Finding.RoslynReader,
            ReaderIsQualified = true,
        });
    }

    /// <summary>Reads every Android destination class of the frontend, one route fact per destination.</summary>
    /// <param name="frontend">The Android frontend whose destination classes are read.</param>
    /// <param name="problems">The problems collected so far, added to in place.</param>
    /// <param name="reported">The files already reported, so each file reports one problem.</param>
    /// <returns>One route fact per destination class, in file, line, and address order.</returns>
    private static IReadOnlyList<RouteFact> ReadAndroid(
        FrontendConfig frontend, List<Finding> problems, HashSet<string> reported)
    {
        var found = new List<RouteFact>();

        foreach (var file in SourceTree.Files(frontend.Path, ".cs"))
        {
            SyntaxTree tree;

            try
            {
                tree = CSharpSyntaxTree.ParseText(
                    SourceTree.Read(file), new CSharpParseOptions(LanguageVersion.Preview), file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                AddProblem(problems, reported, file, $"Screen file '{file}' could not be read: {exception.Message}");

                continue;
            }

            var parseError = tree.GetDiagnostics()
                .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

            if (parseError is not null)
            {
                AddProblem(problems, reported, file, $"Screen file '{file}' could not be parsed: {parseError.GetMessage(CultureInfo.InvariantCulture)}");

                continue;
            }

            var relative = PathText.Relative(frontend.Path, file);
            var root = tree.GetRoot();

            foreach (var destination in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (destination.Parent is TypeDeclarationSyntax || !DerivesDestination(destination))
                {
                    continue;
                }

                var clients = ClientMembers(destination);
                var marks = ScreenMarks(destination);

                found.Add(new RouteFact(
                    relative,
                    tree.GetLineSpan(destination.Identifier.Span).StartLinePosition.Line + 1,
                    destination.Identifier.Text,
                    [.. marks.Select(mark => mark.Id)],
                    ClientCalls(tree, relative, destination, clients, lineShift: 0),
                    [.. marks.Select(mark => mark.Style)]));
            }
        }

        return [.. found
            .OrderBy(route => route.File, StringComparer.Ordinal)
            .ThenBy(route => route.Line)
            .ThenBy(route => route.Address, StringComparer.Ordinal)];
    }

    /// <summary>Whether the class directly derives <c>Activity</c> or <c>Fragment</c>, by simple base type name.</summary>
    /// <param name="destination">The destination class declaration whose base types are checked.</param>
    /// <returns>True when a base type is named Activity or Fragment; otherwise false.</returns>
    private static bool DerivesDestination(ClassDeclarationSyntax destination)
    {
        if (destination.BaseList is null)
        {
            return false;
        }

        foreach (var baseType in destination.BaseList.Types)
        {
            if (RightmostType(baseType.Type) is "Activity" or "Fragment")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>One screen mark with the style it was written in.</summary>
    private sealed record ScreenMark(string Id, MarkStyle Style);

    /// <summary>
    ///     The screen marks on the class declaration: the record identifier of every
    ///     <c>implements</c> mark that names a record, in source order, each with
    ///     the style it was written in.
    /// </summary>
    /// <param name="destination">The class declaration whose marks are read.</param>
    /// <returns>The screen marks in source order, each with the style it was written in.</returns>
    private static IReadOnlyList<ScreenMark> ScreenMarks(ClassDeclarationSyntax destination) =>
        [.. MarkReader.Read(destination)
            .Where(mark => mark.Verb == MarkVerb.Implements && mark.HasRecordId)
            .Select(mark => new ScreenMark(mark.RecordId, mark.Style))];

    /// <summary>
    ///     The members of the class that are typed like the generated client: fields, properties,
    ///     parameters, and locals declared with a <c>*Client</c> type or created with <c>new *Client</c>.
    /// </summary>
    /// <param name="scope">The syntax node searched or followed.</param>
    /// <returns>The names of the members typed or created as the generated client.</returns>
    private static HashSet<string> ClientMembers(SyntaxNode scope)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in scope.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (IsClientType(property.Type))
            {
                names.Add(property.Identifier.Text);
            }
        }

        foreach (var parameter in scope.DescendantNodes().OfType<ParameterSyntax>())
        {
            if (parameter.Type is not null
                && parameter.Identifier.Text.Length > 0
                && IsClientType(parameter.Type))
            {
                names.Add(parameter.Identifier.Text);
            }
        }

        foreach (var variable in scope.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            var declared = variable.Parent is VariableDeclarationSyntax declaration
                && IsClientType(declaration.Type);

            if (declared || CreatesClient(variable.Initializer?.Value))
            {
                names.Add(variable.Identifier.Text);
            }
        }

        return names;
    }

    /// <summary>
    ///     Every generated-client operation invoked under the node: an <c>*Async</c> method call on
    ///     a receiver typed or named like the generated client, reported without the suffix.
    /// </summary>
    /// <param name="tree">The syntax tree that maps nodes to source lines.</param>
    /// <param name="relative">The file path relative to the frontend root, recorded on each call.</param>
    /// <param name="scope">The syntax node searched or followed.</param>
    /// <param name="clients">The names of the members that hold the generated client.</param>
    /// <param name="lineShift">How far the block's lines sit below the file's first line.</param>
    /// <returns>One call fact per generated-client invocation under the node, without the Async suffix.</returns>
    private static IReadOnlyList<CallFact> ClientCalls(
        SyntaxTree tree, string relative, SyntaxNode scope, HashSet<string> clients, int lineShift)
    {
        var found = new List<CallFact>();

        foreach (var invocation in scope.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            string name;
            ExpressionSyntax? receiver;

            if (invocation.Expression is MemberAccessExpressionSyntax access)
            {
                name = access.Name.Identifier.Text;
                receiver = access.Expression;
            }
            else if (invocation.Expression is MemberBindingExpressionSyntax binding
                && invocation.Parent is ConditionalAccessExpressionSyntax conditional)
            {
                name = binding.Name.Identifier.Text;
                receiver = conditional.Expression;
            }
            else
            {
                continue;
            }

            if (name.Length <= "Async".Length || !name.EndsWith("Async", StringComparison.Ordinal))
            {
                continue;
            }

            var target = Rightmost(receiver);

            if (target.Length > 0
                && (target.EndsWith("Client", StringComparison.Ordinal) || clients.Contains(target)))
            {
                found.Add(new CallFact(
                    name[..^"Async".Length],
                    relative,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1 + lineShift));
            }
        }

        return [.. found
            .OrderBy(call => call.File, StringComparer.Ordinal)
            .ThenBy(call => call.Line)
            .ThenBy(call => call.Operation, StringComparer.Ordinal)];
    }

    /// <summary>The rightmost identifier of a receiver: <c>_client</c> of <c>this._client</c>.</summary>
    /// <param name="receiver">The receiver expression, or null for a bare call.</param>
    /// <returns>The rightmost identifier text, or an empty string when the receiver has none.</returns>
    private static string Rightmost(ExpressionSyntax? receiver) => receiver switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        MemberAccessExpressionSyntax access => access.Name.Identifier.Text,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
        _ => "",
    };

    /// <summary>Whether the type names a generated client, by its rightmost name ending with Client.</summary>
    /// <param name="type">The type syntax or declaration to inspect.</param>
    /// <returns>True when the rightmost type name ends with Client; otherwise false.</returns>
    private static bool IsClientType(TypeSyntax type) =>
        RightmostType(type).EndsWith("Client", StringComparison.Ordinal);

    /// <summary>The rightmost name of a type syntax, without generic arguments.</summary>
    /// <param name="type">The type syntax or declaration to inspect.</param>
    /// <returns>The rightmost type name text, without generic arguments.</returns>
    private static string RightmostType(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        GenericNameSyntax generic => generic.Identifier.Text,
        QualifiedNameSyntax qualified => RightmostType(qualified.Right),
        NullableTypeSyntax nullable => RightmostType(nullable.ElementType),
        ArrayTypeSyntax array => RightmostType(array.ElementType),
        PointerTypeSyntax pointer => RightmostType(pointer.ElementType),
        _ => "",
    };

    /// <summary>Whether the value creates a generated client type.</summary>
    /// <param name="value">The value expression that may create a client, or null when there is none.</param>
    /// <returns>True when the value creates a client type; otherwise false.</returns>
    private static bool CreatesClient(ExpressionSyntax? value) =>
        value is ObjectCreationExpressionSyntax creation && IsClientType(creation.Type);

}
