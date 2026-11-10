using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Authoring;

/// <summary>
///     Proves every place that names the release names the same one: a tag publishes the
///     packages and the site from these files, so a mismatch ships two versions at once.
/// </summary>
public sealed class ReleaseVersionTests
{
    static string Read(params string[] parts) => File.ReadAllText(Path.Combine([TestCli.Repository, .. parts]));

    static string Manifest() =>
        ((string?)DataFile.Read(Path.Combine(TestCli.Repository, "litespec.manifest.yaml"))!["version"])!;

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
        Path.GetRelativePath(TestCli.Repository, file).Replace('\\', '/');

    static IEnumerable<string> FilesUnder(string folder, string pattern) =>
        Directory.EnumerateFiles(Path.Combine(TestCli.Repository, folder), pattern, SearchOption.AllDirectories)
            .OrderBy(file => file, StringComparer.Ordinal);

    // The project-file reference page quotes one error that names a reviewed
    // release beside the installed release to show a mismatch. That line
    // deliberately names another version, so the install check skips it.
    static bool IsDeliberateMismatchExample(string relative, string line) =>
        relative == "docs/reference/project-file.md"
        && line.Contains("does not match the installed LiteSpec tool release", StringComparison.Ordinal);

    /// <summary>The Tool Package Names The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void TheToolPackageNamesTheManifestRelease() =>
        Assert.Equal(Manifest(), PackageVersion("apps", "cli", "src", "LiteSpec.Cli", "LiteSpec.Cli.csproj"));

    /// <summary>The security policy supports the release line the manifest prepares.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void TheSecurityPolicySupportsTheManifestReleaseLine()
    {
        var parts = Manifest().Split('.');
        Assert.Contains($"| {parts[0]}.{parts[1]}.x | Yes |", Read("SECURITY.md"), StringComparison.Ordinal);
    }

    /// <summary>The Analyzer Package Names The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void TheAnalyzerPackageNamesTheManifestRelease() =>
        Assert.Equal(Manifest(), PackageVersion("apps", "analyzers", "src", "LiteSpec.Analyzers", "LiteSpec.Analyzers.csproj"));

    /// <summary>The Changelog Opens With The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void TheChangelogOpensWithTheManifestRelease() =>
        Assert.Equal($"## v{Manifest()}", Read("CHANGELOG.md").Split('\n').Select(line => line.TrimEnd('\r')).First(line => line.StartsWith("## ", StringComparison.Ordinal)));

    /// <summary>The Consumer Template Reviews The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void TheConsumerTemplateReviewsTheManifestRelease() =>
        Assert.Equal(
            Manifest(),
            (string?)DataFile.Read(Path.Combine(TestCli.Repository, "templates", "consumer", "project.yaml"))!["reviewedLiteSpecVersion"]);

    /// <summary>Every Schema Id Names The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void EverySchemaIdNamesTheManifestRelease()
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

    /// <summary>Every Template And Example Schema Names The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void EveryTemplateAndExampleSchemaNamesTheManifestRelease()
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

    /// <summary>Every Versioned Url And Install Version Names The Manifest Release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact]
    public void EveryVersionedUrlAndInstallVersionNamesTheManifestRelease()
    {
        var manifest = Manifest();
        var mismatches = new List<string>();
        var files = new List<string>
        {
            Path.Combine(TestCli.Repository, "apps", "cli", "src", "LiteSpec.Cli", "README.md"),
            Path.Combine(TestCli.Repository, "README.md"),
            Path.Combine(TestCli.Repository, "CONTRIBUTING.md"),
        };
        files.AddRange(FilesUnder("docs", "*.md"));
        files.AddRange(FilesUnder(".github", "*"));
        files.AddRange(FilesUnder(Path.Combine("apps", "cli", "install"), "*"));

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
