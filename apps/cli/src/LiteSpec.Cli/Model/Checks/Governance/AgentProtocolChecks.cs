using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.ProseText;

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
        var file = Path.Combine(model.Config.Root, AgentProtocol.FileName);

        // A project without an agent context has no region to keep current; init writes one.
        if (!File.Exists(file))
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var match = AgentProtocol.FindRegion(text);

        if (match is null)
        {
            findings.Add(new Finding(MissingCode, "AGENTS.md holds no agent-protocol generated region.", file, 1, null));
            return;
        }

        if (AgentProtocol.ExpectedRegion(model) is not { } expected)
        {
            return;
        }

        // Both sides are compared with LF endings, so a CRLF page never drifts on its line endings alone.
        var actual = match.Value.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (actual != expected.Replace("\r\n", "\n", StringComparison.Ordinal))
        {
            findings.Add(new Finding(DriftCode, "The agent-protocol region in AGENTS.md differs from what model render writes.", file, LineOf(LineStarts(text), match.Index), null));
        }
    }
}
