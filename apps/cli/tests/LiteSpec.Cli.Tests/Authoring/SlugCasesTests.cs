using System.Text.Json.Nodes;
using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Slug fixture cases: HeadingSlug returns each listed slug.</summary>
public sealed class SlugCasesTests
{
    /// <summary>Heading slugs match the fixture.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "heading slugs match the fixture")]
    public void SlugsMatchFixture()
    {
        var file = Path.Combine(Cli.Repository, "apps", "cli", "tests", "LiteSpec.Cli.Tests", "Authoring", "SlugCases.json");
        var cases = JsonNode.Parse(File.ReadAllText(file))!.AsArray();
        Assert.NotEmpty(cases);
        foreach (var entry in cases)
        {
            var heading = entry!["heading"]!.GetValue<string>();
            var slug = entry["slug"]!.GetValue<string>();
            Assert.Equal(slug, Provisions.HeadingSlug(heading));
        }
    }
}
