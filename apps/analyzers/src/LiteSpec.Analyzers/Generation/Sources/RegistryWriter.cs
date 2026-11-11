using System;
using System.Collections.Generic;
using System.Text;
using LiteSpec.Analyzers.Rules;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;

using static LiteSpec.Analyzers.Generation.Sources.GeneratedText;
using static LiteSpec.Analyzers.Generation.Sources.GeneratedSourceWriter;

namespace LiteSpec.Analyzers.Generation.Sources;

/// <summary>Writes the assembly registry: the use case each message and published operation runs, and the operation map of the endpoints a module-layout host holds.</summary>
/// <follows>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
internal static class RegistryWriter
{
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

    /// <summary>Emits the registry.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    /// <param name="grants">The access grants read from the record pages, by use-case identifier.</param>
    /// <param name="webApi">Whether the compilation hosts endpoints.</param>
    internal static void EmitRegistry(
        SourceProductionContext context,
        List<MarkedType> types,
        Dictionary<string, string> grants,
        bool webApi)
    {
        var messages = MessageUseCasesOf(context, types);
        var operations = OperationUseCasesOf(types);
        context.AddSource("LiteSpecRegistry.g.cs", RegistryText(types, grants, messages, operations, webApi));
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
    internal static List<OperationUseCase> OperationUseCasesOf(List<MarkedType> types)
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
    internal sealed class OperationUseCase
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

    /// <summary>Returns the operation map of the endpoints a module-layout host holds.</summary>
    /// <param name="operations">The operation names and the use cases their endpoints implement.</param>
    /// <returns>The generated source text.</returns>
    internal static string HostOperationsText(List<OperationUseCase> operations)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("#nullable enable");
        text.AppendLine("// The operations of the endpoints this host holds, which the analyzer package generates from their marks.");
        text.AppendLine();
        text.AppendLine("namespace LiteSpec");
        text.AppendLine("{");
        text.AppendLine("    internal static class LiteSpecHostOperations");
        text.AppendLine("    {");
        text.AppendLine("        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> OperationUseCases = new System.Collections.Generic.Dictionary<string, string>");
        text.AppendLine("        {");

        foreach (var operation in operations)
        {
            text.AppendLine("            [\"" + Escape(operation.Operation) + "\"] = \"" + Escape(operation.UseCase) + "\",");
        }

        text.AppendLine("        };");
        text.AppendLine();
        text.AppendLine("        public static string? UseCaseOfOperation(string operationName) =>");
        text.AppendLine("            OperationUseCases.TryGetValue(operationName, out var useCase) ? useCase : null;");
        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }
}
