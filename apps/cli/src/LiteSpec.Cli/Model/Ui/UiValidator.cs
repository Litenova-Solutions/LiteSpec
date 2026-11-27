namespace LiteSpec.Cli.Model.Ui;

/// <summary>
///     The controlled web UI validator: the in-process <see cref="Validate"/> the consumer pass calls.
/// </summary>
/// <follows>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</follows>
/// <follows>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</follows>
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
