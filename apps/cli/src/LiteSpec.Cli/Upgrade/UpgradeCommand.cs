using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Upgrade;

/// <summary>
///     The <c>litespec upgrade</c> command: reports the structured release changes
///     between the reviewed LiteSpec version of the consumer and the carried
///     release, read from the embedded <c>migrations/&lt;version&gt;.yaml</c>
///     manifests. With <c>--check</c> it exits 1 when the project has unapplied changes.
/// </summary>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
static class UpgradeCommand
{
    /// <summary>The usage text for the upgrade command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec upgrade --check --root <consumer> [--format json]
          litespec upgrade --help

        Reports the release changes between the reviewed LiteSpec version the consumer
        records in reviewedLiteSpecVersion and the carried release: the schema field
        renames, the renamed or removed finding codes and analyzer IDs, the retired
        commands, the removed extensions, and the pack changes that apply, in text
        and with --format json.

          --check     List the release changes that apply; exit 1 when any apply.
          --root      The consumer repository to read. Defaults to the working directory.
          --format    json for a JSON object, or text. Defaults to text.
          --help      Print this text and exit.

        Exit codes: 0 no changes apply, 1 changes apply with --check, 2 the options are invalid, the project file is missing, or a version cannot be read.
        """;

    /// <summary>Runs the upgrade command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 none apply, 1 changes apply, 2 usage error or unreadable input.</returns>
    public static int Run(string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            ["--check"],
            ["--root", "--format"],
            maxPositionals: 0);

        if (parsed.Help && parsed.Error is null && parsed.FormatError is null)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (parsed.FormatError is { } formatError)
        {
            return CommandFailure.UsageError(formatError, Usage, json: false);
        }

        if (!parsed.Has("--check"))
        {
            return CommandFailure.UsageError("Upgrade needs --check.", Usage, json: false);
        }

        var json = parsed.Json;
        var config = ConsumerRoot.Open(parsed.Value("--root"), json, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        if (ReviewedOf(config.Root, json, out var reviewed, out var reviewedExit) is false)
        {
            return reviewedExit;
        }

        var carried = LiteSpecHome.Version;

        if (carried is null)
        {
            return CommandFailure.Fail(
                "Cannot find the carried LiteSpec release: set LITESPEC_HOME to the folder that holds litespec.manifest.yaml.",
                json,
                code: 2);
        }

        if (!UpgradeManifest.TryBase(reviewed, out var reviewedBase))
        {
            return CommandFailure.Fail(
                $"Cannot upgrade: reviewedLiteSpecVersion '{reviewed}' is not a version.",
                json,
                code: 2);
        }

        if (!UpgradeManifest.TryBase(carried, out var carriedBase))
        {
            return CommandFailure.Fail(
                $"Cannot upgrade: the carried release version '{carried}' is not a version.",
                json,
                code: 2);
        }

        var (migrations, manifestError) = UpgradeManifest.Load();

        if (manifestError is not null)
        {
            return CommandFailure.Fail(manifestError, json, code: 2);
        }

        var applies = migrations
            .Where(migration => UpgradeManifest.TryBase(migration.Version, out var baseVersion)
                && reviewedBase.CompareTo(baseVersion) < 0
                && baseVersion.CompareTo(carriedBase) <= 0)
            .ToList();

        if (reviewedBase.CompareTo(carriedBase) > 0)
        {
            var newer = $"Project reviews {reviewed}, newer than the carried release {carried}; no changes apply.";

            if (json)
            {
                CliHost.Out.WriteLine(CliJson.Serialize(JsonOf(reviewed, carried, [])));
            }
            else
            {
                CliHost.Out.WriteLine(newer);
            }

            return 0;
        }

        if (applies.Count == 0)
        {
            if (json)
            {
                CliHost.Out.WriteLine(CliJson.Serialize(JsonOf(reviewed, carried, [])));
            }
            else
            {
                CliHost.Out.WriteLine($"Project reviews {reviewed}; the carried release holds no newer changes.");
            }

            return 0;
        }

        if (json)
        {
            CliHost.Out.WriteLine(CliJson.Serialize(JsonOf(reviewed, carried, applies)));
        }
        else
        {
            Print(reviewed, carried, applies);
        }

        return 1;
    }

    /// <summary>Reads the reviewed LiteSpec version the consumer records.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="json">True when the caller requested JSON output.</param>
    /// <param name="reviewed">The reviewed version.</param>
    /// <param name="exitCode">The exit code for the caller to return when the version is refused; 0 otherwise.</param>
    /// <returns>True when the version was read; otherwise false.</returns>
    static bool ReviewedOf(string root, bool json, out string reviewed, out int exitCode)
    {
        reviewed = "";
        exitCode = 0;

        JsonNode? project;

        try
        {
            project = RecordText.Read(ConsumerRoot.ProjectFile(root));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonTextException)
        {
            exitCode = CommandFailure.Fail($"Cannot read {ConsumerRoot.ProjectFileDisplay}: {exception.Message}", json, 2);
            return false;
        }

        if (project is JsonObject container
            && JsonValues.AsString(container["reviewedLiteSpecVersion"]) is { Length: > 0 } version)
        {
            reviewed = version;
            return true;
        }

        exitCode = CommandFailure.Fail(
            $"Cannot upgrade: {ConsumerRoot.ProjectFileDisplay} names no reviewedLiteSpecVersion.",
            json,
            2);
        return false;
    }

    /// <summary>Prints the applying migrations as text, with a closing line naming the reviewed and carried releases.</summary>
    /// <param name="reviewed">The reviewed version.</param>
    /// <param name="carried">The carried release version.</param>
    /// <param name="applies">The applying migrations in version order.</param>
    static void Print(string reviewed, string carried, List<UpgradeManifest.Migration> applies)
    {
        CliHost.Out.WriteLine($"Upgrade from {reviewed} to {carried}: {applies.Count} {(applies.Count == 1 ? "release applies" : "releases apply")}.");
        CliHost.Out.WriteLine();

        foreach (var migration in applies)
        {
            CliHost.Out.WriteLine($"Release {migration.Version}: {migration.Summary}");

            if (migration.FieldRenames.Count > 0)
            {
                CliHost.Out.WriteLine("  Field renames:");

                foreach (var rename in migration.FieldRenames)
                {
                    var types = rename.RecordTypes.Count > 0 ? $" ({string.Join(", ", rename.RecordTypes)})" : "";
                    var change = rename.To is null
                        ? $"`{rename.From}` removed{types}"
                        : $"`{rename.From}` -> `{rename.To}`{types}";
                    var note = rename.Note.Length > 0 ? $": {rename.Note}" : "";
                    CliHost.Out.WriteLine($"  - {change}{note}");
                }
            }

            if (migration.FindingRenames.Count > 0 || migration.FindingAdded.Count > 0 || migration.FindingRemoved.Count > 0)
            {
                CliHost.Out.WriteLine("  Finding codes:");

                foreach (var rename in migration.FindingRenames)
                {
                    CliHost.Out.WriteLine($"  - `{rename.From}` renamed to `{rename.To}`");
                }

                foreach (var removed in migration.FindingRemoved)
                {
                    CliHost.Out.WriteLine($"  - removed: `{removed}`");
                }

                foreach (var added in migration.FindingAdded)
                {
                    CliHost.Out.WriteLine($"  - new: `{added}`");
                }
            }

            if (migration.AnalyzerAdded.Count > 0 || migration.AnalyzerChanged.Count > 0 || migration.AnalyzerRemoved.Count > 0)
            {
                CliHost.Out.WriteLine("  Analyzer IDs:");

                foreach (var removed in migration.AnalyzerRemoved)
                {
                    CliHost.Out.WriteLine($"  - removed: {removed}");
                }

                foreach (var added in migration.AnalyzerAdded)
                {
                    CliHost.Out.WriteLine($"  - new: {added}");
                }

                if (migration.AnalyzerChanged.Count > 0)
                {
                    CliHost.Out.WriteLine($"  - changed: {string.Join(", ", migration.AnalyzerChanged)}");
                }
            }

            if (migration.CommandsRetired.Count > 0)
            {
                CliHost.Out.WriteLine("  Retired commands:");

                foreach (var command in migration.CommandsRetired)
                {
                    var replacement = command.To is null ? "" : $" (replaced by `{command.To}`)";
                    CliHost.Out.WriteLine($"  - `{command.From}`{replacement}");
                }
            }

            if (migration.ExtensionsFolded.Count > 0)
            {
                CliHost.Out.WriteLine($"  Extensions folded into Core: {string.Join(", ", migration.ExtensionsFolded)}");
            }

            if (migration.ExtensionsRetired.Count > 0)
            {
                CliHost.Out.WriteLine($"  Removed extensions: {string.Join(", ", migration.ExtensionsRetired)}");
            }

            if (migration.StacksRemoved.Count > 0)
            {
                CliHost.Out.WriteLine($"  Removed stacks: {string.Join(", ", migration.StacksRemoved)}");
            }

            if (migration.PackChanges.Count > 0)
            {
                CliHost.Out.WriteLine("  Pack changes:");

                foreach (var change in migration.PackChanges)
                {
                    CliHost.Out.WriteLine($"  - {change}");
                }
            }
        }

        CliHost.Out.WriteLine();
        CliHost.Out.WriteLine($"Project reviews {reviewed}; {applies.Count} {(applies.Count == 1 ? "release applies" : "releases apply")}. Reread the contract and record {carried}.");
    }

    /// <summary>The JSON object of an upgrade report: the reviewed and carried versions with their migrations.</summary>
    /// <param name="reviewed">The reviewed version.</param>
    /// <param name="carried">The carried release version.</param>
    /// <param name="applies">The applying migrations in version order.</param>
    /// <returns>The JSON object the command prints.</returns>
    static JsonObject JsonOf(string reviewed, string carried, List<UpgradeManifest.Migration> applies)
    {
        var migrations = new JsonArray([.. applies.Select(migration => (JsonNode?)new JsonObject
        {
            ["version"] = migration.Version,
            ["summary"] = migration.Summary,
            ["fieldRenames"] = new JsonArray([.. migration.FieldRenames.Select(rename => (JsonNode?)FieldRenameJson(rename))]),
            ["findingRenames"] = new JsonArray([.. migration.FindingRenames.Select(rename => (JsonNode?)new JsonObject
            {
                ["from"] = rename.From,
                ["to"] = rename.To,
            })]),
            ["findingAdded"] = CliJson.Strings(migration.FindingAdded),
            ["findingRemoved"] = CliJson.Strings(migration.FindingRemoved),
            ["analyzerAdded"] = CliJson.Strings(migration.AnalyzerAdded),
            ["analyzerChanged"] = CliJson.Strings(migration.AnalyzerChanged),
            ["analyzerRemoved"] = CliJson.Strings(migration.AnalyzerRemoved),
            ["commandsRetired"] = new JsonArray([.. migration.CommandsRetired.Select(command => (JsonNode?)CommandJson(command))]),
            ["extensionsFolded"] = CliJson.Strings(migration.ExtensionsFolded),
            ["extensionsRetired"] = CliJson.Strings(migration.ExtensionsRetired),
            ["stacksRemoved"] = CliJson.Strings(migration.StacksRemoved),
            ["packChanges"] = CliJson.Strings(migration.PackChanges),
        })]);

        return new JsonObject
        {
            ["reviewed"] = reviewed,
            ["carried"] = carried,
            ["migrations"] = migrations,
        };
    }

    /// <summary>The JSON object of one schema field rename, holding its replacement only when one exists.</summary>
    /// <param name="rename">The field rename.</param>
    /// <returns>The JSON object the report holds.</returns>
    static JsonObject FieldRenameJson(UpgradeManifest.FieldRename rename)
    {
        var entry = new JsonObject
        {
            ["from"] = rename.From,
            ["recordTypes"] = CliJson.Strings(rename.RecordTypes),
            ["note"] = rename.Note,
        };

        if (rename.To is not null)
        {
            entry["to"] = rename.To;
        }

        return entry;
    }

    /// <summary>The JSON object of one retired command, holding its replacement only when one exists.</summary>
    /// <param name="command">The retired command.</param>
    /// <returns>The JSON object the report holds.</returns>
    static JsonObject CommandJson(UpgradeManifest.CommandChange command)
    {
        var entry = new JsonObject { ["from"] = command.From };

        if (command.To is not null)
        {
            entry["to"] = command.To;
        }

        return entry;
    }
}
