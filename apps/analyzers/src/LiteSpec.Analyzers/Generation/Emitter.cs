using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// Writes the generator's sources from the records, the marked types and the host: the mark
/// attributes, the event and exception value parts, the assembly registry and the permission
/// constants. Every value is written only where no member of that name is declared in source,
/// and only where its record is known.
/// </summary>
internal static class Emitter
{
    /// <summary>Writes every source the inputs call for, or nothing without a usable project file or with no records and no marks.</summary>
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
        if (!records.Valid)
        {
            return;
        }

        if (records.EventIds.IsEmpty
            && records.RuleCodes.IsEmpty
            && records.PermissionIds.IsEmpty
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

        context.AddSource("LiteSpecRegistry.g.cs", RegistryText(ordered, grants));

        if (host.Layer == Layer.Application
            || host.Layer == Layer.Infrastructure
            || host.Layer == Layer.WebApi)
        {
            EmitPermissions(context, records.PermissionIds);
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

        var built = new StringBuilder();
        var words = name.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

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

    private static string HintName(string kind, MarkedType type) =>
        kind + "." + type.FullName.Replace('`', '_') + ".g.cs";

    private static void EmitPermissions(SourceProductionContext context, ImmutableArray<string> ids)
    {
        var named = new List<KeyValuePair<string, string>>();

        foreach (var id in ids)
        {
            var name = ConstantName(id);

            if (name is not null)
            {
                named.Add(new KeyValuePair<string, string>(name, id));
            }
        }

        named.Sort(static (left, right) =>
        {
            var names = string.Compare(left.Key, right.Key, StringComparison.Ordinal);

            return names != 0
                ? names
                : string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        });

        var collisions = 0;

        for (var index = 1; index < named.Count; index++)
        {
            if (named[index].Key != named[index - 1].Key
                || named[index].Value == named[index - 1].Value)
            {
                continue;
            }

            collisions++;

            if (index == 1 || named[index - 1].Key != named[index - 2].Key)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnostics.PermissionCollision,
                    Location.None,
                    named[index].Key,
                    named[index - 1].Value,
                    named[index].Value));
            }
        }

        if (collisions > 0)
        {
            return;
        }

        context.AddSource("LiteSpecPermissions.g.cs", PermissionsText(named));
    }

    private static string AttributesText(bool xunit)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
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

    private static string EventText(MarkedType type, string eventId)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("// The event classification the analyzer package generates from the event record.");
        text.AppendLine();

        return text.Append(PartialText(
            type,
            "public const string Classification = \"" + Escape(eventId) + "\";")).ToString();
    }

    private static string ExceptionText(MarkedType type, string code)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("// The refusal code the analyzer package generates from the rule's refusal result.");
        text.AppendLine();
        var member = type.BaseHasVirtualCode
            ? "public override string Code => \"" + Escape(code) + "\";"
            : "public string Code => \"" + Escape(code) + "\";";

        return text.Append(PartialText(type, member)).ToString();
    }

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

    private static string RegistryText(List<MarkedType> types, Dictionary<string, string> grants)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
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

        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }

    private static string PermissionsText(List<KeyValuePair<string, string>> named)
    {
        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/>");
        text.AppendLine("// The permission constants the analyzer package generates from the permission records.");
        text.AppendLine();
        text.AppendLine("namespace LiteSpec");
        text.AppendLine("{");
        text.AppendLine("    internal static class LiteSpecPermissions");
        text.AppendLine("    {");

        foreach (var permission in named)
        {
            text.AppendLine("        public const string " + permission.Key + " = \"" + Escape(permission.Value) + "\";");
        }

        text.AppendLine("    }");
        text.AppendLine("}");

        return text.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static bool IsAsciiLetterOrDigit(char character) =>
        (character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9');
}
