using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The agent protocol checks: the project's <c>AGENTS.md</c> holds the <c>agent-protocol</c>
///     region, and the region matches what <c>model render</c> writes for the carried release.
/// </summary>
/// <follows>litespec/core/agent#keep-the-agent-protocol-current</follows>
internal static class AgentProtocolChecks
{
    /// <summary>The finding code for an <c>AGENTS.md</c> with no region.</summary>
    internal const string MissingCode = "agent-protocol-missing";

    /// <summary>The finding code for a region that differs from the rendered one.</summary>
    internal const string DriftCode = "agent-protocol-drift";

    /// <summary>Reports agent-protocol-missing and agent-protocol-drift for the project's <c>AGENTS.md</c>.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRegion(ProjectModel model, List<Finding> findings)
    {
    }
}
