using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The compensation checks: a reaction that undoes a committed step names that step's use case in compensates, runs another use case, and answers an event the undone use case does not raise.</summary>
/// <follows>litespec/core/automation#name-the-step-a-compensating-reaction-undoes</follows>
internal static class CompensationChecks
{
    /// <summary>The reaction field naming the use case a compensating reaction undoes.</summary>
    const string Compensates = "compensates";

    /// <summary>Reports each compensating reaction whose target is no command, which runs the step it undoes, or which answers an event the undone step raises.</summary>
    /// <param name="model">The loaded project model that holds every reaction, use case and event.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var reaction in model.Records.Where(record => record.Type == RecordTypes.Reaction))
        {
            if (reaction.Field(Compensates) is not { Length: > 0 } compensatedId)
            {
                continue;
            }

            // An unknown identifier is reference-unknown through the citation checks.
            if (!model.ById.TryGetValue(Identifiers.BaseId(compensatedId), out var compensated))
            {
                continue;
            }

            if (compensated.Type != RecordTypes.UseCase)
            {
                findings.Add(new Finding("compensation-target-invalid", $"Reaction '{reaction.Id}' compensates '{compensatedId}', which is a {compensated.Type}.", reaction.File, reaction.Line, SubjectOf(reaction)));
                continue;
            }

            var kind = compensated.Field(RecordFields.Kind);

            if (kind != "command")
            {
                findings.Add(new Finding(
                    "compensation-target-invalid",
                    kind is null
                        ? $"Reaction '{reaction.Id}' compensates '{compensatedId}', which has no kind."
                        : $"Reaction '{reaction.Id}' compensates '{compensatedId}', which is a {kind}.",
                    reaction.File,
                    reaction.Line,
                    SubjectOf(reaction)));
            }

            if (reaction.EdgeIds(RecordFields.Runs).Contains(compensatedId, StringComparer.Ordinal))
            {
                findings.Add(new Finding(
                    "compensation-target-invalid",
                    $"Reaction '{reaction.Id}' compensates '{compensatedId}' and runs it; a compensating reaction runs the undo, not the undone step.",
                    reaction.File,
                    reaction.Line,
                    SubjectOf(reaction)));
            }

            // The trigger check is a minimum: it reports a trigger the compensated use case
            // lists in its raises, which is the step's own success fact. Whether the trigger
            // reports a later step's failure is left to a reviewer.
            var raised = new HashSet<string>(
                compensated.Ids(RecordFields.Raises).Select(id => Identifiers.BaseId(id)),
                StringComparer.Ordinal);

            foreach (var trigger in ReactionEventTriggers(reaction).Order(StringComparer.Ordinal))
            {
                if (raised.Contains(Identifiers.BaseId(trigger)))
                {
                    findings.Add(new Finding(
                        "compensation-trigger-invalid",
                        $"Reaction '{reaction.Id}' compensates '{compensatedId}' and answers '{trigger}', which '{compensatedId}' raises.",
                        reaction.File,
                        reaction.Line,
                        SubjectOf(reaction)));
                }
            }
        }
    }
}
