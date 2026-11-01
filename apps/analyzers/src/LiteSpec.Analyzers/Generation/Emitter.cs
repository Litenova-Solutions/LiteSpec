using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Analyzers.Rules;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// Writes the generator's sources from the records, the marked types and the host: the mark
/// attributes, the event and exception value parts, the assembly registry, the permission
/// constants and the role entries. Every value is written only where no member of that name is
/// declared in source, and only where its record is known.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
/// <follows>litespec/core/access#state-the-scope-of-every-permission</follows>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class Emitter
{
    /// <summary>Writes every source the inputs call for, or nothing without a usable project file, with no records and no marks, or in an opted-out project.</summary>
    /// <param name="context">The source production context.</param>
    /// <param name="records">The record facts.</param>
    /// <param name="types">The marked types.</param>
    /// <param name="host">The compilation facts.</param>
    public static void Execute(
        SourceProductionContext context,
        RecordModel records,
        ImmutableArray<MarkedType> types,
        HostModel host)
    {
        if (host.Layer == Layer.None)
        {
            return;
        }

        if (!records.Valid)
        {
            return;
        }

        if (records.EventIds.IsEmpty
            && records.RuleCodes.IsEmpty
            && records.Permissions.IsEmpty
            && records.Roles.IsEmpty
            && records.Grants.IsEmpty
            && types.IsEmpty)
        {
            return;
        }

        var ordered = OrderedByFullName(types);

        if (records.Style == MarkStyle.Attribute)
        {
            context.AddSource("LiteSpecAttributes.g.cs", AttributesText(host.XunitV3));
        }

        var events = new HashSet<string>(records.EventIds, StringComparer.Ordinal);
        var codes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var rule in records.RuleCodes)
        {
            codes[rule.RuleId] = rule.Code;
        }

        var grants = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var grant in records.Grants)
        {
            grants[grant.UseCaseId] = grant.Grant;
        }

        foreach (var type in ordered)
        {
            if (type.Kind == CodeKind.DomainEvent
                && type.IsPartial
                && !type.HasClassification
                && FirstWithPrefix(type.MarkIds, "event/") is string eventId
                && events.Contains(eventId))
            {
                context.AddSource(HintName("LiteSpecEvent", type), EventText(type, eventId));
            }
            else if (type.Kind == CodeKind.RuleException
                && type.IsPartial
                && !type.HasCode
                && FirstWithPrefix(type.MarkIds, "rule/") is string ruleId
                && codes.TryGetValue(ruleId, out var code))
            {
                context.AddSource(HintName("LiteSpecException", type), ExceptionText(type, code));
            }
        }

        EmitRegistry(context, ordered, grants, host.Layer);

        if (host.Layer == Layer.Domain
            || host.Layer == Layer.Application
            || host.Layer == Layer.Infrastructure
            || host.Layer == Layer.WebApi)
        {
            EmitPermissions(context, records);
            EmitRoles(context, records);
        }
    }

    /// <summary>
    /// Names the permission constant: each kebab word with its first letter capitalized, keeping
    /// letters and digits only. A name that stays empty, or one the identifier cannot carry, yields
    /// null and the permission is skipped; the checks own the malformed record.
    /// </summary>
    /// <param name="id">The permission identifier.</param>
    /// <returns>The constant name, or null when the identifier carries none.</returns>
    internal static string? ConstantName(string id)
    {
        if (!Identifiers.TrySplit(id, out var prefix, out var home, out var name)
            || prefix != "permission"
            || home is not null
            || name.Length == 0)
        {
            return null;
        }

        return PascalName(name);
    }

    /// <summary>
    /// Names the role constant from the role name, as <see cref="ConstantName"/> names a
    /// permission. An identifier that carries no role name yields null and the role is skipped;
    /// the checks own the malformed record.
    /// </summary>
    /// <param name="id">The role identifier.</param>
    /// <returns>The constant name, or null when the identifier carries none.</returns>
    internal static string? RoleConstantName(string id)
    {
        if (!Identifiers.TrySplit(id, out var prefix, out var home, out var name)
            || prefix != "role"
            || home is not null
            || name.Length == 0)
        {
            return null;
        }

        return PascalName(name);
    }

    /// <summary>
    /// Names the endpoint operation from the use-case name: the part after the module dot, with
    /// each kebab word capitalized. An identifier that carries no use-case name yields null and
    /// the endpoint takes no operation entry; the checks own the malformed mark.
    /// </summary>
    /// <param name="id">The use-case identifier.</param>
    /// <returns>The operation name, or null when the identifier carries none.</returns>
    internal static string? OperationName(string id)
    {
        if (!Identifiers.TrySplit(id, out var prefix, out var home, out var name)
            || prefix != "use-case"
            || home is null
            || name.Length == 0)
        {
            return null;
        }

        return PascalName(name);
    }

    /// <summary>
    /// Capitalizes each kebab word of the name, keeping letters and digits only. A name that
    /// stays empty yields null, and one opening with a digit takes a leading underscore, so the
    /// result always carries an identifier.
    /// </summary>
    /// <param name="name">The kebab-case name.</param>
    /// <returns>The PascalCase name, or null when the name carries none.</returns>
    private static string? PascalName(string name)
    {
        var built = new StringBuilder();
        var words = name.Split(['-'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var kept = new StringBuilder();

            foreach (var character in word)
            {
                if (IsAsciiLetterOrDigit(character))
                {
                    kept.Append(character);
                }
            }

            if (kept.Length == 0)
            {
                continue;
            }

            kept[0] = char.ToUpperInvariant(kept[0]);
            built.Append(kept.ToString());
        }

        if (built.Length == 0)
        {
            return null;
        }

        var text = built.ToString();

        if (text[0] >= '0' && text[0] <= '9')
        {
            text = "_" + text;
        }

        return text;
    }

    /// <summary>Returns the by full name.</summary>
    /// <param name="types">The types collected so far, in discovery order.</param>
    /// <returns>The by full name, in the order this method builds them, or an empty collection when none apply.</returns>
    private static List<MarkedType> OrderedByFullName(ImmutableArray<MarkedType> types)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<MarkedType>();

        foreach (var type in types)
        {
            if (seen.Add(type.FullName))
            {
                ordered.Add(type);
            }
        }

        ordered.Sort(static (left, right) => string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));

        return ordered;
    }

    /// <summary>Returns the with prefix.</summary>
    /// <param name="ids">The identifiers collected so far, without duplicates.</param>
    /// <param name="prefix">The text written before each generated entry.</param>
    /// <returns>The with prefix as text, or null when none applies.</returns>
    private static string? FirstWithPrefix(ImmutableArray<string> ids, string prefix)
    {
        foreach (var id in ids)
        {
            if (id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>Returns the name.</summary>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The name as text.</returns>
    private static string HintName(string kind, MarkedType type) =>
        kind + "." + type.FullName.Replace('`', '_') + ".g.cs";

    /// <summary>Emits the registry.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    /// <param name="grants">The access grants read from the record pages, by use-case identifier.</param>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    private static void EmitRegistry(
        SourceProductionContext context,
        List<MarkedType> types,
        Dictionary<string, string> grants,
        Layer layer)
    {
        var messages = MessageUseCasesOf(context, types);
        var operations = OperationUseCasesOf(types);
        context.AddSource("LiteSpecRegistry.g.cs", RegistryText(types, grants, messages, operations, layer == Layer.WebApi));
    }

    /// <summary>
    /// Maps each message one marked handler serves to that handler's use case: a class with
    /// exactly one use-case mark whose handler interface names a message. A message two marked
    /// handlers serve with different use cases reports LS5011 naming both handlers and takes no
    /// entry; two handlers sharing one use case take one entry.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="types">The marked types in full-name order.</param>
    /// <returns>The message entries in message order.</returns>
    private static List<MessageUseCase> MessageUseCasesOf(SourceProductionContext context, List<MarkedType> types)
    {
        var grouped = new Dictionary<string, List<MessageClaim>>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            if (type.MessageFullName.Length == 0
                || type.UseCaseMarkCount != 1
                || FirstWithPrefix(type.MarkIds, "use-case/") is not string useCase)
            {
                continue;
            }

            if (!grouped.TryGetValue(type.MessageFullName, out var group))
            {
                group = new List<MessageClaim>();
                grouped[type.MessageFullName] = group;
            }

            group.Add(new MessageClaim(type.FullName, useCase));
        }

        var messages = new List<MessageUseCase>();

        foreach (var pair in grouped)
        {
            var first = pair.Value[0];
            MessageClaim? second = null;

            foreach (var candidate in pair.Value)
            {
                if (!string.Equals(candidate.UseCase, first.UseCase, StringComparison.Ordinal))
                {
                    second = candidate;
                    break;
                }
            }

            if (second is null)
            {
                messages.Add(new MessageUseCase(pair.Key, first.UseCase));
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS5011,
                Location.None,
                pair.Key,
                first.Handler,
                second.Handler));
        }

        messages.Sort(static (left, right) => string.Compare(left.Message, right.Message, StringComparison.Ordinal));

        return messages;
    }

    /// <summary>One handler claiming a message for its use case.</summary>
    private sealed class MessageClaim
    {
        /// <summary>Initializes a new instance of the <see cref="MessageClaim"/> class.Creates a message claim.</summary>
        /// <param name="handler">The handler's full name.</param>
        /// <param name="useCase">The handler's use case.</param>
        public MessageClaim(string handler, string useCase)
        {
            Handler = handler;
            UseCase = useCase;
        }

        /// <summary>Gets the handler's full name.</summary>
        public string Handler { get; }

        /// <summary>Gets the handler's use case.</summary>
        public string UseCase { get; }
    }

    /// <summary>One message type with the use case its marked handler serves.</summary>
    private sealed class MessageUseCase
    {
        /// <summary>Initializes a new instance of the <see cref="MessageUseCase"/> class.Creates a message entry.</summary>
        /// <param name="message">The fully qualified message name.</param>
        /// <param name="useCase">The handler's use case.</param>
        public MessageUseCase(string message, string useCase)
        {
            Message = message;
            UseCase = useCase;
        }

        /// <summary>Gets the fully qualified message name.</summary>
        public string Message { get; }

        /// <summary>Gets the handler's use case.</summary>
        public string UseCase { get; }
    }

    /// <summary>
    /// Maps each operation name to its use case: the PascalCase use-case name of every use case
    /// an endpoint in this assembly carries in an implements mark. Use-case names are unique
    /// project-wide, so each operation name maps to one use case; the first endpoint in
    /// full-name order wins when two marks produce one name.
    /// </summary>
    /// <param name="types">The marked types in full-name order.</param>
    /// <returns>The operation entries in operation order.</returns>
    private static List<OperationUseCase> OperationUseCasesOf(List<MarkedType> types)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            if (type.Kind != CodeKind.Endpoint
                || FirstWithPrefix(type.MarkIds, "use-case/") is not string useCase
                || OperationName(useCase) is not string operation)
            {
                continue;
            }

            if (!entries.ContainsKey(operation))
            {
                entries.Add(operation, useCase);
            }
        }

        var operations = new List<OperationUseCase>();

        foreach (var pair in entries)
        {
            operations.Add(new OperationUseCase(pair.Key, pair.Value));
        }

        operations.Sort(static (left, right) => string.Compare(left.Operation, right.Operation, StringComparison.Ordinal));

        return operations;
    }

    /// <summary>One operation name with the use case its marked endpoint serves.</summary>
    private sealed class OperationUseCase
    {
        /// <summary>Initializes a new instance of the <see cref="OperationUseCase"/> class.Creates an operation entry.</summary>
        /// <param name="operation">The PascalCase use-case name.</param>
        /// <param name="useCase">The endpoint's use case.</param>
        public OperationUseCase(string operation, string useCase)
        {
            Operation = operation;
            UseCase = useCase;
        }

        /// <summary>Gets the PascalCase use-case name.</summary>
        public string Operation { get; }

        /// <summary>Gets the endpoint's use case.</summary>
        public string UseCase { get; }
    }

    /// <summary>Emits the permissions.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="records">The record model the generator reads the declared values from.</param>
    private static void EmitPermissions(SourceProductionContext context, RecordModel records)
    {
        var named = new List<NamedPermission>();

        foreach (var permission in records.Permissions)
        {
            var name = ConstantName(permission.Id);

            if (name is not null)
            {
                named.Add(new NamedPermission(name, permission, Reads(permission.Id, records)));
            }
        }

        named.Sort(static (left, right) =>
        {
            var names = string.Compare(left.Name, right.Name, StringComparison.Ordinal);

            return names != 0
                ? names
                : string.Compare(left.Facts.Id, right.Facts.Id, StringComparison.Ordinal);
        });

        var collisions = 0;

        for (var index = 1; index < named.Count; index++)
        {
            if (named[index].Name != named[index - 1].Name
                || named[index].Facts.Id == named[index - 1].Facts.Id)
            {
                continue;
            }

            collisions++;

            if (index == 1 || named[index - 1].Name != named[index - 2].Name)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnostics.PermissionCollision,
                    Location.None,
                    named[index].Name,
                    named[index - 1].Facts.Id,
                    named[index].Facts.Id));
            }
        }

        if (collisions > 0)
        {
            return;
        }

        foreach (var permission in named)
        {
            if (VerbOf(permission.Facts.Id, records) is string verb
                && !UseCaseVerbs.IsPermissionDeclared(verb, records.Verbs, records.QueryVerbs, records.PhrasalVerbs))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS5010,
                    Location.None,
                    permission.Facts.Id,
                    verb));
            }
        }

        context.AddSource("LiteSpecPermissions.g.cs", PermissionsText(named));
    }

    /// <summary>
    /// Whether the permission only reads: its name opens with a reading verb from the use-case
    /// vocabulary. A permission whose first word is no known verb reads false, and the caller
    /// reports it.
    /// </summary>
    /// <param name="id">The permission identifier.</param>
    /// <param name="records">The record facts with the project's vocabulary additions.</param>
    /// <returns>Whether the permission only reads.</returns>
    private static bool Reads(string id, RecordModel records)
    {
        if (VerbOf(id, records) is not string verb
            || !UseCaseVerbs.IsPermissionDeclared(verb, records.Verbs, records.QueryVerbs, records.PhrasalVerbs))
        {
            return false;
        }

        return UseCaseVerbs.PermissionKindOf(verb, records.QueryVerbs) == UseCaseVerbs.VerbKind.Query;
    }

    /// <summary>The opening verb of the permission name, or null when the identifier carries none.</summary>
    /// <param name="id">The permission identifier.</param>
    /// <param name="records">The record facts with the project's vocabulary additions.</param>
    /// <returns>The opening verb, or null.</returns>
    private static string? VerbOf(string id, RecordModel records)
    {
        if (!Identifiers.TrySplit(id, out var prefix, out var home, out var name)
            || prefix != "permission"
            || home is not null
            || name.Length == 0)
        {
            return null;
        }

        return UseCaseVerbs.VerbOf(name, records.PhrasalVerbs);
    }

    /// <summary>One permission with its constant name and whether it only reads.</summary>
    private sealed class NamedPermission
    {
        /// <summary>Initializes a new instance of the <see cref="NamedPermission"/> class.Creates a named permission.</summary>
        /// <param name="name">The constant name.</param>
        /// <param name="facts">The facts the generated entry carries.</param>
        /// <param name="reads">Whether the permission only reads.</param>
        public NamedPermission(string name, PermissionFacts facts, bool reads)
        {
            Name = name;
            Facts = facts;
            Reads = reads;
        }

        /// <summary>Gets the constant name.</summary>
        public string Name { get; }

        /// <summary>Gets the facts the generated entry carries.</summary>
        public PermissionFacts Facts { get; }

        /// <summary>Gets a value indicating whether whether the permission only reads.</summary>
        public bool Reads { get; }
    }

    /// <summary>Emits the roles.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="records">The record model the generator reads the declared values from.</param>
    private static void EmitRoles(SourceProductionContext context, RecordModel records)
    {
        var named = new List<NamedRole>();

        foreach (var role in records.Roles)
        {
            var name = RoleConstantName(role.Id);

            if (name is not null)
            {
                named.Add(new NamedRole(name, role));
            }
        }

        named.Sort(static (left, right) =>
        {
            var names = string.Compare(left.Name, right.Name, StringComparison.Ordinal);

            return names != 0
                ? names
                : string.Compare(left.Facts.Id, right.Facts.Id, StringComparison.Ordinal);
        });

        var collisions = 0;

        for (var index = 1; index < named.Count; index++)
        {
            if (named[index].Name != named[index - 1].Name
                || named[index].Facts.Id == named[index - 1].Facts.Id)
            {
                continue;
            }

            collisions++;

            if (index == 1 || named[index - 1].Name != named[index - 2].Name)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnostics.RoleCollision,
                    Location.None,
                    named[index].Name,
                    named[index - 1].Facts.Id,
                    named[index].Facts.Id));
            }
        }

        if (collisions > 0)
        {
            return;
        }

        var declared = new HashSet<string>(StringComparer.Ordinal);

        foreach (var permission in records.Permissions)
        {
            declared.Add(permission.Id);
        }

        foreach (var role in named)
        {
            foreach (var allowed in role.Facts.Allows)
            {
                if (!declared.Contains(allowed))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.RoleAllowsUndeclaredPermission,
                        Location.None,
                        role.Facts.Id,
                        allowed));
                }
            }
        }

        context.AddSource("LiteSpecRoles.g.cs", RolesText(named));
    }

    /// <summary>One role with its constant name.</summary>
    private sealed class NamedRole
    {
        /// <summary>Initializes a new instance of the <see cref="NamedRole"/> class.Creates a named role.</summary>
        /// <param name="name">The constant name.</param>
        /// <param name="facts">The facts the generated entry carries.</param>
        public NamedRole(string name, RoleFacts facts)
        {
            Name = name;
            Facts = facts;
        }

        /// <summary>Gets the constant name.</summary>
        public string Name { get; }

        /// <summary>Gets the facts the generated entry carries.</summary>
        public RoleFacts Facts { get; }
    }

    /// <summary>Returns the attribute declaration text, in the xUnit form when requested.</summary>
    /// <param name="xunit">Whether the generated test attributes use the xUnit form instead of the baseline form.</param>
    /// <returns>The attribute declaration text.</returns>
    private static string AttributesText(bool xunit)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The mark attributes the analyzer package generates for the attribute mark style.");
        text.AppendLine();
        text.AppendLine("namespace Microsoft.CodeAnalysis");
        text.AppendLine("{");
        text.AppendLine("    [Microsoft.CodeAnalysis.EmbeddedAttribute]");
        text.AppendLine("    internal sealed partial class EmbeddedAttribute : System.Attribute");
        text.AppendLine("    {");
        text.AppendLine("    }");
        text.AppendLine("}");
        text.AppendLine();
        text.AppendLine("namespace LiteSpec");
        text.AppendLine("{");
        text.AppendLine("    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct | System.AttributeTargets.Method, AllowMultiple = true)]");
        text.AppendLine("    [Microsoft.CodeAnalysis.EmbeddedAttribute]");
        text.AppendLine("    internal sealed class ImplementsAttribute : System.Attribute");
        text.AppendLine("    {");
        text.AppendLine("        public ImplementsAttribute(string recordId)");
        text.AppendLine("        {");
        text.AppendLine("            RecordId = recordId;");
        text.AppendLine("        }");
        text.AppendLine();
        text.AppendLine("        public string RecordId { get; }");
        text.AppendLine("    }");
        text.AppendLine();
        text.AppendLine("    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct | System.AttributeTargets.Method, AllowMultiple = true)]");
        text.AppendLine("    [Microsoft.CodeAnalysis.EmbeddedAttribute]");
        text.AppendLine("    internal sealed class FollowsAttribute : System.Attribute");
        text.AppendLine("    {");
        text.AppendLine("        public FollowsAttribute(string recordId)");
        text.AppendLine("        {");
        text.AppendLine("            RecordId = recordId;");
        text.AppendLine("        }");
        text.AppendLine();
        text.AppendLine("        public string RecordId { get; }");
        text.AppendLine("    }");
        text.AppendLine();
        text.AppendLine("    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]");
        text.AppendLine("    [Microsoft.CodeAnalysis.EmbeddedAttribute]");

        if (xunit)
        {
            text.AppendLine("    internal sealed class ProvesAttribute : System.Attribute, global::Xunit.v3.ITraitAttribute");
        }
        else
        {
            text.AppendLine("    internal sealed class ProvesAttribute : System.Attribute");
        }

        text.AppendLine("    {");
        text.AppendLine("        public ProvesAttribute(string recordId)");
        text.AppendLine("        {");
        text.AppendLine("            RecordId = recordId;");
        text.AppendLine("        }");
        text.AppendLine();
        text.AppendLine("        public string RecordId { get; }");

        if (xunit)
        {
            text.AppendLine();
            text.AppendLine("        public System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, string>> GetTraits()");
            text.AppendLine("        {");
            text.AppendLine("            return new System.Collections.Generic.KeyValuePair<string, string>[]");
            text.AppendLine("            {");
            text.AppendLine("                new System.Collections.Generic.KeyValuePair<string, string>(\"Scenario\", RecordId),");
            text.AppendLine("            };");
            text.AppendLine("        }");
        }

        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }

    /// <summary>Returns the generated partial declaration text for the marked event.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="eventId">The event identifier to resolve.</param>
    /// <returns>The partial declaration text for the event.</returns>
    private static string EventText(MarkedType type, string eventId)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The event classification the analyzer package generates from the event record.");
        text.AppendLine();

        return text.Append(PartialText(
            type,
            "public const string Classification = \"" + Escape(eventId) + "\";")).ToString();
    }

    /// <summary>Returns the generated partial declaration text for the marked exception.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="code">The finding code that names the outcome, such as mark-missing.</param>
    /// <returns>The partial declaration text for the exception.</returns>
    private static string ExceptionText(MarkedType type, string code)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The refusal code the analyzer package generates from the rule's refusal result.");
        text.AppendLine();
        var member = type.BaseHasVirtualCode
            ? "public override string Code => \"" + Escape(code) + "\";"
            : "public string Code => \"" + Escape(code) + "\";";

        return text.Append(PartialText(type, member)).ToString();
    }

    /// <summary>Returns the generated text that adds the runtime value to the marked type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="member">The member declaration under inspection.</param>
    /// <returns>The generated runtime value text.</returns>
    private static string PartialText(MarkedType type, string member)
    {
        var text = new StringBuilder();
        var indent = string.Empty;

        if (type.Namespace.Length > 0)
        {
            text.AppendLine("namespace " + type.Namespace);
            text.AppendLine("{");
            indent = "    ";
        }

        foreach (var enclosing in type.Nesting)
        {
            text.AppendLine(indent + enclosing);
            text.AppendLine(indent + "{");
            indent += "    ";
        }

        text.AppendLine(indent + "partial " + type.Keyword + " " + type.Name + type.TypeParameters);
        text.AppendLine(indent + "{");
        text.AppendLine(indent + "    " + member);
        text.AppendLine(indent + "}");

        for (var depth = type.Nesting.Length - 1; depth >= 0; depth--)
        {
            indent = indent.Substring(0, indent.Length - 4);
            text.AppendLine(indent + "}");
        }

        if (type.Namespace.Length > 0)
        {
            text.AppendLine("}");
        }

        return text.ToString();
    }

    /// <summary>Returns the generated assembly registry text for the marked types.</summary>
    /// <param name="types">The types collected so far, in discovery order.</param>
    /// <param name="grants">The access grants read from the record pages, by use-case identifier.</param>
    /// <param name="messages">The use cases whose message the registry maps, in record order.</param>
    /// <param name="operations">The use cases whose operation the registry maps, in record order.</param>
    /// <param name="webApi">Whether the application has a WebApi project that hosts endpoints.</param>
    /// <returns>The assembly registry text.</returns>
    private static string RegistryText(
        List<MarkedType> types,
        Dictionary<string, string> grants,
        List<MessageUseCase> messages,
        List<OperationUseCase> operations,
        bool webApi)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The assembly registry the analyzer package generates from the marks and the records.");
        text.AppendLine();
        text.AppendLine("namespace LiteSpec");
        text.AppendLine("{");
        text.AppendLine("    internal static class LiteSpecRegistry");
        text.AppendLine("    {");

        if (types.Count == 0)
        {
            text.AppendLine("        public static readonly System.Collections.Generic.IReadOnlyDictionary<string, string[]> MarkedTypes = new System.Collections.Generic.Dictionary<string, string[]>();");
        }
        else
        {
            text.AppendLine("        public static readonly System.Collections.Generic.IReadOnlyDictionary<string, string[]> MarkedTypes = new System.Collections.Generic.Dictionary<string, string[]>");
            text.AppendLine("        {");

            foreach (var type in types)
            {
                var ids = new StringBuilder();

                foreach (var id in type.MarkIds)
                {
                    if (ids.Length > 0)
                    {
                        ids.Append(", ");
                    }

                    ids.Append("\"" + Escape(id) + "\"");
                }

                text.AppendLine("            [\"" + Escape(type.FullName) + "\"] = new string[] { " + ids.ToString() + " },");
            }

            text.AppendLine("        };");
        }

        text.AppendLine();

        var useCases = new List<string>();

        foreach (var type in types)
        {
            foreach (var id in type.MarkIds)
            {
                if (id.StartsWith("use-case/", StringComparison.Ordinal)
                    && grants.ContainsKey(id)
                    && !useCases.Contains(id))
                {
                    useCases.Add(id);
                }
            }
        }

        useCases.Sort(static (left, right) => string.Compare(left, right, StringComparison.Ordinal));

        if (useCases.Count == 0)
        {
            text.AppendLine("        public static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> UseCaseGrants = new System.Collections.Generic.Dictionary<string, string>();");
        }
        else
        {
            text.AppendLine("        public static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> UseCaseGrants = new System.Collections.Generic.Dictionary<string, string>");
            text.AppendLine("        {");

            foreach (var id in useCases)
            {
                text.AppendLine("            [\"" + Escape(id) + "\"] = \"" + Escape(grants[id]) + "\",");
            }

            text.AppendLine("        };");
        }

        text.AppendLine();

        if (messages.Count == 0)
        {
            text.AppendLine("        private static readonly System.Collections.Generic.IReadOnlyDictionary<System.Type, string> MessageUseCases = new System.Collections.Generic.Dictionary<System.Type, string>();");
        }
        else
        {
            text.AppendLine("        private static readonly System.Collections.Generic.IReadOnlyDictionary<System.Type, string> MessageUseCases = new System.Collections.Generic.Dictionary<System.Type, string>");
            text.AppendLine("        {");

            foreach (var message in messages)
            {
                text.AppendLine("            [typeof(" + message.Message + ")] = \"" + Escape(message.UseCase) + "\",");
            }

            text.AppendLine("        };");
        }

        text.AppendLine();
        text.AppendLine("        public static string? UseCaseOf(System.Type messageType) =>");
        text.AppendLine("            MessageUseCases.TryGetValue(messageType, out var useCase) ? useCase : null;");
        text.AppendLine();
        text.AppendLine("        public static string? GrantOf(System.Type messageType) =>");
        text.AppendLine("            UseCaseOf(messageType) is string useCase && UseCaseGrants.TryGetValue(useCase, out var grant) ? grant : null;");
        text.AppendLine();
        text.AppendLine("        public static string? UseCaseOfType(System.Type type)");
        text.AppendLine("        {");
        text.AppendLine("            string? name = type.FullName?.Replace('+', '.');");
        text.AppendLine();
        text.AppendLine("            if (name is not null && MarkedTypes.TryGetValue(name, out var ids))");
        text.AppendLine("            {");
        text.AppendLine("                foreach (string id in ids)");
        text.AppendLine("                {");
        text.AppendLine("                    if (id.StartsWith(\"use-case/\", System.StringComparison.Ordinal))");
        text.AppendLine("                    {");
        text.AppendLine("                        return id;");
        text.AppendLine("                    }");
        text.AppendLine("                }");
        text.AppendLine("            }");
        text.AppendLine();
        text.AppendLine("            return null;");
        text.AppendLine("        }");

        if (webApi)
        {
            text.AppendLine();

            if (operations.Count == 0)
            {
                text.AppendLine("        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> OperationUseCases = new System.Collections.Generic.Dictionary<string, string>();");
            }
            else
            {
                text.AppendLine("        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> OperationUseCases = new System.Collections.Generic.Dictionary<string, string>");
                text.AppendLine("        {");

                foreach (var operation in operations)
                {
                    text.AppendLine("            [\"" + Escape(operation.Operation) + "\"] = \"" + Escape(operation.UseCase) + "\",");
                }

                text.AppendLine("        };");
            }

            text.AppendLine();
            text.AppendLine("        public static string? UseCaseOfOperation(string operationName) =>");
            text.AppendLine("            OperationUseCases.TryGetValue(operationName, out var useCase) ? useCase : null;");
        }

        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }

    /// <summary>Returns the generated permission registry text.</summary>
    /// <param name="named">The named entries to write, in record order.</param>
    /// <returns>The permission registry text.</returns>
    private static string PermissionsText(List<NamedPermission> named)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The permission constants the analyzer package generates from the permission records.");
        text.AppendLine();
        text.AppendLine("namespace LiteSpec");
        text.AppendLine("{");
        text.AppendLine("    internal static class LiteSpecPermissions");
        text.AppendLine("    {");

        foreach (var permission in named)
        {
            text.AppendLine("        public const string " + permission.Name + " = \"" + Escape(permission.Facts.Id) + "\";");
        }

        if (named.Count > 0)
        {
            text.AppendLine();
        }

        text.AppendLine("        internal sealed record LiteSpecPermission(string Id, string Scope, string? Group, bool Reads, System.Collections.Generic.IReadOnlyList<string> Risks);");
        text.AppendLine();
        text.AppendLine("        public static System.Collections.Generic.IReadOnlyList<LiteSpecPermission> All { get; } = new LiteSpecPermission[]");
        text.AppendLine("        {");

        foreach (var permission in named)
        {
            text.AppendLine("            " + PermissionEntryText(permission) + ",");
        }

        text.AppendLine("        };");
        text.AppendLine();
        text.AppendLine("        public static LiteSpecPermission Of(string id)");
        text.AppendLine("        {");
        text.AppendLine("            foreach (LiteSpecPermission permission in All)");
        text.AppendLine("            {");
        text.AppendLine("                if (permission.Id == id)");
        text.AppendLine("                {");
        text.AppendLine("                    return permission;");
        text.AppendLine("                }");
        text.AppendLine("            }");
        text.AppendLine();
        text.AppendLine("            throw new System.ArgumentException(\"Unknown permission '\" + id + \"'.\", \"id\");");
        text.AppendLine("        }");
        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }

    /// <summary>Returns the entry text.</summary>
    /// <param name="permission">The permission entry to write.</param>
    /// <returns>The entry text as text.</returns>
    private static string PermissionEntryText(NamedPermission permission)
    {
        var risks = new StringBuilder();

        foreach (var risk in permission.Facts.Risks)
        {
            if (risks.Length > 0)
            {
                risks.Append(", ");
            }

            risks.Append("\"" + Escape(risk) + "\"");
        }

        return "new LiteSpecPermission(\""
            + Escape(permission.Facts.Id) + "\", \""
            + Escape(permission.Facts.Scope) + "\", "
            + (permission.Facts.Group is string group ? "\"" + Escape(group) + "\"" : "null") + ", "
            + (permission.Reads ? "true" : "false") + ", new string[] { "
            + (risks.Length > 0 ? risks.ToString() + " " : string.Empty) + "})";
    }

    /// <summary>Returns the generated role registry text.</summary>
    /// <param name="named">The named entries to write, in record order.</param>
    /// <returns>The role registry text.</returns>
    private static string RolesText(List<NamedRole> named)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The role constants the analyzer package generates from the role records.");
        text.AppendLine();
        text.AppendLine("namespace LiteSpec");
        text.AppendLine("{");
        text.AppendLine("    internal static class LiteSpecRoles");
        text.AppendLine("    {");

        foreach (var role in named)
        {
            text.AppendLine("        public const string " + role.Name + " = \"" + Escape(role.Facts.Id) + "\";");
        }

        if (named.Count > 0)
        {
            text.AppendLine();
        }

        text.AppendLine("        internal sealed record LiteSpecRole(string Id, string Scope, System.Collections.Generic.IReadOnlyList<string> Allows);");
        text.AppendLine();
        text.AppendLine("        public static System.Collections.Generic.IReadOnlyList<LiteSpecRole> All { get; } = new LiteSpecRole[]");
        text.AppendLine("        {");

        foreach (var role in named)
        {
            text.AppendLine("            " + RoleEntryText(role) + ",");
        }

        text.AppendLine("        };");
        text.AppendLine();
        text.AppendLine("        public static LiteSpecRole Of(string id)");
        text.AppendLine("        {");
        text.AppendLine("            foreach (LiteSpecRole role in All)");
        text.AppendLine("            {");
        text.AppendLine("                if (role.Id == id)");
        text.AppendLine("                {");
        text.AppendLine("                    return role;");
        text.AppendLine("                }");
        text.AppendLine("            }");
        text.AppendLine();
        text.AppendLine("            throw new System.ArgumentException(\"Unknown role '\" + id + \"'.\", \"id\");");
        text.AppendLine("        }");
        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }

    /// <summary>Returns the entry text.</summary>
    /// <param name="role">The role entry to write.</param>
    /// <returns>The entry text as text.</returns>
    private static string RoleEntryText(NamedRole role)
    {
        var allows = new StringBuilder();

        foreach (var allowed in role.Facts.Allows)
        {
            if (allows.Length > 0)
            {
                allows.Append(", ");
            }

            allows.Append("\"" + Escape(allowed) + "\"");
        }

        return "new LiteSpecRole(\""
            + Escape(role.Facts.Id) + "\", \""
            + Escape(role.Facts.Scope) + "\", new string[] { "
            + (allows.Length > 0 ? allows.ToString() + " " : string.Empty) + "})";
    }

    /// <summary>Escapes the value for use inside a generated string literal.</summary>
    /// <param name="value">The value to convert or test.</param>
    /// <returns>The string as text.</returns>
    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Returns true when the character is ascii letter or digit; otherwise false.</summary>
    /// <param name="character">The character to convert.</param>
    /// <returns>True when the character is ascii letter or digit; otherwise false.</returns>
    private static bool IsAsciiLetterOrDigit(char character) =>
        (character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9');
}
