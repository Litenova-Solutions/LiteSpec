using LiteSpec.Ui;

namespace LiteSpec.Consumer;

/// <summary>
///     The controlled UI delegation of the consumer validator: the UI validator's
///     findings carried into this run's own list.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class ControlledUiChecks
{
    /// <summary>
    ///     Carries the controlled UI validator's findings into this run's own list, so a caller
    ///     reading the structured output has no separate block to look at.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckControlledUi(ConsumerRun run)
    {
        // A controlled-web consumer opts into the deterministic UI validator through its
        // frontend platform declaration or UI block.
        if (run.ControlledFrontends.Count == 0 && !JsonValues.IsTruthy(run.UiPackage))
        {
            return;
        }

        var report = UiValidator.Validate(run.Root);

        foreach (var problem in report.Problems)
        {
            run.Err($"controlled UI: {problem}");
        }

        if (report.ExitCode != 0 && report.Problems.Count == 0)
        {
            run.Err($"controlled UI validation failed with exit code {report.ExitCode}");
        }
    }
}
