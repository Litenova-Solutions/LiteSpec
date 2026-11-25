using LiteSpec.Cli.Model.Configuration;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>The framework reference assemblies from the targeting pack matching the consumer's target framework.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class TargetingPack
{
    /// <summary>Loads the framework assemblies for the consumer: the pack matching its target framework, highest patch wins.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The framework references, with the reason when none was found.</returns>
    public static (IReadOnlyList<MetadataReference> References, string? UnavailableReason) Load(ConsumerConfig config) =>
        Load(ProjectAssets.TargetFrameworks(config), DotnetRoots.Candidates());

    /// <summary>Loads from explicit frameworks and roots, so a test can name fake packs.</summary>
    /// <param name="frameworks">The target frameworks, such as net10.0.</param>
    /// <param name="roots">The dotnet roots to search.</param>
    /// <returns>The framework references, with the reason when none was found.</returns>
    internal static (IReadOnlyList<MetadataReference> References, string? UnavailableReason) Load(IReadOnlyList<string> frameworks, IReadOnlyList<string> roots)
    {
        if (roots.Count == 0)
        {
            return ([], "no dotnet installation was found");
        }

        var packs = AvailablePacks(roots);

        if (packs.Count == 0)
        {
            return ([], "no targeting pack was found under the dotnet installation");
        }

        var wanted = new List<Version>();

        foreach (var framework in frameworks)
        {
            if (TryParseFrameworkVersion(framework, out var version) && !wanted.Contains(version))
            {
                wanted.Add(version);
            }
        }

        wanted.Sort((left, right) => right.CompareTo(left));

        (string Path, Version Version)? selected = null;

        if (wanted.Count > 0)
        {
            foreach (var version in wanted)
            {
                (string Path, Version Version)? candidate = null;

                foreach (var pack in packs)
                {
                    if (pack.Version.Major != version.Major || pack.Version.Minor != version.Minor)
                    {
                        continue;
                    }

                    if (candidate is null || pack.Version.CompareTo(candidate.Value.Version) > 0)
                    {
                        candidate = pack;
                    }
                }

                if (candidate is not null)
                {
                    selected = candidate;
                    break;
                }
            }

            if (selected is null)
            {
                return ([], $"no targeting pack for {string.Join(", ", frameworks)} was found");
            }
        }
        else
        {
            selected = packs[0];

            foreach (var pack in packs)
            {
                if (pack.Version.CompareTo(selected.Value.Version) > 0)
                {
                    selected = pack;
                }
            }
        }

        var packName = Path.GetFileName(selected.Value.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var packPath = selected.Value.Path;
        var packVersion = selected.Value.Version;

        var refFolder = Path.Combine(packPath, "ref", $"net{packVersion.Major}.{packVersion.Minor}");

        if (!Directory.Exists(refFolder))
        {
            refFolder = FirstRefFolder(packPath) ?? "";

            if (refFolder.Length == 0)
            {
                return ([], $"targeting pack {packName} holds no reference assemblies");
            }
        }

        var found = new List<MetadataReference>();

        foreach (var name in new[] { "System.Private.CoreLib.dll", "System.Runtime.dll" })
        {
            var candidate = Path.Combine(refFolder, name);

            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                found.Add(MetadataReference.CreateFromFile(candidate));
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                continue;
            }
        }

        if (found.Count == 0)
        {
            return ([], $"targeting pack {packName} holds no reference assemblies");
        }

        return (found, null);
    }

    /// <summary>Every targeting pack under the dotnet roots: each version folder of the framework reference pack.</summary>
    /// <param name="roots">The dotnet roots to search.</param>
    /// <returns>The pack folder with its version.</returns>
    internal static IReadOnlyList<(string Path, Version Version)> AvailablePacks(IReadOnlyList<string> roots)
    {
        var found = new List<(string Path, Version Version)>();

        foreach (var root in roots)
        {
            var packs = Path.Combine(root, "packs", "Microsoft.NETCore.App.Ref");

            if (!Directory.Exists(packs))
            {
                continue;
            }

            string[] folders;

            try
            {
                folders = Directory.GetDirectories(packs);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var folder in folders)
            {
                var name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                if (TryParsePackVersion(name, out var version))
                {
                    found.Add((folder, version));
                }
            }
        }

        return found;
    }

    /// <summary>The first reference folder under the pack, so a pack whose layout moved still resolves.</summary>
    /// <param name="pack">The pack version folder.</param>
    /// <returns>The first ref folder, or null when the pack holds none.</returns>
    static string? FirstRefFolder(string pack)
    {
        var refRoot = Path.Combine(pack, "ref");

        if (!Directory.Exists(refRoot))
        {
            return null;
        }

        try
        {
            foreach (var folder in Directory.GetDirectories(refRoot))
            {
                return folder;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    /// <summary>Parses a target framework to its major and minor version, for pack matching.</summary>
    /// <param name="framework">The target framework, such as net10.0 or .NETCoreApp,Version=v8.0.</param>
    /// <param name="version">The major and minor version, or 0.0 when the framework names no .NET version.</param>
    /// <returns>True when the framework names a .NET version; otherwise false.</returns>
    internal static bool TryParseFrameworkVersion(string framework, out Version version)
    {
        version = new Version(0, 0);
        var text = framework.Trim();

        if (text.Length == 0)
        {
            return false;
        }

        const string marker = ",Version=v";
        var at = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (at >= 0)
        {
            if (!text.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            text = text[(at + marker.Length)..];
            var comma = text.IndexOf(',');

            if (comma >= 0)
            {
                text = text[..comma];
            }

            return TryParseMajorMinor(text, out version);
        }

        string number;

        if (text.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
        {
            number = text["netcoreapp".Length..];
        }
        else if (text.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            number = text["net".Length..];

            if (number.StartsWith("standard", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        var dash = number.IndexOf('-');

        if (dash >= 0)
        {
            number = number[..dash];
        }

        return TryParseMajorMinor(number, out version);
    }

    /// <summary>Parses a major and minor version, such as 10.0.</summary>
    /// <param name="text">The version text.</param>
    /// <param name="version">The major and minor version, or 0.0 when the text names none.</param>
    /// <returns>True when the text names a major and minor version; otherwise false.</returns>
    static bool TryParseMajorMinor(string text, out Version version)
    {
        version = new Version(0, 0);
        var dot = text.IndexOf('.');

        if (dot <= 0)
        {
            return false;
        }

        var rest = text[(dot + 1)..];
        var digits = 0;

        while (digits < rest.Length && char.IsDigit(rest[digits]))
        {
            digits++;
        }

        if (digits == 0)
        {
            return false;
        }

        if (!int.TryParse(text[..dot], out var major) || !int.TryParse(rest[..digits], out var minor))
        {
            return false;
        }

        version = new Version(major, minor);
        return true;
    }

    /// <summary>Parses a pack version folder name, such as 10.0.14.</summary>
    /// <param name="folder">The pack version folder name.</param>
    /// <param name="version">The pack version, or 0.0 when the folder names none.</param>
    /// <returns>True when the folder names a pack version; otherwise false.</returns>
    internal static bool TryParsePackVersion(string folder, out Version version)
    {
        version = new Version(0, 0);
        var text = folder.Trim();
        var dash = text.IndexOf('-');

        if (dash >= 0)
        {
            text = text[..dash];
        }

        if (!Version.TryParse(text, out var parsed) || parsed.Minor < 0)
        {
            return false;
        }

        version = parsed;
        return true;
    }
}
