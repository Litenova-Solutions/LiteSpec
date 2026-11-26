using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LiteSpec.CommandLine;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.PublishLocal;

/// <summary>
///     The <c>litespec-maintain publish-local</c> command: packs the checkout's CLI and analyzers
///     at one dev version into a local feed, moves a consumer onto that build, and restores its tool.
/// </summary>
/// <follows>litespec/core/release#run-one-litespec-build</follows>
static class PublishLocalCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec-maintain publish-local --consumer <path> [--feed <dir>] [--help]

        Hands the current checkout's CLI and analyzers to a consumer without waiting for
        NuGet: it packs LiteSpec.Cli and LiteSpec.Analyzers at one dev version, the manifest
        release with a -dev.<yyyyMMddHHmmss> suffix in UTC, into the feed, points the
        consumer's nuget.config at the feed, moves the tool manifest and every central
        package file declaring LiteSpec.Analyzers to that version, and runs
        dotnet tool restore in the consumer. The CLI packs are the top-level tool
        package, the framework-dependent package, and the native package of the
        machine the command runs on, which is what the restore installs.

          --consumer    The consumer folder holding .litespec/project.yaml. Required.
          --feed        The package feed folder. Defaults to .agents/local-feed under
                        the consumer; a relative path resolves under the consumer.
          --help        Print this text and exit.

        Exit codes: 0 published, 1 a publish step failed, 2 usage error or unreadable input.
        """;

    /// <summary>The minutes one pack may run before the command stops it.</summary>
    const int PackTimeoutMinutes = 10;

    /// <summary>The minutes the tool restore may run before the command stops it.</summary>
    const int RestoreTimeoutMinutes = 5;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 published, 1 a publish step failed, 2 usage error or unreadable input.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--consumer", "--feed"]);

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            Console.Error.WriteLine(parsed.Error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var consumerOption = parsed.Value("--consumer");

        if (consumerOption is null)
        {
            Console.Error.WriteLine("Missing --consumer; name the consumer folder holding .litespec/project.yaml.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (consumerOption.Length == 0)
        {
            Console.Error.WriteLine("Option --consumer needs a value.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var feedOption = parsed.Value("--feed");

        if (feedOption is not null && feedOption.Length == 0)
        {
            Console.Error.WriteLine("Option --feed needs a value.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string consumer;

        try
        {
            consumer = Path.GetFullPath(consumerOption);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Invalid consumer '{consumerOption}'.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (!Directory.Exists(consumer))
        {
            Console.Error.WriteLine($"Consumer '{consumer}' does not exist.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (!File.Exists(Path.Combine(consumer, ".litespec", "project.yaml")))
        {
            Console.Error.WriteLine($"Consumer '{consumer}' holds no .litespec/project.yaml.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string feed;

        try
        {
            feed = feedOption is null
                ? Path.Combine(consumer, ".agents", "local-feed")
                : Path.GetFullPath(Path.IsPathRooted(feedOption) ? feedOption : Path.Combine(consumer, feedOption));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Invalid feed '{feedOption}'.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;

        try
        {
            root = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var cliProject = Path.Combine(root, "apps", "cli", "src", "LiteSpec.Cli", "LiteSpec.Cli.csproj");
        var analyzersProject = Path.Combine(root, "apps", "analyzers", "src", "LiteSpec.Analyzers", "LiteSpec.Analyzers.csproj");

        if (!File.Exists(cliProject) || !File.Exists(analyzersProject))
        {
            Console.Error.WriteLine($"Root '{root}' holds no CLI and analyzers projects to pack.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string baseVersion;

        try
        {
            baseVersion = LiteSpecHome.ReadManifest(root)["version"]?.GetValue<string>() ?? "";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or LiteSpec.Model.JsonTextException)
        {
            Console.Error.WriteLine($"Cannot read the release version: {exception.Message}");
            return 2;
        }

        if (baseVersion.Length == 0)
        {
            Console.Error.WriteLine("The LiteSpec release holds no readable version.");
            return 2;
        }

        var version = PublishLocal.DevVersion(baseVersion, DateTime.UtcNow);

        // Every consumer file is read and rewritten in memory before the first pack runs,
        // so a broken manifest or config fails fast instead of after minutes of packing.
        string? toolsText;
        string? configText;
        string toolsNew;
        string configNew;
        bool toolsChanged;
        bool configChanged;

        try
        {
            toolsText = ReadIfExists(Path.Combine(consumer, ".config", "dotnet-tools.json"));
            configText = ReadIfExists(NuGetConfig(consumer));
            (toolsNew, toolsChanged) = PublishLocal.SetToolVersion(toolsText, version);
            (configNew, configChanged) = PublishLocal.EnsureFeedSource(configText, PublishLocal.FeedValueFor(feed, consumer));
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        var centralFiles = CentralFiles(consumer);
        var centralNew = new Dictionary<string, string>(StringComparer.Ordinal);
        var centralCurrent = new List<string>();

        foreach (var file in centralFiles)
        {
            string text;

            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Cannot read '{file}': {exception.Message}");
                return 2;
            }

            if (PublishLocal.AnalyzerVersionIn(text) is null)
            {
                continue;
            }

            var (updated, changed) = PublishLocal.SetAnalyzerVersion(text, version);

            if (changed > 0)
            {
                centralNew[file] = updated;
            }
            else
            {
                centralCurrent.Add(file);
            }
        }

        Console.WriteLine($"Consumer: {consumer}");
        Console.WriteLine($"Feed: {feed}");
        Console.WriteLine($"Version: {version}");

        try
        {
            Directory.CreateDirectory(feed);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Cannot create the feed '{feed}': {exception.Message}");
            return 1;
        }

        var before = FeedPackages(feed);

        if (Pack(root, analyzersProject, version, feed, []) is { } analyzersFailure)
        {
            Console.Error.WriteLine(analyzersFailure);
            return 1;
        }

        if (Pack(root, cliProject, version, feed, ["-p:TopLevelToolPackage=true"]) is { } toolFailure)
        {
            Console.Error.WriteLine(toolFailure);
            return 1;
        }

        if (Pack(root, cliProject, version, feed, ["--runtime", "any", "-p:PublishAot=false"]) is { } runtimeFailure)
        {
            Console.Error.WriteLine(runtimeFailure);
            return 1;
        }

        // The restore installs the native package of its own machine, so the feed holds this machine's build too.
        // Its restore adds runtime assets the committed lock files do not cover, so their bytes are held aside
        // and written back after the pack, leaving the checkout exactly as it was.
        var locks = SnapshotLockFiles(root);

        try
        {
            if (Pack(root, cliProject, version, feed, ["--runtime", RuntimeInformation.RuntimeIdentifier]) is { } nativeFailure)
            {
                Console.Error.WriteLine(nativeFailure);
                return 1;
            }
        }
        finally
        {
            RestoreLockFiles(locks);
        }

        foreach (var package in FeedPackages(feed).Except(before, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal))
        {
            Console.WriteLine($"Packed {package}.");
        }

        if (!WriteIfChanged(consumer, NuGetConfig(consumer), configText, configNew, configChanged, "the feed source", out var configError))
        {
            Console.Error.WriteLine(configError);
            return 1;
        }

        if (!WriteIfChanged(
            consumer,
            Path.Combine(consumer, ".config", "dotnet-tools.json"),
            toolsText, toolsNew, toolsChanged, "the tool manifest", out var toolsError))
        {
            Console.Error.WriteLine(toolsError);
            return 1;
        }

        foreach (var (file, updated) in centralNew.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            try
            {
                File.WriteAllText(file, updated);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Cannot write '{file}': {exception.Message}");
                return 1;
            }

            Console.WriteLine($"Set LiteSpec.Analyzers {version} in {Relative(consumer, file)}.");
        }

        foreach (var file in centralCurrent.OrderBy(path => path, StringComparer.Ordinal))
        {
            Console.WriteLine($"{Relative(consumer, file)} already states LiteSpec.Analyzers {version}.");
        }

        if (centralNew.Count == 0 && centralCurrent.Count == 0)
        {
            Console.WriteLine("No Directory.Packages.props under the consumer declares LiteSpec.Analyzers; left alone.");
        }

        foreach (var (file, pinned) in DirectAnalyzerPins(consumer, version))
        {
            Console.WriteLine($"{Relative(consumer, file)} still references LiteSpec.Analyzers {pinned}; move it to {version}.");
        }

        var restore = Run("dotnet", ["tool", "restore"], consumer, TimeSpan.FromMinutes(RestoreTimeoutMinutes));

        if (restore.TimedOut)
        {
            Console.Error.WriteLine($"dotnet tool restore did not finish in {RestoreTimeoutMinutes} minutes.");
            return 1;
        }

        if (restore.Status != 0)
        {
            Console.Error.WriteLine($"dotnet tool restore failed in '{consumer}':{LastLine(restore)}");
            return 1;
        }

        Console.WriteLine($"Restored LiteSpec.Cli {version} in the consumer.");
        return 0;
    }

    /// <summary>The consumer's NuGet config: the existing spelling, or the lowercase name when neither exists.</summary>
    /// <param name="consumer">The absolute consumer folder.</param>
    /// <returns>The absolute config path.</returns>
    static string NuGetConfig(string consumer)
    {
        var lower = Path.Combine(consumer, "nuget.config");

        if (File.Exists(lower))
        {
            return lower;
        }

        var upper = Path.Combine(consumer, "NuGet.config");

        return File.Exists(upper) ? upper : lower;
    }

    /// <summary>Reads a file, or null when it is absent.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <returns>The file text, or null when the file does not exist.</returns>
    static string? ReadIfExists(string file)
    {
        try
        {
            return File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Cannot read '{file}': {exception.Message}", exception);
        }
    }

    /// <summary>Every central package file under the consumer.</summary>
    /// <param name="consumer">The absolute consumer folder.</param>
    /// <returns>The absolute paths of the files found; folders that cannot be read are skipped.</returns>
    static List<string> CentralFiles(string consumer)
    {
        var found = new List<string>();
        var waiting = new Stack<string>();
        waiting.Push(consumer);

        while (waiting.Count > 0)
        {
            var folder = waiting.Pop();
            string[] entries;

            try
            {
                entries = Directory.GetFileSystemEntries(folder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                bool folderEntry;

                try
                {
                    folderEntry = Directory.Exists(entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (folderEntry)
                {
                    waiting.Push(entry);
                    continue;
                }

                if (Path.GetFileName(entry).Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(entry);
                }
            }
        }

        return found;
    }

    /// <summary>Every project under the consumer that references the analyzers directly at another version.</summary>
    /// <param name="consumer">The absolute consumer folder.</param>
    /// <param name="version">The dev version the command publishes.</param>
    /// <returns>The project path with its pinned version, in path order.</returns>
    static List<(string File, string Pinned)> DirectAnalyzerPins(string consumer, string version)
    {
        var pins = new List<(string File, string Pinned)>();
        var waiting = new Stack<string>();
        waiting.Push(consumer);

        while (waiting.Count > 0)
        {
            var folder = waiting.Pop();
            string[] entries;

            try
            {
                entries = Directory.GetFileSystemEntries(folder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                bool folderEntry;

                try
                {
                    folderEntry = Directory.Exists(entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (folderEntry)
                {
                    waiting.Push(entry);
                    continue;
                }

                if (!Path.GetFileName(entry).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string text;

                try
                {
                    text = File.ReadAllText(entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (PublishLocal.AnalyzerVersionIn(text) is { } pinned && pinned != version)
                {
                    pins.Add((entry, pinned));
                }
            }
        }

        pins.Sort((left, right) => string.Compare(left.File, right.File, StringComparison.Ordinal));

        return pins;
    }

    /// <summary>Reads the bytes of every package lock file under the root, so the native pack can give them back.</summary>
    /// <param name="root">The LiteSpec repository root the command packs from.</param>
    /// <returns>Each lock file with the bytes it held; a file that cannot be read is left out.</returns>
    static List<(string File, byte[] Bytes)> SnapshotLockFiles(string root)
    {
        var snapshot = new List<(string File, byte[] Bytes)>();

        foreach (var file in LockFiles(root))
        {
            try
            {
                snapshot.Add((file, File.ReadAllBytes(file)));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Cannot read the lock file '{file}': {exception.Message}");
            }
        }

        return snapshot;
    }

    /// <summary>Writes the held lock file bytes back.</summary>
    /// <param name="snapshot">The snapshot the native pack ran with.</param>
    static void RestoreLockFiles(List<(string File, byte[] Bytes)> snapshot)
    {
        foreach (var (file, bytes) in snapshot)
        {
            try
            {
                File.WriteAllBytes(file, bytes);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Cannot restore the lock file '{file}': {exception.Message}");
            }
        }
    }

    /// <summary>Every package lock file under the root.</summary>
    /// <param name="root">The LiteSpec repository root the command packs from.</param>
    /// <returns>The absolute paths of the files found; folders that cannot be read are skipped.</returns>
    static List<string> LockFiles(string root)
    {
        var found = new List<string>();
        var waiting = new Stack<string>();
        waiting.Push(root);

        while (waiting.Count > 0)
        {
            var folder = waiting.Pop();
            string[] entries;

            try
            {
                entries = Directory.GetFileSystemEntries(folder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                bool folderEntry;

                try
                {
                    folderEntry = Directory.Exists(entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (folderEntry)
                {
                    waiting.Push(entry);
                    continue;
                }

                if (Path.GetFileName(entry).Equals("packages.lock.json", StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(entry);
                }
            }
        }

        return found;
    }

    /// <summary>Packs one project into the feed at the dev version.</summary>
    /// <param name="root">The LiteSpec repository root the command packs from.</param>
    /// <param name="project">The absolute project path.</param>
    /// <param name="version">The dev version to pack.</param>
    /// <param name="feed">The absolute feed folder.</param>
    /// <param name="extra">The extra pack settings, such as the top-level tool flag.</param>
    /// <returns>The failure message, or null when the pack succeeded.</returns>
    static string? Pack(string root, string project, string version, string feed, IReadOnlyList<string> extra)
    {
        var args = new List<string> { "pack", project, "--configuration", "Release", "-p:Version=" + version, "--output", feed };
        args.AddRange(extra);
        var packed = Run("dotnet", args, root, TimeSpan.FromMinutes(PackTimeoutMinutes));

        if (packed.TimedOut)
        {
            return $"dotnet pack {Path.GetFileName(project)} did not finish in {PackTimeoutMinutes} minutes.";
        }

        if (packed.Status != 0)
        {
            return $"dotnet pack {Path.GetFileName(project)} failed:{LastLine(packed)}";
        }

        return null;
    }

    /// <summary>The package file names the feed holds.</summary>
    /// <param name="feed">The absolute feed folder.</param>
    /// <returns>The file names of the packages in the feed.</returns>
    static HashSet<string> FeedPackages(string feed)
    {
        try
        {
            return Directory.GetFiles(feed, "*.nupkg").Select(Path.GetFileName).OfType<string>().ToHashSet(StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Writes a consumer file when its new text differs, creating its folder, and prints what changed.</summary>
    /// <param name="consumer">The absolute consumer folder.</param>
    /// <param name="file">The absolute file path.</param>
    /// <param name="oldText">The text the file held, or null when it was absent.</param>
    /// <param name="newText">The text the file takes.</param>
    /// <param name="changed">Whether the new text differs.</param>
    /// <param name="what">What the file holds: the feed source or the tool manifest.</param>
    /// <param name="error">The failure message when the write fails.</param>
    /// <returns>True when the file was written or already current; false when the write failed.</returns>
    static bool WriteIfChanged(string consumer, string file, string? oldText, string newText, bool changed, string what, out string error)
    {
        error = "";
        var display = Relative(consumer, file);

        if (!changed)
        {
            Console.WriteLine($"{display} is already current.");
            return true;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, newText);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = $"Cannot write '{file}': {exception.Message}";
            return false;
        }

        Console.WriteLine(oldText is null ? $"Created {display} with {what}." : $"Updated {what} in {display}.");

        return true;
    }

    /// <summary>The path of a consumer file relative to the consumer, with forward-slash segments.</summary>
    /// <param name="consumer">The absolute consumer folder.</param>
    /// <param name="file">The absolute file path.</param>
    /// <returns>The relative path, or the absolute path when it sits outside the consumer.</returns>
    static string Relative(string consumer, string file)
    {
        var relative = Path.GetRelativePath(consumer, file);

        return Path.IsPathRooted(relative) ? file : relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>The last informative line a failed program wrote, for the failure message.</summary>
    /// <param name="run">The captured run.</param>
    /// <returns>The last non-empty error or output line, or the exit status when the program wrote nothing.</returns>
    static string LastLine(RunResult run)
    {
        var detail = run.Stderr.Split('\n').Select(line => line.Trim()).LastOrDefault(line => line.Length > 0)
            ?? run.Stdout.Split('\n').Select(line => line.Trim()).LastOrDefault(line => line.Length > 0);

        return detail is null ? $" the program exited {run.Status}." : $" {detail}";
    }

    /// <summary>Runs a program without a shell and captures what it writes.</summary>
    /// <param name="file">The program to start, found on the PATH when it is not a full path.</param>
    /// <param name="args">The arguments, passed one by one without a shell.</param>
    /// <param name="workdir">The folder the program runs in.</param>
    /// <param name="timeout">The longest time the program may run.</param>
    /// <returns>The exit status with the captured output and error text. A timeout and a missing program report status 1.</returns>
    static RunResult Run(string file, IReadOnlyList<string> args, string workdir, TimeSpan timeout)
    {
        var start = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workdir,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(start)!;
            var output = new StringBuilder();
            var stdout = Task.Run(() =>
            {
                int read;
                var buffer = new char[4096];

                while ((read = process.StandardOutput.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Append(buffer, 0, read);
                }
            });
            var stderr = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeout) || !stdout.Wait(timeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the wait and the kill.
                }

                return new RunResult(1, output.ToString(), "", TimedOut: true);
            }

            return new RunResult(process.ExitCode, output.ToString(), stderr.GetAwaiter().GetResult());
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return new RunResult(1, "", $"{file} is not available: {exception.Message}");
        }
    }

    /// <summary>What a captured program left behind: its exit status with its output and error text.</summary>
    /// <param name="Status">The exit status; 1 when the program timed out or could not start.</param>
    /// <param name="Stdout">The text the program wrote to standard output.</param>
    /// <param name="Stderr">The text the program wrote to standard error, or the runner's own message.</param>
    /// <param name="TimedOut">Whether the runner killed the program for running past its timeout.</param>
    sealed record RunResult(int Status, string Stdout, string Stderr, bool TimedOut = false);
}
