using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteSpec.Analyzers.Records;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     What a traceability fix needs for one diagnostic: the consumer's records,
///     the marked member, its symbol and its kind.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class TraceabilityFix
{
    /// <summary>Initializes a new instance of the <see cref="TraceabilityFix"/> class.</summary>
    /// <param name="store">The record store that holds the project settings and the record index.</param>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <param name="folders">The nearest folder names of the source file, containing first.</param>
    private TraceabilityFix(
        RecordStore store,
        MemberDeclarationSyntax member,
        ISymbol? symbol,
        CodeKind? kind,
        IReadOnlyList<string> folders)
    {
        Store = store;
        Member = member;
        Symbol = symbol;
        Kind = kind;
        Folders = folders;
    }

    /// <summary>Gets the consumer's records.</summary>
    public RecordStore Store { get; }

    /// <summary>Gets the member declaration the diagnostic sits on or in.</summary>
    public MemberDeclarationSyntax Member { get; }

    /// <summary>Gets the declared symbol, or null when the tree declares none.</summary>
    public ISymbol? Symbol { get; }

    /// <summary>Gets the LiteSpec kind, or null when the member is of no known kind.</summary>
    public CodeKind? Kind { get; }

    /// <summary>Gets the three nearest folder names of the source file, containing first.</summary>
    public IReadOnlyList<string> Folders { get; }

    /// <summary>
    ///     Reads the fix context for one diagnostic: the records from the
    ///     solution's additional documents, the member under the diagnostic span,
    ///     and its kind. Returns null when the records stay silent or no member
    ///     holds the span.
    /// </summary>
    /// <param name="document">The document holding the diagnostic.</param>
    /// <param name="span">The diagnostic span.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The fix context, or null.</returns>
    public static async Task<TraceabilityFix?> ReadAsync(
        Document document, Microsoft.CodeAnalysis.Text.TextSpan span, CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(document.Project, cancellationToken).ConfigureAwait(false);

        if (store is null)
        {
            return null;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || FindMember(root, span) is not MemberDeclarationSyntax member)
        {
            return null;
        }

        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = model?.GetDeclaredSymbol(member, cancellationToken);
        var kind = await KindOfAsync(document, member, symbol, cancellationToken).ConfigureAwait(false);

        return new TraceabilityFix(store, member, symbol, kind, FoldersOf(document.FilePath));
    }

    /// <summary>Returns the store.</summary>
    /// <param name="project">The project that owns the document.</param>
    /// <param name="cancellationToken">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the store.</returns>
    private static async Task<RecordStore?> LoadStoreAsync(Project project, CancellationToken cancellationToken)
    {
        var files = new List<RecordFile>();

        foreach (var additional in project.AdditionalDocuments)
        {
            var text = await additional.GetTextAsync(cancellationToken).ConfigureAwait(false);

            if (text is not null)
            {
                files.Add(new RecordFile(additional.FilePath ?? additional.Name, text.ToString()));
            }
        }

        return RecordStore.Load(files);
    }

    /// <summary>The member declaration holding the span, ascending from the token at its start.</summary>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <returns>The member, or null when none applies.</returns>
    public static MemberDeclarationSyntax? FindMember(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var node = root.FindToken(span.Start).Parent;

        while (node is not null && node is not MemberDeclarationSyntax)
        {
            node = node.Parent;
        }

        return node as MemberDeclarationSyntax;
    }

    /// <summary>Determines the LiteSpec kind of the member from its symbol or its declaration.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="cancellationToken">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the kind, or null when the member is of no known kind.</returns>
    private static async Task<CodeKind?> KindOfAsync(
        Document document, MemberDeclarationSyntax member, ISymbol? symbol, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var context = compilation is null ? KindContext.Empty : PortContext(compilation);
        var project = document.Project.AssemblyName;

        if (symbol is INamedTypeSymbol type)
        {
            return KindReader.KindOf(type, context, project);
        }

        if (symbol is IMethodSymbol method)
        {
            return KindReader.KindOf(method, context);
        }

        if (member is TypeDeclarationSyntax typeDeclaration)
        {
            return KindReader.KindOf(typeDeclaration, context, project);
        }

        if (member is MethodDeclarationSyntax methodDeclaration)
        {
            return KindReader.KindOf(methodDeclaration, context);
        }

        return null;
    }

    /// <summary>
    ///     The kind context with the marked ports: every interface a source type
    ///     implements that carries an external-system mark, in source or in metadata.
    /// </summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The context.</returns>
    private static KindContext PortContext(Compilation compilation)
    {
        var ports = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(compilation.Assembly.GlobalNamespace);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (var nested in current.GetMembers())
            {
                if (nested is INamespaceSymbol space)
                {
                    stack.Push(space);
                }
                else if (nested is INamedTypeSymbol type)
                {
                    stack.Push(type);

                    if (!IsSource(type))
                    {
                        continue;
                    }

                    foreach (var contract in type.AllInterfaces)
                    {
                        if (IsMarkedPort(contract))
                        {
                            ports.Add(contract.Name);
                        }
                    }
                }
            }
        }

        return new KindContext(Array.Empty<string>(), Array.Empty<string>(), ports);
    }

    /// <summary>Returns true when the type is source; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is source; otherwise false.</returns>
    private static bool IsSource(INamedTypeSymbol type)
    {
        foreach (var location in type.Locations)
        {
            if (location.IsInSource)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the contract is marked port; otherwise false.</summary>
    /// <param name="contract">The interface contract under inspection.</param>
    /// <returns>True when the contract is marked port; otherwise false.</returns>
    private static bool IsMarkedPort(INamedTypeSymbol contract)
    {
        foreach (var attribute in contract.GetAttributes())
        {
            if (MarkVerbNames.TryParseAttributeName(attribute.AttributeClass?.Name, out _)
                && attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is string value
                && value.Trim().StartsWith("external-system/", StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var reference in contract.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MemberDeclarationSyntax member)
            {
                foreach (var mark in MarkReader.Read(member))
                {
                    if (mark.RecordId.StartsWith("external-system/", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Returns the folders of the file path.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The folders of the file path.</returns>
    private static List<string> FoldersOf(string? filePath)
    {
        var folders = new List<string>();

        if (filePath is null || filePath.Length == 0)
        {
            return folders;
        }

        var directory = filePath.Replace('\\', '/');
        var slash = directory.LastIndexOf('/');

        if (slash < 0)
        {
            return folders;
        }

        directory = directory.Substring(0, slash);
        var parts = directory.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        for (var index = parts.Length - 1; index >= 0 && folders.Count < 3; index--)
        {
            if (parts[index].Length > 0 && !parts[index].Contains(":"))
            {
                folders.Add(parts[index]);
            }
        }

        return folders;
    }
}
