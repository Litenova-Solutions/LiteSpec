using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Authoring;

/// <summary>Slug fixture cases: HeadingSlug returns each listed slug.</summary>
public sealed class SlugCasesTests
{
    /// <summary>Heading slugs match the fixture.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-identifier-grammar</proves>
    [Fact(DisplayName = "heading slugs match the fixture")]
    public void SlugsMatchFixture()
    {
        var file = Path.Combine(TestCli.Repository, "apps", "cli", "tests", "LiteSpec.Cli.Tests", "Authoring", "SlugCases.json");
        var cases = RecordText.Read(file)!.AsArray();
        Assert.NotEmpty(cases);
        foreach (var entry in cases)
        {
            var heading = entry!["heading"]!.GetValue<string>();
            var slug = entry["slug"]!.GetValue<string>();
            Assert.Equal(slug, Provisions.HeadingSlug(heading));
        }
    }
}
