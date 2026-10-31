using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     The links the builder resolves from the marks and records: each record's
///     links with their targets, the implements and follows links the code marks
///     carry, and the declared refusals those marks name.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class ModelLinks
{
    /// <summary>The record's links with their targets, for model.json.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="record">The record whose links are resolved.</param>
    /// <returns>The record's links with their targets.</returns>
    public static Dictionary<string, List<string>> ResolvedLinks(ProjectModel model, Record record)
    {
        var links = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return links;
        }

        foreach (var property in record.Raw.EnumerateObject())
        {
            if (RecordTypes.LinkTargets.TryGetValue(property.Name, out _)
                || RecordTypes.NestedLinkFields.Contains(property.Name)
                || property.Name is RecordFields.Replaces or RecordFields.Refusal or "provision")
            {
                var targets = ModelBuilder.TargetsOf(property.Value);

                if (targets.Count > 0)
                {
                    links[property.Name] = targets;
                }
            }
        }

        return links;
    }

    /// <summary>
    ///     Keeps a domain event's record only when its mark names an event record.
    ///     A mark naming nothing, or a record of another type, is left for
    ///     <c>mark-unknown</c>, with no class-name fallback.
    /// </summary>
    /// <param name="model">The project model.</param>
    public static void MatchEvents(ProjectModel model)
    {
        for (var index = 0; index < model.Code.Events.Count; index++)
        {
            var code = model.Code.Events[index];

            if (code.EventId is null)
            {
                continue;
            }

            if (!model.ById.TryGetValue(code.EventId, out var record) || record.Type != RecordTypes.Event)
            {
                model.Code.Events[index] = code with { EventId = null };
            }
        }
    }

    /// <summary>
    ///     Adds one implements link per implements mark on every element. A mark is
    ///     the link: no name, folder or trigger fallback adds one.
    /// </summary>
    /// <param name="model">The project model.</param>
    public static void ComputeImplements(ProjectModel model)
    {
        foreach (var element in model.Code.Elements)
        {
            if (LinkKind(element.Kind) is not { } kind)
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb != MarkVerb.Implements)
                {
                    continue;
                }

                model.Implements.Add(new ImplementsLink(
                    model.Config.Relative(mark.File), mark.RecordId, kind));
            }
        }

        MarkDeclaredRefusals(model);

        var codes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var literal in model.Code.Literals)
        {
            codes.TryAdd(literal.Value, model.Config.Relative(literal.File));
        }

        foreach (var result in model.Records.Where(record => record.Type == RecordTypes.Result))
        {
            if (result.Field(RecordFields.Code) is { } code && codes.TryGetValue(code, out var path))
            {
                model.Implements.Add(new ImplementsLink(path, result.Id, "result"));
            }
        }
    }

    /// <summary>
    ///     Adds one follows link per follows mark on every element, to the provision,
    ///     decision or exception the mark names, exported as written.
    /// </summary>
    /// <param name="model">The project model.</param>
    public static void ComputeFollows(ProjectModel model)
    {
        foreach (var element in model.Code.Elements)
        {
            foreach (var mark in element.Marks.Where(mark => mark.Verb == MarkVerb.Follows))
            {
                model.Follows.Add(new FollowsLink(
                    model.Config.Relative(mark.File), mark.RecordId, element.Kind.ToString()));
            }
        }
    }

    /// <summary>The implements-link kind for a code kind, or null when its marks prove rather than implement.</summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>The implements-link kind for the code kind, or null when its marks prove rather than implement.</returns>
    private static string? LinkKind(CodeKind kind) => kind switch
    {
        CodeKind.Handler => "handler",
        CodeKind.EventHandler => "reaction",
        CodeKind.Job => "job",
        CodeKind.Endpoint => "endpoint",
        CodeKind.DomainEvent => "event",
        CodeKind.AggregateRoot => "aggregate-root",
        CodeKind.AggregateMethod => "aggregate-method",
        CodeKind.RuleException => "rule-exception",
        CodeKind.DomainService => "domain-service",
        CodeKind.Port => "port",
        CodeKind.Adapter => "adapter",
        _ => null,
    };

    /// <summary>
    ///     Counts a marked rule exception as declaring its refusal code: the exception
    ///     holds a declaring literal for the code of the refusal result its rule names,
    ///     so the result link and <c>refusal-unimplemented</c> read the mark.
    /// </summary>
    /// <param name="model">The project model.</param>
    private static void MarkDeclaredRefusals(ProjectModel model)
    {
        var held = model.Code.Literals
            .Select(literal => (literal.Value, literal.File, literal.Class))
            .ToHashSet();

        foreach (var element in model.Code.Elements)
        {
            // A hand-written Code is what the exception throws: its literal is already read,
            // and a value that differs from the record is runtime-value-drift, not a thrown record code.
            if (element.Kind != CodeKind.RuleException || element.RuntimeValue is not null)
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (!model.ById.TryGetValue(mark.RecordId, out var rule) || rule.Type != RecordTypes.Rule)
                {
                    continue;
                }

                if (RefusalCodes.ResultCode(model, rule) is not { } code)
                {
                    continue;
                }

                if (held.Add((code, element.File, element.Class)))
                {
                    model.Code.Literals.Add(new CodeLiteral(code, element.File, element.Line, element.Class, Declares: true));
                }

                break;
            }
        }
    }
}
