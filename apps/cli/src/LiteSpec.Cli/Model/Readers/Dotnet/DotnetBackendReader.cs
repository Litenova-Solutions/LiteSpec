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
    /// <summary>The interfaces whose mark names an external system: the only ports a call counts on.</summary>
    private static HashSet<string> externalPorts = new(StringComparer.Ordinal);

    /// <summary>A kebab-case literal, the shape of a key a composed refusal code is built from.</summary>
    private static readonly System.Text.RegularExpressions.Regex KeyLiteralShape = new(
        "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    ///     Collects the cross-file names kind recognition needs: the aggregate roots,
    ///     the domain exceptions, the marked ports, the technical classes, and the
    ///     handler, event handler, job host and domain event contracts. Roots, exceptions
    ///     and contracts seed from their direct bases and close over inheritance:
    ///     any type whose declared base list names a type already known to carry
    ///     the contract carries it too. Ports are the interfaces carrying an
    ///     external-system mark. Technical classes are the top-level classes outside
    ///     Domain that declare a method, so a partial part without a method reads
    ///     through its name. Collected once per read, from the parsed file cache.
    /// </summary>
    private static KindContext KindsOf(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees)
    {
        var backends = new[]
        {
            config.ApplicationRoot,
            config.ApplicationRoot + ".Abstractions",
            config.DomainRoot,
            config.WebApiRoot,
            config.InfrastructureRoot,
            config.WorkerRoot,
        };
        var bases = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var ports = new HashSet<string>(StringComparer.Ordinal);
        var technical = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in backends)
        {
            var portRoot = root == config.ApplicationRoot
                || root == config.ApplicationRoot + ".Abstractions"
                || root == config.InfrastructureRoot;
            var technicalRoot = root != config.DomainRoot;

            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                if (!trees.TryGetValue(file, out var tree))
                {
                    continue;
                }

                foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    var name = declared.Identifier.ValueText;

                    if (!bases.TryGetValue(name, out var known))
                    {
                        known = [];
                        bases[name] = known;
                    }

                    if (declared.BaseList is not null)
                    {
                        foreach (var baseType in declared.BaseList.Types)
                        {
                            var face = SimpleName(KindOf(BaseName(baseType.Type)));

                            if (face.Length > 0 && !known.Contains(face, StringComparer.Ordinal))
                            {
                                known.Add(face);
                            }
                        }
                    }

                    if (portRoot
                        && declared is InterfaceDeclarationSyntax faceType
                        && MarkReader.Read(faceType).Any(mark =>
                            mark.RecordId.StartsWith("external-system/", StringComparison.Ordinal)))
                    {
                        ports.Add(name);
                    }

                    if (technicalRoot && IsTechnicalCandidate(file, declared))
                    {
                        technical.Add(name);
                    }
                }
            }
        }

        return new KindContext(
            Fixpoint(bases, parents => parents.Any(parent =>
                parent.StartsWith("AggregateRoot", StringComparison.Ordinal))),
            Fixpoint(bases, parents => parents.Contains("DomainException")),
            ports,
            Fixpoint(bases, parents => parents.Contains("ICommandHandler")),
            Fixpoint(bases, parents => parents.Contains("IQueryHandler")),
            Fixpoint(bases, parents => parents.Contains("IEventHandler") || parents.Contains("IDomainEventReaction")),
            Fixpoint(bases, parents => parents.Contains("BackgroundService") || parents.Contains("IHostedService")),
            Fixpoint(bases, parents => parents.Contains("IDomainEvent")),
            technical);
    }

    /// <summary>
    ///     Whether the declaration names a technical class for the kind context: a
    ///     top-level class declaring an ordinary method, outside generated files,
    ///     generated-code attributes, migrations, and model snapshots.
    /// </summary>
    private static bool IsTechnicalCandidate(string file, TypeDeclarationSyntax declared)
    {
        if (declared is not ClassDeclarationSyntax
            || declared.Parent is TypeDeclarationSyntax
            || !declared.Members.OfType<MethodDeclarationSyntax>().Any())
        {
            return false;
        }

        return !IsGeneratedFileName(file) && !HasGeneratedCode(declared) && !IsMigrationBase(declared);
    }

    /// <summary>Whether the file name is one the compiler treats as generated.</summary>
    private static bool IsGeneratedFileName(string file)
    {
        var name = file.Replace('\\', '/');
        var slash = name.LastIndexOf('/');
        var shortName = slash < 0 ? name : name.Substring(slash + 1);

        return shortName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the declaration carries a <c>GeneratedCode</c> attribute.</summary>
    private static bool HasGeneratedCode(TypeDeclarationSyntax declared)
    {
        foreach (var list in declared.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                if (AttributeName(attribute.Name) == "GeneratedCode")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>The simple attribute name without qualification or the <c>Attribute</c> suffix.</summary>
    private static string AttributeName(NameSyntax name)
    {
        var simple = name switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            _ => name.ToString(),
        };

        const string suffix = "Attribute";

        return simple.Length > suffix.Length && simple.EndsWith(suffix, StringComparison.Ordinal)
            ? simple[..^suffix.Length]
            : simple;
    }

    /// <summary>Whether the declaration derives from a type named <c>Migration</c> or <c>ModelSnapshot</c>.</summary>
    private static bool IsMigrationBase(TypeDeclarationSyntax declared)
    {
        if (declared.BaseList is null)
        {
            return false;
        }

        foreach (var baseType in declared.BaseList.Types)
        {
            if (SimpleName(KindOf(BaseName(baseType.Type))) is "Migration" or "ModelSnapshot")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Closes the seeded names over direct inheritance: a type deriving a known name is known.</summary>
    private static HashSet<string> Fixpoint(
        Dictionary<string, List<string>> bases,
        Func<List<string>, bool> seeds)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (name, parents) in bases)
        {
            if (seeds(parents))
            {
                known.Add(name);
            }
        }

        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var (name, parents) in bases)
            {
                if (!known.Contains(name) && parents.Any(known.Contains))
                {
                    known.Add(name);
                    changed = true;
                }
            }
        }

        return known;
    }

    /// <summary>The short project name holding the file, or null when it sits under no backend root.</summary>
    private static string? ProjectOf(ConsumerConfig config, string file)
    {
        var abstractions = config.ApplicationRoot + ".Abstractions";

        if (IsUnder(abstractions, file))
        {
            return FolderName(abstractions);
        }

        if (IsUnder(config.ApplicationRoot, file))
        {
            return FolderName(config.ApplicationRoot);
        }

        if (IsUnder(config.DomainRoot, file))
        {
            return FolderName(config.DomainRoot);
        }

        if (IsUnder(config.WebApiRoot, file))
        {
            return FolderName(config.WebApiRoot);
        }

        if (IsUnder(config.InfrastructureRoot, file))
        {
            return FolderName(config.InfrastructureRoot);
        }

        if (IsUnder(config.WorkerRoot, file))
        {
            return FolderName(config.WorkerRoot);
        }

        return null;
    }

    /// <summary>The folder name of a source root: the short project name.</summary>
    private static string FolderName(string root) =>
        Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <summary>Reads every configured source root, parsing each file once.</summary>
    public CodeModel Read(ConsumerConfig config) => Read(config, onParsed: null);

    /// <summary>
    ///     Reads every configured source root from one parse of each file: the
    ///     kind closure, the helper index and the parallel read all share the
    ///     same cached syntax trees.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="onParsed">Invoked with each file after it parses, or null for no callback.</param>
    public CodeModel Read(ConsumerConfig config, Action<string>? onParsed)
    {
        var model = new CodeModel();

        var files = new List<string>();

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var trees = ParseTrees(files, onParsed);
        var context = KindsOf(config, trees);

        externalPorts = new HashSet<string>(context.MarkedPorts, StringComparer.Ordinal);
        var helpers = IndexHelpers(config, trees);
        var aggregateRoots = new HashSet<string>(context.AggregateRoots, StringComparer.Ordinal);
        var holders = RepositoryHoldersOf(config, trees);

        var merged = new CodeModel();
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => new CodeModel(),
            (file, _, local) =>
            {
                if (trees.TryGetValue(file, out var tree))
                {
                    ReadFile(config, file, tree, local, context, helpers, aggregateRoots, holders);
                }

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    merged.Handlers.AddRange(local.Handlers);
                    merged.Automations.AddRange(local.Automations);
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
        model.Elements.AddRange(MergeTechnicalPartials(merged.Elements
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
        model.Automations.AddRange(merged.Automations
            .OrderBy(automation => automation.File, StringComparer.Ordinal)
            .ThenBy(automation => automation.Line)
            .ThenBy(automation => automation.Class, StringComparer.Ordinal));
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
        model.DomainTypes.AddRange(MergeDomainPartials(merged.DomainTypes
            .OrderBy(type => type.File, StringComparer.Ordinal)
            .ThenBy(type => type.Line)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .ToList()));
        model.RepositoryImplementations.AddRange(merged.RepositoryImplementations
            .OrderBy(entry => entry.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Line)
            .ThenBy(entry => entry.Interface, StringComparer.Ordinal)
            .ToList());

        return model;
    }

    /// <summary>
    ///     Parses every file once, in parallel, into a cache from file path to
    ///     syntax tree. A file that cannot be read is left out.
    /// </summary>
    private static IReadOnlyDictionary<string, SyntaxTree> ParseTrees(IReadOnlyList<string> files, Action<string>? onParsed)
    {
        var trees = new Dictionary<string, SyntaxTree>(files.Count, StringComparer.Ordinal);
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            file =>
            {
                SyntaxTree tree;

                try
                {
                    tree = CSharpSyntaxTree.ParseText(SourceTree.Read(file), new CSharpParseOptions(LanguageVersion.Preview), file);
                }
                catch (IOException)
                {
                    return;
                }

                lock (gate)
                {
                    trees[file] = tree;
                }

                onParsed?.Invoke(file);
            });

        return trees;
    }

    private static void ReadFile(
        ConsumerConfig config,
        string file,
        SyntaxTree tree,
        CodeModel model,
        KindContext context,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders)
    {
        var root = tree.GetRoot();
        var classes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToList();
        var elements = MarkFacts.Read(tree, context, ProjectOf(config, file));
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .ToDictionary(type => type.Identifier.Span, type => type);
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .ToDictionary(method => method.Identifier.Span, method => method);

        ReadElements(file, tree, elements, types, methods, model, helpers, aggregateRoots, holders, context);
        ReadAggregateMethods(file, tree, elements, types, model);
        ReadAuditActions(file, tree, classes, model);
        ReadDomainTypes(config, file, tree, model);
        ReadRepositoryImplementations(config, file, tree, model);

        foreach (var token in root.DescendantTokens())
        {
            if (LiteralText(token) is { } literal)
            {
                // Every dotted literal is evidence that a recorded code is thrown, but only
                // a literal in a declaring position states a code of its own. Elsewhere a
                // dotted string is an audit name, a field key, a host or a file name.
                var declares = Declares(token);

                if (KeyLiteralShape.IsMatch(literal))
                {
                    model.KeyLiterals.Add(literal);
                }

                foreach (System.Text.RegularExpressions.Match match in Identifiers.RefusalCode().Matches(literal))
                {
                    model.Literals.Add(new CodeLiteral(
                        match.Value,
                        file,
                        tree.GetLineSpan(token.Span).StartLinePosition.Line + 1,
                        EnclosingClass(token),
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

    /// <summary>
    ///     Merges the partial parts of one technical class into one element: parts
    ///     count as one class and a mark on any part counts. The merged element
    ///     keeps the earliest part's file and line and carries every part's marks.
    /// </summary>
    private static List<MarkedElement> MergeTechnicalPartials(List<MarkedElement> elements)
    {
        if (elements.All(element => element.Kind != CodeKind.Technical))
        {
            return elements;
        }

        var merged = new List<MarkedElement>();
        var parts = new Dictionary<string, List<MarkedElement>>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            if (element.Kind != CodeKind.Technical)
            {
                merged.Add(element);
                continue;
            }

            if (!parts.TryGetValue(element.Class, out var group))
            {
                group = [];
                parts[element.Class] = group;
            }

            group.Add(element);
        }

        foreach (var group in parts.Values)
        {
            var first = group
                .OrderBy(part => part.File, StringComparer.Ordinal)
                .ThenBy(part => part.Line)
                .First();
            var marks = group
                .SelectMany(part => part.Marks)
                .OrderBy(mark => mark.File, StringComparer.Ordinal)
                .ThenBy(mark => mark.Line)
                .ToList();

            merged.Add(first with { Marks = marks });
        }

        return merged
            .OrderBy(element => element.File, StringComparer.Ordinal)
            .ThenBy(element => element.Line)
            .ThenBy(element => element.Class, StringComparer.Ordinal)
            .ThenBy(element => element.Member ?? "", StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    ///     Reads every top-level Domain type of one file into the model: its name, namespace,
    ///     file, line, and whether it carries a placing mark. Only files under the Domain root
    ///     are read. Nested types, generated types, enumerations and delegates are left out:
    ///     enumerations and delegates are not type declarations, so the type walk skips them.
    /// </summary>
    /// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
    private static void ReadDomainTypes(ConsumerConfig config, string file, SyntaxTree tree, CodeModel model)
    {
        if (!IsUnder(config.DomainRoot, file))
        {
            return;
        }

        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (declared.Parent is TypeDeclarationSyntax)
            {
                continue;
            }

            if (IsGeneratedFileName(file) || HasGeneratedCode(declared))
            {
                continue;
            }

            var placed = MarkReader.Read(declared).Any(mark => mark.Verb != MarkVerb.Follows);

            model.DomainTypes.Add(new DomainType(
                declared.Identifier.ValueText,
                NamespaceOf(declared),
                file,
                tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1,
                placed));
        }
    }

    /// <summary>
    ///     Reads every Infrastructure class implementing a domain repository
    ///     interface into the model, through its declared base list: the class
    ///     name, the repository interface it names, and its location. Only files
    ///     under the Infrastructure root are read, and nested types are left out.
    /// </summary>
    private static void ReadRepositoryImplementations(ConsumerConfig config, string file, SyntaxTree tree, CodeModel model)
    {
        if (!IsUnder(config.InfrastructureRoot, file))
        {
            return;
        }

        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (declared.Parent is TypeDeclarationSyntax || declared is InterfaceDeclarationSyntax || declared.BaseList is null)
            {
                continue;
            }

            foreach (var baseType in declared.BaseList.Types)
            {
                var face = SimpleName(KindOf(BaseName(baseType.Type)));

                if (IsRepositoryInterface(face))
                {
                    model.RepositoryImplementations.Add(new CodeRepositoryImplementation(
                        face,
                        declared.Identifier.ValueText,
                        file,
                        tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1));
                }
            }
        }
    }

    /// <summary>Whether the name is a domain repository interface: an <c>I</c>-prefixed name ending in <c>Repository</c>.</summary>
    private static bool IsRepositoryInterface(string face) =>
        face.Length > "IRepository".Length
        && face.StartsWith("I", StringComparison.Ordinal)
        && char.IsUpper(face[1])
        && face.EndsWith("Repository", StringComparison.Ordinal);

    /// <summary>The namespace holding the declaration, or empty in the global namespace.</summary>
    private static string NamespaceOf(TypeDeclarationSyntax declared)
    {
        for (var node = declared.Parent; node is not null; node = node.Parent)
        {
            if (node is BaseNamespaceDeclarationSyntax space)
            {
                return space.Name.ToString();
            }
        }

        return "";
    }

    /// <summary>
    ///     Merges the partial parts of one Domain type into one entry: parts count as one type
    ///     and a mark on any part counts. The merged entry keeps the first file in ordinal path
    ///     order. Parts group by namespace and type name.
    /// </summary>
    private static List<DomainType> MergeDomainPartials(List<DomainType> types)
    {
        var merged = new List<DomainType>();
        var parts = new Dictionary<string, List<DomainType>>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            var key = type.Namespace + "\0" + type.Name;

            if (!parts.TryGetValue(key, out var group))
            {
                group = [];
                parts[key] = group;
            }

            group.Add(type);
        }

        foreach (var group in parts.Values)
        {
            var first = group
                .OrderBy(part => part.File, StringComparer.Ordinal)
                .ThenBy(part => part.Line)
                .First();

            merged.Add(first with { HasMark = group.Any(part => part.HasMark) });
        }

        return merged
            .OrderBy(type => type.File, StringComparer.Ordinal)
            .ThenBy(type => type.Line)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    ///     Reads every recognized element of one file into the model: the element itself
    ///     with its marks, and the handler, automation, event or endpoint row its kind owns.
    ///     Every record link comes from a mark the element carries.
    /// </summary>
    private static void ReadElements(
        string file,
        SyntaxTree tree,
        IReadOnlyList<MarkableElement> elements,
        IReadOnlyDictionary<TextSpan, TypeDeclarationSyntax> types,
        IReadOnlyDictionary<TextSpan, MethodDeclarationSyntax> methods,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        KindContext context)
    {
        foreach (var element in elements)
        {
            if (element.Kind is not CodeKind kind)
            {
                ReadStrayMark(file, element, methods, model);
                continue;
            }

            if (kind is CodeKind.Test or CodeKind.FrontendModule)
            {
                continue;
            }

            var marks = element.Marks
                .Select(mark => new CodeMark(mark.RecordId, mark.Style, mark.Verb, file, mark.Line))
                .ToList();

            if (element.IsType)
            {
                if (!types.TryGetValue(element.Span, out var declared))
                {
                    continue;
                }

                model.Elements.Add(new MarkedElement(
                    kind, element.Name, null, file, element.Line, marks, false,
                    RuntimeValueOf(declared, kind)));

                switch (kind)
                {
                    case CodeKind.Handler:
                        ReadHandler(file, tree, declared, marks, model, helpers, aggregateRoots, holders, context);
                        break;
                    case CodeKind.EventHandler:
                    case CodeKind.Job:
                        ReadAutomation(file, tree, declared, marks, model, helpers, holders);
                        break;
                    case CodeKind.DomainEvent:
                        model.Events.Add(new DomainEventInfo(
                            element.Name,
                            file,
                            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
                            SingleMark(marks, "event/")));
                        break;
                    case CodeKind.Endpoint:
                        ReadEndpoint(file, tree, declared, marks, model);
                        break;
                }
            }
            else
            {
                if (!methods.TryGetValue(element.Span, out var method))
                {
                    continue;
                }

                var containing = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

                model.Elements.Add(new MarkedElement(
                    kind, containing?.Identifier.ValueText ?? "", element.Name,
                    file, element.Line, marks, false));
            }
        }
    }

    /// <summary>
    ///     Reads a marked element of no known kind into the stray marks: a reading
    ///     method, a private helper, or an unrecognized class carrying a mark.
    /// </summary>
    private static void ReadStrayMark(
        string file,
        MarkableElement element,
        IReadOnlyDictionary<TextSpan, MethodDeclarationSyntax> methods,
        CodeModel model)
    {
        var marks = element.Marks
            .Select(mark => new CodeMark(mark.RecordId, mark.Style, mark.Verb, file, mark.Line))
            .ToList();

        if (element.IsType)
        {
            model.StrayMarks.Add(new StrayMark(element.Name, null, file, element.Line, marks));
            return;
        }

        if (!methods.TryGetValue(element.Span, out var method))
        {
            return;
        }

        var containing = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

        model.StrayMarks.Add(new StrayMark(
            containing?.Identifier.ValueText ?? "", element.Name, file, element.Line, marks));
    }

    /// <summary>Whether the type is class-like, as the shared kind reader decides: a class or a non-struct record.</summary>
    private static bool IsClassLike(TypeDeclarationSyntax type) =>
        type switch
        {
            ClassDeclarationSyntax => true,
            RecordDeclarationSyntax record => !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
            _ => false,
        };

    /// <summary>Whether the name is a job name, as the shared kind reader decides.</summary>
    private static bool HasJobName(string name) =>
        name.EndsWith("Job", StringComparison.Ordinal) || name.EndsWith("Worker", StringComparison.Ordinal);

    /// <summary>The one mark naming the prefix, or null when the element carries none or several.</summary>
    private static string? SingleMark(IReadOnlyList<CodeMark> marks, string prefix)
    {
        string? found = null;

        foreach (var mark in marks)
        {
            if (mark.RecordId.StartsWith(prefix, StringComparison.Ordinal))
            {
                if (found is not null)
                {
                    return null;
                }

                found = mark.RecordId;
            }
        }

        return found;
    }

    /// <summary>The one automation mark the element carries, or null when it carries none or several.</summary>
    private static string? SingleAutomationMark(IReadOnlyList<CodeMark> marks)
    {
        string? found = null;

        foreach (var mark in marks)
        {
            if (mark.RecordId.StartsWith("automation/", StringComparison.Ordinal))
            {
                if (found is not null)
                {
                    return null;
                }

                found = mark.RecordId;
            }
        }

        return found;
    }

    /// <summary>Reads one handler: its use case from its one use-case mark, its shape from its class.</summary>
    private static void ReadHandler(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        KindContext context)
    {
        var useCase = SingleMark(marks, "use-case/") ?? "";

        Identifiers.TrySplit(useCase, out _, out var home, out var name);

        var message = MessageTypeOf(declared);
        var handlesQuery = ImplementsFace(declared, "IQueryHandler") || context.QueryHandlers.Contains(declared.Identifier.ValueText);
        var handlesCommand = ImplementsFace(declared, "ICommandHandler") || context.CommandHandlers.Contains(declared.Identifier.ValueText);
        var kind = handlesQuery && !handlesCommand
            ? "query"
            : "command";
        var details = HandlerDetailsOf(declared, tree, file, helpers, aggregateRoots, holders, kind == "query");

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

    /// <summary>Reads one event handler or job host: its automation mark, and a use-case mark when one sits beside it.</summary>
    private static void ReadAutomation(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> holders)
    {
        model.Automations.Add(new AutomationInfo(
            declared.Identifier.ValueText,
            file,
            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
            SingleMark(marks, "use-case/"),
            SingleAutomationMark(marks),
            RepositoriesOf(declared, tree, file),
            RefusalsOf(declared, tree, file),
            ThrownOf(declared, tree, file),
            DispatchesOf(declared, tree, file, helpers),
            DependencyCountOf(declared),
            CollaboratorsOf(declared, tree, file, holders),
            LoopReadsOf(declared, tree, file)));
    }

    /// <summary>Reads one endpoint's operations: the route and operation literals its class maps.</summary>
    private static void ReadEndpoint(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model)
    {
        var useCase = SingleMark(marks, "use-case/") ?? "";

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var called = CalledName(invocation);

            if (called == "WithName" && FirstLiteral(invocation) is { } operation)
            {
                model.Endpoints.Add(new EndpointOperation(
                    operation, useCase, "", "", file,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
            else if (called is "MapGet" or "MapPost" or "MapPut" or "MapDelete" or "MapPatch"
                && FirstLiteral(invocation) is { } route)
            {
                model.Endpoints.Add(new EndpointOperation(
                    "", useCase, called["Map".Length..].ToUpperInvariant(), route, file,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
        }
    }

    /// <summary>The message stem: the message type name without its Command or Query suffix.</summary>
    private static string OperationOf(string message)
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
    private static bool ImplementsFace(TypeDeclarationSyntax declared, string face) =>
        declared.BaseList is not null
        && declared.BaseList.Types
            .Select(type => BaseName(type.Type))
            .Any(baseName => baseName == face || baseName.StartsWith(face + "<", StringComparison.Ordinal));

    /// <summary>
    ///     The hand-written runtime value the type declares: an event's Classification
    ///     literal or a rule exception's Code literal. Null when the type declares none.
    /// </summary>
    private static string? RuntimeValueOf(TypeDeclarationSyntax declared, CodeKind kind)
    {
        var wanted = kind switch
        {
            CodeKind.DomainEvent => "Classification",
            CodeKind.RuleException => "Code",
            _ => null,
        };

        if (wanted is null)
        {
            return null;
        }

        foreach (var member in declared.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (variable.Identifier.ValueText == wanted
                            && ValueOf(variable.Initializer?.Value) is { } fieldValue)
                        {
                            return fieldValue;
                        }
                    }

                    break;
                case PropertyDeclarationSyntax property when property.Identifier.ValueText == wanted:
                    if (ValueOf(property.ExpressionBody?.Expression) is { } body)
                    {
                        return body;
                    }

                    if (ValueOf(property.Initializer?.Value) is { } initial)
                    {
                        return initial;
                    }

                    if (property.AccessorList is { } accessors)
                    {
                        foreach (var accessor in accessors.Accessors)
                        {
                            if (ValueOf(accessor.ExpressionBody?.Expression) is { } held)
                            {
                                return held;
                            }
                        }
                    }

                    break;
            }
        }

        return null;

        static string? ValueOf(ExpressionSyntax? expression) =>
            expression is LiteralExpressionSyntax literal && literal.Token.Value is string text
                ? text
                : null;
    }

    private static bool IsUnder(string root, string file)
    {
        if (root.Length == 0)
        {
            return false;
        }

        var relative = Path.GetRelativePath(root, file);

        return !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>
    ///     Reads the public methods of every aggregate root the file declares with the
    ///     domain events each one raises and the use-case marks each one carries, the
    ///     raised events read through the shared raised-event rule.
    /// </summary>
    private static void ReadAggregateMethods(
        string file,
        SyntaxTree tree,
        IReadOnlyList<MarkableElement> elements,
        IReadOnlyDictionary<TextSpan, TypeDeclarationSyntax> types,
        CodeModel model)
    {
        foreach (var element in elements)
        {
            if (!element.IsType
                || element.Kind != CodeKind.AggregateRoot
                || !types.TryGetValue(element.Span, out var declared))
            {
                continue;
            }

            foreach (var method in declared.Members.OfType<MethodDeclarationSyntax>())
            {
                if (!method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                {
                    continue;
                }

                var uses = MarkReader.Read(method)
                    .Where(mark => mark.RecordId.StartsWith("use-case/", StringComparison.Ordinal))
                    .Select(mark => mark.RecordId)
                    .ToList();

                model.AggregateMethods.Add(new AggregateMethod(
                    declared.Identifier.ValueText,
                    method.Identifier.ValueText,
                    RaisedEvents.Of(method, declared),
                    file,
                    tree.GetLineSpan(method.Span).StartLinePosition.Line + 1)
                {
                    UseCases = uses,
                });
            }
        }
    }

    /// <summary>
    ///     Reads the audit action literal each message class declares in its
    ///     <c>[Audited("...")]</c> attribute, keyed by the message stem.
    /// </summary>
    private static void ReadAuditActions(
        string file,
        SyntaxTree tree,
        IReadOnlyList<BaseTypeDeclarationSyntax> classes,
        CodeModel model)
    {
        foreach (var declared in classes)
        {
            foreach (var attribute in declared.AttributeLists.SelectMany(list => list.Attributes))
            {
                var name = attribute.Name.ToString();
                var dot = name.LastIndexOf('.');
                var simple = dot >= 0 ? name[(dot + 1)..] : name;

                if (simple is not ("Audited" or "AuditedAttribute"))
                {
                    continue;
                }

                if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                        is LiteralExpressionSyntax literal
                    && literal.Token.Value is string action
                    && action.Length > 0)
                {
                    var operation = declared.Identifier.ValueText;

                    foreach (var suffix in new[] { "Command", "Query" })
                    {
                        if (operation.EndsWith(suffix, StringComparison.Ordinal) && operation.Length > suffix.Length)
                        {
                            operation = operation[..^suffix.Length];
                            break;
                        }
                    }

                    model.AuditActions.Add(new CodeAuditAction(
                        operation,
                        action,
                        file,
                        tree.GetLineSpan(attribute.Span).StartLinePosition.Line + 1));
                }
            }
        }
    }

    private static string? LiteralText(SyntaxToken token) =>
        token.Kind() switch
        {
            SyntaxKind.StringLiteralToken
                or SyntaxKind.Utf8StringLiteralToken
                or SyntaxKind.SingleLineRawStringLiteralToken
                or SyntaxKind.MultiLineRawStringLiteralToken
                when token.Value is string text => text,
            SyntaxKind.InterpolatedStringTextToken => token.ValueText,
            _ => null,
        };

    /// <summary>
    ///     Whether a literal sits where code declares a refusal code: inside a property, field or
    ///     constant named <c>Code</c>, as an argument of a <c>new ...Exception(...)</c>, or as a
    ///     value named <c>code</c> (a named argument, an anonymous object member or an assignment).
    /// </summary>
    private static bool Declares(SyntaxToken token)
    {
        foreach (var node in token.Parent?.AncestorsAndSelf() ?? [])
        {
            switch (node)
            {
                case PropertyDeclarationSyntax property:
                    return property.Identifier.ValueText == "Code" && RefusalType(property);
                case FieldDeclarationSyntax field:
                    return field.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "Code") && RefusalType(field);
                case ArgumentSyntax { NameColon.Name.Identifier.ValueText: var name } when IsCodeName(name):
                    return true;
                case AnonymousObjectMemberDeclaratorSyntax { NameEquals.Name.Identifier.ValueText: var name }:
                    return IsCodeName(name);
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax } assignment:
                    return IsCodeName(LastName(assignment.Left));
                case BaseObjectCreationExpressionSyntax creation:
                    return creation is ObjectCreationExpressionSyntax { Type: var type }
                        && LastName(type).EndsWith("Exception", StringComparison.Ordinal);
                case StatementSyntax or MemberDeclarationSyntax:
                    return false;
            }
        }

        return false;

        // A Code member declares a refusal on a type named for one; on a closed set of
        // features or kinds the same member name holds a label instead.
        static bool RefusalType(SyntaxNode member) =>
            member.Parent is BaseTypeDeclarationSyntax type
            && (type.Identifier.ValueText.EndsWith("Exception", StringComparison.Ordinal)
                || type.Identifier.ValueText.EndsWith("Failure", StringComparison.Ordinal)
                || type.Identifier.ValueText.EndsWith("Refusal", StringComparison.Ordinal));

        static bool IsCodeName(string name) => string.Equals(name, "code", StringComparison.OrdinalIgnoreCase);

        static string LastName(SyntaxNode node) => node switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            ElementAccessExpressionSyntax element => element.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax key
                ? key.Token.ValueText
                : "",
            _ => "",
        };
    }

    private static string BaseName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => $"{generic.Identifier.ValueText}<{string.Join(",", generic.TypeArgumentList.Arguments)}>",
        IdentifierNameSyntax plain => plain.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.ToString(),
        _ => type.ToString(),
    };

    private static string? EnclosingClass(SyntaxToken token)
    {
        var parent = token.Parent;

        while (parent is not null && parent is not BaseTypeDeclarationSyntax)
        {
            parent = parent.Parent;
        }

        return (parent as BaseTypeDeclarationSyntax)?.Identifier.ValueText;
    }

    private static string? CalledName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            _ => null,
        };

    private static string? FirstLiteral(InvocationExpressionSyntax invocation)
    {
        var first = invocation.ArgumentList.Arguments.FirstOrDefault();

        return first?.Expression is LiteralExpressionSyntax literal
            && literal.Token.Value is string text
                ? text
                : null;
    }

    /// <summary>One class's collected details: its repositories, refusals, thrown types, dispatches and handler shape.</summary>
    sealed record ClassDetails(
        string Class,
        int Line,
        List<CodeRepository> Repositories,
        List<CodeRefusal> Refusals,
        List<CodeThrown> Thrown,
        List<CodeDispatch> Dispatches,
        string MessageType,
        int DependencyCount,
        List<CodeCall> AggregateCalls,
        List<CodeCall> FactoryCalls,
        List<CodeCollaborator> Collaborators,
        List<CodeLoopRead> LoopReads,
        List<CodeMemoryFilter> MemoryFilters);

    /// <summary>The collected details of a handler class.</summary>
    private static ClassDetails HandlerDetailsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        bool isQuery)
    {
        return new ClassDetails(
            declared.Identifier.ValueText,
            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
            RepositoriesOf(declared, tree, file),
            RefusalsOf(declared, tree, file),
            ThrownOf(declared, tree, file),
            DispatchesOf(declared, tree, file, helpers),
            MessageTypeOf(declared),
            DependencyCountOf(declared),
            AggregateCallsOf(declared, tree, file, aggregateRoots),
            FactoryCallsOf(declared, tree, file, aggregateRoots),
            CollaboratorsOf(declared, tree, file, holders),
            LoopReadsOf(declared, tree, file),
            isQuery ? MemoryFiltersOf(declared, tree, file) : []);
    }

    /// <summary>The repository interfaces a class takes in its primary or explicit constructors.</summary>
    private static List<CodeRepository> RepositoriesOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new List<CodeRepository>();

        foreach (var (type, line) in ConstructorTypes(declared, tree))
        {
            var simple = type.Contains('<') ? type[..type.IndexOf('<')] : type;

            if (simple.EndsWith("Repository", StringComparison.Ordinal))
            {
                found.Add(new CodeRepository(type, file, line));
            }
        }

        return found;
    }

    /// <summary>Every constructor parameter type with its line: the primary constructor first, then each explicit one.</summary>
    private static List<(string Type, int Line)> ConstructorTypes(BaseTypeDeclarationSyntax declared, SyntaxTree tree)
    {
        var found = new List<(string Type, int Line)>();

        if (declared is not TypeDeclarationSyntax typed)
        {
            return found;
        }

        if (typed.ParameterList is { } primary)
        {
            foreach (var parameter in primary.Parameters)
            {
                if (parameter.Type is not null)
                {
                    found.Add((SimpleTypeName(parameter.Type), tree.GetLineSpan(parameter.Span).StartLinePosition.Line + 1));
                }
            }
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type is not null)
                {
                    found.Add((SimpleTypeName(parameter.Type), tree.GetLineSpan(parameter.Span).StartLinePosition.Line + 1));
                }
            }
        }

        return found;
    }

    /// <summary>The refusal codes a class produces in declaring positions, earliest line first.</summary>
    private static List<CodeRefusal> RefusalsOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var token in declared.DescendantTokens())
        {
            if (LiteralText(token) is not { } literal || !Declares(token))
            {
                continue;
            }

            foreach (System.Text.RegularExpressions.Match match in Identifiers.RefusalCode().Matches(literal))
            {
                if (match.Value == literal)
                {
                    found.TryAdd(match.Value, tree.GetLineSpan(token.Span).StartLinePosition.Line + 1);
                }
            }
        }

        return [.. found
            .OrderBy(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new CodeRefusal(entry.Key, file, entry.Value))];
    }

    /// <summary>The exception and failure types a class constructs, earliest line first.</summary>
    private static List<CodeThrown> ThrownOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var creation in declared.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var name = SimpleTypeName(creation.Type);
            var simple = name.Contains('<') ? name[..name.IndexOf('<')] : name;

            if (simple.EndsWith("Exception", StringComparison.Ordinal)
                || simple.EndsWith("Failure", StringComparison.Ordinal)
                || simple.EndsWith("Refusal", StringComparison.Ordinal))
            {
                found.TryAdd(simple, tree.GetLineSpan(creation.Span).StartLinePosition.Line + 1);
            }
        }

        return [.. found
            .OrderBy(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new CodeThrown(entry.Key, file, entry.Value))];
    }

    /// <summary>
    ///     The simple names of the classes whose own constructor takes a repository: a
    ///     parameter whose kind starts with <c>I</c> and ends with <c>Repository</c>.
    /// </summary>
    private static HashSet<string> RepositoryHoldersOf(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees)
    {
        var holders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                if (!trees.TryGetValue(file, out var tree))
                {
                    continue;
                }

                foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (ConstructorTypes(declared, tree)
                        .Select(entry => KindOf(entry.Type))
                        .Any(kind => kind.StartsWith("I", StringComparison.Ordinal) && kind.EndsWith("Repository", StringComparison.Ordinal)))
                    {
                        holders.Add(declared.Identifier.ValueText);
                    }
                }
            }
        }

        return holders;
    }

    /// <summary>The message a handler handles: the first parameter type of its Handle method, or empty.</summary>
    private static string MessageTypeOf(BaseTypeDeclarationSyntax declared)
    {
        if (declared is not TypeDeclarationSyntax typed)
        {
            return "";
        }

        foreach (var method in typed.Members.OfType<MethodDeclarationSyntax>())
        {
            if (method.Identifier.ValueText is "Handle" or "HandleAsync"
                && method.ParameterList.Parameters.FirstOrDefault()?.Type is { } message)
            {
                return SimpleTypeName(message);
            }
        }

        return "";
    }

    /// <summary>The constructor dependencies a class declares: the widest constructor's parameter count.</summary>
    private static int DependencyCountOf(BaseTypeDeclarationSyntax declared)
    {
        if (declared is not TypeDeclarationSyntax typed)
        {
            return 0;
        }

        var counts = new List<int>();

        if (typed.ParameterList is { } primary)
        {
            counts.Add(primary.Parameters.Count);
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            counts.Add(constructor.ParameterList.Parameters.Count);
        }

        return counts.Count == 0 ? 0 : counts.Max();
    }

    /// <summary>
    ///     The calls a handler makes on an aggregate root it holds: an invocation on a
    ///     local, parameter or field whose type is a domain aggregate root, in line order.
    /// </summary>
    private static List<CodeCall> AggregateCallsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        HashSet<string> aggregateRoots)
    {
        var found = new List<CodeCall>();

        if (aggregateRoots.Count == 0)
        {
            return found;
        }

        var receivers = ReceiverTypes(declared);
        var fields = FieldTypes(declared);

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            var receiver = member.Expression switch
            {
                IdentifierNameSyntax plain => plain.Identifier.ValueText,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
                _ => null,
            };

            // A name the class neither injects, holds nor declares is a static call on a
            // type rather than a call on a held aggregate.
            if (receiver is null)
            {
                continue;
            }

            if (!receivers.TryGetValue(receiver, out var type)
                && !fields.TryGetValue(receiver, out type))
            {
                if (AggregateLocalType(invocation, receiver, receivers) is not { } local)
                {
                    continue;
                }

                type = local;
            }

            if (aggregateRoots.Contains(SimpleName(type)))
            {
                found.Add(new CodeCall(
                    receiver,
                    member.Name.Identifier.ValueText,
                    file,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>
    ///     The static calls a handler makes on aggregate root types: an invocation
    ///     whose bare receiver names a root type rather than a held value, with the
    ///     root type name as receiver, in line order.
    /// </summary>
    private static List<CodeCall> FactoryCallsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        HashSet<string> aggregateRoots)
    {
        var found = new List<CodeCall>();

        if (aggregateRoots.Count == 0)
        {
            return found;
        }

        var receivers = ReceiverTypes(declared);
        var fields = FieldTypes(declared);

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Expression is not IdentifierNameSyntax plain)
            {
                continue;
            }

            var name = plain.Identifier.ValueText;

            // A name the class injects, holds or declares is a call on a held
            // aggregate; only a bare root type name is a static factory call.
            if (receivers.ContainsKey(name)
                || fields.ContainsKey(name)
                || AggregateLocalType(invocation, name, receivers) is not null
                || !aggregateRoots.Contains(name))
            {
                continue;
            }

            found.Add(new CodeCall(
                name,
                member.Name.Identifier.ValueText,
                file,
                tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>The declared field and property types a class holds, by member name.</summary>
    private static Dictionary<string, string> FieldTypes(BaseTypeDeclarationSyntax declared)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        if (declared is not TypeDeclarationSyntax typed)
        {
            return types;
        }

        foreach (var field in typed.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                types.TryAdd(variable.Identifier.ValueText, SimpleTypeName(field.Declaration.Type));
            }
        }

        foreach (var property in typed.Members.OfType<PropertyDeclarationSyntax>())
        {
            types.TryAdd(property.Identifier.ValueText, SimpleTypeName(property.Type));
        }

        return types;
    }

    /// <summary>
    ///     The type a local holds: what it builds with <c>new</c>, its declared type, or
    ///     the aggregate a repository load returns, inferred from the repository interface.
    /// </summary>
    private static string? AggregateLocalType(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers)
    {
        var scope = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

        if (scope is null)
        {
            return null;
        }

        foreach (var declarator in scope.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText != name)
            {
                continue;
            }

            if (declarator.Initializer?.Value is ObjectCreationExpressionSyntax creation)
            {
                return SimpleTypeName(creation.Type);
            }

            if (declarator.Initializer?.Value is AwaitExpressionSyntax awaited
                && awaited.Expression is InvocationExpressionSyntax load
                && load.Expression is MemberAccessExpressionSyntax member
                && member.Expression is IdentifierNameSyntax repository
                && receivers.TryGetValue(repository.Identifier.ValueText, out var repoType)
                && KindOf(repoType).EndsWith("Repository", StringComparison.Ordinal)
                && member.Name.Identifier.ValueText is { } method
                && method.EndsWith("Async", StringComparison.Ordinal)
                && (method.StartsWith("Get", StringComparison.Ordinal)
                    || method.StartsWith("Find", StringComparison.Ordinal)
                    || method.StartsWith("Load", StringComparison.Ordinal)
                    || method.StartsWith("Single", StringComparison.Ordinal))
                && RepositoryAggregateName(KindOf(repoType)) is { } aggregate)
            {
                return aggregate;
            }

            if (declarator.Parent is VariableDeclarationSyntax declaration
                && declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" })
            {
                return SimpleTypeName(declaration.Type);
            }
        }

        return null;
    }

    /// <summary>The aggregate a repository interface serves, or null when it names none.</summary>
    private static string? RepositoryAggregateName(string face)
    {
        var type = face;

        if (type.StartsWith("I", StringComparison.Ordinal) && type.Length > 1 && char.IsUpper(type[1]))
        {
            type = type[1..];
        }

        return type.EndsWith("Repository", StringComparison.Ordinal) && type.Length > "Repository".Length
            ? type[..^"Repository".Length]
            : null;
    }

    /// <summary>The simple type name without generics or qualification.</summary>
    private static string SimpleName(string type)
    {
        var kind = KindOf(type);
        var dot = kind.LastIndexOf('.');

        return dot >= 0 ? kind[(dot + 1)..] : kind;
    }

    /// <summary>
    ///     The collaborators a class builds with <c>new</c> whose own constructor takes
    ///     a repository, in line order.
    /// </summary>
    private static List<CodeCollaborator> CollaboratorsOf(
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
            var type = SimpleName(SimpleTypeName(creation.Type));
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
    private static List<CodeLoopRead> LoopReadsOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new List<CodeLoopRead>();
        var receivers = ReceiverTypes(declared);

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
                    && KindOf(type).EndsWith("Repository", StringComparison.Ordinal)
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

    /// <summary>The LINQ operators that filter, order, page or count in memory.</summary>
    private static readonly HashSet<string> MemoryFilterOperators = new(StringComparer.Ordinal)
    {
        "Where", "OrderBy", "Skip", "Take", "Count", "Any", "SingleOrDefault",
    };

    /// <summary>
    ///     The LINQ filters a query handler applies to the result of a repository list
    ///     call, chained or through a local, one per line.
    /// </summary>
    private static List<CodeMemoryFilter> MemoryFiltersOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new List<CodeMemoryFilter>();
        var seen = new HashSet<int>();
        var receivers = ReceiverTypes(declared);

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
    private static bool FiltersRepositoryList(
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
    private static bool IsRepositoryListCall(MemberAccessExpressionSyntax member, Dictionary<string, string> receivers)
    {
        var receiver = member.Expression switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
            _ => null,
        };

        return receiver is not null
            && receivers.TryGetValue(receiver, out var type)
            && KindOf(type).EndsWith("Repository", StringComparison.Ordinal)
            && member.Name.Identifier.ValueText.StartsWith("List", StringComparison.Ordinal)
            && member.Name.Identifier.ValueText.EndsWith("Async", StringComparison.Ordinal);
    }

    /// <summary>Whether the local holds the awaited result of a repository list call.</summary>
    private static bool LocalHoldsRepositoryList(
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

    /// <summary>
    ///     One helper method the source tree declares: the type declaring it, its parameters by
    ///     name with their simple types, and the dispatches it makes through each receiver.
    /// </summary>
    sealed record HelperMethod(
        string Type,
        string Method,
        Dictionary<string, string> Parameters,
        List<(string Receiver, CodeDispatch Dispatch)> Dispatches);

    /// <summary>
    ///     The dispatch calls a class makes: commands through <c>ICommandMediator</c>, events
    ///     through a publisher or the outbox, notifications through a messenger, notifier or
    ///     sender, and a call on an interface anchored to an external-system record as a calls edge.
    /// </summary>
    private static List<CodeDispatch> DispatchesOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        Dictionary<string, List<HelperMethod>> helpers)
    {
        var receivers = ReceiverTypes(declared);

        if (receivers.Count == 0)
        {
            return [];
        }

        var found = ScanDispatches(declared, receivers, tree, file)
            .Select(entry => entry.Dispatch)
            .ToList();
        found.AddRange(HelperDispatches(declared, tree, file, receivers, helpers));

        found.Sort((left, right) =>
        {
            var line = left.Line.CompareTo(right.Line);

            if (line != 0)
            {
                return line;
            }

            var target = string.Compare(left.Target, right.Target, StringComparison.Ordinal);

            return target != 0
                ? target
                : string.Compare(left.Via ?? "", right.Via ?? "", StringComparison.Ordinal);
        });

        return found;
    }

    /// <summary>
    ///     The dispatches a scope makes through its receivers, each with the receiver name
    ///     that carried it, so a helper's dispatch is attributed only through the receiver
    ///     the caller passes.
    /// </summary>
    private static List<(string Receiver, CodeDispatch Dispatch)> ScanDispatches(
        SyntaxNode scope,
        Dictionary<string, string> receivers,
        SyntaxTree tree,
        string file)
    {
        var found = new List<(string Receiver, CodeDispatch Dispatch)>();

        foreach (var invocation in scope.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            var receiver = member.Expression switch
            {
                IdentifierNameSyntax plain => plain.Identifier.ValueText,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
                _ => null,
            };

            if (receiver is null || !receivers.TryGetValue(receiver, out var type))
            {
                continue;
            }

            var kind = KindOf(type);
            var method = member.Name.Identifier.ValueText;
            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;

            if (kind == "ICommandMediator" && method == "SendAsync")
            {
                if (MessageTarget(invocation, templateCounts: false) is { } runs)
                {
                    found.Add((receiver, new CodeDispatch("runs", runs.Target, "", file, line)));
                }
            }
            else if (EmitsThrough(kind))
            {
                if (MessageTarget(invocation, templateCounts: false) is { } emits)
                {
                    found.Add((receiver, new CodeDispatch("emits", emits.Target, "", file, line)));
                }
            }
            else if (SendsThrough(kind))
            {
                if (MessageTarget(invocation, templateCounts: true) is { } sends)
                {
                    // A message built from a rendered template names that template: the key the
                    // enclosing method passes to Render is the notification the send carries.
                    if (sends.Detail == "type" && RenderedTemplate(invocation) is { } rendered)
                    {
                        sends = (rendered, "template");
                    }

                    found.Add((receiver, new CodeDispatch("sends", sends.Target, sends.Detail, file, line)));
                }
            }
            else if (!ExcludedReceiver(kind) && externalPorts.Contains(kind))
            {
                found.Add((receiver, new CodeDispatch("calls", method, kind, file, line)));
            }
        }

        return found;
    }

    /// <summary>Whether a receiver kind emits events: a publisher or the outbox.</summary>
    private static bool EmitsThrough(string kind) =>
        kind.EndsWith("Publisher", StringComparison.Ordinal)
        || kind.Contains("Outbox", StringComparison.Ordinal);

    /// <summary>Whether a receiver kind sends notifications: a messenger, notifier or sender.</summary>
    private static bool SendsThrough(string kind) =>
        kind.Contains("Messenger", StringComparison.Ordinal)
        || kind.EndsWith("Notifier", StringComparison.Ordinal)
        || kind.EndsWith("Sender", StringComparison.Ordinal);

    /// <summary>Whether a held receiver kind can carry a dispatch the checks read.</summary>
    private static bool DispatchCapable(string type)
    {
        var kind = KindOf(type);

        return kind == "ICommandMediator"
            || EmitsThrough(kind)
            || SendsThrough(kind)
            || (!ExcludedReceiver(kind) && externalPorts.Contains(kind));
    }

    /// <summary>The receiver kind: the simple type name without its generic arguments.</summary>
    private static string KindOf(string type) =>
        type.Contains('<') ? type[..type.IndexOf('<')] : type;

    /// <summary>
    ///     Every helper method the configured source roots declare, by <c>Type.Method</c>,
    ///     each under its declaring type and under every interface it implements, so a call
    ///     through a held interface resolves to the method behind it. A method without
    ///     parameters, or one making no dispatch, is indexed as nothing to attribute.
    /// </summary>
    private static Dictionary<string, List<HelperMethod>> IndexHelpers(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees)
    {
        var files = new List<string>();

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var merged = new Dictionary<string, List<HelperMethod>>(StringComparer.Ordinal);
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => new List<(HelperMethod Helper, List<string> Aliases)>(),
            (file, _, local) =>
            {
                if (trees.TryGetValue(file, out var tree))
                {
                    CollectHelpers(file, tree, local);
                }

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    foreach (var (helper, aliases) in local)
                    {
                        foreach (var key in aliases.Prepend($"{helper.Type}.{helper.Method}"))
                        {
                            if (!merged.TryGetValue(key, out var overloads))
                            {
                                overloads = [];
                                merged[key] = overloads;
                            }

                            overloads.Add(helper);
                        }
                    }
                }
            });

        return merged;
    }

    /// <summary>Collects the dispatching helper methods one cached tree declares.</summary>
    private static void CollectHelpers(string file, SyntaxTree tree, List<(HelperMethod Helper, List<string> Aliases)> into)
    {
        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (declared is not TypeDeclarationSyntax typed)
            {
                continue;
            }

            var interfaces = typed.BaseList is not null
                ? typed.BaseList.Types.Select(type => BaseName(type.Type)).ToList()
                : [];
            var aliases = interfaces
                .Select(face => KindOf(face))
                .Where(face => face.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var method in typed.Members.OfType<MethodDeclarationSyntax>())
            {
                var parameters = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var parameter in method.ParameterList.Parameters)
                {
                    if (parameter.Type is not null && parameter.Identifier.ValueText.Length > 0)
                    {
                        parameters.TryAdd(parameter.Identifier.ValueText, SimpleTypeName(parameter.Type));
                    }
                }

                if (parameters.Count == 0)
                {
                    continue;
                }

                var scope = (SyntaxNode?)method.Body ?? method.ExpressionBody;

                if (scope is null)
                {
                    continue;
                }

                var dispatches = ScanDispatches(scope, parameters, tree, file);

                if (dispatches.Count == 0)
                {
                    continue;
                }

                into.Add((
                    new HelperMethod(declared.Identifier.ValueText, method.Identifier.ValueText, parameters, dispatches),
                    aliases.Select(face => $"{face}.{method.Identifier.ValueText}").ToList()));
            }
        }
    }

    /// <summary>
    ///     The dispatches a held receiver carries through a helper: when the class passes a
    ///     mediator, publisher, messenger or external port it holds to a helper method in the
    ///     same source tree, the helper's dispatches through that receiver count for the class,
    ///     one hop deep and named with the helper. The helper's own helper calls are never
    ///     followed, so an attribution chain can neither recurse nor cycle.
    /// </summary>
    private static List<CodeDispatch> HelperDispatches(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        Dictionary<string, string> receivers,
        Dictionary<string, List<HelperMethod>> helpers)
    {
        if (helpers.Count == 0)
        {
            return [];
        }

        var held = receivers
            .Where(entry => DispatchCapable(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        if (held.Count == 0)
        {
            return [];
        }

        var found = new List<CodeDispatch>();
        var seen = new HashSet<(string Via, string Verb, string Target, string Detail)>();

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var passed = invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression as IdentifierNameSyntax)
                .Where(argument => argument is not null && held.ContainsKey(argument.Identifier.ValueText))
                .Select(argument => held[argument!.Identifier.ValueText])
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (passed.Count == 0)
            {
                continue;
            }

            if (CalleeOf(invocation, declared, receivers) is not { } callee
                || !helpers.TryGetValue($"{callee.Type}.{callee.Method}", out var overloads))
            {
                continue;
            }

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;

            foreach (var helper in overloads)
            {
                var bound = helper.Parameters
                    .Where(parameter => passed.Contains(parameter.Value, StringComparer.Ordinal))
                    .Select(parameter => parameter.Key)
                    .ToHashSet(StringComparer.Ordinal);

                if (bound.Count == 0)
                {
                    continue;
                }

                var via = $"{helper.Type}.{helper.Method}";

                foreach (var (receiver, dispatch) in helper.Dispatches)
                {
                    if (!bound.Contains(receiver)
                        || !seen.Add((via, dispatch.Verb, dispatch.Target, dispatch.Detail)))
                    {
                        continue;
                    }

                    found.Add(dispatch with { File = file, Line = line, Via = via });
                }
            }
        }

        return found;
    }

    /// <summary>
    ///     The helper a call names: a static <c>Type.Method</c> call, or an instance call on a
    ///     held or locally built value whose type names it. Null for a call onto the class
    ///     itself, whose body the direct scan already reads, and for anything else.
    /// </summary>
    private static (string Type, string Method)? CalleeOf(
        InvocationExpressionSyntax invocation,
        BaseTypeDeclarationSyntax declared,
        Dictionary<string, string> receivers)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return null;
        }

        var method = member.Name.Identifier.ValueText;

        switch (member.Expression)
        {
            case IdentifierNameSyntax plain:
                var name = plain.Identifier.ValueText;

                if (name == declared.Identifier.ValueText)
                {
                    return null;
                }

                if (receivers.TryGetValue(name, out var instance))
                {
                    return InstanceCallee(instance, method, declared);
                }

                if (char.IsUpper(name[0]))
                {
                    return (name, method);
                }

                return LocalType(invocation, name) is { } built
                    ? InstanceCallee(built, method, declared)
                    : null;

            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }:
                return null;

            case MemberAccessExpressionSyntax chain:
                var type = chain.Name.Identifier.ValueText;

                return type == declared.Identifier.ValueText ? null : (type, method);

            default:
                return null;
        }
    }

    /// <summary>The helper an instance call names, or null when the instance is the class itself.</summary>
    private static (string Type, string Method)? InstanceCallee(string instance, string method, BaseTypeDeclarationSyntax declared)
    {
        var type = KindOf(instance);
        var dot = type.LastIndexOf('.');

        if (dot >= 0)
        {
            type = type[(dot + 1)..];
        }

        return type.Length == 0 || type == declared.Identifier.ValueText ? null : (type, method);
    }

    /// <summary>The type a local holds: what it builds, or its declared type when it names one.</summary>
    private static string? LocalType(InvocationExpressionSyntax invocation, string name)
    {
        var scope = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

        if (scope is null)
        {
            return null;
        }

        foreach (var declarator in scope.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText != name)
            {
                continue;
            }

            if (declarator.Initializer?.Value is ObjectCreationExpressionSyntax creation)
            {
                return SimpleTypeName(creation.Type);
            }

            if (declarator.Parent is VariableDeclarationSyntax declaration
                && declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" })
            {
                return SimpleTypeName(declaration.Type);
            }
        }

        return null;
    }

    /// <summary>
    ///     The injected names a class may call: its constructor parameters plus the fields and
    ///     properties an explicit constructor assigns from them, each with its simple type name.
    /// </summary>
    private static Dictionary<string, string> ReceiverTypes(BaseTypeDeclarationSyntax declared)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        if (declared is not TypeDeclarationSyntax typed)
        {
            return types;
        }

        if (typed.ParameterList is { } primary)
        {
            foreach (var parameter in primary.Parameters)
            {
                AddParameter(parameter);
            }
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                AddParameter(parameter);
            }

            if (constructor.Body is null)
            {
                continue;
            }

            foreach (var assignment in constructor.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Right is not IdentifierNameSyntax source
                    || !types.TryGetValue(source.Identifier.ValueText, out var type))
                {
                    continue;
                }

                var target = assignment.Left switch
                {
                    IdentifierNameSyntax plain => plain.Identifier.ValueText,
                    MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member => member.Name.Identifier.ValueText,
                    _ => null,
                };

                if (target is not null)
                {
                    types.TryAdd(target, type);
                }
            }
        }

        return types;

        void AddParameter(ParameterSyntax parameter)
        {
            if (parameter.Type is not null && parameter.Identifier.ValueText.Length > 0)
            {
                types.TryAdd(parameter.Identifier.ValueText, SimpleTypeName(parameter.Type));
            }
        }
    }

    /// <summary>
    ///     The message a dispatch call carries: the constructed type, or the template key when
    ///     the call sends through the messenger. A topic or key first with the message second
    ///     reads the message. Null when the call names neither.
    /// </summary>
    private static (string Target, string Detail)? MessageTarget(InvocationExpressionSyntax invocation, bool templateCounts)
    {
        var args = invocation.ArgumentList.Arguments;

        if (args.Count == 0)
        {
            return null;
        }

        if (args.Count > 1
            && args[0].Expression is LiteralExpressionSyntax
            && ConstructionOf(args[1].Expression, invocation) is { } second)
        {
            return (second, "type");
        }

        if (ConstructionOf(args[0].Expression, invocation) is { } first)
        {
            return (first, "type");
        }

        if (templateCounts
            && args[0].Expression is LiteralExpressionSyntax literal
            && literal.Token.Value is string text
            && text.Length > 0)
        {
            return (text, "template");
        }

        return null;
    }

    /// <summary>
    ///     The template key the send's enclosing method renders: the string literal passed first to a
    ///     call named Render. Null when the method renders no template, or renders more than one.
    /// </summary>
    private static string? RenderedTemplate(InvocationExpressionSyntax send)
    {
        var scope = send.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

        if (scope is null)
        {
            return null;
        }

        var keys = scope.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Where(call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Render" }
                                       && call.ArgumentList.Arguments.Count > 0
                                       && call.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax { Token.Value: string })
                        .Select(call => (string)((LiteralExpressionSyntax)call.ArgumentList.Arguments[0].Expression).Token.Value!)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

        return keys.Count == 1 ? keys[0] : null;
    }

    /// <summary>The constructed type an argument carries, directly or through a local it names.</summary>
    private static string? ConstructionOf(ExpressionSyntax expression, InvocationExpressionSyntax invocation) =>
        expression switch
        {
            ObjectCreationExpressionSyntax creation => SimpleTypeName(creation.Type),
            IdentifierNameSyntax variable => LocalConstruction(invocation, variable.Identifier.ValueText),
            _ => null,
        };

    /// <summary>The type a local builds before the call, for a message held in a local.</summary>
    private static string? LocalConstruction(InvocationExpressionSyntax invocation, string name)
    {
        var scope = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

        if (scope is null)
        {
            return null;
        }

        foreach (var declarator in scope.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText == name
                && declarator.Initializer?.Value is ObjectCreationExpressionSyntax creation)
            {
                return SimpleTypeName(creation.Type);
            }
        }

        return null;
    }

    /// <summary>The simple type name: unqualified, without nullability, keeping generic arguments.</summary>
    private static string SimpleTypeName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => $"{generic.Identifier.ValueText}<{string.Join(",", generic.TypeArgumentList.Arguments.Select(argument => argument.ToString()))}>",
        IdentifierNameSyntax plain => plain.Identifier.ValueText,
        QualifiedNameSyntax qualified => SimpleTypeName(qualified.Right),
        NullableTypeSyntax nullable => SimpleTypeName(nullable.ElementType),
        ArrayTypeSyntax array => SimpleTypeName(array.ElementType),
        _ => type.ToString(),
    };

    /// <summary>
    ///     Whether an injected type is infrastructure rather than an external port: repositories,
    ///     mediators, publishers, messengers, sessions, clocks, loggers, validators and the rest
    ///     a handler takes without taking an edge to an external system.
    /// </summary>
    private static bool ExcludedReceiver(string type)
    {
        if (type.EndsWith("Repository", StringComparison.Ordinal)
            || type.EndsWith("Mediator", StringComparison.Ordinal)
            || type.EndsWith("Publisher", StringComparison.Ordinal)
            || type.Contains("Messenger", StringComparison.Ordinal)
            || type.EndsWith("Notifier", StringComparison.Ordinal)
            || type.EndsWith("Sender", StringComparison.Ordinal)
            || type.Contains("Outbox", StringComparison.Ordinal))
        {
            return true;
        }

        if (type is "IClock" or "Clock" or "TimeProvider"
            or "IQuerySession" or "IDocumentSession" or "IApplicationDbContext"
            or "IMapper" or "IConfiguration"
            or "ICurrentUser" or "IUserContext" or "ICurrentActor" or "IActorAccessor" or "IActorContext"
            or "IMemoryCache" or "IDistributedCache"
            or "IGuidGenerator" or "IGuidFactory" or "IIdGenerator"
            or "ISerializer" or "IJsonSerializer"
            or "IMetrics" or "IMeterFactory" or "ActivitySource")
        {
            return true;
        }

        return type.StartsWith("ILogger", StringComparison.Ordinal)
            || type.EndsWith("DbContext", StringComparison.Ordinal)
            || type.EndsWith("Validator", StringComparison.Ordinal)
            || type.StartsWith("IOptions", StringComparison.Ordinal)
            || type.EndsWith("Environment", StringComparison.Ordinal)
            || type.EndsWith("Accessor", StringComparison.Ordinal)
            || type.Contains("Localiz", StringComparison.Ordinal);
    }
}
