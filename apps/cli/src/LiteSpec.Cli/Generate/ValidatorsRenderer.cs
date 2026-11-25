using System.Text;
using System.Text.Json;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Model;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     Renders one input validator per command and query carrying constrained inputs: an
///     internal sealed partial class implementing the LiteBus validator for its message,
///     with one rule per record constraint reporting a validity finding and a partial
///     hook for hand-written checks. A constraint citing a rule that states a refusal
///     renders no rule: Domain refuses it through its written result, and the pipeline
///     refuses every generated finding through the derived invalid-input result.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
/// <follows>litespec/backend/application#report-input-problems-as-validity-findings</follows>
/// <follows>litespec/backend/dotnet/application#report-input-problems-as-validity</follows>
static class ValidatorsRenderer
{
    /// <summary>One generated rule: the member it judges with the finding it reports when the condition holds.</summary>
    /// <param name="Member">The message property the rule judges.</param>
    /// <param name="Code">The stable reason code the finding carries.</param>
    /// <param name="Condition">The C# condition holding when the input breaks the rule.</param>
    /// <param name="Message">The caller-safe message naming the field.</param>
    public sealed record ValidationRule(string Member, string Code, string Condition, string Message);

    /// <summary>One validator the plan renders. The validator carries no mark: the handler beside it carries the use case mark.</summary>
    /// <param name="UseCaseName">The record use case name, such as submit-expense.</param>
    /// <param name="Kind">The use case kind: command or query.</param>
    /// <param name="MessageType">The simple message type name, such as SubmitExpenseCommand.</param>
    /// <param name="Namespace">The C# namespace of the generated class.</param>
    /// <param name="Usings">The C# namespaces the target declares for messages outside their use case folder.</param>
    /// <param name="Rules">The generated rules in written input order.</param>
    /// <param name="UsesGuid">True when a rule compares against Guid.Empty.</param>
    /// <param name="UsesRegex">True when a rule matches a regular expression.</param>
    /// <param name="UsesLinq">True when a rule reads map values through Linq.</param>
    /// <param name="ValidatorInterface">The validator interface as the target declares it, qualified or simple.</param>
    public sealed record ValidatorData(
        string UseCaseName,
        string Kind,
        string MessageType,
        string Namespace,
        IReadOnlyList<string> Usings,
        IReadOnlyList<ValidationRule> Rules,
        bool UsesGuid,
        bool UsesRegex,
        bool UsesLinq,
        string ValidatorInterface);

    /// <summary>The validator interface a command implements when the target names none: the LiteBus command validator.</summary>
    public const string DefaultCommandValidatorInterface = "LiteBus.Commands.Abstractions.ICommandValidator";

    /// <summary>The validator interface a query implements when the target names none: the LiteBus query validator.</summary>
    public const string DefaultQueryValidatorInterface = "LiteBus.Queries.Abstractions.IQueryValidator";

    /// <summary>The validator interface one use case implements: the target interface for its kind, or the LiteBus default when the target names none.</summary>
    /// <param name="kind">The use case kind: command or query.</param>
    /// <param name="commandInterface">The target command validator interface, or empty.</param>
    /// <param name="queryInterface">The target query validator interface, or empty.</param>
    /// <returns>The qualified or simple interface name.</returns>
    public static string ValidatorInterfaceFor(string kind, string commandInterface, string queryInterface)
    {
        var named = kind == "query" ? queryInterface : commandInterface;

        if (named.Length > 0)
        {
            return named;
        }

        return kind == "query" ? DefaultQueryValidatorInterface : DefaultCommandValidatorInterface;
    }

    /// <summary>The validator type name: the message type with a Validator suffix.</summary>
    /// <param name="messageType">The simple message type name.</param>
    /// <returns>The validator type name.</returns>
    public static string ValidatorTypeName(string messageType) => messageType + "Validator";

    /// <summary>
    ///     Renders one validator as C#: an internal sealed partial class implementing the
    ///     LiteBus validator for its message, reporting one validity finding per broken
    ///     rule and running the hand-written checks after the generated rules.
    /// </summary>
    /// <param name="data">The validator the plan renders.</param>
    /// <returns>The file text with LF line endings.</returns>
    public static string RenderDotnet(ValidatorData data)
    {
        var type = ValidatorTypeName(data.MessageType);
        var parameter = data.Kind == "query" ? "query" : "command";
        var lines = new List<string>
        {
            GenerateNaming.HeaderLineFor(GenerateNaming.CurrentVersion()),
            "// <auto-generated/>",
            "",
        };

        lines.AddRange(UsingLines(data));

        lines.Add($"namespace {data.Namespace};");
        lines.Add("");
        lines.Add($"/// <summary>Validates the {data.UseCaseName} {data.Kind} against its record constraints.</summary>");
        lines.Add($"internal sealed partial class {type} : {SimpleName(data.ValidatorInterface)}<{data.MessageType}>");
        lines.Add("{");
        lines.Add("    /// <summary>Validates the message, merging every constraint finding with the hand-written checks.</summary>");
        lines.Add($"    /// <param name=\"{parameter}\">The message to validate.</param>");
        lines.Add("    /// <param name=\"cancellationToken\">The cancellation token.</param>");
        lines.Add("    /// <returns>The validity holding every finding.</returns>");
        lines.Add($"    public Task<Validity> ValidateAsync({data.MessageType} {parameter}, CancellationToken cancellationToken = default)");
        lines.Add("    {");
        lines.Add("        var failures = new List<ValidationFailure>();");
        lines.Add("");

        foreach (var rule in data.Rules)
        {
            lines.Add($"        if ({rule.Condition})");
            lines.Add("        {");
            lines.Add($"            failures.Add(new ValidationFailure(\"{GenerateNaming.EscapeLiteral(rule.Message)}\", \"{rule.Member}\", \"{rule.Code}\"));");
            lines.Add("        }");
            lines.Add("");
        }

        lines.Add($"        ValidateExtra({parameter}, failures);");
        lines.Add("");
        lines.Add("        return Task.FromResult(failures.Count == 0 ? Validity.Valid : Validity.Invalid(failures));");
        lines.Add("    }");
        lines.Add("");
        lines.Add("    /// <summary>Hand-written checks the records do not state, run after the generated rules.</summary>");
        lines.Add($"    /// <param name=\"{parameter}\">The message to validate.</param>");
        lines.Add("    /// <param name=\"failures\">The findings, appended in place.</param>");
        lines.Add($"    partial void ValidateExtra({data.MessageType} {parameter}, List<ValidationFailure> failures);");
        lines.Add("}");

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The using lines the file opens with: the framework namespaces the rules read with the LiteBus abstractions and the target usings.</summary>
    /// <param name="data">The validator the plan renders.</param>
    /// <returns>The using lines with one trailing blank line.</returns>
    private static List<string> UsingLines(ValidatorData data)
    {
        var lines = new List<string>();

        if (data.UsesGuid)
        {
            lines.Add("using System;");
        }

        lines.Add("using System.Collections.Generic;");

        if (data.UsesLinq)
        {
            lines.Add("using System.Linq;");
        }

        if (data.UsesRegex)
        {
            lines.Add("using System.Text.RegularExpressions;");
        }

        lines.Add("using System.Threading;");
        lines.Add("using System.Threading.Tasks;");
        lines.Add("using LiteBus.Messaging.Abstractions;");

        if (InterfaceNamespace(data.ValidatorInterface) is { Length: > 0 } interfaceNamespace
            && interfaceNamespace != "LiteBus.Messaging.Abstractions"
            && interfaceNamespace != data.Namespace
            && !data.Namespace.StartsWith(interfaceNamespace + ".", StringComparison.Ordinal))
        {
            lines.Add($"using {interfaceNamespace};");
        }

        foreach (var usingNamespace in data.Usings
            .Where(candidate => candidate.Length > 0
                && candidate != data.Namespace
                && !data.Namespace.StartsWith(candidate + ".", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            lines.Add($"using {usingNamespace};");
        }

        lines.Add("");

        return lines;
    }

    /// <summary>The simple interface name: the declared validator interface type after its last dot.</summary>
    /// <param name="validatorInterface">The validator interface as the target declares it.</param>
    /// <returns>The simple type name.</returns>
    private static string SimpleName(string validatorInterface) =>
        validatorInterface.Contains('.', StringComparison.Ordinal)
            ? validatorInterface[(validatorInterface.LastIndexOf('.') + 1)..]
            : validatorInterface;

    /// <summary>The namespace a qualified validator interface lives in, or empty when the target names a simple name.</summary>
    /// <param name="validatorInterface">The validator interface as the target declares it.</param>
    /// <returns>The namespace before the last dot, or empty.</returns>
    private static string InterfaceNamespace(string validatorInterface) =>
        validatorInterface.Contains('.', StringComparison.Ordinal)
            ? validatorInterface[..validatorInterface.LastIndexOf('.')]
            : string.Empty;

    /// <summary>Appends the rules one input field states: its presence rule with one rule per constraint the validator owns.</summary>
    /// <param name="rules">The rules, appended in place.</param>
    /// <param name="member">The message property judging the field.</param>
    /// <param name="parameter">The validate parameter name: command or query.</param>
    /// <param name="domainOwned">True when a cited rule states a refusal, so Domain refuses the constraints.</param>
    /// <param name="input">The input field as the record states it.</param>
    /// <returns>Whether the appended rules compare against Guid.Empty, match a regular expression, or read map values through Linq.</returns>
    internal static (bool UsesGuid, bool UsesRegex, bool UsesLinq) AddRulesFor(
        List<ValidationRule> rules, string member, string parameter, bool domainOwned, JsonElement input)
    {
        var usesGuid = false;
        var usesRegex = false;
        var type = RecordEntries.Str(input, RecordFields.Type);
        var required = input.ValueKind == JsonValueKind.Object
            && input.TryGetProperty(RecordFields.Required, out var flag)
            && flag.ValueKind == JsonValueKind.True;
        var many = input.ValueKind == JsonValueKind.Object
            && input.TryGetProperty(RecordFields.Many, out var plural)
            && plural.ValueKind == JsonValueKind.True;

        if (type is null || many)
        {
            return (false, false, false);
        }

        var display = DisplayOf(member);

        if (required && PresenceCondition(member, parameter, type, input) is { } presence)
        {
            usesGuid = presence.Contains("Guid.Empty", StringComparison.Ordinal);
            rules.Add(new ValidationRule(member, "required", presence, $"{ArticleFor(display)} {display} is required."));
        }

        if (domainOwned || type is not ("text" or "email" or "integer" or "decimal" or "money" or "map"))
        {
            return (usesGuid, false, false);
        }

        if (type is "text" or "email")
        {
            var guarded = required ? $"!string.IsNullOrEmpty({parameter}.{member})" : $"{parameter}.{member} is not null";

            if (NumberOf(input, RecordFields.MinLength) is { } minimum)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-short",
                    $"{guarded} && {parameter}.{member}.Length < {minimum}",
                    $"The {display} must be at least {minimum} characters long."));
            }

            if (NumberOf(input, RecordFields.MaxLength) is { } maximum)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-long",
                    $"{guarded} && {parameter}.{member}.Length > {maximum}",
                    $"The {display} must be at most {maximum} characters long."));
            }

            if (RecordEntries.Str(input, RecordFields.Matches) is { Length: > 0 } pattern)
            {
                usesRegex = true;
                rules.Add(new ValidationRule(
                    member,
                    "invalid-format",
                    $"{guarded} && !Regex.IsMatch({parameter}.{member}, \"{GenerateNaming.EscapeLiteral(pattern)}\")",
                    $"The {display} must match the required format."));
            }

            return (usesGuid, usesRegex, false);
        }

        if (type == "map")
        {
            var before = rules.Count;
            var mapRegex = AddMapRulesFor(rules, member, parameter, required, display, input);

            return (usesGuid, usesRegex || mapRegex, rules.Count > before);
        }

        if (input.TryGetProperty(RecordFields.Bounds, out var bounds) && bounds.ValueKind == JsonValueKind.Object)
        {
            var exclusive = bounds.TryGetProperty(RecordFields.Inclusive, out var inclusive)
                && inclusive.ValueKind == JsonValueKind.False;
            var literal = type == "integer" ? string.Empty : "m";
            var money = type == "money" && RecordEntries.Str(input, RecordFields.CurrencyHome) is not { Length: > 0 };

            if (NumberOf(bounds, RecordFields.Min) is { } min)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-small",
                    BoundCondition(member, parameter, required, "<", min, literal, exclusive, money),
                    exclusive ? $"The {display} must be more than {min}." : $"The {display} must be at least {min}."));
            }

            if (NumberOf(bounds, RecordFields.Max) is { } max)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-large",
                    BoundCondition(member, parameter, required, ">", max, literal, exclusive, money),
                    exclusive ? $"The {display} must be less than {max}." : $"The {display} must be at most {max}."));
            }
        }

        return (usesGuid, false, false);
    }

    /// <summary>Appends the rules one map input field states: one rule per constraint, judging each value of the map.</summary>
    /// <param name="rules">The rules, appended in place.</param>
    /// <param name="member">The message property judging the field.</param>
    /// <param name="parameter">The validate parameter name: command or query.</param>
    /// <param name="required">True when the message always carries the field.</param>
    /// <param name="display">The field display name the messages name.</param>
    /// <param name="input">The input field as the record states it.</param>
    /// <returns>True when an appended rule matches a regular expression.</returns>
    private static bool AddMapRulesFor(
        List<ValidationRule> rules, string member, string parameter, bool required, string display, JsonElement input)
    {
        var usesRegex = false;
        var valueType = RecordEntries.Str(input, RecordFields.ValueType);
        var values = $"{parameter}.{member}.Values";
        var absent = required ? null : $"{parameter}.{member} is not null";

        string Condition(string test) =>
            absent is null ? $"{values}.Any(value => {test})" : $"{absent} && {values}.Any(value => {test})";

        if (valueType is "text" or "email")
        {
            if (NumberOf(input, RecordFields.MinLength) is { } minimum)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-short",
                    Condition($"value.Length < {minimum}"),
                    $"The {display} must each be at least {minimum} characters long."));
            }

            if (NumberOf(input, RecordFields.MaxLength) is { } maximum)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-long",
                    Condition($"value.Length > {maximum}"),
                    $"The {display} must each be at most {maximum} characters long."));
            }

            if (RecordEntries.Str(input, RecordFields.Matches) is { Length: > 0 } pattern)
            {
                usesRegex = true;
                rules.Add(new ValidationRule(
                    member,
                    "invalid-format",
                    Condition($"!Regex.IsMatch(value, \"{GenerateNaming.EscapeLiteral(pattern)}\")"),
                    $"The {display} must each match the required format."));
            }

            return usesRegex;
        }

        if (valueType is not ("integer" or "decimal" or "money"))
        {
            return false;
        }

        if (input.TryGetProperty(RecordFields.Bounds, out var bounds) && bounds.ValueKind == JsonValueKind.Object)
        {
            var exclusive = bounds.TryGetProperty(RecordFields.Inclusive, out var inclusive)
                && inclusive.ValueKind == JsonValueKind.False;
            var literal = valueType == "integer" ? string.Empty : "m";
            var amount = valueType == "money" && RecordEntries.Str(input, RecordFields.CurrencyHome) is not { Length: > 0 }
                ? "value.Amount"
                : "value";

            if (NumberOf(bounds, RecordFields.Min) is { } min)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-small",
                    Condition($"{amount} {ComparisonOf("<", exclusive)} {min}{literal}"),
                    exclusive ? $"The {display} must each be more than {min}." : $"The {display} must each be at least {min}."));
            }

            if (NumberOf(bounds, RecordFields.Max) is { } max)
            {
                rules.Add(new ValidationRule(
                    member,
                    "too-large",
                    Condition($"{amount} {ComparisonOf(">", exclusive)} {max}{literal}"),
                    exclusive ? $"The {display} must each be less than {max}." : $"The {display} must each be at most {max}."));
            }
        }

        return usesRegex;
    }

    /// <summary>The presence condition of a required field, or null when the mapped type is always present.</summary>
    /// <param name="member">The message property judging the field.</param>
    /// <param name="parameter">The validate parameter name: command or query.</param>
    /// <param name="type">The record field type.</param>
    /// <param name="input">The input field as the record states it.</param>
    /// <returns>The condition holding when the field is missing, or null.</returns>
    private static string? PresenceCondition(string member, string parameter, string type, JsonElement input) => type switch
    {
        "text" or "email" => $"string.IsNullOrEmpty({parameter}.{member})",
        "id" => RecordEntries.Str(input, RecordFields.Identifies) is { } identifies
            && identifies.StartsWith("actor/", StringComparison.Ordinal)
            ? $"{parameter}.{member} == Guid.Empty"
            : $"{parameter}.{member} == default",
        "choice" or "list" or "map" or "object" => $"{parameter}.{member} is null",
        _ => null,
    };

    /// <summary>The bound condition of a numeric field: the direct comparison, or the comparison of the value when the field is optional. A money bound compares the amount of the shared money object.</summary>
    /// <param name="member">The message property judging the field.</param>
    /// <param name="parameter">The validate parameter name: command or query.</param>
    /// <param name="required">True when the message always carries the field.</param>
    /// <param name="direction">The comparison direction: &lt; for a minimum, &gt; for a maximum.</param>
    /// <param name="bound">The bound as the record states it.</param>
    /// <param name="literal">The numeric suffix: m for a decimal or money field, empty for an integer.</param>
    /// <param name="exclusive">True when the bound itself breaks the rule.</param>
    /// <param name="money">True when the field rides the shared money object.</param>
    /// <returns>The condition holding when the value breaks the bound.</returns>
    private static string BoundCondition(
        string member, string parameter, bool required, string direction, string bound, string literal, bool exclusive, bool money)
    {
        var comparison = ComparisonOf(direction, exclusive);

        if (required)
        {
            return money
                ? $"{parameter}.{member}.Amount {comparison} {bound}{literal}"
                : $"{parameter}.{member} {comparison} {bound}{literal}";
        }

        return money
            ? $"{parameter}.{member} is not null && {parameter}.{member}.Amount {comparison} {bound}{literal}"
            : $"{parameter}.{member}.HasValue && {parameter}.{member}.Value {comparison} {bound}{literal}";
    }

    /// <summary>The comparison operator a bound breaks with: the bound itself breaks an exclusive bound.</summary>
    /// <param name="direction">The comparison direction: &lt; for a minimum, &gt; for a maximum.</param>
    /// <param name="exclusive">True when the bound itself breaks the rule.</param>
    /// <returns>The operator text.</returns>
    private static string ComparisonOf(string direction, bool exclusive) => direction == "<"
        ? (exclusive ? "<=" : "<")
        : (exclusive ? ">=" : ">");

    /// <summary>The number a constraint states: its raw text, or null when the property holds no number.</summary>
    /// <param name="element">The object holding the constraint.</param>
    /// <param name="name">The constraint property name.</param>
    /// <returns>The raw number text, or null.</returns>
    private static string? NumberOf(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
            ? value.GetRawText()
            : null;

    /// <summary>The field display name: the member without its identity suffix, split into lowercase words.</summary>
    /// <param name="member">The message property judging the field.</param>
    /// <returns>The display name the messages name.</returns>
    internal static string DisplayOf(string member)
    {
        var stripped = member.EndsWith("Id", StringComparison.Ordinal) && member.Length > 2
            ? member[..^2]
            : member;
        var built = new StringBuilder(stripped.Length + 4);

        for (var index = 0; index < stripped.Length; index++)
        {
            var character = stripped[index];

            if (index > 0 && char.IsUpper(character) && (char.IsLower(stripped[index - 1]) || char.IsDigit(stripped[index - 1])))
            {
                built.Append(' ');
            }

            built.Append(char.ToLowerInvariant(character));
        }

        var display = built.ToString();

        return display.Length > 0 ? display : member.ToLowerInvariant();
    }

    /// <summary>The indefinite article opening a required message: An before a vowel, A otherwise.</summary>
    /// <param name="display">The field display name.</param>
    /// <returns>An or A.</returns>
    private static string ArticleFor(string display) =>
        display.Length > 0 && "aeiou".Contains(display[0], StringComparison.Ordinal) ? "An" : "A";
}
