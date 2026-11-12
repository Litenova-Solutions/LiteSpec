using System.Diagnostics;
using LiteSpec.Maintain.Changelog;
using LiteSpec.Maintain.Tests.Support;

namespace LiteSpec.Maintain.Tests.Changelog;

/// <summary>Tests for <c>litespec-maintain changelog</c>.</summary>
public sealed class ProvisionDiffTests
{
    /// <summary>Compare finds the added and removed identifiers.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void CompareFindsTheAddedAndRemovedIdentifiers()
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
    public void ComparePairsARenameOnTheSamePageAndStatement()
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
    public void CompareKeepsARewordedStatementAsRemovedAndAdded()
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
    public void CompareDoesNotPairARenameAcrossPages()
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
    public void ComparePairsSeveralRenamesInIdentifierOrder()
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
    public void CompareDiffsTheExtensionNames()
    {
        var result = ProvisionDiff.Compare([], [], ["alpha", "retired"], ["alpha", "delivery"]);

        Assert.Equal(["delivery"], result.ExtensionsAdded);
        Assert.Equal(["retired"], result.ExtensionsRemoved);
    }

    /// <summary>Read rows reads the identifier, the page, and the statement.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ReadRowsReadsTheIdentifierThePageAndTheStatement()
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
    public void ReadRowsSkipsARowWithoutAnIdentifier()
    {
        var rows = ProvisionDiff.ReadRows(IndexJson(new ProvisionDiff.Row("", "docs/core/topic.md", "Consumers MUST date the topic.")), "provisions.json");

        Assert.Empty(rows);
    }

    /// <summary>Read rows refuses provision data without a provisions array.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ReadRowsRefusesProvisionDataWithoutAProvisionsArray()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ProvisionDiff.ReadRows("{ \"provisions\": {} }", "provisions.json"));

        Assert.Contains("holds invalid provision data", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>The extensions are the ext pages the provision rows name, in order, without the index.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ExtensionsOfReadsTheExtPagesInOrder()
    {
        var rows = new[]
        {
            new ProvisionDiff.Row("litespec/ext/delivery#a", "docs/ext/delivery.md", "A."),
            new ProvisionDiff.Row("litespec/backend/dotnet/ext/bdd#b", "docs/backend/dotnet/ext/bdd.md", "B."),
            new ProvisionDiff.Row("litespec/ext/delivery#c", "docs/ext/delivery.md", "C."),
            new ProvisionDiff.Row("litespec/ext/README#d", "docs/ext/README.md", "D."),
            new ProvisionDiff.Row("litespec/core/scope#e", "docs/core/scope.md", "E."),
        };

        Assert.Equal(["bdd", "delivery"], ProvisionDiff.ExtensionsOf(rows));
    }

    /// <summary>Rows with no ext page name no extension.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ExtensionsOfReadsNoExtPagesAsEmpty()
    {
        Assert.Empty(ProvisionDiff.ExtensionsOf([new ProvisionDiff.Row("litespec/core/scope#e", "docs/core/scope.md", "E.")]));
    }

    /// <summary>Render writes every group with its count.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void RenderWritesEveryGroupWithItsCount()
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
    public void RenderWritesNoneForEveryEmptyGroup()
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
    public void ApplyReplacesTheMarkedSectionInPlace()
    {
        var changelog = "# Changelog\n\n## v0.2.0\n\nProse.\n\n<!-- generated:provision-diff -->\nOld.\n<!-- /generated:provision-diff -->\n\n## v0.1.0\n\nOld prose.\n";

        var updated = ProvisionDiff.Apply(changelog, "<!-- generated:provision-diff -->\nNew.\n<!-- /generated:provision-diff -->");

        Assert.Equal("# Changelog\n\n## v0.2.0\n\nProse.\n\n<!-- generated:provision-diff -->\nNew.\n<!-- /generated:provision-diff -->\n\n## v0.1.0\n\nOld prose.\n", updated);
    }

    /// <summary>Apply adds the section at the end of the topmost release.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ApplyAddsTheSectionAtTheEndOfTheTopmostRelease()
    {
        var changelog = "# Changelog\n\n## v0.2.0\n\nProse.\n\n### Part A\n\n- One thing.\n\n## v0.1.0\n\nOld prose.\n";

        var updated = ProvisionDiff.Apply(changelog, "<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->");

        Assert.Equal("# Changelog\n\n## v0.2.0\n\nProse.\n\n### Part A\n\n- One thing.\n\n<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->\n\n## v0.1.0\n\nOld prose.\n", updated);
    }

    /// <summary>Apply appends the section when the changelog holds one release.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ApplyAppendsTheSectionWhenTheChangelogHoldsOneRelease()
    {
        var updated = ProvisionDiff.Apply("# Changelog\n\n## v0.2.0\n\nProse.\n", "<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->");

        Assert.Equal("# Changelog\n\n## v0.2.0\n\nProse.\n\n<!-- generated:provision-diff -->\nBody.\n<!-- /generated:provision-diff -->\n", updated);
    }

    /// <summary>Changelog help exits zero.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogHelpExitsZero()
    {
        var result = Cli.Run("changelog", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec-maintain changelog", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Changelog with an unknown option is a usage error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogWithAnUnknownOptionIsAUsageError()
    {
        var result = Cli.Run("changelog", "--bogus");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--bogus'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog without from is a usage error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogWithoutFromIsAUsageError()
    {
        var result = Cli.Run("changelog");

        Assert.Equal(2, result.Status);
        Assert.Contains("Missing --from; name the git tag or commit to compare with.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog with an empty from is a usage error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogWithAnEmptyFromIsAUsageError()
    {
        var result = Cli.Run("changelog", "--from=");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --from needs a value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog refuses a root without docs.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogRefusesARootWithoutDocs()
    {
        using var folder = Cli.Temp("changelog");

        var result = Cli.Run("changelog", "--from", "v3.0.0", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no docs folder", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Changelog with an unknown ref is an input error.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogWithAnUnknownRefIsAnInputError()
    {
        using var folder = Cli.Temp("changelog");
        StandardsFixture.Base(folder.Path);
        StandardsFixture.Write(folder.Path, "litespec.manifest.yaml", ManifestYaml("alpha"));
        StandardsFixture.Write(folder.Path, "CHANGELOG.md", "# Changelog\n\n## v0.2.0\n\nProse.\n");

        var result = Cli.Run("changelog", "--from", "no-such-ref", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Changelog writes the provision changes of a git checkout.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogWritesTheProvisionChangesOfAGitCheckout()
    {
        using var folder = Cli.Temp("changelog");
        Git(folder.Path, "-c", "init.defaultBranch=main", "init");
        StandardsFixture.Write(folder.Path, "CHANGELOG.md", "# Changelog\n\n## v0.2.0\n\nProse.\n");
        StandardsFixture.Write(
            folder.Path,
            "docs/reference/provisions.json",
            IndexJson(
                new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary."),
                new ProvisionDiff.Row("litespec/core/topic#old-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
                new ProvisionDiff.Row("litespec/core/topic#retired-rule", "docs/core/topic.md", "Consumers MUST file the topic weekly."),
                new ProvisionDiff.Row("litespec/ext/alpha#keep-alpha", "docs/ext/alpha.md", "Consumers MUST keep alpha.")));
        Git(folder.Path, "add", "-A");
        Git(folder.Path, "-c", "user.email=test@example.com", "-c", "user.name=Test", "-c", "commit.gpgsign=false", "commit", "-m", "seed");
        Git(folder.Path, "tag", "v0.1.0");
        StandardsFixture.Write(
            folder.Path,
            "docs/reference/provisions.json",
            IndexJson(
                new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary."),
                new ProvisionDiff.Row("litespec/core/topic#new-name", "docs/core/topic.md", "Consumers MUST record the topic owner."),
                new ProvisionDiff.Row("litespec/core/topic#added-rule", "docs/core/topic.md", "Consumers MUST date the topic."),
                new ProvisionDiff.Row("litespec/ext/alpha#keep-alpha", "docs/ext/alpha.md", "Consumers MUST keep alpha."),
                new ProvisionDiff.Row("litespec/ext/beta#keep-beta", "docs/ext/beta.md", "Consumers MUST keep beta.")));

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
        Assert.Contains("Added (2):", changelog, StringComparison.Ordinal);
        Assert.Contains("- `litespec/ext/beta#keep-beta`", changelog, StringComparison.Ordinal);
        Assert.Contains("Removed (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("Renamed (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("Extensions added (1):", changelog, StringComparison.Ordinal);
        Assert.Contains("- `beta`", changelog, StringComparison.Ordinal);
        Assert.Contains("Extensions removed (0):", changelog, StringComparison.Ordinal);

        var current = Cli.Run("changelog", "--from", "v0.1.0", "--check", "--root", folder.Path);
        Assert.Equal(0, current.Status);
        Assert.Contains("CHANGELOG.md is current.", current.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Changelog rebuilds the index from the pages at a ref that predates the generated index.</summary>
    /// <proves>litespec/core/authoring#generate-the-provision-diff-for-each-release</proves>
    [Fact]
    public void ChangelogReadsThePagesAtARefWithoutAGeneratedIndex()
    {
        using var folder = Cli.Temp("changelog");
        Git(folder.Path, "-c", "init.defaultBranch=main", "init");
        StandardsFixture.Write(folder.Path, "CHANGELOG.md", "# Changelog\n\n## v0.2.0\n\nProse.\n");
        StandardsFixture.Write(folder.Path, "litespec.manifest.yaml", ManifestYaml("alpha"));
        StandardsFixture.Write(folder.Path, "docs/core/topic.md", StandardsFixture.TopicPage);
        Git(folder.Path, "add", "-A");
        Git(folder.Path, "-c", "user.email=test@example.com", "-c", "user.name=Test", "-c", "commit.gpgsign=false", "commit", "-m", "seed");
        Git(folder.Path, "tag", "v0.1.0");
        StandardsFixture.Write(
            folder.Path,
            "docs/reference/provisions.json",
            IndexJson(new ProvisionDiff.Row("litespec/core/topic#keep-the-topic-bounded", "docs/core/topic.md", "Consumers MUST keep the topic inside its declared boundary.")));

        var written = Cli.Run("changelog", "--from", "v0.1.0", "--root", folder.Path);

        Assert.Equal(0, written.Status);
        var changelog = File.ReadAllText(Path.Combine(folder.Path, "CHANGELOG.md"));
        Assert.Contains("- `litespec/core/topic#use-the-default-location`", changelog, StringComparison.Ordinal);
        Assert.Contains("Removed (1):", changelog, StringComparison.Ordinal);
        Assert.DoesNotContain("- `litespec/core/topic#keep-the-topic-bounded`", changelog, StringComparison.Ordinal);
    }

    /// <summary>Builds a minimal provision data text from the rows.</summary>
    /// <param name="rows">The provision identities.</param>
    /// <returns>The provision data text.</returns>
    static string IndexJson(params ProvisionDiff.Row[] rows) =>
        "{\"provisions\": [" + string.Join(", ", rows.Select(row => $"{{\"id\": \"{row.Id}\", \"page\": \"{row.Page}\", \"statement\": \"{row.Statement}\"}}")) + "]}";

    /// <summary>Builds a minimal manifest text from the extension names.</summary>
    /// <param name="extensions">The extension names.</param>
    /// <returns>The manifest text, in YAML.</returns>
    static string ManifestYaml(params string[] extensions) =>
        "version: 0.2.0\nextensions:\n" + string.Concat(extensions.Select(name => $"  {name}: {{}}\n"));

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
