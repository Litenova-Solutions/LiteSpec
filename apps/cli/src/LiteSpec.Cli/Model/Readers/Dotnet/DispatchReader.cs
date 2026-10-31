using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     The dispatch calls a class makes: commands through <c>ICommandMediator</c>, events
///     through a publisher or the outbox, notifications through a messenger, notifier or
///     sender, and a call on an interface anchored to an external-system record as a calls edge.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class DispatchReader
{
    /// <summary>
    ///     The dispatch calls a class makes, direct and through a helper, in line order.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <returns>The dispatch calls the class makes, in line order.</returns>
    public static List<CodeDispatch> DispatchesOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        Dictionary<string, List<HelperMethod>> helpers,
        QualifiedKinds qualified)
    {
        var receivers = ClassDetailsReader.ReceiverTypes(declared);

        if (receivers.Count == 0)
        {
            return [];
        }

        var found = ScanDispatches(declared, receivers, tree, file, qualified)
            .Select(entry => entry.Dispatch)
            .ToList();
        found.AddRange(HelperDispatchReader.HelperDispatches(declared, tree, file, receivers, helpers, qualified));

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
    /// <param name="scope">The scope to scan.</param>
    /// <param name="receivers">The names the scope may call, each with its type.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <returns>The dispatches the scope makes through its receivers.</returns>
    public static List<(string Receiver, CodeDispatch Dispatch)> ScanDispatches(
        SyntaxNode scope,
        Dictionary<string, string> receivers,
        SyntaxTree tree,
        string file,
        QualifiedKinds qualified)
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

            var kind = DotnetNames.KindOf(type);
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
            else if (!ExcludedReceiver(kind) && qualified.IsExternalPort(scope, kind))
            {
                found.Add((receiver, new CodeDispatch("calls", method, kind, file, line)));
            }
        }

        return found;
    }

    /// <summary>Whether a receiver kind emits events: a publisher or the outbox.</summary>
    /// <param name="kind">The receiver kind.</param>
    /// <returns>True when a receiver kind emits events.</returns>
    public static bool EmitsThrough(string kind) =>
        kind.EndsWith("Publisher", StringComparison.Ordinal)
        || kind.Contains("Outbox", StringComparison.Ordinal);

    /// <summary>Whether a receiver kind sends notifications: a messenger, notifier or sender.</summary>
    /// <param name="kind">The receiver kind.</param>
    /// <returns>True when a receiver kind sends notifications.</returns>
    public static bool SendsThrough(string kind) =>
        kind.Contains("Messenger", StringComparison.Ordinal)
        || kind.EndsWith("Notifier", StringComparison.Ordinal)
        || kind.EndsWith("Sender", StringComparison.Ordinal);

    /// <summary>Whether a held receiver kind can carry a dispatch the checks read.</summary>
    /// <param name="type">The held receiver type.</param>
    /// <param name="scope">The scope the name is written in.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <returns>True when a held receiver kind can carry a dispatch the checks read.</returns>
    public static bool DispatchCapable(string type, SyntaxNode scope, QualifiedKinds qualified)
    {
        var kind = DotnetNames.KindOf(type);

        return kind == "ICommandMediator"
            || EmitsThrough(kind)
            || SendsThrough(kind)
            || (!ExcludedReceiver(kind) && qualified.IsExternalPort(scope, kind));
    }

    /// <summary>
    ///     The message a dispatch call carries: the constructed type, or the template key when
    ///     the call sends through the messenger. A topic or key first with the message second
    ///     reads the message. Null when the call names neither.
    /// </summary>
    /// <param name="invocation">The dispatch invocation.</param>
    /// <param name="templateCounts">Whether a template key first names the message.</param>
    /// <returns>The message a dispatch call carries, or null when it names neither.</returns>
    public static (string Target, string Detail)? MessageTarget(InvocationExpressionSyntax invocation, bool templateCounts)
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
    /// <param name="send">The send invocation.</param>
    /// <returns>The template key the send's enclosing method renders, or null when it renders none or several.</returns>
    public static string? RenderedTemplate(InvocationExpressionSyntax send)
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
    /// <param name="expression">The argument expression.</param>
    /// <param name="invocation">The dispatch invocation.</param>
    /// <returns>The constructed type an argument carries, or null when it carries none.</returns>
    public static string? ConstructionOf(ExpressionSyntax expression, InvocationExpressionSyntax invocation) =>
        expression switch
        {
            ObjectCreationExpressionSyntax creation => DotnetNames.SimpleTypeName(creation.Type),
            IdentifierNameSyntax variable => LocalConstruction(invocation, variable.Identifier.ValueText),
            _ => null,
        };

    /// <summary>The type a local builds before the call, for a message held in a local.</summary>
    /// <param name="invocation">The dispatch invocation.</param>
    /// <param name="name">The local's name.</param>
    /// <returns>The type a local builds before the call, or null when it builds none.</returns>
    public static string? LocalConstruction(InvocationExpressionSyntax invocation, string name)
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
                return DotnetNames.SimpleTypeName(creation.Type);
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether an injected type is infrastructure rather than an external port: repositories,
    ///     mediators, publishers, messengers, sessions, clocks, loggers, validators and the rest
    ///     a handler takes without taking an edge to an external system.
    /// </summary>
    /// <param name="type">The injected type name.</param>
    /// <returns>True when an injected type is infrastructure rather than an external port.</returns>
    public static bool ExcludedReceiver(string type)
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
