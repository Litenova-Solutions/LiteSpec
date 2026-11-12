namespace LiteSpec.Cli.Model.Ui;

/// <summary>
///     The controlled web UI validator: the in-process <see cref="Validate"/> the consumer pass calls.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class UiValidator
{
    /// <summary>
    ///     Validates the consumer at <paramref name="consumerRoot"/> in-process.
    ///     Never writes to the console and never exits the process.
    /// </summary>
    /// <param name="consumerRoot">The consumer root to validate.</param>
    /// <returns>The collected report.</returns>
    public static UiReport Validate(string consumerRoot) => new UiSession(consumerRoot).Execute();
}
