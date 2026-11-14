using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Init;

/// <summary>Tests for <c>litespec init</c>.</summary>
public sealed class InitCommandTests
{
    const string FixtureVersion = "9.9.9";

    /// <summary>A minimal agent context template with the region and every placeholder, for exact fill assertions.</summary>
    const string RegionTemplate = """
        # <Project> Agent Context

        <!-- generated:agent-protocol -->
        When __LITESPEC_COMMAND__ disagrees, the tool wins.
        Release __LITESPEC_VERSION__ size __SIZE__ shape __SHAPE__ stacks __STACKS__ extensions __EXTENSIONS__ layers __LAYERS__ key __SELECTION_KEY__ command __LITESPEC_COMMAND__.
        <!-- /generated:agent-protocol -->

        ## Project Commands

        Run things.
        """ + "\n";

    /// <summary>
    ///     The project schema, loaded once for every preset case. The JSON schema
    ///     registry is process-wide and the evaluator caches its compilation per
    ///     schema node, so reusing this one node registers the schema once.
    /// </summary>
    static readonly Lazy<JsonNode?> ProjectSchema = new(LoadProjectSchema);

    /// <summary>Init keeps the shared UI package only for a controlled web frontend.</summary>
    /// <param name="preset">The preset name that selects the stack combination.</param>
    /// <param name="expected">Whether the preset keeps the shared UI package.</param>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Theory(DisplayName = "init keeps the shared UI package only for a controlled web frontend")]
    [InlineData("dotnet-react", true)]
    [InlineData("dotnet-svelte", true)]
    [InlineData("blazor-client", false)]
    [InlineData("android-client", false)]
    [InlineData("records", false)]
    public void InitKeepsTheUiPackageOnlyForControlledFrontends(string preset, bool expected)
    {
        using var consumer = TestCli.Temp("init-ui");

        var result = TestCli.Run("init", "--preset", preset, "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var project = RecordText.Read(Path.Combine(consumer.Path, ".litespec/project.yaml"))!.AsObject();
        Assert.Equal(expected, project.ContainsKey("ui"));
    }

    /// <summary>Init writes a project file that validates against the project schema for each preset.</summary>
    /// <param name="preset">The preset name that selects the stack combination.</param>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Theory(DisplayName = "init writes a project file that validates against the project schema for each preset")]
    [InlineData("android-client")]
    [InlineData("blazor-client")]
    [InlineData("dotnet-react")]
    [InlineData("dotnet-svelte")]
    [InlineData("records")]
    public void InitOutputValidatesAgainstTheProjectSchema(string preset)
    {
        using var consumer = TestCli.Temp("init-schema");

        var result = TestCli.Run("init", "--preset", preset, "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var project = RecordText.Read(Path.Combine(consumer.Path, ".litespec/project.yaml"));
        var schemaFile = Path.Combine(TestCli.Repository, "schemas", "litespec-project.schema.yaml");
        var problems = JsonSchema.Problems(schemaFile, project, ".litespec/project.yaml", _ => ProjectSchema.Value);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    /// <summary>Init with the records preset writes a records project with no stacks and no frontends.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "init with the records preset writes a records project with no stacks and no frontends")]
    public void InitWithTheRecordsPresetWritesNoStacksOrFrontends()
    {
        using var consumer = TestCli.Temp("init-records");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var project = RecordText.Read(Path.Combine(consumer.Path, ".litespec/project.yaml"))!.AsObject();
        Assert.Equal(2, project["layers"]!.AsObject().Count);
        Assert.Equal("litespec", project["layers"]!["behavior"]!.GetValue<string>());
        Assert.Equal("server", project["shape"]!.GetValue<string>());
        Assert.False(project.ContainsKey("stacks"));
        var paths = project["paths"]!.AsObject();
        Assert.False(project.ContainsKey("frontends"));
        Assert.False(project.ContainsKey("ui"));
        Assert.False(paths.ContainsKey("uiPatterns"));
        var agents = File.ReadAllText(Path.Combine(consumer.Path, "AGENTS.md"));
        Assert.Contains("<!-- generated:agent-protocol -->", agents, StringComparison.Ordinal);
        Assert.Contains("<!-- /generated:agent-protocol -->", agents, StringComparison.Ordinal);
        Assert.Contains(
            "When this file disagrees with `dotnet litespec rules`, `dotnet litespec model query` or a record, the tool output and the record win; this file only points to them.",
            agents,
            StringComparison.Ordinal);
        Assert.DoesNotContain("__STACKS__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__SELECTION_KEY__", agents, StringComparison.Ordinal);
    }

    /// <summary>Init fills the project name and leaves no placeholder to fill.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init fills the project name and leaves no placeholder to fill")]
    public void InitFillsTheProjectNameAndLeavesNoPlaceholders()
    {
        using var consumer = TestCli.Temp("init-placeholders");

        var result = TestCli.Run("init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path, "--name", "shop");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("Fill these placeholders before validating", result.Stdout, StringComparison.Ordinal);
        var text = File.ReadAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"));
        Assert.Contains("apps/api/src/shop.Modules", text, StringComparison.Ordinal);
        Assert.Contains("\"@shop/ui\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__PROJECT__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("fixture", text, StringComparison.Ordinal);
    }

    /// <summary>Init lists a template placeholder it cannot fill.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init lists a template placeholder it cannot fill")]
    public void InitListsAnUnfilledPlaceholder()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);
        var templatePath = Path.Combine(home.Path, "templates", "consumer", "project.yaml");
        File.WriteAllText(
            templatePath,
            File.ReadAllText(templatePath).Replace(
                "\"selectedExtensions\": []",
                "\"selectedExtensions\": [],\n              \"note\": \"__EXAMPLE__\"",
                StringComparison.Ordinal));

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.Contains("Fill these placeholders before validating: __EXAMPLE__.", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init writes a capitalized project name as a lowercase package scope.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init writes a capitalized project name as a lowercase package scope")]
    public void InitLowercasesThePackageScope()
    {
        using var consumer = TestCli.Temp("init-scope");

        var result = TestCli.Run("init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses");

        Assert.Equal(0, result.Status);
        var text = File.ReadAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"));
        Assert.Contains("\"@team-expenses/ui\"", text, StringComparison.Ordinal);
        Assert.Contains("apps/api/src/Team Expenses.Modules", text, StringComparison.Ordinal);
    }

    /// <summary>Init copies the agent context, glossary, and example cast beside the project file.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init copies the agent context, glossary, and example cast beside the project file")]
    public void CopiesScaffoldFilesBesideTheProjectFile()
    {
        using var consumer = TestCli.Temp("init-scaffold");

        var result = TestCli.Run("init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var agents = Path.Combine(consumer.Path, "AGENTS.md");
        var glossary = Path.Combine(consumer.Path, ".litespec", "glossary.yaml");
        var cast = Path.Combine(consumer.Path, ".litespec", "example-cast.md");
        Assert.Contains(".litespec/project.yaml", File.ReadAllText(agents), StringComparison.Ordinal);
        Assert.Contains("glossary.schema.yaml", File.ReadAllText(glossary), StringComparison.Ordinal);
        Assert.Contains("type: example-cast", File.ReadAllText(cast), StringComparison.Ordinal);
        Assert.Contains($"Wrote {agents}.", result.Stdout, StringComparison.Ordinal);
        Assert.Contains($"Wrote {glossary}.", result.Stdout, StringComparison.Ordinal);
        Assert.Contains($"Wrote {cast}.", result.Stdout, StringComparison.Ordinal);
        Assert.Equal("model/\nbook/\n", File.ReadAllText(Path.Combine(consumer.Path, ".litespec", ".gitignore")));
        Assert.Contains("Fill every <placeholder> in the copied files before validating.", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init writes the tool manifest pinning the tool at the release.</summary>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "init writes the tool manifest pinning the tool at the release")]
    public void WritesToolManifestAtTheRelease()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var tools = Path.Combine(consumer.Path, ".config", "dotnet-tools.json");
        Assert.Contains($"Wrote {tools}.", result.Stdout, StringComparison.Ordinal);
        var manifest = RecordText.Read(tools)!;
        Assert.Equal(ReleaseVersion.PackageOf(FixtureVersion), manifest["tools"]!["LiteSpec.Cli"]!["version"]!.GetValue<string>());
        Assert.Equal("litespec", manifest["tools"]!["LiteSpec.Cli"]!["commands"]![0]!.GetValue<string>());
    }

    /// <summary>Init for a tool install names the local tool command in the agent context.</summary>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "init for a tool install names the local tool command in the agent context")]
    public void NamesTheToolCommandForAToolInstall()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--install", "tool", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var agents = File.ReadAllText(Path.Combine(consumer.Path, "AGENTS.md"));
        Assert.Contains(
            "When this file disagrees with `dotnet litespec rules`, `dotnet litespec model query` or a record, the tool output and the record win; this file only points to them.",
            agents,
            StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_COMMAND__", agents, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".config", "dotnet-tools.json")));
        Assert.Contains("Next step: dotnet litespec model check", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init for a standalone install writes no tool manifest and names the binary in the agent context.</summary>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "init for a standalone install writes no tool manifest and names the binary in the agent context")]
    public void NamesTheBinaryForAStandaloneInstall()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--install", "standalone", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var agents = File.ReadAllText(Path.Combine(consumer.Path, "AGENTS.md"));
        Assert.Contains(
            "When this file disagrees with `litespec rules`, `litespec model query` or a record, the tool output and the record win; this file only points to them.",
            agents,
            StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet litespec", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_COMMAND__", agents, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".config", "dotnet-tools.json")));
        Assert.Contains("Next step: litespec model check", result.Stdout, StringComparison.Ordinal);

        var project = RecordText.Read(Path.Combine(consumer.Path, ".litespec", "project.yaml"))!;
        Assert.Equal(ReleaseVersion.PackageOf(FixtureVersion), project["reviewedLiteSpecVersion"]!.GetValue<string>());
    }

    /// <summary>Init refuses an unknown install with exit code two.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init refuses an unknown install with exit code two")]
    public void RefusesAnUnknownInstall()
    {
        using var consumer = TestCli.Temp("init-consumer");

        var result = TestCli.Run("init", "--preset", "dotnet-react", "--size", "small", "--install", "global", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown install 'global'; use tool or standalone.", result.Stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.yaml")));
    }

    /// <summary>Init leaves an existing tool manifest alone and still exits zero.</summary>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "init leaves an existing tool manifest alone and still exits zero")]
    public void LeavesExistingToolManifestAlone()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var tools = Path.Combine(consumer.Path, ".config", "dotnet-tools.json");
        Directory.CreateDirectory(Path.GetDirectoryName(tools)!);
        File.WriteAllText(tools, "{\"sentinel\": true}\n");

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.Equal("{\"sentinel\": true}\n", File.ReadAllText(tools));
        Assert.Contains($"'{tools}' already exists; left alone.", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init fills the agent-protocol region of a fresh agent context.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "init fills the agent-protocol region of a fresh agent context")]
    public void FillsTheAgentProtocolRegion()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var agents = File.ReadAllText(Path.Combine(consumer.Path, "AGENTS.md"));
        Assert.Contains("<!-- generated:agent-protocol -->", agents, StringComparison.Ordinal);
        Assert.Contains("<!-- /generated:agent-protocol -->", agents, StringComparison.Ordinal);
        Assert.Contains(
            "When this file disagrees with `dotnet litespec rules`, `dotnet litespec model query` or a record, the tool output and the record win; this file only points to them.",
            agents,
            StringComparison.Ordinal);
        Assert.Contains(FixtureVersion, agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_VERSION__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__SIZE__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__SHAPE__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__STACKS__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__EXTENSIONS__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__LAYERS__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__SELECTION_KEY__", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_COMMAND__", agents, StringComparison.Ordinal);
    }

    /// <summary>Init inserts the region into an existing agent context and leaves the other scaffold files alone.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init inserts the region into an existing agent context and leaves the other scaffold files alone")]
    public void InsertsTheRegionIntoAnExistingAgentContext()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var agents = Path.Combine(consumer.Path, "AGENTS.md");
        File.WriteAllText(agents, "# Ours\n\nOur notes.\n");

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var text = File.ReadAllText(agents);
        Assert.Contains("# Ours\n", text, StringComparison.Ordinal);
        Assert.Contains("Our notes.\n", text, StringComparison.Ordinal);
        Assert.Contains("<!-- generated:agent-protocol -->", text, StringComparison.Ordinal);
        Assert.Contains("<!-- /generated:agent-protocol -->", text, StringComparison.Ordinal);
        Assert.Contains($"Updated {agents}.", result.Stdout, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".litespec", "glossary.yaml")));
    }

    /// <summary>Init leaves an existing agent context that already holds the region alone.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init leaves an existing agent context that already holds the region alone")]
    public void LeavesAnExistingRegionAlone()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var agents = Path.Combine(consumer.Path, "AGENTS.md");
        var existing = "# Ours\n\n<!-- generated:agent-protocol -->\nstale\n<!-- /generated:agent-protocol -->\n";
        File.WriteAllText(agents, existing);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.Equal(existing, File.ReadAllText(agents));
        Assert.Contains($"'{agents}' already exists; left alone.", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init fills every region placeholder with the new project's selection.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init fills every region placeholder with the new project's selection")]
    public void FillsEveryRegionPlaceholder()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);
        File.WriteAllText(Path.Combine(home.Path, "templates", "consumer", "project-agents.md"), RegionTemplate);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var agents = File.ReadAllText(Path.Combine(consumer.Path, "AGENTS.md"));
        Assert.Contains("When dotnet litespec disagrees, the tool wins.", agents, StringComparison.Ordinal);
        Assert.Contains(
            "Release 9.9.9 size small shape server stacks dotnet, react extensions none layers intent and behavior: litespec, implementation and proof: litespec key server.dotnet.react command dotnet litespec.",
            agents,
            StringComparison.Ordinal);
    }

    /// <summary>Init fills the records selection with no stacks and the intent and behavior layers.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "init fills the records selection with no stacks and the intent and behavior layers")]
    public void FillsTheRecordsSelection()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);
        File.WriteAllText(Path.Combine(home.Path, "templates", "consumer", "project-agents.md"), RegionTemplate);

        var result = TestCli.Run(
            ["init", "--preset", "records", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var agents = File.ReadAllText(Path.Combine(consumer.Path, "AGENTS.md"));
        Assert.Contains(
            "Release 9.9.9 size small shape server stacks none extensions none layers intent and behavior: litespec key server command dotnet litespec.",
            agents,
            StringComparison.Ordinal);
    }

    /// <summary>Init inserts the region after the first heading, leaving the text around it untouched.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init inserts the region after the first heading, leaving the text around it untouched")]
    public void InsertsTheRegionAfterTheFirstHeading()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);
        File.WriteAllText(Path.Combine(home.Path, "templates", "consumer", "project-agents.md"), RegionTemplate);

        var agents = Path.Combine(consumer.Path, "AGENTS.md");
        File.WriteAllText(agents, "# Ours\n\nOur notes.\n");

        var result = TestCli.Run(
            ["init", "--preset", "records", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.Equal(
            "# Ours\n\n"
            + "<!-- generated:agent-protocol -->\n"
            + "When dotnet litespec disagrees, the tool wins.\n"
            + "Release 9.9.9 size small shape server stacks none extensions none layers intent and behavior: litespec key server command dotnet litespec.\n"
            + "<!-- /generated:agent-protocol -->\n"
            + "\nOur notes.\n",
            File.ReadAllText(agents));
    }

    /// <summary>Init inserts the region at the end when the agent context holds no heading.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init inserts the region at the end when the agent context holds no heading")]
    public void InsertsTheRegionAtTheEndWhenNoHeadingExists()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);
        File.WriteAllText(Path.Combine(home.Path, "templates", "consumer", "project-agents.md"), RegionTemplate);

        var agents = Path.Combine(consumer.Path, "AGENTS.md");
        File.WriteAllText(agents, "Our notes without a heading.\n");

        var result = TestCli.Run(
            ["init", "--preset", "records", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var text = File.ReadAllText(agents);
        Assert.StartsWith("Our notes without a heading.\n\n<!-- generated:agent-protocol -->\n", text, StringComparison.Ordinal);
        Assert.Contains("stacks none extensions none layers intent and behavior: litespec", text, StringComparison.Ordinal);
    }

    /// <summary>Init with a missing scaffold template names the checkout file and writes nothing.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with a missing scaffold template names the checkout file and writes nothing")]
    public void MissingScaffoldTemplateNamesTheCheckoutFile()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path, scaffold: false);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(1, result.Status);
        Assert.Contains("holds no readable templates/consumer/project-agents.md", result.Stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec/project.yaml")));
    }

    /// <summary>Init writes a project file from each preset.</summary>
    /// <param name="preset">The preset name that selects the stack combination.</param>
    /// <param name="size">The project size the preset writes.</param>
    /// <param name="shape">The project shape the preset writes.</param>
    /// <param name="stacks">The comma-separated stack names the preset writes.</param>
    /// <param name="frontend">The frontend name the preset writes.</param>
    /// <param name="path">The frontend path the preset writes.</param>
    /// <param name="platform">The frontend platform the preset writes.</param>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Theory(DisplayName = "init writes a project file from each preset")]
    [InlineData("dotnet-react", "small", "server", "dotnet,react", "web", "apps/web", "react")]
    [InlineData("dotnet-react", "large", "server", "dotnet,react", "web", "apps/web", "react")]
    [InlineData("dotnet-svelte", "small", "server", "dotnet,svelte", "web", "apps/web", "svelte")]
    [InlineData("dotnet-svelte", "large", "server", "dotnet,svelte", "web", "apps/web", "svelte")]
    [InlineData("blazor-client", "small", "client-only", "blazor", "app", "apps/app", "blazor")]
    [InlineData("blazor-client", "large", "client-only", "blazor", "app", "apps/app", "blazor")]
    [InlineData("android-client", "small", "client-only", "android", "android", "apps/mobile/src/testshop.Android", "android")]
    public void WritesProjectFileFromPreset(
        string preset, string size, string shape, string stacks, string frontend, string path, string platform)
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", preset, "--size", size, "--root", consumer.Path, "--name", "testshop"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var target = Path.Combine(consumer.Path, ".litespec", "project.yaml");
        Assert.Contains(target, result.Stdout);
        Assert.Contains("litespec model check", result.Stdout);

        var project = RecordText.Read(target)!.AsObject();
        Assert.Equal(shape, project["shape"]!.GetValue<string>());
        Assert.Equal(stacks.Split(','), project["stacks"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(size, project["size"]!.GetValue<string>());
        Assert.Equal("testshop", project["project"]!["name"]!.GetValue<string>());
        Assert.Equal(ReleaseVersion.PackageOf(FixtureVersion), project["reviewedLiteSpecVersion"]!.GetValue<string>());

        var frontends = project["frontends"]!.AsArray();
        var only = Assert.Single(frontends);
        Assert.Equal(frontend, only!["name"]!.GetValue<string>());
        Assert.Equal(path, only["path"]!.GetValue<string>());
        Assert.Equal(platform, only["platform"]!.GetValue<string>());

        // Fields the template owns and the preset does not name survive the overlay.
        Assert.Equal(".litespec", project["paths"]!["docs"]!.GetValue<string>());
    }

    /// <summary>Init defaults the project name to the root folder's name.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init defaults the project name to the root folder's name")]
    public void DefaultsNameToRootFolder()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var project = RecordText.Read(Path.Combine(consumer.Path, ".litespec/project.yaml"))!.AsObject();
        Assert.Equal(new DirectoryInfo(consumer.Path).Name, project["project"]!["name"]!.GetValue<string>());
    }

    /// <summary>Init never overwrites an existing project file.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init never overwrites an existing project file")]
    public void NeverOverwritesExistingFile()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var target = Path.Combine(consumer.Path, ".litespec/project.yaml");
        File.WriteAllText(target, "{\"sentinel\": true}\n");

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(2, result.Status);
        Assert.Contains("already exists", result.Stderr);
        Assert.Equal("{\"sentinel\": true}\n", File.ReadAllText(target));
        Assert.False(File.Exists(Path.Combine(consumer.Path, "AGENTS.md")));
    }

    /// <summary>Init with an unknown preset lists the known ones.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with an unknown preset lists the known ones")]
    public void UnknownPresetListsKnownOnes()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "nope", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown preset 'nope'", result.Stderr);
        Assert.Contains("android-client, blazor-client, dotnet-react, dotnet-svelte, records", result.Stderr);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec/project.yaml")));
    }

    /// <summary>Init with a bad size names the accepted sizes.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with a bad size names the accepted sizes")]
    public void BadSizeNamesAcceptedSizes()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--size", "medium", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown size 'medium'; use small or large.", result.Stderr);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec/project.yaml")));
    }

    /// <summary>Init without a size is a usage error.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init without a size is a usage error")]
    public void MissingSizeIsUsageError()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-react", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(2, result.Status);
        Assert.Contains("Missing --size; use small or large.", result.Stderr);
    }

    /// <summary>Init without a preset is a usage error.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init without a preset is a usage error")]
    public void MissingPresetIsUsageError()
    {
        using var home = TestCli.Temp("init-home");
        using var consumer = TestCli.Temp("init-consumer");
        FixtureHome(home.Path);

        var result = TestCli.Run(
            ["init", "--size", "small", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(2, result.Status);
        Assert.Contains("Missing --preset.", result.Stderr);
    }

    /// <summary>Init --help prints the usage.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init --help prints the usage")]
    public void HelpPrintsUsage()
    {
        var result = TestCli.Run("init", "--help");
        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec init", result.Stdout);
    }

    /// <summary>Loads the project schema the preset outputs validate against.</summary>
    /// <returns>The parsed project schema.</returns>
    static JsonNode? LoadProjectSchema()
    {
        var schemaFile = Path.Combine(TestCli.Repository, "schemas", "litespec-project.schema.yaml");
        return RecordText.Read(schemaFile);
    }

    /// <summary>
    ///     Builds a self-contained LiteSpec checkout: a manifest with a version no
    ///     release ships, a minimal consumer template, the repository's scaffold
    ///     templates, and the repository's presets.
    /// </summary>
    /// <param name="home">The folder to build the fixture checkout in.</param>
    /// <param name="scaffold">Whether to copy the scaffold templates into the fixture.</param>
    static void FixtureHome(string home, bool scaffold = true)
    {
        File.WriteAllText(Path.Combine(home, "litespec.manifest.yaml"), $$"""
            {
              "version": "{{FixtureVersion}}"
            }
            """);

        var templateDir = Path.Combine(home, "templates", "consumer");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "project.yaml"), """
            {
              "project": {
                "name": "template-name"
              },
              "size": "small",
              "stacks": [],
              "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
              "reviewedLiteSpecVersion": "0.0.0-template",
              "paths": {
                "docs": ".litespec",
                "frontends": []
              },
              "selectedExtensions": []
            }
            """);

        if (scaffold)
        {
            foreach (var name in new[] { "project-agents.md", "glossary.yaml", "example-cast.md" })
            {
                File.Copy(
                    Path.Combine(TestCli.Repository, "templates", "consumer", name),
                    Path.Combine(templateDir, name));
            }
        }

        var presetDir = Path.Combine(home, "templates", "presets");
        Directory.CreateDirectory(presetDir);
        foreach (var name in new[] { "dotnet-react", "dotnet-svelte", "blazor-client", "android-client", "records" })
        {
            File.Copy(
                Path.Combine(TestCli.Repository, "templates", "presets", $"{name}.yaml"),
                Path.Combine(presetDir, $"{name}.yaml"));
        }
    }
}
