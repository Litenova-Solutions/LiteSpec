using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     Reads the backend through the parser: handlers by folder, reactions and jobs
///     by interface, events by name, failure codes from Code members, and comments as text.
/// </summary>
sealed class DotnetBackendReader : IBackendReader
{
    /// <summary>The interfaces whose anchor names an external system: the only ports a call counts on.</summary>
    private static HashSet<string> externalPorts = new(StringComparer.Ordinal);

    /// <summary>An external-system anchor followed by the interface it sits on.</summary>
    private static readonly System.Text.RegularExpressions.Regex ExternalPortTag = new(
        @"<implements>\s*external-system/[A-Za-z0-9][A-Za-z0-9._#-]*\s*</implements>[\s\S]*?interface\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    ///     Collects the port interfaces that anchor up to an external-system record, so a call
    ///     through any other injected interface is internal work rather than a calls edge.
    /// </summary>
    private static HashSet<string> ExternalPortsOf(ConsumerConfig config)
    {
        var ports = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.InfrastructureRoot })
        {
            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                var text = SourceTree.Read(file);

                if (!text.Contains("external-system/", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (System.Text.RegularExpressions.Match match in ExternalPortTag.Matches(text))
                {
                    ports.Add(match.Groups["name"].Value);
                }
            }
        }

        return ports;
    }

    /// <summary>Reads every configured source root, parsing each file once.</summary>
    public CodeModel Read(ConsumerConfig config)
    {
        var model = new CodeModel();

        externalPorts = ExternalPortsOf(config);
        var helpers = IndexHelpers(config);
        var aggregateRoots = AggregateRootsOf(config);
        var holders = RepositoryHoldersOf(config);
        ReadHandlers(config, model, helpers, aggregateRoots, holders);

        var files = new List<string>();

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot })
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var merged = new CodeModel();
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => new CodeModel(),
            (file, _, local) =>
            {
                ReadFile(config, file, local, helpers, aggregateRoots, holders);

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    merged.Handlers.AddRange(local.Handlers);
                    merged.Reactions.AddRange(local.Reactions);
                    merged.Events.AddRange(local.Events);
                    merged.Literals.AddRange(local.Literals);
                    merged.Comments.AddRange(local.Comments);
                    merged.Endpoints.AddRange(local.Endpoints);
                    merged.AggregateMethods.AddRange(local.AggregateMethods);
                    merged.AuditActions.AddRange(local.AuditActions);
                }
            });

        model.Reactions.AddRange(merged.Reactions
            .OrderBy(reaction => reaction.File, StringComparer.Ordinal)
            .ThenBy(reaction => reaction.Line)
            .ThenBy(reaction => reaction.Class, StringComparer.Ordinal));
        model.Events.AddRange(merged.Events
            .OrderBy(domainEvent => domainEvent.File, StringComparer.Ordinal)
            .ThenBy(domainEvent => domainEvent.Line)
            .ThenBy(domainEvent => domainEvent.Class, StringComparer.Ordinal));
        model.Literals.AddRange(merged.Literals
            .OrderBy(literal => literal.File, StringComparer.Ordinal)
            .ThenBy(literal => literal.Line)
            .ThenBy(literal => literal.Value, StringComparer.Ordinal));
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

        return model;
    }

    /// <summary>
    ///     The use case whose Application folder holds a file: <c>&lt;Module&gt;/&lt;Aggregate&gt;/&lt;UseCase&gt;/</c>
    ///     names <c>use-case/&lt;module&gt;.&lt;use-case&gt;</c>, as for a handler. A reaction or job without an
    ///     implements tag belongs to the use case whose folder holds it; outside such a folder it belongs to none.
    /// </summary>
    private static string? UseCaseOfFolder(ConsumerConfig config, string file)
    {
        var relative = Path.GetRelativePath(config.ApplicationRoot, file);

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            return null;
        }

        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length < 4 ? null : $"use-case/{Naming.Kebab(parts[0])}.{Naming.Kebab(parts[2])}";
    }

    private static void ReadHandlers(
        ConsumerConfig config,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders)
    {
        foreach (var file in SourceTree.Files(config.ApplicationRoot, ".cs"))
        {
            var name = Path.GetFileName(file);

            if (!name.EndsWith("Handler.cs", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = Path.GetRelativePath(config.ApplicationRoot, file)
                            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (parts.Length < 4)
            {
                continue;
            }

            var module = Naming.Kebab(parts[0]);
            var operation = parts[2];
            var kind = name.EndsWith("QueryHandler.cs", StringComparison.Ordinal) ? "query" : "command";
            var details = HandlerDetails(file, helpers, aggregateRoots, holders, kind == "query");

            model.Handlers.Add(new HandlerInfo(
                $"use-case/{module}.{Naming.Kebab(operation)}",
                module,
                Naming.Kebab(operation),
                operation,
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
                details.MemoryFilters));
        }

        model.Handlers.Sort((left, right) => string.Compare(left.UseCaseId, right.UseCaseId, StringComparison.Ordinal));
    }

    private static void ReadFile(
        ConsumerConfig config,
        string file,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders)
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

        var root = tree.GetRoot();
        var classes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToList();

        ReadEvent(config, file, tree, classes, model);
        ReadEndpoint(config, file, tree, root, model);
        ReadAggregateMethods(config, file, tree, classes, model);
        ReadAuditActions(file, tree, classes, model);

        foreach (var token in root.DescendantTokens())
        {
            if (LiteralText(token) is { } literal)
            {
                // Every dotted literal is evidence that a recorded code is thrown, but only
                // a literal in a declaring position states a code of its own. Elsewhere a
                // dotted string is an audit name, a field key, a host or a file name.
                var declares = Declares(token);

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

        foreach (var declared in classes)
        {
            var bases = declared is TypeDeclarationSyntax typed && typed.BaseList is not null
                ? typed.BaseList.Types.Select(type => BaseName(type.Type)).ToList()
                : [];

            var tags = Identifiers.ImplementsTag()
                .Matches(declared.GetLeadingTrivia().ToString())
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(match => match.Groups["id"].Value)
                .ToList();

            // A hosted service named for the host rather than the job still runs a use case
            // when its implements tag says so.
            var kind = Classify(declared.Identifier.ValueText, bases)
                ?? (tags.Count > 0 && bases.Any(baseName =>
                        baseName is "BackgroundService" or "IHostedService"
                        || baseName.StartsWith("BackgroundService<", StringComparison.Ordinal))
                    ? "job"
                    : null);

            if (kind is not null)
            {

                model.Reactions.Add(new ReactionInfo(
                    declared.Identifier.ValueText,
                    file,
                    tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
                    kind,
                    tags.FirstOrDefault() ?? UseCaseOfFolder(config, file),
                    RecordTag(declared),
                    RepositoriesOf(declared, tree, file),
                    RefusalsOf(declared, tree, file),
                    ThrownOf(declared, tree, file),
                    DispatchesOf(declared, tree, file, helpers),
                    DependencyCountOf(declared),
                    CollaboratorsOf(declared, tree, file, holders),
                    LoopReadsOf(declared, tree, file)));
            }
        }
    }

    private static void ReadEvent(
        ConsumerConfig config,
        string file,
        SyntaxTree tree,
        IReadOnlyList<BaseTypeDeclarationSyntax> classes,
        CodeModel model)
    {
        if (!IsUnder(config.DomainRoot, file)
            || !file.Contains($"{Path.DirectorySeparatorChar}Events{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return;
        }

        var name = Path.GetFileNameWithoutExtension(file);

        if (!name.EndsWith("Event", StringComparison.Ordinal) || name.Length <= 5)
        {
            return;
        }

        var declared = classes.FirstOrDefault();

        // An event class that states its record id in a string literal names its record
        // outright; the builder falls back to the class name when none is stated.
        var stated = declared?.DescendantTokens()
            .Select(LiteralText)
            .FirstOrDefault(text => text is not null && Identifiers.EventId().IsMatch(text));

        model.Events.Add(new DomainEventInfo(
            declared?.Identifier.ValueText ?? name,
            file,
            declared is null ? 1 : tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
            stated));
    }

    private static void ReadEndpoint(ConsumerConfig config, string file, SyntaxTree tree, SyntaxNode root, CodeModel model)
    {
        if (!Path.GetFileName(file).EndsWith("Endpoint.cs", StringComparison.Ordinal)
            || !IsUnder(config.WebApiRoot, file))
        {
            return;
        }

        var parts = Path.GetRelativePath(config.WebApiRoot, file)
                        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .SkipWhile(part => part == "Endpoints")
                        .ToArray();

        // Module/Aggregate/UseCase/XEndpoint.cs, or the single-aggregate flat form Module/UseCase/XEndpoint.cs.
        string useCase;

        if (parts.Length >= 4)
        {
            useCase = $"use-case/{Naming.Kebab(parts[0])}.{Naming.Kebab(parts[2])}";
        }
        else if (parts.Length == 3)
        {
            useCase = $"use-case/{Naming.Kebab(parts[0])}.{Naming.Kebab(parts[1])}";
        }
        else
        {
            return;
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
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

    private static bool IsUnder(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);

        return !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>
    ///     Reads the public methods of every aggregate root the file declares with the
    ///     domain events each one raises: a call to a method starting with <c>Raise</c>
    ///     that takes a constructed event, such as <c>RaiseDomainEvent(new XEvent())</c>.
    /// </summary>
    private static void ReadAggregateMethods(
        ConsumerConfig config,
        string file,
        SyntaxTree tree,
        IReadOnlyList<BaseTypeDeclarationSyntax> classes,
        CodeModel model)
    {
        if (!IsUnder(config.DomainRoot, file))
        {
            return;
        }

        foreach (var declared in classes)
        {
            if (declared is not TypeDeclarationSyntax typed || !DerivesFromAggregateRoot(typed))
            {
                continue;
            }

            foreach (var method in typed.Members.OfType<MethodDeclarationSyntax>())
            {
                if (!method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                {
                    continue;
                }

                var events = new List<string>();

                foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (CalledName(invocation) is not { } called || !called.StartsWith("Raise", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    foreach (var argument in invocation.ArgumentList.Arguments)
                    {
                        if (argument.Expression is BaseObjectCreationExpressionSyntax creation
                            && creation is ObjectCreationExpressionSyntax { Type: var type })
                        {
                            var raised = KindOf(SimpleTypeName(type));

                            if (raised.Length > 0 && !events.Contains(raised, StringComparer.Ordinal))
                            {
                                events.Add(raised);
                            }
                        }
                    }
                }

                model.AggregateMethods.Add(new AggregateMethod(
                    declared.Identifier.ValueText,
                    method.Identifier.ValueText,
                    events,
                    file,
                    tree.GetLineSpan(method.Span).StartLinePosition.Line + 1));
            }
        }
    }

    /// <summary>Whether the type derives from a class whose name starts with <c>AggregateRoot</c>.</summary>
    private static bool DerivesFromAggregateRoot(TypeDeclarationSyntax typed) =>
        typed.BaseList is not null
        && typed.BaseList.Types
            .Select(type => KindOf(BaseName(type.Type)))
            .Any(baseName => baseName.StartsWith("AggregateRoot", StringComparison.Ordinal));

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

    private static string? Classify(string name, List<string> bases)
    {
        // A job is named as one. A hosted service that wires the host at startup (a schema
        // check, a topology, a probe) is plumbing, and a type that only mentions recurring
        // work (its options, its model) runs nothing.
        if (name.EndsWith("Job", StringComparison.Ordinal)
            || name.EndsWith("Worker", StringComparison.Ordinal))
        {
            return "job";
        }

        if (bases.Any(baseName =>
            baseName.StartsWith("IEventHandler<", StringComparison.Ordinal)
            || baseName is "IDomainEventReaction"))
        {
            return "reaction";
        }

        if (name.EndsWith("Reaction", StringComparison.Ordinal))
        {
            return "reaction";
        }

        return null;
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
        List<CodeCollaborator> Collaborators,
        List<CodeLoopRead> LoopReads,
        List<CodeMemoryFilter> MemoryFilters);

    /// <summary>A written anchor on a reaction or job record.</summary>
    private static readonly System.Text.RegularExpressions.Regex RecordImplementsTag = new(
        @"<implements>\s*(?<id>(?:reaction|job)/[A-Za-z0-9][A-Za-z0-9._-]*)\s*</implements>",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>The reaction or job record id a class's anchor tag names, or null when it names none.</summary>
    private static string? RecordTag(BaseTypeDeclarationSyntax declared) =>
        RecordImplementsTag.Match(declared.GetLeadingTrivia().ToString()) is { Success: true } match
            ? match.Groups["id"].Value
            : null;

    /// <summary>The collected details of a handler file's handler class, or empty details when none parses.</summary>
    private static ClassDetails HandlerDetails(
        string file,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        bool isQuery)
    {
        SyntaxTree tree;

        try
        {
            tree = CSharpSyntaxTree.ParseText(SourceTree.Read(file), new CSharpParseOptions(LanguageVersion.Preview), file);
        }
        catch (IOException)
        {
            return new ClassDetails("", 1, [], [], [], [], "", 0, [], [], [], []);
        }

        var declared = tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault(type => type.Identifier.ValueText.EndsWith("Handler", StringComparison.Ordinal));

        if (declared is null)
        {
            return new ClassDetails("", 1, [], [], [], [], "", 0, [], [], [], []);
        }

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
    ///     The simple names of the domain aggregate roots: the classes under the domain
    ///     root deriving from a class whose name starts with <c>AggregateRoot</c>.
    /// </summary>
    private static HashSet<string> AggregateRootsOf(ConsumerConfig config)
    {
        var roots = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in SourceTree.Files(config.DomainRoot, ".cs"))
        {
            SyntaxTree tree;

            try
            {
                tree = CSharpSyntaxTree.ParseText(SourceTree.Read(file), new CSharpParseOptions(LanguageVersion.Preview), file);
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (DerivesFromAggregateRoot(declared))
                {
                    roots.Add(declared.Identifier.ValueText);
                }
            }
        }

        return roots;
    }

    /// <summary>
    ///     The simple names of the classes whose own constructor takes a repository: a
    ///     parameter whose kind starts with <c>I</c> and ends with <c>Repository</c>.
    /// </summary>
    private static HashSet<string> RepositoryHoldersOf(ConsumerConfig config)
    {
        var holders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot })
        {
            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                SyntaxTree tree;

                try
                {
                    tree = CSharpSyntaxTree.ParseText(SourceTree.Read(file), new CSharpParseOptions(LanguageVersion.Preview), file);
                }
                catch (IOException)
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
                if (MessageTarget(invocation, templateCounts: false) is { } issues)
                {
                    found.Add((receiver, new CodeDispatch("issues", issues.Target, "", file, line)));
                }
            }
            else if (PublishesThrough(kind))
            {
                if (MessageTarget(invocation, templateCounts: false) is { } publishes)
                {
                    found.Add((receiver, new CodeDispatch("publishes", publishes.Target, "", file, line)));
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

    /// <summary>Whether a receiver kind publishes events: a publisher or the outbox.</summary>
    private static bool PublishesThrough(string kind) =>
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
            || PublishesThrough(kind)
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
    private static Dictionary<string, List<HelperMethod>> IndexHelpers(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot })
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
                CollectHelpers(file, local);

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

    /// <summary>Collects the dispatching helper methods one file declares.</summary>
    private static void CollectHelpers(string file, List<(HelperMethod Helper, List<string> Aliases)> into)
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
