namespace LiteSpec.Cli.Audit;

/// <summary>The browser the audit runs: Chrome, then Edge, then Chromium, or the named executable.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
static class AuditBrowserDiscovery
{
    /// <summary>Finds the browser executable: the explicit path, or the installed Chrome, Edge, or Chromium.</summary>
    /// <param name="explicitPath">The --browser value, or null to search the usual install places.</param>
    /// <returns>The executable path, or null when no browser is installed.</returns>
    internal static string? Find(string? explicitPath)
    {
        if (explicitPath is not null)
        {
            return explicitPath;
        }

        if (OperatingSystem.IsWindows())
        {
            return FirstExisting(WindowsCandidates());
        }

        if (OperatingSystem.IsMacOS())
        {
            return FirstExisting(MacCandidates());
        }

        if (OperatingSystem.IsLinux())
        {
            return FirstExisting(LinuxCandidates());
        }

        return FirstExisting(FallbackCandidates());
    }

    /// <summary>The Windows candidates: Chrome, then Edge, then Chromium, each in Program Files, Program Files x86, and LocalAppData.</summary>
    /// <returns>The candidate paths in search order.</returns>
    static IEnumerable<string?> WindowsCandidates()
    {
        var programFiles = CliHost.Variable("ProgramFiles");
        var programFilesX86 = CliHost.Variable("ProgramFiles(x86)");
        var localAppData = CliHost.Variable("LocalAppData");

        yield return Join(programFiles, "Google", "Chrome", "Application", "chrome.exe");
        yield return Join(programFilesX86, "Google", "Chrome", "Application", "chrome.exe");
        yield return Join(localAppData, "Google", "Chrome", "Application", "chrome.exe");
        yield return FindOnPath("chrome");

        yield return Join(programFiles, "Microsoft", "Edge", "Application", "msedge.exe");
        yield return Join(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe");
        yield return Join(localAppData, "Microsoft", "Edge", "Application", "msedge.exe");
        yield return FindOnPath("msedge");

        yield return Join(programFiles, "Chromium", "Application", "chrome.exe");
        yield return Join(programFilesX86, "Chromium", "Application", "chrome.exe");
        yield return Join(localAppData, "Chromium", "Application", "chrome.exe");
        yield return FindOnPath("chromium");
    }

    /// <summary>The macOS candidates: Chrome, then Edge, then Chromium, each in /Applications and the home Applications.</summary>
    /// <returns>The candidate paths in search order.</returns>
    static IEnumerable<string?> MacCandidates()
    {
        var home = CliHost.Variable("HOME");
        var homeApplications = string.IsNullOrEmpty(home) ? null : Path.Combine(home, "Applications");

        yield return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
        yield return homeApplications is null ? null : Path.Combine(homeApplications, "Google Chrome.app", "Contents", "MacOS", "Google Chrome");
        yield return FindOnPath("google-chrome");

        yield return "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge";
        yield return homeApplications is null ? null : Path.Combine(homeApplications, "Microsoft Edge.app", "Contents", "MacOS", "Microsoft Edge");
        yield return FindOnPath("microsoft-edge");

        yield return "/Applications/Chromium.app/Contents/MacOS/Chromium";
        yield return homeApplications is null ? null : Path.Combine(homeApplications, "Chromium.app", "Contents", "MacOS", "Chromium");
        yield return FindOnPath("chromium");
    }

    /// <summary>The Linux candidates: Chrome, then Edge, then Chromium, each on PATH.</summary>
    /// <returns>The candidate paths in search order.</returns>
    static IEnumerable<string?> LinuxCandidates()
    {
        yield return FindOnPath("google-chrome");
        yield return FindOnPath("google-chrome-stable");

        yield return FindOnPath("microsoft-edge");
        yield return FindOnPath("microsoft-edge-stable");

        yield return FindOnPath("chromium");
        yield return FindOnPath("chromium-browser");
    }

    /// <summary>The candidates on any other system: every known browser name on PATH, Chrome first.</summary>
    /// <returns>The candidate paths in search order.</returns>
    static IEnumerable<string?> FallbackCandidates()
    {
        yield return FindOnPath("google-chrome");
        yield return FindOnPath("google-chrome-stable");
        yield return FindOnPath("microsoft-edge");
        yield return FindOnPath("microsoft-edge-stable");
        yield return FindOnPath("chromium");
        yield return FindOnPath("chromium-browser");
    }

    /// <summary>The first candidate that names an existing file.</summary>
    /// <param name="candidates">The candidate paths in search order.</param>
    /// <returns>The first existing path, or null when none exists.</returns>
    static string? FirstExisting(IEnumerable<string?> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (candidate is not null && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Joins a folder with path parts, or null when the folder is missing.</summary>
    /// <param name="folder">The folder, or null.</param>
    /// <param name="parts">The path parts below the folder.</param>
    /// <returns>The joined path, or null when the folder is missing.</returns>
    static string? Join(string? folder, params string[] parts)
    {
        if (string.IsNullOrEmpty(folder))
        {
            return null;
        }

        var full = folder;

        foreach (var part in parts)
        {
            full = Path.Combine(full, part);
        }

        return full;
    }

    /// <summary>The executable on PATH, or null when it is absent.</summary>
    /// <param name="name">The executable name without an extension.</param>
    /// <returns>The full path, or null when the name is not on PATH.</returns>
    static string? FindOnPath(string name)
    {
        var path = CliHost.Variable("PATH") ?? "";

        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = folder.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (var candidate in new[] { name, name + ".exe" })
                {
                    var full = Path.Combine(trimmed, candidate);

                    if (File.Exists(full))
                    {
                        return full;
                    }
                }
            }
            else
            {
                var full = Path.Combine(trimmed, name);

                if (File.Exists(full))
                {
                    return full;
                }
            }
        }

        return null;
    }
}
