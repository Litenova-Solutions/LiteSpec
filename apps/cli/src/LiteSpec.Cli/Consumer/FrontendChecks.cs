using System.Text.Json.Nodes;

namespace LiteSpec.Consumer;

/// <summary>
///     The frontend declaration checks of the consumer validator: every declared
///     frontend's directory and platform, and the shared UI package's paths.
/// </summary>
/// <follows>litespec/core/scope#select-a-size-a-shape-and-stacks</follows>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
static class FrontendChecks
{
    /// <summary>The platforms whose frontends compose the shared UI package.</summary>
    static readonly string[] ControlledPlatforms = ["react", "svelte"];

    /// <summary>
    ///     Checks every declared frontend's directory and platform, routes the controlled
    ///     ones to the shared UI package, and checks the package's own paths.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckFrontends(ConsumerRun run)
    {
        // Every frontend declares its stack. Without the field a frontend silently
        // skips every stack-scoped check, and the omission is indistinguishable from a
        // considered position.
        var paths = (run.Project as JsonObject)?["paths"] as JsonObject;
        var frontends = paths?["frontends"] is JsonArray array ? array.ToList() : new List<JsonNode?>();

        foreach (var frontend in frontends)
        {
            var declared = frontend as JsonObject;
            var name = declared is not null && declared.TryGetPropertyValue("name", out var nameValue) && nameValue is not null
                ? JsonValues.Text(nameValue)
                : "(unnamed)";

            // A declared frontend whose directory is absent reads as a frontend with no
            // code, and every check for it passes by having nothing to read.
            if (declared is not null && declared.TryGetPropertyValue("path", out var frontendPath) && JsonRead.AsString(frontendPath) is string frontendDir)
            {
                if (!File.Exists(Path.Join(run.Root, frontendDir)) && !Directory.Exists(Path.Join(run.Root, frontendDir)))
                {
                    run.Err($"litespec.project.json: frontend '{name}' path '{frontendDir}' does not exist; the checks for that frontend read nothing");
                }
            }

            var platform = declared is not null && declared.TryGetPropertyValue("platform", out var platformValue) ? platformValue : null;
            var namedStacks = (run.Project as JsonObject)?["stacks"] is JsonArray namedArray
                ? namedArray.Select(JsonRead.AsString).OfType<string>().ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            // A frontend platform names a frontend stack the project names: a backend
            // stack declared as a platform would select the wrong pages.
            if (!JsonValues.IsTruthy(platform))
            {
                run.Err($"litespec.project.json: frontend '{name}' declares no 'platform'; one of {string.Join(", ", run.KnownStacks)} is required [litespec/core/scope#select-a-size-a-shape-and-stacks]");
            }
            else if (JsonRead.AsString(platform) is not string stack || !run.KnownStacks.Contains(stack))
            {
                run.Err($"litespec.project.json: frontend '{name}' has unknown platform '{JsonValues.Text(platform)}'; expected one of {string.Join(", ", run.KnownStacks)} [litespec/core/scope#select-a-size-a-shape-and-stacks]");
            }
            else if (!namedStacks.Contains(stack))
            {
                run.Err($"litespec.project.json: frontend '{name}' platform '{stack}' is not a stack the project names in stacks [litespec/core/scope#select-a-size-a-shape-and-stacks]");
            }
            else if (ControlledPlatforms.Contains(stack))
            {
                run.ControlledFrontends.Add(frontend);
            }
        }

        // A controlled frontend composes the shared UI package, so the workspace declares
        // it once. The package travels with the workspace rather than with an
        // application, so one missing field is one missing file for every frontend
        // rather than a row per application.
        var uiPackage = paths is not null && paths.TryGetPropertyValue("uiPackage", out var packageValue) ? packageValue : null;
        run.UiPackage = uiPackage;

        if (run.ControlledFrontends.Count > 0 && !JsonValues.IsTruthy(uiPackage))
        {
            run.Err($"litespec.project.json: {run.ControlledFrontends.Count} controlled frontend(s) and no 'paths.uiPackage'; the shared UI package every controlled web frontend composes is undeclared");
        }
        else if (JsonValues.IsTruthy(uiPackage))
        {
            var package = uiPackage as JsonObject;
            var packageName = JsonRead.PropString(package, "name") ?? "(unnamed)";
            var label = $"litespec.project.json: paths.uiPackage '{packageName}'";

            foreach (var key in new[] { "path", "componentsJson", "primitives", "tokens", "sourceLock", "designContract" })
            {
                if (package is not null && package.TryGetPropertyValue(key, out var configured) && JsonRead.AsString(configured) is string configuredPath)
                {
                    if (!File.Exists(Path.Join(run.Root, configuredPath)) && !Directory.Exists(Path.Join(run.Root, configuredPath)))
                    {
                        run.Err($"{label}: {key} path does not exist '{configuredPath}'");
                    }
                }
            }

            foreach (var frontend in run.ControlledFrontends)
            {
                var declared = frontend as JsonObject;
                var ui = declared?["ui"] as JsonObject;

                if (ui is not null && ui.TryGetPropertyValue("globalCss", out var stylesheetValue) && JsonRead.AsString(stylesheetValue) is string stylesheet)
                {
                    if (!File.Exists(Path.Join(run.Root, stylesheet)) && !Directory.Exists(Path.Join(run.Root, stylesheet)))
                    {
                        run.Err($"litespec.project.json: frontend '{JsonRead.PropText(declared, "name")}' ui.globalCss path does not exist '{stylesheet}'");
                    }
                }
            }
        }
    }
}
