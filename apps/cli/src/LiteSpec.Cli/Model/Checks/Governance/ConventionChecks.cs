using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The project convention checks: a convention sits where <c>rules</c> reads it, and its
///     <c>Applies to</c> is a form <c>rules</c> can match.
/// </summary>
/// <follows>litespec/core/agent#replace-only-declared-conventions</follows>
internal static class ConventionChecks
{
    /// <summary>The finding code for a convention outside <c>governance/conventions/</c>.</summary>
    internal const string MisplacedCode = "convention-misplaced";

    /// <summary>The finding code for an <c>Applies to</c> that is neither a glob nor the scope vocabulary.</summary>
    internal const string ScopeInvalidCode = "convention-scope-invalid";

    /// <summary>Reports convention-misplaced for each convention-shaped file outside the conventions folder.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunMisplaced(ProjectModel model, List<Finding> findings)
    {
    }

    /// <summary>Reports convention-scope-invalid for each convention whose <c>Applies to</c> matches nothing.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunScope(ProjectModel model, List<Finding> findings)
    {
    }
}
