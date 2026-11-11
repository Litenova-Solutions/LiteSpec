using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>How a finding words what it is about: the subject it names, record types as prose, and code elements in plain words.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class FindingText
{
    /// <summary>The subject a finding on the record names: its identifier, or null when it holds none.</summary>
    /// <param name="record">The record the finding belongs to.</param>
    /// <returns>The record identifier, or null when the identifier is empty.</returns>
    internal static string? SubjectOf(Record record) => record.Id.Length > 0 ? record.Id : null;

    /// <summary>The prose label a finding shows for a behavior record type.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label, or the generic record label for any other type.</returns>
    internal static string Label(string type) => type switch
    {
        RecordTypes.UseCase => "Use case",
        RecordTypes.Reaction => "Reaction",
        RecordTypes.Job => "Job",
        RecordTypes.Notification => "Notification",
        RecordTypes.Setting => "Setting",
        _ => "Record",
    };

    /// <summary>The record types as prose names joined with commas and a final or.</summary>
    /// <param name="types">The record type identifiers to name.</param>
    /// <returns>The prose names joined with commas and a final or, or an empty string when no type is given.</returns>
    internal static string OrList(IEnumerable<string> types)
    {
        var names = types.Select(type => type.Replace('-', ' ')).ToList();

        return names.Count switch
        {
            0 => "",
            1 => names[0],
            _ => string.Join(", ", names[..^1]) + " or " + names[^1],
        };
    }

    /// <summary>The kind name a mark finding shows: the code element in plain words.</summary>
    /// <param name="kind">The scanned code element kind.</param>
    /// <returns>The plain-words name of the kind.</returns>
    internal static string KindName(CodeKind kind) => kind switch
    {
        CodeKind.AggregateRoot => "Aggregate root",
        CodeKind.AggregateMethod => "Aggregate method",
        CodeKind.DomainEvent => "Domain event",
        CodeKind.RuleException => "Rule exception",
        CodeKind.Handler => "Handler",
        CodeKind.EventHandler => "Event handler",
        CodeKind.Job => "Job",
        CodeKind.Port => "Port",
        CodeKind.Adapter => "Adapter",
        CodeKind.Endpoint => "Endpoint",
        CodeKind.FrontendModule => "Frontend module",
        CodeKind.FrontendRoute => "Route entry",
        CodeKind.BrowserTest => "Browser test",
        CodeKind.Test => "Test",
        CodeKind.DomainService => "Domain service",
        CodeKind.Technical => "Technical class",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown code kind."),
    };

    /// <summary>The element a mark finding names: its type, or its type and member.</summary>
    /// <param name="element">The marked code element whose display name is wanted.</param>
    /// <returns>The type name alone, or the type and member names joined by a dot when a member is marked.</returns>
    internal static string DisplayName(MarkedElement element) =>
        element.Member is { Length: > 0 } member ? $"{element.Class}.{member}" : element.Class;
}
