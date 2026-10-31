using System.Text.Json;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>What the bill of materials read found: direct packages and the configured paths it could not read.</summary>
/// <param name="Packages">Every direct package of every readable bill of materials.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per configured path that could not be read or parsed, is not JSON, is neither SPDX nor CycloneDX, or yields no packages.</param>
sealed record SbomRead(IReadOnlyList<DirectPackage> Packages, IReadOnlyList<Finding> Problems);

/// <summary>
///     Reads the direct packages of every bill of materials the project names in paths.sbom:
///     every .json file under each named file or folder, read as SPDX 2.x or CycloneDX 1.x.
///     A configured path that is missing, is not JSON, cannot be read or parsed, is neither
///     SPDX nor CycloneDX, or yields no packages reports one <c>reader-missing</c> finding
///     naming that path. No <c>paths.sbom</c> configured stays silent.
/// </summary>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
static class SbomReader
{
    /// <summary>Reads every direct package of every named bill of materials, in file order, with its problems.</summary>
    public static SbomRead Read(ConsumerConfig config)
    {
        var problems = new List<Finding>();

        if (config.Sboms.Count == 0)
        {
            return new SbomRead([], problems);
        }

        var files = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var entry in config.Sboms)
        {
            if (File.Exists(entry))
            {
                if (entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(entry);
                }
                else
                {
                    problems.Add(Missing(entry, $"Bill of materials '{entry}' could not be read: the path is not a JSON file."));
                }
            }
            else if (Directory.Exists(entry))
            {
                var found = SourceTree.Files(entry, ".json");

                if (found.Count == 0)
                {
                    problems.Add(Missing(entry, $"Bill of materials '{entry}' yields no packages: the folder holds no JSON bill of materials."));
                }

                foreach (var file in found)
                {
                    files.Add(file);
                }
            }
            else
            {
                problems.Add(Missing(entry, $"Bill of materials '{entry}' could not be read: the path does not exist."));
            }
        }

        var packages = new List<DirectPackage>();

        foreach (var file in files)
        {
            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(SourceTree.Read(file));
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                problems.Add(Missing(file, $"Bill of materials '{file}' could not be read: {exception.Message}"));

                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                var before = packages.Count;

                if (SpdxSbomReader.Is(root))
                {
                    packages.AddRange(SpdxSbomReader.Read(file, root));
                }
                else if (CycloneDxSbomReader.Is(root))
                {
                    packages.AddRange(CycloneDxSbomReader.Read(file, root));
                }
                else
                {
                    problems.Add(Missing(file, $"Bill of materials '{file}' could not be read: the file is neither an SPDX nor a CycloneDX bill of materials."));

                    continue;
                }

                if (packages.Count == before)
                {
                    problems.Add(Missing(file, $"Bill of materials '{file}' yields no packages."));
                }
            }
        }

        problems.Sort((left, right) =>
        {
            var file = string.Compare(left.Path, right.Path, StringComparison.Ordinal);

            return file != 0 ? file : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        });

        return new SbomRead(packages, problems);
    }

    /// <summary>One <c>reader-missing</c> finding naming the bill of materials path.</summary>
    private static Finding Missing(string path, string message) =>
        new("reader-missing", message, path, 1, null)
        {
            Reader = Finding.RoslynReader,
            ReaderIsQualified = true,
        };
}
