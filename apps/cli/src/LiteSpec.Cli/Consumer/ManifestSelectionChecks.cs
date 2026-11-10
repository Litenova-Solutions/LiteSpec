using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Consumer;

/// <summary>
///     The manifest and selection checks of the consumer validator: the recorded
///     release, the stack names and the extension selections against the
///     pinned manifest, and stage deferrals.
/// </summary>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
/// <follows>litespec/core/scope#select-a-size-a-shape-and-stacks</follows>
/// <follows>litespec/core/scope#declare-the-conformance-level</follows>
/// <follows>litespec/core/scope#record-selected-extensions</follows>
static class ManifestSelectionChecks
{
    /// <summary>The frontend stacks assumed when no manifest is present.</summary>
    static readonly string[] DefaultStacks = ["android", "blazor", "react", "svelte"];

    // Whether a manifest path sits under a folder, compared by text.
    /// <summary>
    ///     Whether a manifest path sits under a folder, compared by text.
    /// </summary>
    /// <param name="path">The manifest path.</param>
    /// <param name="folder">The folder to compare against.</param>
    /// <returns>True when the path is the folder or sits under it.</returns>
    static bool IsUnder(string path, string folder) =>
        path == folder || path.StartsWith(folder + "/", StringComparison.Ordinal);

    /// <summary>One provision row of the provision index.</summary>
    static readonly Regex ProvisionRow = new(@"^\| (litespec/[^\s|]*#[^\s|]*) \|", RegexOptions.Multiline);

    /// <summary>
    ///     Holds the recorded release, the stack names and the extension selections
    ///     against the pinned manifest.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckManifest(ConsumerRun run)
    {
        var projectFile = ConsumerRoot.ProjectFile(run.Root);
        var manifest = run.ReadManifest();

        // Adopting a release means accepting its complete contract, so the project
        // records the release it reviewed. A mismatch means the installed tool
        // carries a release nobody re-read the contract for.
        if (JsonValues.IsTruthy((manifest as JsonObject)?["version"]))
        {
            var version = (manifest as JsonObject)!["version"];
            var reviewed = (run.Project as JsonObject)?["reviewedLiteSpecVersion"];

            if (!JsonValues.IsTruthy(reviewed))
            {
                run.Err(new Finding(
                    "release-drift",
                    $"missing 'reviewedLiteSpecVersion'; the installed LiteSpec tool release is {JsonValues.Text(version)}",
                    projectFile,
                    1,
                    null));
            }
            else if (!JsonValues.StrictEqual(reviewed, version))
            {
                run.Err(new Finding(
                    "release-drift",
                    $"reviewedLiteSpecVersion '{JsonValues.Text(reviewed)}' does not match the installed LiteSpec tool release {JsonValues.Text(version)}; install the reviewed release with 'dotnet tool update LiteSpec.Cli --version {JsonValues.Text(reviewed)}' and 'dotnet tool restore' for the .NET tool, or rerun the install script in the project root for the standalone binary, or re-review the contract and record {JsonValues.Text(version)}",
                    projectFile,
                    1,
                    null));
            }
        }

        // A stack the pinned release does not ship selects no page, so every
        // stack-scoped provision would pass by applying to nothing.
        var project = run.Project as JsonObject;
        var manifestObj = manifest as JsonObject;
        var shape = JsonValues.PropString(project, "shape") ?? "server";
        var named = project?["stacks"] is JsonArray declaredStacks
            ? declaredStacks.Select(JsonValues.AsString).OfType<string>().ToList()
            : new List<string>();

        if (manifestObj is not null)
        {
            var stacks = LiteSpec.Authoring.Layout.Stacks(manifestObj);
            var byId = stacks.ToDictionary(stack => stack.Id, stack => stack, StringComparer.Ordinal);
            var backendIds = stacks.Where(stack => stack.Kind == "backend").Select(stack => stack.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();
            var frontendIds = stacks.Where(stack => stack.Kind == "frontend").Select(stack => stack.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();

            run.KnownStacks.AddRange(frontendIds);

            foreach (var stack in named.Where(stack => !byId.ContainsKey(stack)))
            {
                run.Err(new Finding(
                    "stack-invalid",
                    $"stack '{stack}' is not a stack the pinned LiteSpec release ships [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                    projectFile,
                    1,
                    null));
            }

            var backends = named.Where(byId.ContainsKey).Where(stack => byId[stack].Kind == "backend").ToList();

            // At the records level stacks are informational, so a project may name none;
            // every other level selects its stacks here.
            var claims = project?["layers"] as JsonObject;
            var recordsLevel = ProjectSettings.LevelOf(JsonValues.PropString(claims, "implementation"), JsonValues.PropString(claims, "proof")) == ConformanceLevel.Records;

            // A server project names exactly one backend stack; a client-only
            // project names none, and reads the backend pages its shape lists.
            if (!recordsLevel && shape == "client-only")
            {
                foreach (var backend in backends)
                {
                    run.Err(new Finding(
                        "stack-invalid",
                        $"shape 'client-only' names backend stack '{backend}'; a client-only project names no backend stack [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                        projectFile,
                        1,
                        null));
                }
            }
            else if (!recordsLevel && backends.Count != 1)
            {
                run.Err(new Finding(
                    "stack-invalid",
                    $"stacks name {backends.Count} backend stacks; name exactly one of {string.Join(", ", backendIds)} [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                    projectFile,
                    1,
                    null));
            }

            // A stack whose requires is not also named misses the pages its own
            // stack builds on, so the selection applies half a stack.
            foreach (var stack in named.Where(byId.ContainsKey))
            {
                foreach (var required in byId[stack].Requires)
                {
                    if (!named.Contains(required))
                    {
                        run.Err(new Finding(
                            "stack-invalid",
                            $"stack '{stack}' requires '{required}', which the project does not name in stacks [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                            projectFile,
                            1,
                            null));
                    }
                }
            }

            // A frontend stack whose pairsWith excludes the project's backend stack
            // works beside another backend only; absent pairsWith means any backend.
            foreach (var stack in named.Where(byId.ContainsKey).Where(stack => byId[stack].Kind == "frontend"))
            {
                var pairs = byId[stack].PairsWith;

                if (pairs.Count == 0)
                {
                    continue;
                }

                foreach (var backend in backends.Where(backend => !pairs.Contains(backend)))
                {
                    run.Err(new Finding(
                        "stack-invalid",
                        $"frontend stack '{stack}' pairs only with {string.Join(", ", pairs)}, not with backend stack '{backend}' [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                        projectFile,
                        1,
                        null));
                }
            }

            if (manifestObj["extensions"] is JsonObject extensions)
            {
                // A selection is checked against the manifest, not only against itself, so a
                // release that renamed an extension fails the stale selection rather than
                // passing a contract nobody selected.
                var known = extensions.Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToList();
                var reported = new HashSet<string>(StringComparer.Ordinal);

                foreach (var selection in run.Selections)
                {
                    if (!reported.Add(selection.IdText))
                    {
                        continue;
                    }

                    if (selection.Id is null || !extensions.TryGetPropertyValue(selection.Id, out var shipped) || !JsonValues.IsTruthy(shipped))
                    {
                        run.Err(new Finding(
                            "extension-invalid",
                            $"extensions '{selection.IdText}' is not an extension in the pinned LiteSpec release; known ids are {string.Join(", ", known)}",
                            projectFile,
                            1,
                            null));
                    }
                    else
                    {
                        // An extension under a stack folder supports that stack only;
                        // under docs/ext/ it supports every stack. Selecting it without
                        // its stack applies nothing it states.
                        var path = JsonValues.AsString((shipped as JsonObject)?["path"]);

                        if (path is not null && !IsUnder(path, "docs/ext"))
                        {
                            var supported = stacks.Where(stack => IsUnder(path, stack.Folder)).Select(stack => stack.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();

                            if (supported.Count > 0 && !named.Any(supported.Contains))
                            {
                                run.Err(new Finding(
                                    "extension-invalid",
                                    $"extensions '{selection.IdText}' supports only the {string.Join(", ", supported)} stack, which the project does not name in stacks [litespec/core/scope#select-a-size-a-shape-and-stacks]",
                                    projectFile,
                                    1,
                                    null));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            run.KnownStacks.AddRange(DefaultStacks);
        }
    }

    /// <summary>
    ///     Holds stage deferrals to their exception records: each departure names its
    ///     deferred provision in covers, so a deferral of an unknown provision is
    ///     refused here. A deferral past its date belongs to the model check, which
    ///     reports exception-expired.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckStageDeferrals(ConsumerRun run)
    {
        var stage = run.Selections.FirstOrDefault(selection => selection.Id == "stage");

        if (stage is null)
        {
            return;
        }

        // The page scan already reported every malformed record file, so this pass reads
        // quietly and keeps the departure exceptions only.
        var deferrals = new List<(string File, string Id, string? Provision)>();

        foreach (var file in run.Files)
        {
            if (!RecordFile.IsRecordFile(file))
            {
                continue;
            }

            if (!RecordFile.TryParse(run.ReadText(file), out var root, out _, out _))
            {
                continue;
            }

            var page = JsonNode.Parse(root.GetRawText()) as JsonObject;

            if (page is not null
                && JsonValues.PropString(page, "type") == "exception"
                && JsonValues.PropString(page, "kind") == "departure")
            {
                deferrals.Add((file, JsonValues.PropText(page, "id"), JsonValues.AsString(page["covers"])));
            }
        }

        if (deferrals.Count == 0)
        {
            return;
        }

        var indexFile = Path.Combine(run.StandardsRoot, "docs", "reference", "provisions.md");
        var knownProvisions = new HashSet<string>(StringComparer.Ordinal);

        if (File.Exists(indexFile))
        {
            foreach (Match match in ProvisionRow.Matches(run.ReadText(indexFile)))
            {
                knownProvisions.Add(match.Groups[1].Value);
            }
        }

        foreach (var (file, id, provision) in deferrals)
        {
            // A covers that is no string already failed the record schema beside this page.
            if (provision is null || knownProvisions.Contains(provision))
            {
                continue;
            }

            run.Err(new Finding(
                "stage-deferral-invalid",
                $"exception '{id}' defers '{provision}', which is not a provision in the pinned LiteSpec release",
                file,
                1,
                id));
        }
    }
}
