using System.Reflection;

namespace LiteSpec.Cli;

/// <summary>
///     The version names a tool build carries. The release is the base version the carried
///     manifest names, such as 6.0.0. A release candidate is packed as 6.0.0-rc.1: the build
///     stamps the suffix into the assembly's informational version, and the tool reads it back
///     so the packages it writes into a consumer name the candidate it came from.
/// </summary>
/// <follows>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</follows>
static class ReleaseVersion
{
    /// <summary>The package version of this build: the carried release with the suffix it was packed with.</summary>
    /// <param name="release">The base version the carried release names.</param>
    /// <returns>The package version, such as 6.0.0 or 6.0.0-rc.1.</returns>
    public static string PackageOf(string release) => PackageOf(release, Informational());

    /// <summary>The package version for a release and an informational version.</summary>
    /// <param name="release">The base version the carried release names.</param>
    /// <param name="informational">The informational version the build was stamped with, which may carry +commit metadata.</param>
    /// <returns>The release followed by the prerelease suffix of the informational version, if any.</returns>
    public static string PackageOf(string release, string informational) => release + Suffix(informational);

    /// <summary>The base version of a version: the part before any prerelease suffix.</summary>
    /// <param name="version">A version such as 6.0.0 or 6.0.0-rc.1.</param>
    /// <returns>The base version, such as 6.0.0.</returns>
    public static string BaseOf(string version)
    {
        var dash = version.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? version : version[..dash];
    }

    /// <summary>Whether a reviewed version names the same release as the installed one, comparing base versions.</summary>
    /// <param name="reviewed">The version a project records as reviewed, which may carry a prerelease suffix.</param>
    /// <param name="installed">The base version of the installed release.</param>
    /// <returns>True when both name the same base version.</returns>
    public static bool SameRelease(string reviewed, string installed) =>
        string.Equals(BaseOf(reviewed), BaseOf(installed), StringComparison.Ordinal);

    /// <summary>The prerelease suffix of an informational version, with its leading dash, or empty.</summary>
    /// <param name="informational">The informational version, which may carry +commit metadata.</param>
    /// <returns>The suffix such as -rc.1, or an empty string for a release.</returns>
    public static string Suffix(string informational)
    {
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        var core = plus < 0 ? informational : informational[..plus];
        var dash = core.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? string.Empty : core[dash..];
    }

    /// <summary>The informational version the CLI assembly was stamped with, or empty when none was.</summary>
    /// <returns>The informational version text.</returns>
    static string Informational() =>
        typeof(ReleaseVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
}
