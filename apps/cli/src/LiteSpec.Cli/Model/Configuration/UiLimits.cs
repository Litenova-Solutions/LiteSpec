using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     The screen composition limits the checks read: the most stacked sections a page holds,
///     the most fields a form holds ungrouped, and the most use cases a screen offers outside
///     a hub frame. The project file states them under <c>ui.limits</c>; anything missing or
///     outside the schema reads as its default. The project schema reports a value of the
///     wrong shape, so this reader never reports one.
/// </summary>
/// <param name="Sections">The most stacked sections a page holds.</param>
/// <param name="FormFields">The most fields a form holds ungrouped.</param>
/// <param name="Offers">The most use cases a screen offers outside a hub frame.</param>
sealed record UiLimits(int Sections, int FormFields, int Offers)
{
    /// <summary>Gets the limits a project without <c>ui.limits</c> reads: three sections, six fields, four offers.</summary>
    public static UiLimits Defaults { get; } = new(3, 6, 4);

    /// <summary>Reads the limits from the project declaration, with the defaults for anything missing or below one.</summary>
    /// <param name="document">The project declaration root object.</param>
    /// <returns>The limits the checks read.</returns>
    public static UiLimits Read(JsonObject document)
    {
        var limits = document["ui"] is JsonObject ui && ui["limits"] is JsonObject found ? found : null;

        return new UiLimits(
            ReadLimit(limits, "sections", Defaults.Sections),
            ReadLimit(limits, "formFields", Defaults.FormFields),
            ReadLimit(limits, "offers", Defaults.Offers));
    }

    /// <summary>Reads one limit, with the default for a missing value or one below one.</summary>
    /// <param name="limits">The <c>ui.limits</c> object, or null when the declaration states none.</param>
    /// <param name="field">The limit field name.</param>
    /// <param name="fallback">The default the check reads.</param>
    /// <returns>The stated limit, or the default.</returns>
    private static int ReadLimit(JsonObject? limits, string field, int fallback) =>
        limits is not null
        && limits[field] is JsonValue value
        && value.TryGetValue<int>(out var limit)
        && limit >= 1
            ? limit
            : fallback;
}
