using System.Text.Json.Nodes;

namespace LiteSpec.Ui;

/// <summary>The collected result of one <see cref="UiValidator.Validate"/> run.</summary>
/// <param name="ExitCode">What <c>litespec ui</c> returns: 0 pass, 1 problems, 2 unusable root.</param>
/// <param name="Problems">The same strings the JSON output's problems array holds.</param>
/// <param name="Consumer">The consumer root the run checked.</param>
/// <param name="SharedPackage">The declared shared package, or null when none resolved.</param>
/// <param name="ControlledFrontends">How many controlled frontends were checked.</param>
/// <param name="SkippedFrontends">The declared frontends outside the controlled contract.</param>
/// <param name="HasPackage">Whether a shared UI package is declared.</param>
/// <param name="Frontends">Every declared frontend, for the skipped scope line.</param>
sealed record UiReport(
    int ExitCode,
    IReadOnlyList<string> Problems,
    string Consumer,
    UiSharedPackage? SharedPackage,
    int ControlledFrontends,
    IReadOnlyList<UiFrontend> SkippedFrontends,
    bool HasPackage,
    IReadOnlyList<UiFrontend> Frontends)
{
    /// <summary>Builds the JSON object <c>--format json</c> writes.</summary>
    /// <returns>The JSON report.</returns>
    public JsonObject ToJson()
    {
        JsonNode? shared = null;
        if (SharedPackage is not null)
        {
            var package = new JsonObject();
            if (SharedPackage.NameFound)
            {
                package["name"] = SharedPackage.Name?.DeepClone();
            }

            if (SharedPackage.PathFound)
            {
                package["path"] = SharedPackage.Path?.DeepClone();
            }

            var exports = new JsonArray();
            foreach (var entry in SharedPackage.Exports)
            {
                exports.Add(entry?.DeepClone());
            }

            package["exports"] = exports;
            shared = package;
        }

        var skipped = new JsonArray();
        foreach (var frontend in SkippedFrontends)
        {
            var entry = new JsonObject();
            if (frontend.NameFound)
            {
                entry["name"] = frontend.Name?.DeepClone();
            }

            entry["platform"] = frontend.Platform?.DeepClone();
            skipped.Add(entry);
        }

        var problems = new JsonArray();
        foreach (var problem in Problems)
        {
            problems.Add(problem);
        }

        return new JsonObject
        {
            ["tool"] = "ui",
            ["consumer"] = Consumer,
            ["ok"] = Problems.Count == 0,
            ["sharedPackage"] = shared,
            ["controlledFrontends"] = ControlledFrontends,
            ["skippedFrontends"] = skipped,
            ["problems"] = problems,
        };
    }
}
