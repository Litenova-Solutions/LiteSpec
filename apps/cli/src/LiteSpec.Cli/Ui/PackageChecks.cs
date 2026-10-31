using System.Text.Json.Nodes;

namespace LiteSpec.Ui;

/// <summary>
///     The registry configuration checks of the UI validator: the shared
///     package's components file against the manifest baseline.
/// </summary>
/// <follows>litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package</follows>
sealed class PackageChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PackageChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public PackageChecks(UiSession session)
    {
        this.session = session;
    }

    // The registry configuration the pinned CLI wrote. It is read against the
    // manifest baseline, so a package describing a preset the release does not pin
    // reports rather than passing unread.
    /// <summary>
    ///     Validates the registry configuration against the manifest baseline.
    /// </summary>
    /// <param name="file">The components file.</param>
    /// <param name="baseline">The manifest baseline.</param>
    /// <param name="baselineOverride">Whether an exception allows another registry.</param>
    /// <param name="platform">The package platform.</param>
    public void ValidateComponentsJson(string file, JsonNode? baseline, bool baselineOverride, string platform)
    {
        var label = session.RelativeToRoot(file);
        var config = session.ReadJson(file, label);
        if (config is null)
        {
            return;
        }

        // Findings cite the stack-neutral provisions whatever the platform, because
        // the UI rules live on one page per concern with small per-stack sections.
        if (platform == "svelte")
        {
            // shadcn-svelte writes tailwind.baseColor with no cssVariables field, and
            // its icon, menu, and registry fields are optional. An omitted field takes
            // the CLI default the baseline records, so only a present field is
            // compared. The style field is deprecated under Tailwind v4 and unread.
            if (!JsonRead.StrictEqual(JsonRead.Dot(JsonRead.Child(config, "tailwind"), "baseColor"), JsonRead.Child(baseline, "baseColor")))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: tailwind.baseColor must be '{JsonRead.Show(JsonRead.Child(baseline, "baseColor"))}'");
            }

            foreach (var (key, baselineKey) in new[] { ("iconLibrary", "icons"), ("menuColor", "menuColor"), ("menuAccent", "menuAccent") })
            {
                var want = JsonRead.Child(baseline, baselineKey);
                var got = JsonRead.Child(config, key);
                if (got.Found && !JsonRead.StrictEqual(got, want))
                {
                    session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: {key} must be {JsonRead.StringifyOrUndefined(want)}");
                }
            }

            var registry = JsonRead.Child(config, "registry");
            if (JsonRead.Truthy(registry) && !JsonRead.StrictEqual(registry, JsonRead.Dot(JsonRead.Child(baseline, "registry"), "url")) && !baselineOverride)
            {
                session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: registry '{JsonRead.Show(registry)}' is not the built-in shadcn-svelte registry; an additional registry requires an exception");
            }

            return;
        }

        foreach (var (key, want) in new (string, JsValue)[]
                 {
                     ("style", JsonRead.Child(baseline, "componentsStyle")),
                     ("iconLibrary", JsonRead.Child(baseline, "icons")),
                     ("menuColor", JsonRead.Child(baseline, "menuColor")),
                     ("menuAccent", JsonRead.Child(baseline, "menuAccent")),
                     ("rtl", new JsValue(true, JsonValue.Create(false))),
                 })
        {
            if (!JsonRead.StrictEqual(JsonRead.Child(config, key), want))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: {key} must be {JsonRead.StringifyOrUndefined(want)}");
            }
        }

        var tailwind = JsonRead.Child(config, "tailwind");
        if (!JsonRead.StrictEqual(JsonRead.Dot(tailwind, "baseColor"), JsonRead.Child(baseline, "baseColor")))
        {
            session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: tailwind.baseColor must be '{JsonRead.Show(JsonRead.Child(baseline, "baseColor"))}'");
        }

        var cssVariables = JsonRead.Child(baseline, "cssVariables");
        if (!JsonRead.StrictEqual(JsonRead.Dot(tailwind, "cssVariables"), cssVariables))
        {
            session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: tailwind.cssVariables must be {JsonRead.Show(cssVariables)}");
        }

        var registries = JsonRead.Child(config, "registries");
        if (registries is { Found: true, Node: not null } && RegistriesNonEmpty(registries.Node) && !baselineOverride)
        {
            session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: an additional registry requires an exception");
        }
    }

    /// <summary>
    ///     Whether a registries value names any registry.
    /// </summary>
    /// <param name="registries">The registries value.</param>
    /// <returns>True when the value names a registry.</returns>
    static bool RegistriesNonEmpty(JsonNode registries) => registries switch
    {
        JsonObject found => found.Count > 0,
        JsonArray array => array.Count > 0,
        JsonValue value when value.TryGetValue<string>(out var text) => text.Length > 0,
        _ => false,
    };
}
