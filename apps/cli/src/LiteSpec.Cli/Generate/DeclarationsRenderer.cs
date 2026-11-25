namespace LiteSpec.Cli.Generate;

/// <summary>
///     Renders one module's LiteBus message declarations: a partial class implementing
///     <c>IMultiMessageDefinition</c> that declares, for each command and query of the
///     module's use cases, the authorization, audit and idempotency its record states.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class DeclarationsRenderer
{
    /// <summary>One message the catalog source declares: its identity with the declaration calls its record renders.</summary>
    /// <param name="UseCaseId">The use case identifier the message answers.</param>
    /// <param name="MessageType">The simple message type name, such as BorrowCopyCommand.</param>
    /// <param name="MessageNamespace">The C# namespace declaring the message type.</param>
    /// <param name="Kind">The use case kind: command or query.</param>
    /// <param name="Authorization">The authorization call, starting with a dot.</param>
    /// <param name="Audit">The audit call, starting with a dot, or null when the record states no audit.</param>
    /// <param name="Idempotency">The idempotency call, starting with a dot, or null when the record states none.</param>
    public sealed record DeclaredMessage(
        string UseCaseId,
        string MessageType,
        string MessageNamespace,
        string Kind,
        string Authorization,
        string? Audit,
        string? Idempotency);

    /// <summary>One part of an idempotency key template: literal text or one message property.</summary>
    /// <param name="Literal">The literal text, or null when the part names a property.</param>
    /// <param name="Property">The message property name, or null when the part is literal text.</param>
    public sealed record KeyPart(string? Literal, string? Property);

    /// <summary>The authorization call for a message the records authorize under a policy.</summary>
    /// <param name="policy">The policy name the project's guard evaluates.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string AuthorizedCall(string policy) =>
        $".Authorized(\"{GenerateNaming.EscapeLiteral(policy)}\")";

    /// <summary>The audit call for a message the records audit, without a target read off the message.</summary>
    /// <param name="action">The use case identifier written to the audit record.</param>
    /// <param name="category">The audit category, or null when the record states none.</param>
    /// <param name="targetKind">The kind of the audited record, or null when the record states none.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string AuditedCall(string action, string? category, string? targetKind) =>
        $".Audited(\"{GenerateNaming.EscapeLiteral(action)}\"{AuditArguments(category, targetKind)})";

    /// <summary>The audit call for a message the records audit with its target read off the message.</summary>
    /// <param name="action">The use case identifier written to the audit record.</param>
    /// <param name="category">The audit category, or null when the record states none.</param>
    /// <param name="targetKind">The kind of the audited record, or null when the record states none.</param>
    /// <param name="messageType">The simple message type name the selector reads.</param>
    /// <param name="parameter">The lambda parameter name: command or query.</param>
    /// <param name="targetProperty">The message property carrying the audited record.</param>
    /// <param name="targetIsText">True when the property is already text; otherwise the selector converts it.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string AuditedWithTargetCall(
        string action,
        string? category,
        string? targetKind,
        string messageType,
        string parameter,
        string targetProperty,
        bool targetIsText)
    {
        var selector = targetIsText ? $"{parameter}.{targetProperty}" : $"{parameter}.{targetProperty}.ToString()";

        return $".Declare<AuditDeclaration>(AuditDeclaration.Audited(\"{GenerateNaming.EscapeLiteral(action)}\"{AuditArguments(category, targetKind)}).WithTargetFrom<{messageType}>({parameter} => {selector}))";
    }

    /// <summary>The audit call for a message the records audit with a composite target read off the message.</summary>
    /// <param name="action">The use case identifier written to the audit record.</param>
    /// <param name="category">The audit category, or null when the record states none.</param>
    /// <param name="targetKind">The kind of the audited record, or null when the record states none.</param>
    /// <param name="messageType">The simple message type name the selector reads.</param>
    /// <param name="parameter">The lambda parameter name: command or query.</param>
    /// <param name="parts">The message properties forming the composite target, in listed order, each with whether it already reads as text.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string AuditedWithCompositeTargetCall(
        string action,
        string? category,
        string? targetKind,
        string messageType,
        string parameter,
        IReadOnlyList<(string Property, bool IsText)> parts)
    {
        var joined = string.Join(
            "/",
            parts.Select(part => part.IsText
                ? $"{{{parameter}.{part.Property}}}"
                : $"{{{parameter}.{part.Property}.ToString()}}"));

        return $".Declare<AuditDeclaration>(AuditDeclaration.Audited(\"{GenerateNaming.EscapeLiteral(action)}\"{AuditArguments(category, targetKind)}).WithTargetFrom<{messageType}>({parameter} => $\"{joined}\"))";
    }

    /// <summary>The audit call for a message the records exempt from auditing.</summary>
    /// <param name="rationale">The recorded reason the message produces no audit record.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string NotAuditedCall(string rationale) =>
        $".NotAudited(\"{GenerateNaming.EscapeLiteral(rationale)}\")";

    /// <summary>The idempotency call for a command the records key.</summary>
    /// <param name="messageType">The simple message type name the key selector reads.</param>
    /// <param name="parameter">The lambda parameter name: command.</param>
    /// <param name="parts">The key template parts in template order.</param>
    /// <param name="scope">The key scope separating the message's keys from every other message's.</param>
    /// <param name="replayResult">True when a recognized repeat returns the first result.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string KeyedIdempotencyCall(
        string messageType,
        string parameter,
        IReadOnlyList<KeyPart> parts,
        string scope,
        bool replayResult)
    {
        var replay = replayResult ? "true" : "false";

        return $".Declare(IdempotencyDeclaration.KeyedBy<{messageType}>({parameter} => {KeyExpression(parameter, parts)}) with {{ Scope = \"{GenerateNaming.EscapeLiteral(scope)}\", ReplayResult = {replay} }})";
    }

    /// <summary>The idempotency call for a command the records exempt from keying.</summary>
    /// <param name="rationale">The recorded reason the command keys nothing.</param>
    /// <returns>The call text, starting with a dot.</returns>
    public static string IdempotencyExemptCall(string rationale) =>
        $".Exempt<IdempotencyDeclaration>(\"{GenerateNaming.EscapeLiteral(rationale)}\")";

    /// <summary>
    ///     Renders one module's declarations as C#: an internal sealed partial class implementing
    ///     <c>IMultiMessageDefinition</c>, with one <c>Describe</c> per message in use case order.
    ///     A hand-written sibling file may add members to the generated type.
    /// </summary>
    /// <param name="module">The record module name.</param>
    /// <param name="messages">The module's declared messages.</param>
    /// <param name="namespace">The C# namespace of the generated class.</param>
    /// <returns>The file text with LF line endings.</returns>
    public static string RenderDotnet(string module, IReadOnlyList<DeclaredMessage> messages, string @namespace)
    {
        var ordered = messages.OrderBy(message => message.UseCaseId, StringComparer.Ordinal).ToList();
        var lines = new List<string>
        {
            GenerateNaming.HeaderLineFor(GenerateNaming.CurrentVersion()),
            "// <auto-generated/>",
            "",
        };

        if (ordered.Any(message => message.Audit is not null))
        {
            lines.Add("using LiteBus.Auditing.Abstractions;");
        }

        lines.Add("using LiteBus.Messaging.Abstractions;");

        foreach (var messageNamespace in ordered
            .Select(message => message.MessageNamespace)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            lines.Add($"using {messageNamespace};");
        }

        var name = GenerateNaming.ModulePascal(module);

        lines.Add("");
        lines.Add($"namespace {@namespace};");
        lines.Add("");
        lines.Add($"/// <summary>Declares every {module} message from its use case record: authorization, audit and idempotency.</summary>");
        lines.Add($"internal sealed partial class {name}Declarations : IMultiMessageDefinition");
        lines.Add("{");
        lines.Add("    /// <inheritdoc />");
        lines.Add("    public void Describe(IMessageDeclarationCatalog catalog)");
        lines.Add("    {");
        lines.Add("        catalog");

        for (var index = 0; index < ordered.Count; index++)
        {
            var message = ordered[index];
            var last = index == ordered.Count - 1;
            var calls = new List<string> { message.Authorization };

            if (message.Audit is not null)
            {
                calls.Add(message.Audit);
            }

            if (message.Idempotency is not null)
            {
                calls.Add(message.Idempotency);
            }

            if (calls.Count == 1)
            {
                lines.Add($"            .Describe<{message.MessageType}>(declarations => declarations{calls[0]}){(last ? ";" : "")}");
                continue;
            }

            lines.Add($"            .Describe<{message.MessageType}>(declarations => declarations");

            for (var call = 0; call < calls.Count; call++)
            {
                var final = call == calls.Count - 1;

                lines.Add($"                {calls[call]}{(final ? (last ? ");" : ")") : "")}");
            }
        }

        lines.Add("    }");
        lines.Add("}");

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The optional category and target kind arguments of an Audited call.</summary>
    /// <param name="category">The audit category, or null when the record states none.</param>
    /// <param name="targetKind">The kind of the audited record, or null when the record states none.</param>
    /// <returns>The argument text, each argument leading with a comma and a space.</returns>
    private static string AuditArguments(string? category, string? targetKind)
    {
        var arguments = "";

        if (category is not null)
        {
            arguments += $", category: \"{GenerateNaming.EscapeLiteral(category)}\"";
        }

        if (targetKind is not null)
        {
            arguments += $", targetKind: \"{GenerateNaming.EscapeLiteral(targetKind)}\"";
        }

        return arguments;
    }

    /// <summary>The key selector body: a plain literal when the template names no property, else an interpolated string over the message's properties.</summary>
    /// <param name="parameter">The lambda parameter name: command.</param>
    /// <param name="parts">The key template parts in template order.</param>
    /// <returns>The selector body text.</returns>
    private static string KeyExpression(string parameter, IReadOnlyList<KeyPart> parts)
    {
        if (parts.All(part => part.Property is null))
        {
            return $"\"{GenerateNaming.EscapeLiteral(string.Concat(parts.Select(part => part.Literal ?? "")))}\"";
        }

        var body = string.Concat(parts.Select(part =>
            part.Property is not null
                ? $"{{{parameter}.{part.Property}}}"
                : GenerateNaming.EscapeLiteral(part.Literal ?? "")
                    .Replace("{", "{{", StringComparison.Ordinal)
                    .Replace("}", "}}", StringComparison.Ordinal)));

        return $"$\"{body}\"";
    }
}
