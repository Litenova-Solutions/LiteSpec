using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Diff;

/// <summary>
///     Finds the client-breaking contract changes between the base model and the
///     working tree: a removed result, a removed refusal code, and a changed
///     integration event. Each becomes a contract-break finding.
/// </summary>
/// <follows>litespec/core/language#keep-wire-codes-stable</follows>
static class ContractBreaks
{
    /// <summary>Finds every breaking contract change from the base to the head.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <returns>The contract-break findings, ordered by subject.</returns>
    public static IReadOnlyList<Finding> Find(ProjectModel baseModel, ProjectModel headModel)
    {
        var baseById = baseModel.ById;
        var headById = headModel.ById;
        var findings = new List<Finding>();

        // A derived generic result follows its use case, so only written
        // records are compared: a derived record never stands on its own.
        foreach (var id in baseById.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            var before = baseById[id];

            if (before.Derived)
            {
                continue;
            }

            if (!headById.TryGetValue(id, out var after) || after.Derived)
            {
                if (before.Type == RecordTypes.Result)
                {
                    findings.Add(RemovedResult(headModel, before));
                }
                else if (before.Type == RecordTypes.IntegrationEvent)
                {
                    findings.Add(RemovedEvent(headModel, before));
                }

                continue;
            }

            if (before.Type == RecordTypes.Result)
            {
                RefusalCode(before, after, findings);
            }
            else if (before.Type == RecordTypes.IntegrationEvent
                && !string.Equals(
                    Checker.SemanticHash(before), Checker.SemanticHash(after), StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "contract-break",
                    $"Integration event '{before.Id}' changed since the base.",
                    after.File,
                    after.Line,
                    before.Id));
            }
        }

        return findings;
    }

    /// <summary>The finding for a result the head no longer holds.</summary>
    /// <param name="headModel">The head model.</param>
    /// <param name="before">The base result.</param>
    /// <returns>The contract-break finding on the holding use case, or the project file when it is gone too.</returns>
    static Finding RemovedResult(ProjectModel headModel, Record before)
    {
        var holder = Identifiers.TrySplit(before.Id, out _, out var home, out _)
            && home is not null
            ? ModelBuilder.UseCaseOf(headModel, home)
            : null;

        return new Finding(
            "contract-break",
            $"Result '{before.Id}' no longer exists.",
            holder?.File ?? ConsumerRoot.ProjectFile(headModel.Config.Root),
            holder?.Line ?? 1,
            before.Id);
    }

    /// <summary>The finding for an integration event the head no longer holds.</summary>
    /// <param name="headModel">The head model.</param>
    /// <param name="before">The base integration event.</param>
    /// <returns>The contract-break finding on the project file, since no head record holds the removed event.</returns>
    static Finding RemovedEvent(ProjectModel headModel, Record before) =>
        new(
            "contract-break",
            $"Integration event '{before.Id}' no longer exists.",
            ConsumerRoot.ProjectFile(headModel.Config.Root),
            1,
            before.Id);

    /// <summary>Adds the finding for a refusal code the head removed or changed, if it did.</summary>
    /// <param name="before">The base result.</param>
    /// <param name="after">The head result.</param>
    /// <param name="findings">The findings a removed or changed code adds to.</param>
    static void RefusalCode(Record before, Record after, List<Finding> findings)
    {
        var oldCode = before.Field(RecordFields.Code);
        var newCode = after.Field(RecordFields.Code);

        if (oldCode is null || string.Equals(oldCode, newCode, StringComparison.Ordinal))
        {
            return;
        }

        findings.Add(new Finding(
            "contract-break",
            newCode is null
                ? $"Refusal code '{oldCode}' of result '{before.Id}' no longer exists."
                : $"Refusal code '{oldCode}' of result '{before.Id}' changed to '{newCode}'.",
            after.File,
            after.Line,
            before.Id));
    }
}
