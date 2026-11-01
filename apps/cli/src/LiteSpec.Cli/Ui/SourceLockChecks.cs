using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>
///     The source-lock checks of the UI validator: the lock the pinned CLI
///     wrote, its preset against the baseline, and each recorded digest.
/// </summary>
/// <follows>litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
sealed class SourceLockChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    /// <summary>A pinned semantic version.</summary>
    static readonly Regex PinnedVersion = new("^[0-9]+\\.[0-9]+\\.[0-9]+$", RegexOptions.Compiled);

    /// <summary>
    ///     Initializes a new instance of the <see cref="SourceLockChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public SourceLockChecks(UiSession session)
    {
        this.session = session;
    }

    // The source lock describes one copy of the registry source. Its paths resolve
    // against the package root rather than an application root, so a second
    // application cannot hold a copy the lock does not see.
    /// <summary>
    ///     Validates the source lock against the baseline and the recorded digests.
    /// </summary>
    /// <param name="file">The source lock file.</param>
    /// <param name="baseline">The manifest baseline.</param>
    /// <param name="packageRoot">The shared package root.</param>
    /// <param name="manifest">The parsed manifest.</param>
    /// <param name="baselineOverride">Whether an exception allows another registry.</param>
    /// <param name="platform">The package platform.</param>
    public void ValidateSourceLock(string file, JsonNode? baseline, string packageRoot, JsonNode? manifest, bool baselineOverride, string platform)
    {
        var label = session.RelativeToRoot(file);
        var lockNode = session.ReadJson(file, label);
        if (lockNode is null)
        {
            return;
        }

        foreach (var key in new[] { "schemaVersion", "generator", "cli", "preset", "registry", "formatter", "components" })
        {
            session.Required(lockNode, key, label);
        }

        session.SchemaCheck("ui-source-lock.schema.json", lockNode, label, "litespec/frontend/web/ui#track-source-changes");
        var cliPackage = platform == "svelte" ? "shadcn-svelte" : "shadcn";
        var expectedCli = JsonRead.Chain(manifest, "packages", "npm", cliPackage);
        var cli = JsonRead.Child(lockNode, "cli");
        var cliText = cli is { Node: JsonValue cliValue } && cliValue.TryGetValue<string>(out var cliRaw) ? cliRaw : "";

        // The pinned-version test already rejects `latest` and every other range, so
        // no separate branch for the word is reachable.
        if (!PinnedVersion.IsMatch(cliText))
        {
            session.Errors.Add($"{label}: cli must be a pinned semantic version");
        }
        else if (JsonRead.Truthy(expectedCli) && !JsonRead.StrictEqual(cli, expectedCli))
        {
            session.Errors.Add($"{label}: cli must match manifest {cliPackage} pin '{JsonRead.Show(expectedCli)}'");
        }

        var lockRegistry = JsonRead.Child(lockNode, "registry");
        var baselineRegistry = JsonRead.Child(baseline, "registry");
        if ((!JsonRead.StrictEqual(JsonRead.Dot(lockRegistry, "name"), JsonRead.Dot(baselineRegistry, "name")) || !JsonRead.StrictEqual(JsonRead.Dot(lockRegistry, "url"), JsonRead.Dot(baselineRegistry, "url"))) && !baselineOverride)
        {
            session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: registry '{JsonRead.ShowOr(JsonRead.Dot(lockRegistry, "name"), "(unnamed)")}' is not the built-in {JsonRead.ShowOr(JsonRead.Dot(baselineRegistry, "name"), "registry")} registry; an additional registry requires an exception");
        }

        foreach (var key in new[] { "style", "base", "cssVariables", "baseColor", "theme", "chartColor", "font", "fontHeading", "icons", "radius", "menuAccent", "menuColor", "componentsStyle" })
        {
            var want = JsonRead.Child(baseline, key);
            if (want.Found && !JsonRead.StrictEqual(JsonRead.Dot(JsonRead.Child(lockNode, "preset"), key), want))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package] {label}: preset.{key} does not match the manifest baseline");
            }
        }

        var pinnedPackages = JsonRead.NpmKeys(manifest);
        var items = session.ReadArray(lockNode, "components", label);
        session.Unique(items.Select(item => JsonRead.Child(item, "name")), $"{label}.components");
        foreach (var item in items)
        {
            var itemLabel = $"{label}.{JsonRead.ShowOr(JsonRead.Child(item, "name"), "component")}";
            foreach (var key in new[] { "name", "address", "status", "paths", "digest", "dependencies" })
            {
                session.Required(item, key, itemLabel);
            }

            foreach (var dependency in session.ReadArray(item, "dependencies", itemLabel))
            {
                var name = dependency is JsonValue dependencyValue && dependencyValue.TryGetValue<string>(out var dependencyName) ? dependencyName : null;
                if (name is null || !pinnedPackages.Contains(name))
                {
                    session.Errors.Add($"[litespec/frontend/web/ui#track-source-changes] {itemLabel}: dependency '{JsonRead.ShowNode(dependency)}' is not pinned in the LiteSpec manifest");
                }
            }

            foreach (var path in session.ReadArray(item, "paths", itemLabel))
            {
                if (path is not JsonValue pathValue || !pathValue.TryGetValue<string>(out var relative))
                {
                    continue;
                }

                var candidate = Path.GetFullPath(Path.Combine(packageRoot, relative));
                if (!FileWalk.Within(packageRoot, candidate))
                {
                    session.Errors.Add($"{itemLabel}: path escapes the shared package root '{relative}'");
                    continue;
                }

                if (!FileWalk.Exists(candidate))
                {
                    session.Errors.Add($"{itemLabel}: source path does not exist '{relative}'");
                    continue;
                }

                var actual = session.Digest(candidate);
                if (actual is null)
                {
                    session.Errors.Add($"{itemLabel}: cannot read source path '{relative}'");
                    continue;
                }

                var recorded = JsonRead.Child(item, "digest");
                var matches = recorded is { Node: JsonValue recordedValue } && recordedValue.TryGetValue<string>(out var recordedText) && recordedText == actual;
                var status = JsonRead.Child(item, "status");
                var isBaseline = status is { Node: JsonValue statusValue } && statusValue.TryGetValue<string>(out var statusText) && statusText == "baseline";
                if (!matches && isBaseline)
                {
                    session.Errors.Add($"[litespec/frontend/web/ui#track-source-changes] {itemLabel}: '{relative}' no longer matches the recorded digest; record it as extended or forked");
                }
            }
        }
    }
}
