using System.Text.RegularExpressions;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     Reads the route entries of a Blazor or Android frontend: every Blazor component
///     carrying an <c>@page</c> template, and every Android destination activity or
///     fragment class, with the screen mark the entry carries and the generated-client
///     operations it calls. A Blazor mark is read from the paired code-behind class or
///     from the first mark in an <c>@code</c> block; an Android mark is read from the
///     destination class. A call is an <c>*Async</c> invocation on a member typed or
///     named like the generated client, reported without the <c>Async</c> suffix.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
static partial class DotnetScreenReader
{
    /// <summary>Reads every route entry of the frontend, in file, line and address order.</summary>
    /// <param name="frontend">The frontend to read.</param>
    /// <returns>One route fact per Blazor template or Android destination.</returns>
    public static IReadOnlyList<RouteFact> Read(FrontendConfig frontend)
    {
        if (frontend.Platform.Equals("android", StringComparison.Ordinal))
        {
            return ReadAndroid(frontend);
        }

        if (frontend.Platform.Equals("blazor", StringComparison.Ordinal))
        {
            return ReadBlazor(frontend);
        }

        return [];
    }

    private static IReadOnlyList<RouteFact> ReadBlazor(FrontendConfig frontend)
    {
        var found = new List<RouteFact>();

        foreach (var file in SourceTree.Files(frontend.Path, ".razor"))
        {
            string text;

            try
            {
                text = SourceTree.Read(file);
            }
            catch (IOException)
            {
                continue;
            }

            var relative = Relative(frontend, file);
            var blocks = CodeBlocks(text);
            var masked = Masked(text, blocks);
            var marks = ComponentMarks(file, text, blocks);
            var calls = ComponentCalls(relative, file, text, blocks);

            foreach (Match directive in PageDirective().Matches(masked))
            {
                // An escaped @@page writes a literal @page to the page instead of declaring a route.
                if (directive.Index > 0 && text[directive.Index - 1] == '@')
                {
                    continue;
                }

                found.Add(new RouteFact(
                    relative,
                    LineOf(text, directive.Index),
                    directive.Groups["template"].Value,
                    marks,
                    calls));
            }
        }

        return [.. found
            .OrderBy(route => route.File, StringComparer.Ordinal)
            .ThenBy(route => route.Line)
            .ThenBy(route => route.Address, StringComparer.Ordinal)];
    }

    private static IReadOnlyList<RouteFact> ReadAndroid(FrontendConfig frontend)
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
            catch (IOException)
            {
                continue;
            }

            var relative = Relative(frontend, file);
            var root = tree.GetRoot();

            foreach (var destination in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (destination.Parent is TypeDeclarationSyntax || !DerivesDestination(destination))
                {
                    continue;
                }

                var clients = ClientMembers(destination);

                found.Add(new RouteFact(
                    relative,
                    tree.GetLineSpan(destination.Identifier.Span).StartLinePosition.Line + 1,
                    destination.Identifier.Text,
                    ScreenMarks(destination),
                    ClientCalls(tree, relative, destination, clients, lineShift: 0)));
            }
        }

        return [.. found
            .OrderBy(route => route.File, StringComparer.Ordinal)
            .ThenBy(route => route.Line)
            .ThenBy(route => route.Address, StringComparer.Ordinal)];
    }

    /// <summary>Whether the class directly derives <c>Activity</c> or <c>Fragment</c>, by simple base type name.</summary>
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

    /// <summary>
    ///     The screen marks on the class declaration: the record identifier of every
    ///     <c>implements</c> mark that names a record, in source order.
    /// </summary>
    private static IReadOnlyList<string> ScreenMarks(ClassDeclarationSyntax destination) =>
        [.. MarkReader.Read(destination)
            .Where(mark => mark.Verb == MarkVerb.Implements && mark.HasRecordId)
            .Select(mark => mark.RecordId)];

    /// <summary>
    ///     The screen marks of a Blazor component: from the paired code-behind class when the
    ///     <c>.razor.cs</c> file exists, else from the first mark in an <c>@code</c> block.
    /// </summary>
    private static IReadOnlyList<string> ComponentMarks(string file, string text, IReadOnlyList<CodeBlock> blocks)
    {
        var behind = file + ".cs";

        if (File.Exists(behind))
        {
            SyntaxTree tree;

            try
            {
                tree = CSharpSyntaxTree.ParseText(
                    SourceTree.Read(behind), new CSharpParseOptions(LanguageVersion.Preview), behind);
            }
            catch (IOException)
            {
                return [];
            }

            if (ComponentClass(tree.GetRoot(), Path.GetFileNameWithoutExtension(file)) is { } component)
            {
                return ScreenMarks(component);
            }

            return [];
        }

        var found = new List<(int Index, string Id)>();

        foreach (var block in blocks)
        {
            var inner = text.Substring(block.InnerStart, block.InnerEnd - block.InnerStart);

            foreach (Match mark in ImplementsTag().Matches(inner))
            {
                if (mark.Groups["id"].Value.Trim() is { Length: > 0 } id)
                {
                    found.Add((block.InnerStart + mark.Index, id));
                }
            }

            foreach (Match mark in ImplementsAttribute().Matches(inner))
            {
                if (mark.Groups["id"].Value.Trim() is { Length: > 0 } id)
                {
                    found.Add((block.InnerStart + mark.Index, id));
                }
            }
        }

        return [.. found.OrderBy(mark => mark.Index).Select(mark => mark.Id)];
    }

    /// <summary>
    ///     The component class of a code-behind file: the top-level class matching the component
    ///     name, else the first top-level class, else the first class of the file.
    /// </summary>
    private static ClassDeclarationSyntax? ComponentClass(SyntaxNode root, string stem)
    {
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        return classes.FirstOrDefault(candidate =>
                candidate.Parent is not TypeDeclarationSyntax
                && candidate.Identifier.Text.Equals(stem, StringComparison.Ordinal))
            ?? classes.FirstOrDefault(candidate => candidate.Parent is not TypeDeclarationSyntax)
            ?? classes.FirstOrDefault();
    }

    /// <summary>
    ///     Every generated-client operation the component calls: from the code-behind component
    ///     class, from each <c>@code</c> block, and from the inline markup expressions.
    /// </summary>
    private static IReadOnlyList<CallFact> ComponentCalls(
        string relative, string file, string text, IReadOnlyList<CodeBlock> blocks)
    {
        var clients = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<CallFact>();
        ClassDeclarationSyntax? component = null;
        SyntaxTree? behindTree = null;
        var behind = file + ".cs";

        if (File.Exists(behind))
        {
            try
            {
                behindTree = CSharpSyntaxTree.ParseText(
                    SourceTree.Read(behind), new CSharpParseOptions(LanguageVersion.Preview), behind);
                component = ComponentClass(behindTree.GetRoot(), Path.GetFileNameWithoutExtension(file));
            }
            catch (IOException)
            {
                behindTree = null;
            }
        }

        var parsedBlocks = new List<(SyntaxTree Tree, CodeBlock Block)>();

        foreach (var block in blocks)
        {
            var inner = text.Substring(block.InnerStart, block.InnerEnd - block.InnerStart);
            var tree = CSharpSyntaxTree.ParseText(
                "partial class __Component {\n" + inner + "\n}\n",
                new CSharpParseOptions(LanguageVersion.Preview),
                file);
            parsedBlocks.Add((tree, block));

            if (tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault() is { } wrapped)
            {
                foreach (var name in ClientMembers(wrapped))
                {
                    clients.Add(name);
                }
            }
        }

        if (component is not null)
        {
            foreach (var name in ClientMembers(component))
            {
                clients.Add(name);
            }
        }

        if (component is not null && behindTree is not null)
        {
            found.AddRange(ClientCalls(behindTree, RelativeOf(behind, file, relative), component, clients, lineShift: 0));
        }

        foreach (var (tree, block) in parsedBlocks)
        {
            if (tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault() is { } wrapped)
            {
                // The wrapped inner text starts on line 2, so a wrapped line maps back
                // to the razor line that many lines below the inner text's first line.
                found.AddRange(ClientCalls(tree, relative, wrapped, clients, LineOf(text, block.InnerStart) - 2));
            }
        }

        var masked = Masked(text, blocks);

        foreach (Match call in InlineCall().Matches(masked))
        {
            var receiver = call.Groups["receiver"].Value;
            var method = call.Groups["method"].Value;

            if (method.Length > "Async".Length
                && (receiver.EndsWith("Client", StringComparison.Ordinal) || clients.Contains(receiver)))
            {
                found.Add(new CallFact(
                    method[..^"Async".Length], relative, LineOf(text, call.Index)));
            }
        }

        return [.. found
            .OrderBy(call => call.File, StringComparer.Ordinal)
            .ThenBy(call => call.Line)
            .ThenBy(call => call.Operation, StringComparer.Ordinal)];
    }

    /// <summary>The code-behind path relative to the frontend root, beside its component's path.</summary>
    private static string RelativeOf(string behind, string file, string relative)
    {
        var folder = relative.Contains('/')
            ? relative[..(relative.LastIndexOf('/') + 1)]
            : "";

        return folder + Path.GetFileName(behind);
    }

    /// <summary>
    ///     The members of the class that are typed like the generated client: fields, properties,
    ///     parameters, and locals declared with a <c>*Client</c> type or created with <c>new *Client</c>.
    /// </summary>
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
    private static string Rightmost(ExpressionSyntax? receiver) => receiver switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        MemberAccessExpressionSyntax access => access.Name.Identifier.Text,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
        _ => "",
    };

    private static bool IsClientType(TypeSyntax type) =>
        RightmostType(type).EndsWith("Client", StringComparison.Ordinal);

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

    private static bool CreatesClient(ExpressionSyntax? value) =>
        value is ObjectCreationExpressionSyntax creation && IsClientType(creation.Type);

    /// <summary>One <c>@code</c> or <c>@functions</c> block: its full and inner spans in the razor text.</summary>
    private sealed record CodeBlock(int Start, int End, int InnerStart, int InnerEnd);

    /// <summary>
    ///     Every <c>@code</c> or <c>@functions</c> block of the razor text, skipping blocks
    ///     inside razor comments. Braces inside strings, characters, and comments do not count.
    /// </summary>
    private static IReadOnlyList<CodeBlock> CodeBlocks(string text)
    {
        var comments = CommentSpans(text);
        var blocks = new List<CodeBlock>();
        var index = 0;

        while (index < text.Length)
        {
            var at = text.IndexOf("@code", index, StringComparison.Ordinal);
            var functions = text.IndexOf("@functions", index, StringComparison.Ordinal);
            var start = at < 0 ? functions : functions < 0 ? at : Math.Min(at, functions);

            if (start < 0)
            {
                return blocks;
            }

            var keyword = start == functions ? "@functions" : "@code";
            var brace = start + keyword.Length;

            while (brace < text.Length && char.IsWhiteSpace(text[brace]))
            {
                brace++;
            }

            if (brace >= text.Length
                || text[brace] != '{'
                || comments.Any(span => start >= span.Start && start < span.End))
            {
                index = start + keyword.Length;
                continue;
            }

            if (ClosingBrace(text, brace) is { } close)
            {
                blocks.Add(new CodeBlock(start, close + 1, brace + 1, close));
                index = close + 1;
            }
            else
            {
                index = brace + 1;
            }
        }

        return blocks;
    }

    /// <summary>The spans of the razor comments in the text, each from <c>@*</c> to <c>*@</c>.</summary>
    private static IReadOnlyList<(int Start, int End)> CommentSpans(string text)
    {
        var spans = new List<(int Start, int End)>();
        var index = 0;

        while (index < text.Length)
        {
            var start = text.IndexOf("@*", index, StringComparison.Ordinal);

            if (start < 0)
            {
                return spans;
            }

            var end = text.IndexOf("*@", start + 2, StringComparison.Ordinal);

            if (end < 0)
            {
                spans.Add((start, text.Length));

                return spans;
            }

            spans.Add((start, end + 2));
            index = end + 2;
        }

        return spans;
    }

    /// <summary>The offset of the brace closing the block opened at <c>open</c>, or null when unbalanced.</summary>
    private static int? ClosingBrace(string text, int open)
    {
        var depth = 0;

        for (var index = open; index < text.Length; index++)
        {
            var current = text[index];

            if (current == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                while (index < text.Length && text[index] != '\n')
                {
                    index++;
                }

                continue;
            }

            if (current == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                var end = text.IndexOf("*/", index + 2, StringComparison.Ordinal);

                if (end < 0)
                {
                    return null;
                }

                index = end + 1;
                continue;
            }

            if (current == '@' && index + 1 < text.Length && text[index + 1] == '*')
            {
                var end = text.IndexOf("*@", index + 2, StringComparison.Ordinal);

                if (end < 0)
                {
                    return null;
                }

                index = end + 1;
                continue;
            }

            if (current == '@' && index + 1 < text.Length && text[index + 1] == '"')
            {
                index = VerbatimEnd(text, index + 1) ?? text.Length;

                continue;
            }

            if (current == '"')
            {
                index = QuotedEnd(text, index) ?? text.Length;

                continue;
            }

            if (current == '\'')
            {
                index = QuotedEnd(text, index) ?? text.Length;

                continue;
            }

            if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return null;
    }

    private static int? VerbatimEnd(string text, int quote)
    {
        for (var index = quote + 1; index < text.Length; index++)
        {
            if (text[index] == '"')
            {
                if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                }
                else
                {
                    return index;
                }
            }
        }

        return null;
    }

    private static int? QuotedEnd(string text, int quote)
    {
        var delimiter = text[quote];

        for (var index = quote + 1; index < text.Length; index++)
        {
            if (text[index] == '\\')
            {
                index++;
            }
            else if (text[index] == delimiter)
            {
                return index;
            }
            else if (text[index] == '\n')
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    ///     The razor text with razor comments and <c>@code</c> blocks blanked to spaces, keeping
    ///     newlines so offsets still map to lines. Directives and markup stay readable.
    /// </summary>
    private static string Masked(string text, IReadOnlyList<CodeBlock> blocks)
    {
        var masked = text.ToCharArray();

        foreach (var (start, end) in CommentSpans(text))
        {
            Blank(masked, start, end);
        }

        foreach (var block in blocks)
        {
            Blank(masked, block.Start, block.End);
        }

        return new string(masked);
    }

    private static void Blank(char[] masked, int start, int end)
    {
        for (var index = start; index < end && index < masked.Length; index++)
        {
            if (masked[index] != '\n')
            {
                masked[index] = ' ';
            }
        }
    }

    private static string Relative(FrontendConfig frontend, string file) =>
        Path.GetRelativePath(frontend.Path, file).Replace(Path.DirectorySeparatorChar, '/');

    private static int LineOf(string text, int offset)
    {
        var line = 1;

        for (var index = 0; index < offset && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    [GeneratedRegex(@"@page\s+""(?<template>[^""]+)""")]
    private static partial Regex PageDirective();

    [GeneratedRegex(@"<implements\s*>(?<id>[^<]+)</implements\s*>")]
    private static partial Regex ImplementsTag();

    [GeneratedRegex(@"\[\s*(?:\w+\.)*Implements(?:Attribute)?\s*\(\s*""(?<id>[^""]+)""")]
    private static partial Regex ImplementsAttribute();

    [GeneratedRegex(@"(?<receiver>[A-Za-z_]\w*)\s*\??\.\s*(?<method>[A-Za-z_]\w*Async)\s*\(")]
    private static partial Regex InlineCall();
}
