using System.Diagnostics;
using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Tests for <c>litespec-maintain changelog</c>.</summary>
public sealed class ProvisionDiffTests
{
    /// <summary>Compare finds the added and removed identifiers.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Compare_Finds_The_Added_And_Removed_Identifiers()
    {
        var oldRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary."),
            new("litespec/core/topic#retired-rule", "docs/core/topic.md", "Consumers MUST file the topic weekly."),
        };
        var newRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary."),
            new("litespec/core/topic#added-rule", "docs/core/topic.md", "Consumers MUST date the topic."),
        };

        var result = ProvisionDiff.Compare(oldRows, newRows, [], []);

        Assert.Equal(["litespec/core/topic#added-rule"], result.Added);
        Assert.Equal(["litespec/core/topic#retired-rule"], result.Removed);
        Assert.Empty(result.Renamed);
    }

    /// <summary>Compare pairs a rename on the same page and statement.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Compare_Pairs_A_Rename_On_The_Same_Page_And_Statement()
    {
        var oldRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#old-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
        };
        var newRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#new-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
        };

        var result = ProvisionDiff.Compare(oldRows, newRows, [], []);

        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Equal([new ProvisionDiff.Rename("litespec/core/topic#old-name", "litespec/core/topic#new-name")], result.Renamed);
    }

    /// <summary>Compare keeps a reworded statement as removed and added.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Compare_Keeps_A_Reworded_Statement_As_Removed_And_Added()
    {
        var oldRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#old-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
        };
        var newRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#new-name", "docs/core/topic.md", "Consumers MUST record the topic keeper."),
        };

        var result = ProvisionDiff.Compare(oldRows, newRows, [], []);

        Assert.Equal(["litespec/core/topic#new-name"], result.Added);
        Assert.Equal(["litespec/core/topic#old-name"], result.Removed);
        Assert.Empty(result.Renamed);
    }

    /// <summary>Compare does not pair a rename across pages.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Compare_Does_Not_Pair_A_Rename_Across_Pages()
    {
        var oldRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#old-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
        };
        var newRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#new-name", "docs/core/other.md", "Consumers MUST record the topic owner."),
        };

        var result = ProvisionDiff.Compare(oldRows, newRows, [], []);

        Assert.Equal(["litespec/core/topic#new-name"], result.Added);
        Assert.Equal(["litespec/core/topic#old-name"], result.Removed);
        Assert.Empty(result.Renamed);
    }

    /// <summary>Compare pairs several renames in identifier order.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Compare_Pairs_Several_Renames_In_Identifier_Order()
    {
        var oldRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#old-second", "docs/core/topic.md", "Consumers MUST record the topic owner."),
            new("litespec/core/topic#old-first", "docs/core/topic.md", "Consumers MUST record the topic owner."),
            new("litespec/core/topic#old-unpaired", "docs/core/topic.md", "Consumers MUST record the topic owner."),
        };
        var newRows = new List<ProvisionDiff.Row>
        {
            new("litespec/core/topic#new-second", "docs/core/topic.md", "Consumers MUST record the topic owner."),
            new("litespec/core/topic#new-first", "docs/core/topic.md", "Consumers MUST record the topic owner."),
        };

        var result = ProvisionDiff.Compare(oldRows, newRows, [], []);

        Assert.Empty(result.Added);
        Assert.Equal(["litespec/core/topic#old-unpaired"], result.Removed);
        Assert.Equal(
            [
                new ProvisionDiff.Rename("litespec/core/topic#old-first", "litespec/core/topic#new-first"),
                new ProvisionDiff.Rename("litespec/core/topic#old-second", "litespec/core/topic#new-second"),
            ],
            result.Renamed);
    }

    /// <summary>Compare diffs the extension names.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Compare_Diffs_The_Extension_Names()
    {
        var result = ProvisionDiff.Compare([], [], ["alpha", "retired"], ["alpha", "delivery"]);

        Assert.Equal(["delivery"], result.ExtensionsAdded);
        Assert.Equal(["retired"], result.ExtensionsRemoved);
    }

    /// <summary>Read rows reads the identifier, the page, and the statement.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Read_Rows_Reads_The_Identifier_The_Page_And_The_Statement()
    {
        var rows = ProvisionDiff.ReadRows(
            IndexJson(new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary.")),
            "provisions.json");

        Assert.Equal(
            [new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary.")],
            rows);
    }

    /// <summary>Read rows skips a row without an identifier.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Read_Rows_Skips_A_Row_Without_An_Identifier()
    {
        var rows = ProvisionDiff.ReadRows(IndexJson(new ProvisionDiff.Row("", "docs/core/topic.md", "Consumers MUST date the topic.")), "provisions.json");

        Assert.Empty(rows);
    }

    /// <summary>Read rows refuses provision data without a provisions array.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Read_Rows_Refuses_Provision_Data_Without_A_Provisions_Array()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ProvisionDiff.ReadRows("{ \"provisions\": {} }", "provisions.json"));

        Assert.Contains("holds invalid provision data", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Read extensions reads the extension names in order.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Read_Extensions_Reads_The_Extension_Names_In_Order()
    {
        var extensions = ProvisionDiff.ReadExtensions(ManifestJson("delivery", "alpha"), "litespec.manifest.json");

        Assert.Equal(["alpha", "delivery"], extensions);
    }

    /// <summary>Read extensions reads no extensions as empty.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Read_Extensions_Reads_No_Extensions_As_Empty()
    {
        Assert.Empty(ProvisionDiff.ReadExtensions("{ \"version\": \"0.2.0\" }", "litespec.manifest.json"));
    }

    /// <summary>Render writes every group with its count.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Render_Writes_Every_Group_With_Its_Count()
    {
        var result = new ProvisionDiff.Result(
            ["litespec/core/topic#added-rule"],
            ["litespec/core/topic#retired-rule"],
            [new ProvisionDiff.Rename("litespec/core/topic#old-name", "litespec/core/topic#new-name")],
            ["delivery"],
            []);

        var block = ProvisionDiff.Render(result, "v3.0.0");

        Assert.Equal(Normalize("""
            <!-- generated:provision-diff -->
            ### Provision Changes Since v3.0.0

            Added (1):

            - `litespec/core/topic#added-rule`

            Removed (1):

            - `litespec/core/topic#retired-rule`

            Renamed (1):

            - `litespec/core/topic#old-name` to `litespec/core/topic#new-name`

            Extensions added (1):

            - `delivery`

            Extensions removed (0):

            None.

            <!-- /generated:provision-diff -->
            """), block);
    }

    /// <summary>Render writes none for every empty group.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Render_Writes_None_For_Every_Empty_Group()
    {
        var block = ProvisionDiff.Render(new ProvisionDiff.Result([], [], [], [], []), "v3.0.0");

        Assert.Equal(Normalize("""
            <!-- generated:provision-diff -->
            ### Provision Changes Since v3.0.0

            Added (0):

            None.

            Removed (0):

            None.

            Renamed (0):

            None.

            Extensions added (0):

            None.

            Extensions removed (0):

            None.

            <!-- /generated:provision-diff -->
            """), block);
    }

    /// <summary>Apply replaces the marked section in place.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Apply_Replaces_The_Marked_Section_In_Place()
    {
        var changelog = "# Changelog\n\n## v0.2.0\n\nProse.\n\n<!-- generated:provision-diff -->\nOld.\n<!-- /generated:provision-diff -->\n\n## v0.1.0\n\nOld prose.\n";

        var updated = ProvisionDiff.Apply(changelog, "<!-- generated:provision-diff -->\nNew.\n<!-- /generated:provision-diff -->");

        Assert.Equal("# Changelog\n\n## v0.2.0\n\nProse.\n\n<!-- generated:provision-diff -->\nNew.\n<!-- /generated:provision-diff -->\n\n## v0.1.0\n\nOld prose.\n", updated);
    }

    /// <summary>Apply adds the section at the end of the topmost release.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Apply_Adds_The_Section_At_The_End_Of_The_Topmost_Release()
    {
        var changelog = "# Changelog\n\n## v0.2.0\n\nProse.\n\n### Part A\n\n- One thing.\n\n## v0.1.0\n\nOld prose.\n";

        var updated = ProvisionDiff.Apply(changelog, "<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->");

        Assert.Equal("# Changelog\n\n## v0.2.0\n\nProse.\n\n### Part A\n\n- One thing.\n\n<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->\n\n## v0.1.0\n\nOld prose.\n", updated);
    }

    /// <summary>Apply appends the section when the changelog holds one release.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Apply_Appends_The_Section_When_The_Changelog_Holds_One_Release()
    {
        var updated = ProvisionDiff.Apply("# Changelog\n\n## v0.2.0\n\nProse.\n", "<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->");

        Assert.Equal("# Changelog\n\n## v0.2.0\n\nProse.\n\n<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->\n", updated);
    }

    /// <summary>Changelog help exits zero.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_Help_Exits_Zero()
    {
        var result = Cli.Run("changelog", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec-maintain changelog", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Changelog with an unknown option is a usage error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_With_An_Unknown_Option_Is_A_Usage_Error()
    {
        var result = Cli.Run("changelog", "--bogus");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--bogus'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog without from is a usage error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_Without_From_Is_A_Usage_Error()
    {
        var result = Cli.Run("changelog");

        Assert.Equal(2, result.Status);
        Assert.Contains("Missing --from; name the git tag or commit to compare with.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog with an empty from is a usage error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_With_An_Empty_From_Is_A_Usage_Error()
    {
        var result = Cli.Run("changelog", "--from=");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --from needs a value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog refuses a root without docs.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_Refuses_A_Root_Without_Docs()
    {
        using var folder = Cli.Temp("changelog");

        var result = Cli.Run("changelog", "--from", "v3.0.0", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no docs folder", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog with an unknown ref is an input error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_With_An_Unknown_Ref_Is_An_Input_Error()
    {
        using var folder = Cli.Temp("changelog");
        StandardsFixture.Base(folder.Path);
        StandardsFixture.Write(folder.Path, "litespec.manifest.json", ManifestJson("alpha"));
        StandardsFixture.Write(folder.Path, "CHANGELOG.md", "# Changelog\n\n## v0.2.0\n\nProse.\n");

        var result = Cli.Run("changelog", "--from", "no-such-ref", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Changelog writes the provision changes of a git checkout.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void Changelog_Writes_The_Provision_Changes_Of_A_Git_Checkout()
    {
        using var folder = Cli.Temp("changelog");
        Git(folder.Path, "-c", "init.defaultBranch=main", "init");
        StandardsFixture.Write(folder.Path, "CHANGELOG.md", "# Changelog\n\n## v0.2.0\n\nProse.\n");
        StandardsFixture.Write(folder.Path, "litespec.manifest.json", ManifestJson("alpha"));
        StandardsFixture.Write(
            folder.Path,
            "docs/reference/provisions.json",
            IndexJson(
                new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary."),
                new ProvisionDiff.Row("litespec/core/topic#old-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
                new ProvisionDiff.Row("litespec/core/topic#retired-rule", "docs/core/topic.md", "Consumers MUST file the topic weekly.")));
        Git(folder.Path, "add", "-A");
        Git(folder.Path, "-c", "user.email=test@example.com", "-c", "user.name=Test", "-c", "commit.gpgsign=false", "commit", "-m", "seed");
        Git(folder.Path, "tag", "v0.1.0");
        StandardsFixture.Write(folder.Path, "litespec.manifest.json", ManifestJson("alpha", "beta"));
        StandardsFixture.Write(
            folder.Path,
            "docs/reference/provisions.json",
            IndexJson(
                new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary."),
                new ProvisionDiff.Row("litespec/core/topic#new-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
                new ProvisionDiff.Row("litespec/core/topic#added-rule", "docs/core/topic.md", "Consumers MUST date the topic.")));

        var stale = Cli.Run("changelog", "--from", "v0.1.0", "--check", "--root", folder.Path);
        Assert.Equal(1, stale.Status);
        Assert.Contains("CHANGELOG.md is stale. Run: litespec-maintain changelog --from v0.1.0", stale.Stderr, StringComparison.Ordinal);

        var written = Cli.Run("changelog", "--from", "v0.1.0", "--root", folder.Path);
        Assert.Equal(0, written.Status);
        Assert.Contains("Wrote the provision changes since v0.1.0 into CHANGELOG.md.", written.Stdout, StringComparison.Ordinal);

        var changelog = File.ReadAllText(Path.Combine(folder.Path, "CHANGELOG.md"));
        Assert.Contains("<!-- generated:provision-diff -->", changelog, StringComparison.Ordinal);
        Assert.Contains("### Provision Changes Since v0.1.0", changelog, StringComparison.Ordinal);
        Assert.Contains("- `litespec/core/topic#added-rule`", changelog, StringComparison.Ordinal);
        Assert.Contains("- `litespec/core/topic#retired-rule`", changelog, StringComparison.Ordinal);
        Assert.Contains("- `litespec/core/topic#old-name` to `litespec/core/topic#new-name`", changelog, StringComparison.Ordinal);
        Assert.Contains("Added (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("Removed (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("Renamed (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("Extensions added (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("- `beta`", changelog, StringComparison.Ordinal);
        Assert.Contains("Extensions removed (0):", changelog, StringComparison.Ordinal);

        var current = Cli.Run("changelog", "--from", "v0.1.0", "--check", "--root", folder.Path);
        Assert.Equal(0, current.Status);
        Assert.Contains("CHANGELOG.md is current.", current.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Builds a minimal provision data text from the rows.</summary>
    /// <param name="rows">The provision identities.</param>
    /// <returns>The provision data text.</returns>
    static string IndexJson(params ProvisionDiff.Row[] rows) =>
        "{\"provisions\": [" + string.Join(", ", rows.Select(row => $"{{\"id\": \"{row.Id}\", \"page\": \"{row.Page}\", \"statement\": \"{row.Statement}\"}}")) + "]}";

    /// <summary>Builds a minimal manifest text from the extension names.</summary>
    /// <param name="extensions">The extension names.</param>
    /// <returns>The manifest text.</returns>
    static string ManifestJson(params string[] extensions) =>
        "{\"version\": \"0.2.0\", \"extensions\": {" + string.Join(", ", extensions.Select(name => $"\"{name}\": {{}}")) + "}}";

    /// <summary>Runs git with the arguments in the working directory, failing the test when git fails.</summary>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="args">The git arguments.</param>
    /// <returns>The standard output of the git run.</returns>
    static string Git(string workingDirectory, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(TimeSpan.FromMinutes(2)), $"git {string.Join(' ', args)} did not finish in two minutes.");
        Assert.True(process.ExitCode == 0, $"git {string.Join(' ', args)} exited {process.ExitCode}: {stdout} {stderr}");
        return stdout;
    }

    /// <summary>Converts Windows line ends to Unix line ends.</summary>
    /// <param name="value">The text to convert.</param>
    /// <returns>The text with Unix line ends.</returns>
    static string Normalize(string value) => value.Replace("\r\n", "\n");
}
