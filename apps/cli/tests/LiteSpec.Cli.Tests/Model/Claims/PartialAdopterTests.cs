using LiteSpec.Cli.Tests.Consumer;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Claims;

/// <summary>Proves a project in its own code layout, with two web hosts and smoke proof from a JUnit file, checks and renders under its layer claims.</summary>
public sealed class PartialAdopterTests
{
    /// <summary>The partial adopter reaches no blocking finding under the intent-and-behavior and own-code-and-proof claims.</summary>
    /// <param name="claim">The claim name, as <see cref="ConsumerFixture.Claim" /> reads it.</param>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Theory]
    [InlineData("intent-and-behavior")]
    [InlineData("own-code-and-proof")]
    public void PartialAdopterCheckHasNoBlockingFindings(string claim)
    {
        using var fixture = Prepare(claim);

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.True(result.Status == 0, result.Output);
        Assert.Equal(0, BlockingCount(result.Stdout));

        using var document = JsonDocument.Parse(result.Stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            Assert.NotEqual("blocking", finding.GetProperty("outcome").GetString());
        }
    }

    /// <summary>Under own code and proof the partial adopter reads both web hosts and the JUnit smoke proof, so no use case is unimplemented, unreached by an endpoint or unverified.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void OwnCodeAndProofPartialAdopterReadsBothHostsAndJUnitProof()
    {
        using var fixture = Prepare("own-code-and-proof");

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.True(result.Status == 0, result.Output);

        using var document = JsonDocument.Parse(result.Stdout);
        var codes = document.RootElement.GetProperty("findings").EnumerateArray()
            .Select(finding => finding.GetProperty("code").GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("reader-missing", codes);
        Assert.DoesNotContain("use-case-unimplemented", codes);
        Assert.DoesNotContain("use-case-endpoint-missing", codes);
        Assert.DoesNotContain("use-case-unverified", codes);
        Assert.DoesNotContain("refusal-unimplemented", codes);
    }

    /// <summary>The partial adopter book renders under each claim and writes the claims it declares into its manifest.</summary>
    /// <param name="claim">The claim name, as <see cref="ConsumerFixture.Claim" /> reads it.</param>
    /// <param name="expected">The claims the manifest must list, as layer=practice pairs.</param>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Theory]
    [InlineData("intent-and-behavior", "intent=litespec,behavior=litespec")]
    [InlineData("own-code-and-proof", "intent=litespec,behavior=litespec,implementation=own,proof=own")]
    public void PartialAdopterBookRenders(string claim, string expected)
    {
        using var fixture = Prepare(claim);
        using var temp = TestCli.Temp("partial-adopter-book");
        var outDir = Path.Combine(temp.Path, "out");

        var result = TestCli.Run("book", "--root", fixture.Path, "--out", outDir);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(outDir, "index.html")), "index.html was not written.");

        var manifest = ReadShard(Path.Combine(outDir, "data", "manifest.js"), "manifest");
        var claims = manifest["claims"]!.AsArray()
            .Select(entry => $"{entry!["layer"]!.GetValue<string>()}={entry["practice"]!.GetValue<string>()}");
        Assert.Equal(expected, string.Join(",", claims));
        Assert.Equal("product/library", manifest["project"]!.AsObject()["id"]!.GetValue<string>());
    }

    /// <summary>Copies the partial-adopter fixture to a fresh temp folder and claims the layers in its project file.</summary>
    /// <param name="claim">The claim name, as <see cref="ConsumerFixture.Claim" /> reads it.</param>
    /// <returns>The temp folder holding the prepared fixture; the caller deletes it.</returns>
    static TempFolder Prepare(string claim)
    {
        var temp = TestCli.Temp("partial-adopter");

        try
        {
            CopyContents(TestCli.Fixture("partial-adopter"), temp.Path);
            SetClaim(temp.Path, ConsumerFixture.Claim(claim));
            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    /// <summary>Copies every file under the source folder into the target folder.</summary>
    /// <param name="source">The fixture folder to copy from.</param>
    /// <param name="target">The temp folder to copy into.</param>
    static void CopyContents(string source, string target)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    /// <summary>Writes the claimed layers into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="layers">The layers object the project claims.</param>
    static void SetClaim(string root, JsonObject layers)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(file)!.AsObject();
        project["layers"] = layers;
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Counts the blocking findings in one check run in JSON form.</summary>
    /// <param name="json">The check output in JSON form.</param>
    /// <returns>The number of findings whose outcome is blocking.</returns>
    static int BlockingCount(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("findings").EnumerateArray()
            .Count(finding => finding.GetProperty("outcome").GetString() == "blocking");
    }

    /// <summary>Reads one book data shard as its JSON object.</summary>
    /// <param name="path">The shard file path.</param>
    /// <param name="key">The shard key the file registers.</param>
    /// <returns>The shard object.</returns>
    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }
}
