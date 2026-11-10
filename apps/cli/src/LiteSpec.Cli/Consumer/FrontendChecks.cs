using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Consumer;

/// <summary>
///     The frontend declaration checks of the consumer validator: every declared
///     frontend's directory and stack, and the shared UI package's paths. The project
///     schema states the shape of each frontend and of the package, so this pass checks
///     only what a schema cannot: that the paths exist and that a frontend's stack is one
///     the project names.
/// </summary>
/// <follows>litespec/core/scope#select-a-size-a-shape-and-stacks</follows>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
static class FrontendChecks
{
    /// <summary>The platforms whose frontends compose the shared UI package.</summary>
    static readonly string[] ControlledPlatforms = ["react", "svelte"];

    /// <summary>
    ///     Checks every declared frontend's directory and stack, routes the controlled
    ///     ones to the shared UI package, and checks the package's own paths.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckFrontends(ConsumerRun run)
    {
        var projectFile = ConsumerRoot.ProjectFile(run.Root);
        var frontends = (run.Project as JsonObject)?["frontends"] is JsonArray array ? array.ToList() : new List<JsonNode?>();

        foreach (var frontend in frontends)
        {
            var declared = frontend as JsonObject;
            var name = declared is not null && declared.TryGetPropertyValue("name", out var nameValue) && nameValue is not null
                ? JsonValues.Text(nameValue)
                : "(unnamed)";

            // A declared frontend whose directory is absent reads as a frontend with no
            // code, and every check for it passes by having nothing to read.
            if (declared is not null && declared.TryGetPropertyValue("path", out var frontendPath) && JsonValues.AsString(frontendPath) is string frontendDir)
            {
                if (!File.Exists(Path.Join(run.Root, frontendDir)) && !Directory.Exists(Path.Join(run.Root, frontendDir)))
                {
                    run.Err(new Finding(
                        "frontend-invalid",
                        $"frontend '{name}' path '{frontendDir}' does not exist; the checks for that frontend read nothing",
                        projectFile,
                        1,
                        null));
                }
            }

            // The platform's value and its kind are the schema's to state. What the schema
            // cannot see is whether the project names that stack, so a frontend built on a
            // stack the project leaves out is reported here.
            var stack = declared is not null ? JsonValues.PropString(declared, "platform") : null;
            var namedStacks = (run.Project as JsonObject)?["stacks"] is JsonArray namedArray
                ? namedArray.Select(JsonValues.AsString).OfType<string>().ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            if (stack is null)
            {
                continue;
            }

            if (!namedStacks.Contains(stack))
            {
                run.Err(new Finding(
                    "frontend-invalid",
                    $"frontend '{name}' platform '{stack}' is not a stack the project names in stacks [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                    projectFile,
                    1,
                    null));
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
        var uiPackage = (run.Project as JsonObject)?["ui"] is JsonObject ui && ui.TryGetPropertyValue("package", out var packageValue) ? packageValue : null;
        run.UiPackage = uiPackage;

        if (JsonValues.IsTruthy(uiPackage))
        {
            var package = uiPackage as JsonObject;
            var packageName = JsonValues.PropString(package, "name") ?? "(unnamed)";
            var label = $"ui.package '{packageName}'";

            foreach (var key in new[] { "path", "componentsJson", "primitives", "tokens", "sourceLock", "designContract" })
            {
                if (package is not null && package.TryGetPropertyValue(key, out var configured) && JsonValues.AsString(configured) is string configuredPath)
                {
                    if (!File.Exists(Path.Join(run.Root, configuredPath)) && !Directory.Exists(Path.Join(run.Root, configuredPath)))
                    {
                        run.Err(new Finding("frontend-invalid", $"{label}: {key} path does not exist '{configuredPath}'", projectFile, 1, null));
                    }
                }
            }

            foreach (var frontend in run.ControlledFrontends)
            {
                var declared = frontend as JsonObject;
                var frontendUi = declared?["ui"] as JsonObject;

                if (frontendUi is not null && frontendUi.TryGetPropertyValue("globalCss", out var stylesheetValue) && JsonValues.AsString(stylesheetValue) is string stylesheet)
                {
                    if (!File.Exists(Path.Join(run.Root, stylesheet)) && !Directory.Exists(Path.Join(run.Root, stylesheet)))
                    {
                        run.Err(new Finding(
                            "frontend-invalid",
                            $"frontend '{JsonValues.PropText(declared, "name")}' ui.globalCss path does not exist '{stylesheet}'",
                            projectFile,
                            1,
                            null));
                    }
                }
            }
        }
    }
}
