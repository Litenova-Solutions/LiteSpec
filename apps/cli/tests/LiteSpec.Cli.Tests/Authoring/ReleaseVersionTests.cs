using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>
///     Proves every place that names the release names the same one: a tag publishes the
///     packages and the site from these files, so a mismatch ships two versions at once.
/// </summary>
public sealed class ReleaseVersionTests
{
    static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Cli.Repository, .. parts]));

    static string Manifest() =>
        JsonDocument.Parse(Read("litespec.manifest.json")).RootElement.GetProperty("version").GetString()!;

    static string PackageVersion(params string[] project) =>
        Regex.Match(Read(project), "<Version>(?<v>[^<]+)</Version>").Groups["v"].Value;

    static readonly Regex SchemaReferencePattern = new(
        "\"\\$schema\"\\s*:\\s*\"(?<url>[^\"]+)\"",
        RegexOptions.CultureInvariant);

    static readonly Regex VersionedUrlPattern = new(
        "litespec\\.dev/(?<v>[0-9]+\\.[0-9]+\\.[0-9]+)/",
        RegexOptions.CultureInvariant);

    static readonly Regex InstallVersionPattern = new(
        "--version\\s+(?<v>[0-9]+\\.[0-9]+\\.[0-9]+)",
        RegexOptions.CultureInvariant);

    static string Relative(string file) =>
        Path.GetRelativePath(Cli.Repository, file).Replace('\\', '/');

    static IEnumerable<string> FilesUnder(string folder, string pattern) =>
        Directory.EnumerateFiles(Path.Combine(Cli.Repository, folder), pattern, SearchOption.AllDirectories)
            .OrderBy(file => file, StringComparer.Ordinal);

    // The project-file reference page quotes one error that names a reviewed
    // release beside the installed release to show a mismatch. That line
    // deliberately names another version, so the install check skips it.
    static bool IsDeliberateMismatchExample(string relative, string line) =>
        relative == "docs/reference/project-file.md"
        && line.Contains("does not match the installed LiteSpec tool release", StringComparison.Ordinal);

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void The_Tool_Package_Names_The_Manifest_Release() =>
        Assert.Equal(Manifest(), PackageVersion("apps", "cli", "src", "LiteSpec.Cli", "LiteSpec.Cli.csproj"));

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void The_Analyzer_Package_Names_The_Manifest_Release() =>
        Assert.Equal(Manifest(), PackageVersion("apps", "analyzers", "src", "LiteSpec.Analyzers", "LiteSpec.Analyzers.csproj"));

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void The_Changelog_Opens_With_The_Manifest_Release() =>
        Assert.Equal($"## v{Manifest()}", Read("CHANGELOG.md").Split('\n').Select(line => line.TrimEnd('\r')).First(line => line.StartsWith("## ", StringComparison.Ordinal)));

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void The_Consumer_Template_Reviews_The_Manifest_Release() =>
        Assert.Equal(
            Manifest(),
            JsonDocument.Parse(Read("templates", "consumer", "litespec.project.json")).RootElement.GetProperty("reviewedLiteSpecVersion").GetString());

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void Every_Schema_Id_Names_The_Manifest_Release()
    {
        var manifest = Manifest();
        var mismatches = new List<string>();

        foreach (var file in FilesUnder("schemas", "*.json"))
        {
            var relative = Relative(file);
            var expected = $"https://litespec.dev/{manifest}/schemas/{Path.GetFileName(file)}";
            string? id;

            try
            {
                var root = JsonDocument.Parse(File.ReadAllText(file)).RootElement;
                id = root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("$id", out var value)
                    && value.ValueKind == JsonValueKind.String
                        ? value.GetString()
                        : null;
            }
            catch (JsonException)
            {
                mismatches.Add($"{relative}: $id '<unparsable>' must be '{expected}'");
                continue;
            }

            if (id != expected)
            {
                mismatches.Add($"{relative}: $id '{id ?? "<missing>"}' must be '{expected}'");
            }
        }

        Assert.True(mismatches.Count == 0, "Schema $id values that disagree with the manifest release:\n" + string.Join("\n", mismatches));
    }

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void Every_Template_And_Example_Schema_Names_The_Manifest_Release()
    {
        var manifest = Manifest();
        var mismatches = new List<string>();
        var files = FilesUnder("templates", "*.json").Concat(FilesUnder("examples", "*.json"));

        foreach (var file in files)
        {
            var relative = Relative(file);

            foreach (Match match in SchemaReferencePattern.Matches(File.ReadAllText(file)))
            {
                var url = match.Groups["url"].Value;

                if (!url.StartsWith("https://litespec.dev/", StringComparison.Ordinal))
                {
                    continue;
                }

                var version = url.Substring("https://litespec.dev/".Length).Split('/')[0];

                if (version != manifest)
                {
                    mismatches.Add($"{relative}: $schema '{url}' names version '{version}' instead of '{manifest}'");
                }
            }
        }

        Assert.True(mismatches.Count == 0, "Template and example $schema values that disagree with the manifest release:\n" + string.Join("\n", mismatches));
    }

    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void Every_Versioned_Url_And_Install_Version_Names_The_Manifest_Release()
    {
        var manifest = Manifest();
        var mismatches = new List<string>();
        var files = new List<string>
        {
            Path.Combine(Cli.Repository, "apps", "cli", "src", "LiteSpec.Cli", "PACKAGE.md"),
            Path.Combine(Cli.Repository, "README.md"),
            Path.Combine(Cli.Repository, "CONTRIBUTING.md"),
        };
        files.AddRange(FilesUnder("docs", "*.md"));
        files.AddRange(FilesUnder(".github", "*"));

        foreach (var file in files.OrderBy(candidate => candidate, StringComparer.Ordinal))
        {
            var relative = Relative(file);
            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];

                if (IsDeliberateMismatchExample(relative, line))
                {
                    continue;
                }

                foreach (Match match in VersionedUrlPattern.Matches(line))
                {
                    var version = match.Groups["v"].Value;

                    if (version != manifest)
                    {
                        mismatches.Add($"{relative}:{index + 1}: versioned URL names version '{version}' instead of '{manifest}'");
                    }
                }

                foreach (Match match in InstallVersionPattern.Matches(line))
                {
                    var version = match.Groups["v"].Value;

                    if (version != manifest)
                    {
                        mismatches.Add($"{relative}:{index + 1}: --version names version '{version}' instead of '{manifest}'");
                    }
                }
            }
        }

        Assert.True(mismatches.Count == 0, "Versioned references that disagree with the manifest release:\n" + string.Join("\n", mismatches));
    }
}
