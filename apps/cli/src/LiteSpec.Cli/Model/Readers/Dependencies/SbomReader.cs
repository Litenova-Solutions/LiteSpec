using System.Text.Json;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     Reads the direct packages of every bill of materials the project names in paths.sbom:
///     every .json file under each named file or folder, read as SPDX 2.x or CycloneDX 1.x.
///     A file that is neither, or that cannot be read, holds no direct package.
/// </summary>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
static class SbomReader
{
    /// <summary>Reads every direct package of every named bill of materials, in file order.</summary>
    public static IReadOnlyList<DirectPackage> Read(ConsumerConfig config)
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var entry in config.Sboms)
        {
            if (File.Exists(entry) && entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(entry);
            }
            else if (Directory.Exists(entry))
            {
                foreach (var file in SourceTree.Files(entry, ".json"))
                {
                    files.Add(file);
                }
            }
        }

        var packages = new List<DirectPackage>();

        foreach (var file in files)
        {
            try
            {
                using var document = JsonDocument.Parse(SourceTree.Read(file));
                var root = document.RootElement;

                if (SpdxSbomReader.Is(root))
                {
                    packages.AddRange(SpdxSbomReader.Read(file, root));
                }
                else if (CycloneDxSbomReader.Is(root))
                {
                    packages.AddRange(CycloneDxSbomReader.Read(file, root));
                }
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                continue;
            }
        }

        return packages;
    }
}
