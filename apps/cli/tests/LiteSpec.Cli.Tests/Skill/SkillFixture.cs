namespace LiteSpec.Cli.Tests.Skill;

/// <summary>The fixture release the skill tests read: a manifest with a fixture version and minimal skill templates.</summary>
static class SkillFixture
{
    /// <summary>The version the fixture manifest carries, which no release ships.</summary>
    public const string FixtureVersion = "9.9.9";

    /// <summary>A minimal skill entry template with the release placeholder, in the contract shape.</summary>
    public const string EntryTemplate = """
        ---
        name: litespec
        description: Fixture skill for the skill tests.
        metadata:
          generated-by: litespec
        __LITESPEC_RELEASE_METADATA__
        ---

        # Fixture Skill
        """ + "\n";

    /// <summary>The placeholder line of the entry template, with its line break.</summary>
    public const string PlaceholderLine = "__LITESPEC_RELEASE_METADATA__\n";

    /// <summary>Builds a release checkout holding only the manifest and the skill templates.</summary>
    /// <param name="home">The folder to build the fixture checkout in.</param>
    public static void FixtureHome(string home)
    {
        File.WriteAllText(Path.Combine(home, "litespec.manifest.yaml"), """
            {
              "version": "9.9.9"
            }
            """);

        FixtureTemplates(Path.Combine(home, "templates"));
    }

    /// <summary>Writes the three skill templates under a release templates folder.</summary>
    /// <param name="templates">The release templates folder.</param>
    public static void FixtureTemplates(string templates)
    {
        var skill = Path.Combine(templates, "skill", "litespec");
        Directory.CreateDirectory(Path.Combine(skill, "references"));
        File.WriteAllText(Path.Combine(skill, "SKILL.md"), EntryTemplate);
        File.WriteAllText(Path.Combine(skill, "references", "commands.md"), "# Fixture Commands\n");
        File.WriteAllText(Path.Combine(skill, "references", "report.md"), "# Fixture Report\n");
    }

    /// <summary>A consumer path from forward-slash segments.</summary>
    /// <param name="root">The consumer root or home folder.</param>
    /// <param name="relative">The forward-slash path under the root.</param>
    /// <returns>The full path.</returns>
    public static string Join(string root, string relative) =>
        Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
}
