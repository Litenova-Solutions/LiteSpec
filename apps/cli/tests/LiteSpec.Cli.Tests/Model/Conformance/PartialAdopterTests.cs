using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Conformance;

/// <summary>Proves a project in its own code layout, with two web hosts and smoke proof from a JUnit file, checks and renders at the records and traced levels.</summary>
public sealed class PartialAdopterTests
{
    /// <summary>The partial adopter reaches no blocking finding at the records and traced levels.</summary>
    /// <param name="level">The claimed conformance level: records or traced.</param>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Theory]
    [InlineData("records")]
    [InlineData("traced")]
    public void Partial_Adopter_Check_Has_No_Blocking_Findings(string level)
    {
        using var fixture = Prepare(level);

        var result = Cli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.True(result.Status == 0, result.Output);
        Assert.Equal(0, BlockingCount(result.Stdout));

        using var document = JsonDocument.Parse(result.Stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            Assert.NotEqual("blocking", finding.GetProperty("outcome").GetString());
        }
    }

    /// <summary>At traced the partial adopter reads both web hosts and the JUnit smoke proof, so no use case is unimplemented, unreached by an endpoint or unverified.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Traced_Partial_Adopter_Reads_Both_Hosts_And_JUnit_Proof()
    {
        using var fixture = Prepare("traced");

        var result = Cli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

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

    /// <summary>The partial adopter book renders at the records and traced levels and names the claimed level in its manifest.</summary>
    /// <param name="level">The claimed conformance level: records or traced.</param>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Theory]
    [InlineData("records")]
    [InlineData("traced")]
    public void Partial_Adopter_Book_Renders(string level)
    {
        using var fixture = Prepare(level);
        using var temp = Cli.Temp("partial-adopter-book");
        var outDir = Path.Combine(temp.Path, "out");

        var result = Cli.Run("book", "--root", fixture.Path, "--out", outDir);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(outDir, "index.html")), "index.html was not written.");

        var manifest = ReadShard(Path.Combine(outDir, "data", "manifest.js"), "manifest");
        Assert.Equal(level, manifest["conformance"]!.GetValue<string>());
        Assert.Equal("product/library", manifest["project"]!.AsObject()["id"]!.GetValue<string>());
    }

    /// <summary>Copies the partial-adopter fixture to a fresh temp folder and claims the level in its project file.</summary>
    /// <param name="level">The claimed conformance level: records or traced.</param>
    /// <returns>The temp folder holding the prepared fixture; the caller deletes it.</returns>
    static TempFolder Prepare(string level)
    {
        var temp = Cli.Temp("partial-adopter");

        try
        {
            CopyContents(Cli.Fixture("partial-adopter"), temp.Path);
            SetConformance(temp.Path, level);
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

    /// <summary>Writes the claimed conformance level into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="level">The level name: records, traced or full.</param>
    static void SetConformance(string root, string level)
    {
        var file = Path.Combine(root, ".litespec/project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        project["conformance"] = JsonValue.Create(level);
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
