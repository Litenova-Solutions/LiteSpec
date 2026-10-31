using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Reads one handler, one event handler, or one job host, with its shape.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class HandlerReader
{
    /// <summary>Reads one handler: its use case from its one use-case mark, its shape from its class.</summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="declared">The handler declaration.</param>
    /// <param name="marks">The marks the handler carries.</param>
    /// <param name="model">The code model the handler adds to.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    public static void ReadHandler(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        QualifiedKinds qualified)
    {
        var useCase = ElementReader.SingleMark(marks, "use-case/") ?? "";

        Identifiers.TrySplit(useCase, out _, out var home, out var name);

        var message = ClassDetailsReader.MessageTypeOf(declared);
        var qualifiedName = DotnetNames.QualifiedNameOf(declared);
        var handlesQuery = ImplementsFace(declared, "IQueryHandler") || qualified.QueryHandlers.Contains(qualifiedName);
        var handlesCommand = ImplementsFace(declared, "ICommandHandler") || qualified.CommandHandlers.Contains(qualifiedName);
        var kind = handlesQuery && !handlesCommand
            ? "query"
            : "command";
        var details = ClassDetailsReader.HandlerDetailsOf(declared, tree, file, helpers, aggregateRoots, holders, kind == "query", qualified);

        model.Handlers.Add(new HandlerInfo(
            useCase,
            home ?? "",
            name,
            OperationOf(message),
            kind,
            file,
            details.Class,
            details.Line,
            details.Repositories,
            details.Refusals,
            details.Thrown,
            details.Dispatches,
            details.MessageType,
            details.DependencyCount,
            details.AggregateCalls,
            details.Collaborators,
            details.LoopReads,
            details.MemoryFilters)
        {
            FactoryCalls = details.FactoryCalls,
        });
    }

    /// <summary>Reads one event handler: its reaction mark, and a use-case mark when one sits beside it.</summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="declared">The event handler declaration.</param>
    /// <param name="marks">The marks the event handler carries.</param>
    /// <param name="model">The code model the reaction adds to.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    public static void ReadReaction(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> holders,
        QualifiedKinds qualified)
    {
        model.Reactions.Add(new ReactionInfo(
            declared.Identifier.ValueText,
            file,
            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
            ElementReader.SingleMark(marks, "use-case/"),
            ElementReader.SingleMark(marks, "reaction/"),
            ClassDetailsReader.RepositoriesOf(declared, tree, file),
            ClassDetailsReader.RefusalsOf(declared, tree, file),
            ClassDetailsReader.ThrownOf(declared, tree, file),
            DispatchReader.DispatchesOf(declared, tree, file, helpers, qualified),
            ClassDetailsReader.DependencyCountOf(declared),
            ClassActivityReader.CollaboratorsOf(declared, tree, file, holders),
            ClassActivityReader.LoopReadsOf(declared, tree, file)));
    }

    /// <summary>Reads one job host: its job mark, and a use-case mark when one sits beside it.</summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="declared">The job host declaration.</param>
    /// <param name="marks">The marks the job host carries.</param>
    /// <param name="model">The code model the job adds to.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    public static void ReadJob(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> holders,
        QualifiedKinds qualified)
    {
        model.Jobs.Add(new JobInfo(
            declared.Identifier.ValueText,
            file,
            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
            ElementReader.SingleMark(marks, "use-case/"),
            ElementReader.SingleMark(marks, "job/"),
            ClassDetailsReader.RepositoriesOf(declared, tree, file),
            ClassDetailsReader.RefusalsOf(declared, tree, file),
            ClassDetailsReader.ThrownOf(declared, tree, file),
            DispatchReader.DispatchesOf(declared, tree, file, helpers, qualified),
            ClassDetailsReader.DependencyCountOf(declared),
            ClassActivityReader.CollaboratorsOf(declared, tree, file, holders),
            ClassActivityReader.LoopReadsOf(declared, tree, file)));
    }

    /// <summary>The message stem: the message type name without its Command or Query suffix.</summary>
    /// <param name="message">The message type name.</param>
    /// <returns>The message stem.</returns>
    public static string OperationOf(string message)
    {
        foreach (var suffix in new[] { "Command", "Query" })
        {
            if (message.EndsWith(suffix, StringComparison.Ordinal) && message.Length > suffix.Length)
            {
                return message[..^suffix.Length];
            }
        }

        return message;
    }

    /// <summary>Whether the type lists the interface among its bases, generic or not.</summary>
    /// <param name="declared">The type declaration.</param>
    /// <param name="face">The interface name.</param>
    /// <returns>True when the type lists the interface among its bases.</returns>
    public static bool ImplementsFace(TypeDeclarationSyntax declared, string face) =>
        declared.BaseList is not null
        && declared.BaseList.Types
            .Select(type => DotnetNames.BaseName(type.Type))
            .Any(baseName => baseName == face || baseName.StartsWith(face + "<", StringComparison.Ordinal));
}
