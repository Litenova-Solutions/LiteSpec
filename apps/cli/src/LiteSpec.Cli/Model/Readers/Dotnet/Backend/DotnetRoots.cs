using System.Runtime.InteropServices;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>Every dotnet installation root that exists on disk, in lookup order.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class DotnetRoots
{
    /// <summary>Every dotnet root that exists on disk: the environment roots, the dotnet on PATH, then the standard locations.</summary>
    /// <returns>The existing dotnet roots, deduplicated in lookup order.</returns>
    public static IReadOnlyList<string> Candidates() =>
        Candidates(CliHost.Variable("DOTNET_ROOT"), CliHost.Variable("DOTNET_ROOT_" + ArchSuffix()), CliHost.Variable("PATH"), StandardLocations());

    /// <summary>Every dotnet root from explicit values, so a test can name fake roots.</summary>
    /// <param name="dotnetRoot">The DOTNET_ROOT value, or null when unset.</param>
    /// <param name="dotnetRootArch">The DOTNET_ROOT_ARCH value, or null when unset.</param>
    /// <param name="path">The PATH value, or null when unset.</param>
    /// <param name="standardLocations">The standard install locations for the platform.</param>
    /// <returns>The existing dotnet roots, deduplicated in lookup order.</returns>
    internal static IReadOnlyList<string> Candidates(string? dotnetRoot, string? dotnetRootArch, string? path, IReadOnlyList<string> standardLocations)
    {
        var found = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? candidate)
        {
            if (candidate is { Length: > 0 })
            {
                var trimmed = candidate.Trim();

                if (trimmed.Length > 0 && seen.Add(trimmed) && Directory.Exists(trimmed))
                {
                    found.Add(trimmed);
                }
            }
        }

        Add(dotnetRoot);
        Add(dotnetRootArch);

        foreach (var root in RootsFromPath(path))
        {
            Add(root);
        }

        foreach (var location in standardLocations)
        {
            Add(location);
        }

        return found;
    }

    /// <summary>The architecture suffix of the DOTNET_ROOT_ARCH variable for this process.</summary>
    /// <returns>X64, X86, ARM64, ARM, or the process architecture name uppercased.</returns>
    internal static string ArchSuffix() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "X64",
        Architecture.X86 => "X86",
        Architecture.Arm64 => "ARM64",
        Architecture.Arm => "ARM",
        _ => RuntimeInformation.ProcessArchitecture.ToString().ToUpperInvariant(),
    };

    /// <summary>Every dotnet root the PATH names: the folder of each dotnet executable with links resolved.</summary>
    /// <param name="path">The PATH value, or null when unset.</param>
    /// <returns>The dotnet roots from PATH, whether or not they exist on disk.</returns>
    internal static IReadOnlyList<string> RootsFromPath(string? path)
    {
        var found = new List<string>();

        if (string.IsNullOrEmpty(path))
        {
            return found;
        }

        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in new[] { "dotnet.exe", "dotnet" })
            {
                var candidate = Path.Combine(folder.Trim(), name);

                if (!File.Exists(candidate))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(ResolveLinks(candidate));

                if (directory is { Length: > 0 } && !found.Contains(directory, StringComparer.OrdinalIgnoreCase))
                {
                    found.Add(directory);
                }

                break;
            }
        }

        return found;
    }

    /// <summary>One path with symbolic links resolved, so a dotnet on PATH through a link still names its install folder.</summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The resolved path, or the input when it names no link.</returns>
    internal static string ResolveLinks(string path)
    {
        var current = path;

        for (var depth = 0; depth < 10; depth++)
        {
            string? target;

            try
            {
                target = new FileInfo(current).LinkTarget;
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                break;
            }

            if (target is null)
            {
                break;
            }

            current = Path.IsPathRooted(target) ? target : Path.Combine(Path.GetDirectoryName(current) ?? "", target);
        }

        return current;
    }

    /// <summary>The standard dotnet install locations for this platform.</summary>
    /// <returns>The standard locations, whether or not they exist on disk.</returns>
    internal static IReadOnlyList<string> StandardLocations()
    {
        if (OperatingSystem.IsWindows())
        {
            var locations = new List<string>();
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            if (programFiles.Length > 0)
            {
                locations.Add(Path.Combine(programFiles, "dotnet"));
            }

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (programFilesX86.Length > 0)
            {
                locations.Add(Path.Combine(programFilesX86, "dotnet"));
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (localAppData.Length > 0)
            {
                locations.Add(Path.Combine(localAppData, "Microsoft", "dotnet"));
            }

            locations.Add(@"C:\Program Files\dotnet");
            locations.Add(@"C:\Program Files (x86)\dotnet");

            return locations;
        }

        if (OperatingSystem.IsMacOS())
        {
            var locations = new List<string> { "/usr/local/share/dotnet", "/opt/dotnet", "/opt/homebrew/share/dotnet" };
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (home.Length > 0)
            {
                locations.Add(Path.Combine(home, ".dotnet"));
            }

            return locations;
        }

        var unix = new List<string> { "/usr/share/dotnet", "/usr/lib/dotnet", "/opt/dotnet" };
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (profile.Length > 0)
        {
            unix.Add(Path.Combine(profile, ".dotnet"));
        }

        return unix;
    }
}
