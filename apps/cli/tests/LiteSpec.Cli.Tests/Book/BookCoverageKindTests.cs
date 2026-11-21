using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves every Intent coverage entry the book writes carries one of the four closed kind words.</summary>
public sealed class BookCoverageKindTests
{
    static readonly string[] CoverageKinds = ["requirement", "step", "problem", "constraint"];

    /// <summary>Every coverage entry in the manifest and every flow step in a record detail names a closed kind.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void EveryCoverageEntryCarriesAClosedKind()
    {
        using var temp = TestCli.Temp("book-coverage-kind");
        BookTestBooks.Write(BookTestBooks.LoadFixture("Book/review.json"), temp.Path, temp.Path);
        var data = Path.Combine(temp.Path, "data");

        var entries = ReadEntries(Path.Combine(data, "coverage.js"));
        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            var kind = entry!["kind"]?.GetValue<string>() ?? "";
            Assert.Contains(kind, CoverageKinds);
        }

        var steps = 0;
        foreach (var detail in ReadRecords(data).Values)
        {
            if (detail["intentCoverage"] is not JsonObject coverage || coverage["steps"] is not JsonArray list)
            {
                continue;
            }

            foreach (var step in list)
            {
                steps++;
                Assert.Equal("step", step!["kind"]?.GetValue<string>());
            }
        }

        Assert.True(steps > 0, "The fixture holds no flow step to prove.");
    }

    /// <summary>Reads the Intent coverage entries the book writes as a bare list in its coverage shard.</summary>
    /// <param name="path">The coverage shard path.</param>
    /// <returns>The entries as a JSON array.</returns>
    static JsonArray ReadEntries(string path)
    {
        var text = File.ReadAllText(path);
        const string prefix = "BookData.put(\"coverage\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsArray();
    }

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static Dictionary<string, JsonObject> ReadRecords(string dataDir)
    {
        var details = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(dataDir, "records-*.js").OrderBy(path => path, StringComparer.Ordinal))
        {
            var shard = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in shard)
            {
                details[pair.Key] = pair.Value!.AsObject();
            }
        }

        return details;
    }
}
