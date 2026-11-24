using LiteSpec.Maintain.Tests.Support;
using PublishLocalEdits = LiteSpec.Maintain.PublishLocal.PublishLocal;

namespace LiteSpec.Maintain.Tests.PublishLocal;

/// <summary>Tests for the file edits behind <c>litespec-maintain publish-local</c>.</summary>
public sealed class PublishLocalTests
{
    /// <summary>DevVersion stamps the release with the UTC timestamp.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void DevVersionStampsTheReleaseWithTheUtcTimestamp()
    {
        Assert.Equal(
            "7.0.0-dev.20261124090000",
            PublishLocalEdits.DevVersion("7.0.0", new DateTime(2026, 11, 24, 9, 0, 0, DateTimeKind.Utc)));
    }

    /// <summary>FeedValueFor names a feed under the consumer by its relative path.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FeedValueForNamesAFeedUnderTheConsumerByItsRelativePath()
    {
        using var temp = new TempFolder("publish-local");
        var consumer = Path.Combine(temp.Path, "consumer");
        var feed = Path.Combine(consumer, ".agents", "local-feed");

        Assert.Equal(".agents/local-feed", PublishLocalEdits.FeedValueFor(feed, consumer));
    }

    /// <summary>EnsureFeedSource creates the config when the consumer holds none.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void EnsureFeedSourceCreatesTheConfigWhenTheConsumerHoldsNone()
    {
        var (text, changed) = PublishLocalEdits.EnsureFeedSource(null, ".agents/local-feed");

        Assert.True(changed);
        Assert.Equal(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="litespec-local" value=".agents/local-feed" />
              </packageSources>
            </configuration>

            """,
            text);
    }

    /// <summary>EnsureFeedSource adds the source beside the other sources.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void EnsureFeedSourceAddsTheSourceBesideTheOtherSources()
    {
        var config = """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
              </packageSources>
            </configuration>

            """;

        var (text, changed) = PublishLocalEdits.EnsureFeedSource(config, ".agents/local-feed");

        Assert.True(changed);
        Assert.Equal(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
                <add key="litespec-local" value=".agents/local-feed" />
              </packageSources>
            </configuration>

            """,
            text);
    }

    /// <summary>EnsureFeedSource moves an existing source to the feed.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void EnsureFeedSourceMovesAnExistingSourceToTheFeed()
    {
        var config = """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
                <add key="litespec-local" value="old/feed" />
              </packageSources>
            </configuration>

            """;

        var (text, changed) = PublishLocalEdits.EnsureFeedSource(config, ".agents/local-feed");

        Assert.True(changed);
        Assert.Equal(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
                <add key="litespec-local" value=".agents/local-feed" />
              </packageSources>
            </configuration>

            """,
            text);
    }

    /// <summary>EnsureFeedSource leaves a current source alone.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void EnsureFeedSourceLeavesACurrentSourceAlone()
    {
        var config = """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="litespec-local" value=".agents/local-feed" />
              </packageSources>
            </configuration>

            """;

        var (text, changed) = PublishLocalEdits.EnsureFeedSource(config, ".agents/local-feed");

        Assert.False(changed);
        Assert.Equal(config, text);
    }

    /// <summary>EnsureFeedSource refuses a config with no configuration element.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void EnsureFeedSourceRefusesAConfigWithNoConfigurationElement()
    {
        Assert.Throws<InvalidOperationException>(() => PublishLocalEdits.EnsureFeedSource("not xml\n", ".agents/local-feed"));
    }

    /// <summary>SetToolVersion creates the manifest when the consumer holds none.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetToolVersionCreatesTheManifestWhenTheConsumerHoldsNone()
    {
        var (text, changed) = PublishLocalEdits.SetToolVersion(null, "7.0.0-dev.20261124090000");

        Assert.True(changed);
        Assert.Equal(
            """
            {
              "version": 1,
              "isRoot": true,
              "tools": {
                "LiteSpec.Cli": {
                  "version": "7.0.0-dev.20261124090000",
                  "commands": ["litespec"]
                }
              }
            }

            """,
            text);
    }

    /// <summary>SetToolVersion replaces only the pinned version.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetToolVersionReplacesOnlyThePinnedVersion()
    {
        var manifest = """
            {
              "version": 1,
              "isRoot": true,
              "tools": {
                "LiteSpec.Cli": {
                  "version": "7.0.0",
                  "commands": ["litespec"]
                }
              }
            }

            """;

        var (text, changed) = PublishLocalEdits.SetToolVersion(manifest, "7.0.0-dev.20261124090000");

        Assert.True(changed);
        Assert.Equal(manifest.Replace("7.0.0", "7.0.0-dev.20261124090000", StringComparison.Ordinal), text);
    }

    /// <summary>SetToolVersion leaves a current manifest alone.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetToolVersionLeavesACurrentManifestAlone()
    {
        var manifest = PublishLocalEdits.ToolManifest("7.0.0-dev.20261124090000");

        var (text, changed) = PublishLocalEdits.SetToolVersion(manifest, "7.0.0-dev.20261124090000");

        Assert.False(changed);
        Assert.Equal(manifest, text);
    }

    /// <summary>SetToolVersion adds the entry when the manifest holds other tools.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetToolVersionAddsTheEntryWhenTheManifestHoldsOtherTools()
    {
        var manifest = """
            {
              "version": 1,
              "isRoot": true,
              "tools": {
                "other.tool": {
                  "version": "1.2.3",
                  "commands": ["other"]
                }
              }
            }

            """;

        var (text, changed) = PublishLocalEdits.SetToolVersion(manifest, "7.0.0-dev.20261124090000");

        Assert.True(changed);
        Assert.Contains("\"LiteSpec.Cli\"", text, StringComparison.Ordinal);
        Assert.Contains("\"7.0.0-dev.20261124090000\"", text, StringComparison.Ordinal);
        Assert.Contains("\"other.tool\"", text, StringComparison.Ordinal);
    }

    /// <summary>SetToolVersion refuses a manifest that is not valid JSON.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetToolVersionRefusesAManifestThatIsNotValidJson()
    {
        Assert.Throws<InvalidOperationException>(() => PublishLocalEdits.SetToolVersion("{broken\n", "7.0.0-dev.20261124090000"));
    }

    /// <summary>AnalyzerVersionIn reads the version from central and direct references.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void AnalyzerVersionInReadsTheVersionFromCentralAndDirectReferences()
    {
        Assert.Equal(
            "7.0.0",
            PublishLocalEdits.AnalyzerVersionIn("""
                <Project>
                  <ItemGroup>
                    <PackageVersion Include="LiteSpec.Analyzers" Version="7.0.0" />
                  </ItemGroup>
                </Project>

                """));
        Assert.Equal(
            "7.0.0",
            PublishLocalEdits.AnalyzerVersionIn("""
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Version="7.0.0" Include="LiteSpec.Analyzers" />
                  </ItemGroup>
                </Project>

                """));
        Assert.Equal(
            "7.0.0",
            PublishLocalEdits.AnalyzerVersionIn("""
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="LiteSpec.Analyzers">
                      <Version>7.0.0</Version>
                    </PackageReference>
                  </ItemGroup>
                </Project>

                """));
        Assert.Null(PublishLocalEdits.AnalyzerVersionIn("""
            <Project>
              <ItemGroup>
                <PackageVersion Include="YamlDotNet" Version="18.1.0" />
              </ItemGroup>
            </Project>

            """));
    }

    /// <summary>SetAnalyzerVersion moves every stated analyzer version.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetAnalyzerVersionMovesEveryStatedAnalyzerVersion()
    {
        var props = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="YamlDotNet" Version="18.1.0" />
                <PackageVersion Include="LiteSpec.Analyzers" Version="7.0.0" />
              </ItemGroup>
            </Project>

            """;

        var (text, changed) = PublishLocalEdits.SetAnalyzerVersion(props, "7.0.0-dev.20261124090000");

        Assert.Equal(1, changed);
        Assert.Contains("<PackageVersion Include=\"LiteSpec.Analyzers\" Version=\"7.0.0-dev.20261124090000\" />", text, StringComparison.Ordinal);
        Assert.Contains("<PackageVersion Include=\"YamlDotNet\" Version=\"18.1.0\" />", text, StringComparison.Ordinal);
    }

    /// <summary>SetAnalyzerVersion leaves a current version alone.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void SetAnalyzerVersionLeavesACurrentVersionAlone()
    {
        var props = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="LiteSpec.Analyzers" Version="7.0.0-dev.20261124090000" />
              </ItemGroup>
            </Project>

            """;

        var (text, changed) = PublishLocalEdits.SetAnalyzerVersion(props, "7.0.0-dev.20261124090000");

        Assert.Equal(0, changed);
        Assert.Equal(props, text);
    }
}
