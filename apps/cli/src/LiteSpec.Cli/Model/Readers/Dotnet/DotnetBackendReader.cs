using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     Reads the backend through the parser: every element through the shared mark and kind
///     readers, each record link from the mark the code carries, failure codes from Code
///     members, and comments as text.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
sealed class DotnetBackendReader : IBackendReader
{
    /// <summary>Reads every configured source root, parsing each file once.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The code model.</returns>
    public CodeModel Read(ConsumerConfig config) => Read(config, onParsed: null);

    /// <summary>
    ///     Reads every configured source root from one parse of each file: the
    ///     kind closure, the helper index and the parallel read all share the
    ///     same cached syntax trees.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="onParsed">Invoked with each file after it parses, or null for no callback.</param>
    /// <returns>The code model.</returns>
    public CodeModel Read(ConsumerConfig config, Action<string>? onParsed) =>
        ReadWithProblems(config, onParsed).Code;

    /// <summary>Reads the backend through the backend reader interface, with its problems.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The code model and the reader problems.</returns>
    BackendRead IBackendReader.Read(ConsumerConfig config) => ReadWithProblems(config, onParsed: null);

    /// <summary>Reads every configured source root, with one problem per file that could not be read or parsed.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The code model and the reader problems.</returns>
    public BackendRead ReadWithProblems(ConsumerConfig config) => ReadWithProblems(config, onParsed: null);

    /// <summary>Reads every configured source root, with its problems and a parse callback.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="onParsed">Invoked with each file after it parses, or null for no callback.</param>
    /// <returns>The code model and the reader problems.</returns>
    private BackendRead ReadWithProblems(ConsumerConfig config, Action<string>? onParsed)
    {
        var model = new CodeModel();

        var files = new List<string>();

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var (trees, problems) = SyntaxTreeCache.Parse(files, onParsed);
        var (context, qualified) = KindClosure.Of(config, trees);

        var helpers = HelperIndex.Build(config, trees, qualified);
        var aggregateRoots = new HashSet<string>(context.AggregateRoots, StringComparer.Ordinal);
        var holders = RepositoryImplementationReader.RepositoryHoldersOf(config, trees);

        var merged = new CodeModel();
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => new CodeModel(),
            (file, _, local) =>
            {
                if (trees.TryGetValue(file, out var tree))
                {
                    ReadFile(config, file, tree, local, context, qualified, helpers, aggregateRoots, holders);
                }

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    merged.Handlers.AddRange(local.Handlers);
                    merged.Reactions.AddRange(local.Reactions);
                    merged.Jobs.AddRange(local.Jobs);
                    merged.Events.AddRange(local.Events);
                    merged.Elements.AddRange(local.Elements);
                    merged.StrayMarks.AddRange(local.StrayMarks);
                    merged.Literals.AddRange(local.Literals);
                    merged.KeyLiterals.UnionWith(local.KeyLiterals);
                    merged.Comments.AddRange(local.Comments);
                    merged.Endpoints.AddRange(local.Endpoints);
                    merged.AggregateMethods.AddRange(local.AggregateMethods);
                    merged.AuditActions.AddRange(local.AuditActions);
                    merged.DomainTypes.AddRange(local.DomainTypes);
                    merged.RepositoryImplementations.AddRange(local.RepositoryImplementations);
                }
            });

        model.Handlers.AddRange(merged.Handlers
            .OrderBy(handler => handler.UseCaseId, StringComparer.Ordinal));
        model.Elements.AddRange(ElementKindCorrection.MergeTechnicalPartials(merged.Elements
            .OrderBy(element => element.File, StringComparer.Ordinal)
            .ThenBy(element => element.Line)
            .ThenBy(element => element.Class, StringComparer.Ordinal)
            .ThenBy(element => element.Member ?? "", StringComparer.Ordinal)
            .ToList()));
        model.StrayMarks.AddRange(merged.StrayMarks
            .OrderBy(stray => stray.File, StringComparer.Ordinal)
            .ThenBy(stray => stray.Line)
            .ThenBy(stray => stray.Class, StringComparer.Ordinal)
            .ThenBy(stray => stray.Member ?? "", StringComparer.Ordinal));
        model.Reactions.AddRange(merged.Reactions
            .OrderBy(reaction => reaction.File, StringComparer.Ordinal)
            .ThenBy(reaction => reaction.Line)
            .ThenBy(reaction => reaction.Class, StringComparer.Ordinal));
        model.Jobs.AddRange(merged.Jobs
            .OrderBy(job => job.File, StringComparer.Ordinal)
            .ThenBy(job => job.Line)
            .ThenBy(job => job.Class, StringComparer.Ordinal));
        model.Events.AddRange(merged.Events
            .OrderBy(domainEvent => domainEvent.File, StringComparer.Ordinal)
            .ThenBy(domainEvent => domainEvent.Line)
            .ThenBy(domainEvent => domainEvent.Class, StringComparer.Ordinal));
        model.Literals.AddRange(merged.Literals
            .OrderBy(literal => literal.File, StringComparer.Ordinal)
            .ThenBy(literal => literal.Line)
            .ThenBy(literal => literal.Value, StringComparer.Ordinal));
        model.KeyLiterals.UnionWith(merged.KeyLiterals);
        model.Comments.AddRange(merged.Comments
            .OrderBy(comment => comment.File, StringComparer.Ordinal)
            .ThenBy(comment => comment.Line));
        model.Endpoints.AddRange(merged.Endpoints
            .OrderBy(endpoint => endpoint.File, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Operation, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Route, StringComparer.Ordinal));
        model.AggregateMethods.AddRange(merged.AggregateMethods
            .OrderBy(method => method.File, StringComparer.Ordinal)
            .ThenBy(method => method.Line)
            .ThenBy(method => method.Aggregate, StringComparer.Ordinal)
            .ThenBy(method => method.Method, StringComparer.Ordinal));
        model.AuditActions.AddRange(merged.AuditActions
            .OrderBy(action => action.File, StringComparer.Ordinal)
            .ThenBy(action => action.Line)
            .ThenBy(action => action.Operation, StringComparer.Ordinal));
        model.DomainTypes.AddRange(DomainTypeReader.MergeDomainPartials(merged.DomainTypes
            .OrderBy(type => type.File, StringComparer.Ordinal)
            .ThenBy(type => type.Line)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .ToList()));
        model.RepositoryImplementations.AddRange(merged.RepositoryImplementations
            .OrderBy(entry => entry.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Line)
            .ThenBy(entry => entry.Interface, StringComparer.Ordinal)
            .ToList());

        problems.Sort((left, right) =>
        {
            var file = string.Compare(left.Path, right.Path, StringComparison.Ordinal);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });

        return new BackendRead(model, problems);
    }

    /// <summary>Reads one file into the model: its elements, aggregate methods, audit actions, domain types, repository implementations, literals, and comments.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="model">The code model the file adds to.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    private static void ReadFile(
        ConsumerConfig config,
        string file,
        SyntaxTree tree,
        CodeModel model,
        KindContext context,
        QualifiedKinds qualified,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders)
    {
        var root = tree.GetRoot();
        var classes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToList();
        var readElements = MarkFacts.Read(tree, context, SourceRoots.ProjectOf(config, file));
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .ToDictionary(type => type.Identifier.Span, type => type);
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .ToDictionary(method => method.Identifier.Span, method => method);
        var elements = ElementKindCorrection.CorrectElements(readElements, types, methods, qualified);

        ElementReader.ReadElements(file, tree, elements, types, methods, model, helpers, aggregateRoots, holders, qualified);
        AggregateMethodReader.ReadAggregateMethods(file, tree, elements, types, model);
        AuditActionReader.ReadAuditActions(file, tree, classes, model);
        DomainTypeReader.ReadDomainTypes(config, file, tree, model);
        RepositoryImplementationReader.ReadRepositoryImplementations(config, file, tree, model);

        foreach (var token in root.DescendantTokens())
        {
            if (RefusalLiterals.LiteralText(token) is { } literal)
            {
                // Every dotted literal is evidence that a recorded code is thrown, but only
                // a literal in a declaring position states a code of its own. Elsewhere a
                // dotted string is an audit name, a field key, a host or a file name.
                var declares = RefusalLiterals.Declares(token);

                if (RefusalLiterals.KeyLiteralShape.IsMatch(literal))
                {
                    model.KeyLiterals.Add(literal);
                }

                foreach (System.Text.RegularExpressions.Match match in Identifiers.RefusalCode().Matches(literal))
                {
                    model.Literals.Add(new CodeLiteral(
                        match.Value,
                        file,
                        tree.GetLineSpan(token.Span).StartLinePosition.Line + 1,
                        RefusalLiterals.EnclosingClass(token),
                        declares && match.Value == literal));
                }
            }

            foreach (var trivia in token.LeadingTrivia.Concat(token.TrailingTrivia))
            {
                if (trivia.Kind() is SyntaxKind.SingleLineCommentTrivia
                    or SyntaxKind.MultiLineCommentTrivia
                    or SyntaxKind.SingleLineDocumentationCommentTrivia
                    or SyntaxKind.MultiLineDocumentationCommentTrivia)
                {
                    var span = tree.GetLineSpan(trivia.Span);

                    model.Comments.Add(new CodeComment(
                        trivia.ToString(), file, span.StartLinePosition.Line + 1));
                }
            }
        }
    }
}
