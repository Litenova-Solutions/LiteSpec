using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Init;

/// <summary>Tests for <c>litespec init --bootstrap</c>, through a small fixture starter tree.</summary>
public sealed class InitBootstrapTests
{
    /// <summary>Init with bootstrap copies the starter with every token filled in contents and paths.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with bootstrap copies the starter with every token filled")]
    public void CopiesStarterWithEveryTokenFilled()
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses", "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var readme = File.ReadAllText(Path.Combine(consumer.Path, "README.md"));
        Assert.Contains("Team Expenses", readme, StringComparison.Ordinal);
        Assert.Contains("TeamExpenses", readme, StringComparison.Ordinal);
        Assert.Contains("team-expenses", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("__PROJECT__", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("__NAMESPACE__", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("__SCOPE__", readme, StringComparison.Ordinal);
        var kernel = Path.Combine(consumer.Path, "src", "TeamExpenses.Kernel", "Kernel.cs");
        Assert.True(File.Exists(kernel));
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, "src", "__NAMESPACE__.Kernel")));
        var kernelText = File.ReadAllText(kernel);
        Assert.Contains("TeamExpenses", kernelText, StringComparison.Ordinal);
        Assert.Contains("team-expenses", kernelText, StringComparison.Ordinal);
        Assert.DoesNotContain("__NAMESPACE__", kernelText, StringComparison.Ordinal);
    }

    /// <summary>Init with bootstrap writes a template file without its .template suffix, with its tokens filled.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with bootstrap drops the template suffix")]
    public void DropsTheTemplateSuffix()
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses", "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var folder = Path.Combine(consumer.Path, "src", "TeamExpenses.Kernel");
        var project = Path.Combine(folder, "TeamExpenses.Kernel.csproj");
        Assert.True(File.Exists(project));
        Assert.False(File.Exists(project + ".template"));
        Assert.Contains("<RootNamespace>TeamExpenses.Kernel</RootNamespace>", File.ReadAllText(project), StringComparison.Ordinal);
    }

    /// <summary>Init with bootstrap reads the starter archive an installed tool carries when the folder is absent.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with bootstrap reads the starter archive")]
    public void ReadsTheStarterArchive()
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);
        var folder = Path.Combine(home.Path, "templates", "bootstrap", "dotnet-svelte");
        System.IO.Compression.ZipFile.CreateFromDirectory(folder, folder + ".zip");
        Directory.Delete(folder, recursive: true);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses", "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.Contains("Team Expenses", File.ReadAllText(Path.Combine(consumer.Path, "README.md")), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(consumer.Path, "src", "TeamExpenses.Kernel", "TeamExpenses.Kernel.csproj")));
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.bootstrap.yaml")));
        Assert.Contains("http://localhost:5005", File.ReadAllText(Path.Combine(consumer.Path, ".litespec", "project.yaml")), StringComparison.Ordinal);
    }

    /// <summary>Init with bootstrap copies a binary starter file unchanged.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with bootstrap copies a binary starter file unchanged")]
    public void CopiesBinaryFileUnchanged()
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses", "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        byte[] expected = [0x00, 0x01, 0x02, 0xFF, 0xFE, 0x41];
        Assert.Equal(expected, File.ReadAllBytes(Path.Combine(consumer.Path, "assets", "logo.bin")));
    }

    /// <summary>Init with bootstrap merges the bootstrap project file into the written project and never copies it.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with bootstrap merges the bootstrap project file without copying it")]
    public void MergesBootstrapProjectFileWithoutCopyingIt()
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses", "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.bootstrap.yaml")));
        var project = RecordText.Read(Path.Combine(consumer.Path, ".litespec", "project.yaml"))!.AsObject();
        Assert.Equal("http://localhost:5005", project["verify"]!["baseUrl"]!.GetValue<string>());
        Assert.Equal("Team Expenses", project["verify"]!["project"]!.GetValue<string>());
        Assert.Equal("team-expenses", project["verify"]!["scope"]!.GetValue<string>());
        Assert.Equal("TeamExpenses", project["verify"]!["namespace"]!.GetValue<string>());
    }

    /// <summary>Init with bootstrap refuses an existing target file and writes nothing.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init with bootstrap refuses an existing target file and writes nothing")]
    public void RefusesAnExistingTargetFileAndWritesNothing()
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);

        var taken = Path.Combine(consumer.Path, "README.md");
        File.WriteAllText(taken, "Ours.\n");

        var result = TestCli.Run(
            ["init", "--preset", "dotnet-svelte", "--size", "small", "--root", consumer.Path, "--name", "Team Expenses", "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(1, result.Status);
        Assert.Contains("already exists", result.Stderr, StringComparison.Ordinal);
        Assert.Equal("Ours.\n", File.ReadAllText(taken));
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.yaml")));
        Assert.False(File.Exists(Path.Combine(consumer.Path, "notes.txt")));
    }

    /// <summary>Init with bootstrap and another preset is a usage error and writes no project.</summary>
    /// <param name="preset">The preset that cannot bootstrap.</param>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Theory(DisplayName = "init with bootstrap and another preset is a usage error")]
    [InlineData("records")]
    [InlineData("android-client")]
    public void RefusesAnotherPresetAsAUsageError(string preset)
    {
        using var home = TestCli.Temp("init-bootstrap-home");
        using var consumer = TestCli.Temp("init-bootstrap-consumer");
        FixtureBootstrapHome(home.Path);

        var result = TestCli.Run(
            ["init", "--preset", preset, "--size", "small", "--root", consumer.Path, "--bootstrap"],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(2, result.Status);
        Assert.Contains("--bootstrap needs --preset dotnet-svelte", result.Stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.yaml")));
    }

    /// <summary>
    ///     Builds a self-contained LiteSpec checkout with a small bootstrap starter tree:
    ///     a manifest, a minimal consumer template, the repository scaffold templates and
    ///     presets, and a fixture starter under <c>templates/bootstrap/dotnet-svelte/</c>.
    /// </summary>
    /// <param name="home">The folder to build the fixture checkout in.</param>
    static void FixtureBootstrapHome(string home)
    {
        File.WriteAllText(Path.Combine(home, "litespec.manifest.yaml"), """
            {
              "version": "9.9.9"
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

        foreach (var name in new[] { "project-agents.md", "glossary.yaml", "cast.yaml" })
        {
            File.Copy(
                Path.Combine(TestCli.Repository, "templates", "consumer", name),
                Path.Combine(templateDir, name));
        }

        var presetDir = Path.Combine(home, "templates", "presets");
        Directory.CreateDirectory(presetDir);
        foreach (var name in new[] { "dotnet-svelte", "android-client", "records" })
        {
            File.Copy(
                Path.Combine(TestCli.Repository, "templates", "presets", $"{name}.yaml"),
                Path.Combine(presetDir, $"{name}.yaml"));
        }

        var starter = Path.Combine(home, "templates", "bootstrap", "dotnet-svelte");
        Directory.CreateDirectory(Path.Combine(starter, "src", "__NAMESPACE__.Kernel"));
        Directory.CreateDirectory(Path.Combine(starter, "assets"));
        Directory.CreateDirectory(Path.Combine(starter, ".litespec"));
        File.WriteAllText(
            Path.Combine(starter, "README.md"),
            "# __PROJECT__\n\nNamespace __NAMESPACE__ serves scope __SCOPE__.\n");
        File.WriteAllText(Path.Combine(starter, "notes.txt"), "Starter notes.\n");
        File.WriteAllText(
            Path.Combine(starter, "src", "__NAMESPACE__.Kernel", "Kernel.cs"),
            "// __PROJECT__ kernel in __NAMESPACE__.Kernel for @__SCOPE__.\n");
        File.WriteAllText(
            Path.Combine(starter, "src", "__NAMESPACE__.Kernel", "__NAMESPACE__.Kernel.csproj.template"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>__NAMESPACE__.Kernel</RootNamespace></PropertyGroup></Project>\n");
        File.WriteAllBytes(
            Path.Combine(starter, "assets", "logo.bin"),
            [0x00, 0x01, 0x02, 0xFF, 0xFE, 0x41]);
        File.WriteAllText(
            Path.Combine(starter, ".litespec", "project.bootstrap.yaml"),
            "verify:\n  baseUrl: http://localhost:5005\n  project: __PROJECT__\n  scope: __SCOPE__\n  namespace: __NAMESPACE__\n");
    }
}
