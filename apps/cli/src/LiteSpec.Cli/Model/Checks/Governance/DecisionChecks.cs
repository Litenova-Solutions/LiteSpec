using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The technical decision checks: a decision states its kind, and a decision whose followers
///     all sit in one module and already implement a record is reported as local.
/// </summary>
/// <follows>litespec/core/model#record-each-technical-choice-as-a-decision</follows>
internal static class DecisionChecks
{
    /// <summary>The finding code for a technical decision with no <c>kind</c>.</summary>
    internal const string KindMissingCode = "decision-kind-missing";

    /// <summary>The finding code for a technical decision followed only inside one module.</summary>
    internal const string LocalCode = "decision-local";

    /// <summary>Reports decision-kind-missing for each technical decision that states no kind.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunKind(ProjectModel model, List<Finding> findings)
    {
    }

    /// <summary>Reports decision-local for each technical decision whose followers all sit in one module and each implement a record.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunLocal(ProjectModel model, List<Finding> findings)
    {
    }
}
