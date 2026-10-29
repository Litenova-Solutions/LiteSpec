using System.Text.Json;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     The manifest pins of the carried LiteSpec release: the NuGet pins compared
///     case-insensitively and the npm pins compared case-sensitively, by package name.
/// </summary>
/// <param name="NuGet">The NuGet package pins, by package name.</param>
/// <param name="Npm">The npm package pins, by package name.</param>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
sealed record ManifestPins(IReadOnlyDictionary<string, string> NuGet, IReadOnlyDictionary<string, string> Npm)
{
    /// <summary>
    ///     Reads the NuGet and npm pins from the manifest of the carried release.
    ///     Empty when the release or its manifest cannot be read.
    /// </summary>
    public static ManifestPins Read()
    {
        var nuget = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var npm = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            var home = LiteSpecHome.Root;

            if (home is null)
            {
                return new ManifestPins(nuget, npm);
            }

            var file = Path.Combine(home, LiteSpecHome.ManifestFile);

            if (!File.Exists(file))
            {
                return new ManifestPins(nuget, npm);
            }

            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("packages", out var packages)
                && packages.ValueKind == JsonValueKind.Object)
            {
                AddPins(packages, "nuget", nuget);
                AddPins(packages, "npm", npm);
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return new ManifestPins(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.Ordinal));
        }

        return new ManifestPins(nuget, npm);
    }

    /// <summary>Copies the ecosystem's package pins into the table, first entry winning.</summary>
    private static void AddPins(JsonElement packages, string ecosystem, Dictionary<string, string> pins)
    {
        if (!packages.TryGetProperty(ecosystem, out var table) || table.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var entry in table.EnumerateObject())
        {
            if (entry.Name.Length > 0
                && entry.Value.ValueKind == JsonValueKind.String
                && entry.Value.GetString() is { Length: > 0 } version)
            {
                pins.TryAdd(entry.Name, version);
            }
        }
    }
}
