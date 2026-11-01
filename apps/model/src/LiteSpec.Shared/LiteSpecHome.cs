using System.Text.Json.Nodes;

namespace LiteSpec;

/// <summary>
///     Finds the LiteSpec release this tool carries: the folder that holds
///     <c>litespec.manifest.json</c>, its schemas and its docs. The tool package
///     ships that release beside its own binary, so the tool locates it from its
///     own binary rather than from the consumer.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class LiteSpecHome
{
    /// <summary>The manifest file name.</summary>
    public const string ManifestFile = "litespec.manifest.json";

    /// <summary>The located release root, computed once.</summary>
    static readonly Lazy<string?> root = new(Locate);

    /// <summary>Gets the LiteSpec release root, or null when none is found.</summary>
    public static string? Root => root.Value;

    /// <summary>The LiteSpec release root, or a failure naming how to point at one.</summary>
    /// <returns>The LiteSpec release root.</returns>
    public static string RequireRoot() =>
        Root ?? throw new InvalidOperationException(
            $"No LiteSpec release was found above '{AppContext.BaseDirectory}'. Set LITESPEC_HOME to the folder that holds {ManifestFile}.");

    /// <summary>Gets the <c>version</c> field of the manifest, or null when it cannot be read.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Version reads an optional manifest beside the tool binary and reports null on any read or parse failure, so a tool with no readable release reads as unversioned rather than crashing.")]
    public static string? Version
    {
        get
        {
            if (Root is null)
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(File.ReadAllText(Path.Combine(Root, ManifestFile)))?["version"]?.GetValue<string>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>A path inside the LiteSpec release, from forward-slash segments.</summary>
    /// <param name="relative">The forward-slash path inside the release.</param>
    /// <returns>The full path inside the LiteSpec release.</returns>
    public static string PathTo(string relative) =>
        Path.Combine(RequireRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Locates the LiteSpec release root.</summary>
    /// <returns>The release root, or null when none is found.</returns>
    static string? Locate()
    {
        var configured = Environment.GetEnvironmentVariable("LITESPEC_HOME");

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return File.Exists(Path.Combine(configured, ManifestFile)) ? Path.GetFullPath(configured) : null;
        }

        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, ManifestFile)))
            {
                return folder.FullName;
            }
        }

        return null;
    }
}
