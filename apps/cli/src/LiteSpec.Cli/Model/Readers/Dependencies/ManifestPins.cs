using System.Text.Json.Nodes;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     The manifest pins of the carried LiteSpec release: the NuGet pins compared
///     case-insensitively and the npm pins compared case-sensitively, by package name.
/// </summary>
/// <param name="NuGet">The NuGet package pins, by package name.</param>
/// <param name="Npm">The npm package pins, by package name.</param>
/// <follows>litespec/ext/delivery#match-every-shipped-package-to-its-pin</follows>
sealed record ManifestPins(IReadOnlyDictionary<string, string> NuGet, IReadOnlyDictionary<string, string> Npm)
{
    /// <summary>
    ///     Reads the NuGet and npm pins from the manifest of the carried release.
    ///     Empty when the release or its manifest cannot be read.
    /// </summary>
    /// <returns>The pins by ecosystem and package name; empty when the manifest cannot be read.</returns>
    public static ManifestPins Read()
    {
        var nuget = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var npm = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            var home = LiteSpecHome.Root;

            if (home is null || !File.Exists(Path.Combine(home, LiteSpecHome.ManifestFile)))
            {
                return new ManifestPins(nuget, npm);
            }

            if (LiteSpecHome.ReadManifest(home)["packages"] is JsonObject packages)
            {
                AddPins(packages, "nuget", nuget);
                AddPins(packages, "npm", npm);
            }
        }
        catch (Exception exception) when (exception is IOException or JsonTextException or UnauthorizedAccessException)
        {
            return new ManifestPins(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.Ordinal));
        }

        return new ManifestPins(nuget, npm);
    }

    /// <summary>Copies the ecosystem's package pins into the table, first entry winning.</summary>
    /// <param name="packages">The manifest object that holds the package pins of one ecosystem.</param>
    /// <param name="ecosystem">The ecosystem the pins belong to, such as nuget or npm.</param>
    /// <param name="pins">The pins collected so far, by package name, added to in place.</param>
    private static void AddPins(JsonObject packages, string ecosystem, Dictionary<string, string> pins)
    {
        if (packages[ecosystem] is not JsonObject table)
        {
            return;
        }

        foreach (var entry in table)
        {
            if (entry.Key.Length > 0
                && JsonValues.AsString(entry.Value) is { Length: > 0 } version)
            {
                pins.TryAdd(entry.Key, version);
            }
        }
    }
}
