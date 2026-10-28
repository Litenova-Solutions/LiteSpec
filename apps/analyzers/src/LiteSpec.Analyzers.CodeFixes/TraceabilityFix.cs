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
internal sealed class TraceabilityFix
{
    private TraceabilityFix(
        RecordStore store,
        MemberDeclarationSyntax member,
        ISymbol? symbol,
        CodeKind? kind,
        string folder)
    {
        Store = store;
        Member = member;
        Symbol = symbol;
        Kind = kind;
        Folder = folder;
    }

    /// <summary>The consumer's records.</summary>
    public RecordStore Store { get; }

    /// <summary>The member declaration the diagnostic sits on or in.</summary>
    public MemberDeclarationSyntax Member { get; }

    /// <summary>The declared symbol, or null when the tree declares none.</summary>
    public ISymbol? Symbol { get; }

    /// <summary>The LiteSpec kind, or null when the member is of no known kind.</summary>
    public CodeKind? Kind { get; }

    /// <summary>The containing folder name of the source file, or empty.</summary>
    public string Folder { get; }

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

        return new TraceabilityFix(store, member, symbol, kind, FolderOf(document.FilePath));
    }

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
    public static MemberDeclarationSyntax? FindMember(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var node = root.FindToken(span.Start).Parent;

        while (node is not null && node is not MemberDeclarationSyntax)
        {
            node = node.Parent;
        }

        return node as MemberDeclarationSyntax;
    }

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

    private static string FolderOf(string? filePath)
    {
        if (filePath is null || filePath.Length == 0)
        {
            return string.Empty;
        }

        var slash = Math.Max(filePath.LastIndexOf('/'), filePath.LastIndexOf('\\'));

        if (slash <= 0)
        {
            return string.Empty;
        }

        var directory = filePath.Substring(0, slash);
        var previous = Math.Max(directory.LastIndexOf('/'), directory.LastIndexOf('\\'));

        return previous < 0 ? directory : directory.Substring(previous + 1);
    }
}
