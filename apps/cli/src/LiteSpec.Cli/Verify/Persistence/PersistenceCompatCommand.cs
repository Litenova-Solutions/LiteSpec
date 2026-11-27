using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Verify.Persistence;

/// <summary>
///     The <c>litespec verify persistence-compat</c> command: runs the consumer persistence
///     harness and reports the stored snapshots that no longer read through the current serializer.
/// </summary>
/// <follows>litespec/core/backend-persistence#evolve-stored-document-contracts-explicitly</follows>
internal static class PersistenceCompatCommand
{
    /// <summary>The usage text for the persistence compatibility command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec verify persistence-compat [--root <consumer>] [--record --release <name>] [--help]

        Runs the persistence harness the project file names and reports the stored
        snapshots that no longer read through the current serializer. Without
        --record it checks every release folder under
        .litespec/proof/persistence-snapshots/; with --record --release it records
        the snapshots of one release.

          --root       The consumer repository to read. Defaults to the working directory.
          --record     Record the snapshots of one release instead of checking them.
          --release    The release folder the record writes, under .litespec/proof/persistence-snapshots/.
          --help       Print this text and exit.

        Exit codes: 0 every snapshot reads, 1 a snapshot is incompatible or a release folder holds no snapshot, 2 the options are invalid or the project file names no persistence harness.
        """;

    /// <summary>Runs the persistence compatibility check.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 when every snapshot reads, 1 when a finding is raised, 2 on a usage error.</returns>
    public static int Run(string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            ["--record"],
            ["--root", "--release"],
            maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        var record = parsed.Has("--record");
        var release = parsed.Value("--release");

        if (record && string.IsNullOrEmpty(release))
        {
            return CommandFailure.UsageError("Option --record needs --release <name>.", Usage, json: false);
        }

        if (!record && release is not null)
        {
            return CommandFailure.UsageError("Option --release needs --record.", Usage, json: false);
        }

        if (release is not null && !IsReleaseName(release))
        {
            return CommandFailure.UsageError($"Release '{release}' is not a folder name.", Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        if (config.Verify.Persistence is not { } harness)
        {
            return CommandFailure.UsageError(
                $"{ConsumerRoot.ProjectFileDisplay} names no verify.persistence.command, so there is no harness to run.",
                Usage,
                json: false);
        }

        var snapshots = Path.Combine(config.Root, ".litespec", "proof", "persistence-snapshots");
        var findings = new List<Finding>();
        string mode;

        if (record)
        {
            mode = "record";
            var folder = Path.Combine(snapshots, release!);

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return CommandFailure.Fail($"Cannot write '{PathText.Relative(config.Root, folder)}': {exception.Message}", json: false, code: 1);
            }

            findings.AddRange(CheckFolder(config, harness.Command, mode, folder));

            if (FolderHasSnapshots(config, folder, out var hasSnapshots) is { } snapshotsExit)
            {
                return snapshotsExit;
            }

            if (!hasSnapshots)
            {
                findings.Add(Missing(config, folder));
            }
        }
        else
        {
            mode = "check";

            if (ReleaseFolders(snapshots, config, out var releases) is { } foldersExit)
            {
                return foldersExit;
            }

            foreach (var folder in releases)
            {
                findings.AddRange(CheckFolder(config, harness.Command, mode, folder));

                if (FolderHasSnapshots(config, folder, out var hasSnapshots) is { } snapshotsExit)
                {
                    return snapshotsExit;
                }

                if (!hasSnapshots)
                {
                    findings.Add(Missing(config, folder));
                }
            }
        }

        var ordered = findings
            .OrderBy(finding => finding.Path, StringComparer.Ordinal)
            .ThenBy(finding => finding.Code, StringComparer.Ordinal)
            .ThenBy(finding => finding.Message, StringComparer.Ordinal)
            .ToList();

        foreach (var finding in ordered)
        {
            CliHost.Out.WriteLine($"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [{finding.Tier}] [{finding.Code}] {finding.Message}");
        }

        if (WriteReport(config, mode, record ? release : null, ordered) is { } reportExit)
        {
            return reportExit;
        }

        if (ordered.Count == 0)
        {
            CliHost.Out.WriteLine("Every persistence snapshot reads through the current serializer.");
        }
        else
        {
            CliHost.Out.WriteLine($"{ordered.Count} persistence findings.");
        }

        return ordered.Count > 0 ? 1 : 0;
    }

    /// <summary>Whether the release names one folder under the snapshots folder.</summary>
    /// <param name="release">The release value the caller passed.</param>
    /// <returns>True when the release holds no separator, parent link, or invalid file-name character.</returns>
    private static bool IsReleaseName(string release) =>
        release is not "." and not ".."
        && release.IndexOfAny(['/', '\\']) < 0
        && release.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    /// <summary>The release folders the check runs, in order, or the exit code when they cannot be read.</summary>
    /// <param name="snapshots">The snapshots folder holding one folder per release.</param>
    /// <param name="config">The consumer configuration, for the relative path in the failure message.</param>
    /// <param name="releases">The release folders in order, or empty when the snapshots folder is missing.</param>
    /// <returns>Null when the folders were read; otherwise the exit code for the caller to return.</returns>
    private static int? ReleaseFolders(string snapshots, ConsumerConfig config, out IReadOnlyList<string> releases)
    {
        try
        {
            releases = Directory.Exists(snapshots)
                ? Directory.GetDirectories(snapshots).OrderBy(folder => folder, StringComparer.Ordinal).ToList()
                : [];
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            releases = [];
            return CommandFailure.Fail($"Cannot read '{PathText.Relative(config.Root, snapshots)}': {exception.Message}", json: false, code: 1);
        }
    }

    /// <summary>Whether the release folder holds a snapshot, or the exit code when it cannot be read.</summary>
    /// <param name="config">The consumer configuration, for the relative path in the failure message.</param>
    /// <param name="folder">The release folder to read.</param>
    /// <param name="hasSnapshots">True when the folder holds a snapshot file.</param>
    /// <returns>Null when the folder was read; otherwise the exit code for the caller to return.</returns>
    private static int? FolderHasSnapshots(ConsumerConfig config, string folder, out bool hasSnapshots)
    {
        try
        {
            hasSnapshots = Directory.GetFiles(folder, "*.json").Length > 0;
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            hasSnapshots = false;
            return CommandFailure.Fail($"Cannot read '{PathText.Relative(config.Root, folder)}': {exception.Message}", json: false, code: 1);
        }
    }

    /// <summary>Runs the harness for one release folder and collects the incompatible snapshots it reports.</summary>
    /// <param name="config">The consumer configuration, for the relative paths the findings name.</param>
    /// <param name="command">The harness command the project file names.</param>
    /// <param name="mode">The harness mode: <c>record</c> or <c>check</c>.</param>
    /// <param name="folder">The release folder the harness runs against.</param>
    /// <returns>The incompatible snapshots the harness reported.</returns>
    private static List<Finding> CheckFolder(ConsumerConfig config, string command, string mode, string folder)
    {
        var relative = PathText.Relative(config.Root, folder);
        var run = PersistenceHarness.Run(command, mode, relative, config.Root);
        var findings = new List<Finding>();

        foreach (var line in PersistenceHarness.Parse(run.Stdout))
        {
            if (line.Status != "failed")
            {
                continue;
            }

            var path = line.File.Length > 0 ? Resolve(config.Root, line.File) : folder;
            var message = line.Message.Length > 0
                ? line.Message
                : "The snapshot no longer reads through the current serializer.";
            findings.Add(new Finding(
                "snapshot-read-failed",
                message,
                path,
                1,
                line.File.Length > 0 ? line.File : null));
        }

        if (findings.Count == 0 && (run.Status != 0 || run.TimedOut))
        {
            var detail = run.TimedOut
                ? "the harness did not finish in five minutes"
                : $"the harness exited with code {run.Status}";
            var error = run.Error.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
            findings.Add(new Finding(
                "snapshot-read-failed",
                $"The harness did not check '{relative}': {detail}." + (error.Length > 0 ? $" {error}" : ""),
                folder,
                1,
                null));
        }

        return findings;
    }

    /// <summary>The missing-snapshot finding for a release folder that holds no snapshot.</summary>
    /// <param name="config">The consumer configuration, for the relative path the finding names.</param>
    /// <param name="folder">The empty release folder.</param>
    /// <returns>The finding reporting that the release proves nothing.</returns>
    private static Finding Missing(ConsumerConfig config, string folder) =>
        new(
            "snapshot-missing",
            $"Release folder '{PathText.Relative(config.Root, folder)}' holds no snapshot, so the check proves nothing for that release.",
            folder,
            1,
            null);

    /// <summary>The absolute path a harness file names: as written when absolute, else under the consumer root.</summary>
    /// <param name="root">The consumer root the relative file is resolved against.</param>
    /// <param name="file">The snapshot path the harness names.</param>
    /// <returns>The absolute path of the snapshot.</returns>
    private static string Resolve(string root, string file) =>
        Path.IsPathFullyQualified(file) ? file : Path.GetFullPath(Path.Combine(root, file));

    /// <summary>Writes the JSON report of the run, or the exit code when it cannot be written.</summary>
    /// <param name="config">The consumer configuration holding the root the report is written under.</param>
    /// <param name="mode">The harness mode the run used.</param>
    /// <param name="release">The recorded release, or null for a check run.</param>
    /// <param name="findings">The findings the run raised, in print order.</param>
    /// <returns>Null when the report was written; otherwise the exit code for the caller to return.</returns>
    private static int? WriteReport(ConsumerConfig config, string mode, string? release, IReadOnlyList<Finding> findings)
    {
        var reportFile = Path.Combine(config.Root, ".litespec", "verify", "persistence-compat.json");
        var report = new JsonObject
        {
            ["command"] = "persistence-compat",
            ["mode"] = mode,
            ["release"] = release,
            ["findings"] = new JsonArray([.. findings.Select(finding => (JsonNode?)new JsonObject
            {
                ["code"] = finding.Code,
                ["level"] = finding.Tier,
                ["outcome"] = "blocking",
                ["message"] = finding.Message,
                ["path"] = PathText.Relative(config.Root, finding.Path),
                ["line"] = finding.Line,
                ["subject"] = finding.Subject,
            })]),
        };

        try
        {
            if (Path.GetDirectoryName(reportFile) is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(reportFile, CliJson.Serialize(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            CliHost.Out.WriteLine($"Wrote {PathText.Relative(config.Root, reportFile)}.");
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write '{PathText.Relative(config.Root, reportFile)}': {exception.Message}", json: false, code: 1);
        }
    }
}
