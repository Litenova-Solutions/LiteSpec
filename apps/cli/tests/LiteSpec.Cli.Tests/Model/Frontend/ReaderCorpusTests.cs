using System.Text.Json;
using LiteSpec.Model.Readers.Frontend;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>
///     Runs every golden corpus case of the TypeScript reader through its bundle
///     and compares the response with the expected facts. The bundle is built by
///     <c>pnpm --dir apps/cli/fe-reader install</c> and <c>build</c> before the
///     test run; without it this test proves nothing and returns.
/// </summary>
public sealed class ReaderCorpusTests
{
    /// <summary>Every Corpus Case Matches Its Expected Facts.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Every_Corpus_Case_Matches_Its_Expected_Facts()
    {
        var bundle = Path.Combine(Cli.Repository, "apps", "cli", "fe-reader", "dist", "litespec-fe-reader.mjs");
        var corpus = Path.Combine(Cli.Repository, "apps", "cli", "fe-reader", "corpus");

        Assert.True(File.Exists(bundle), $"The reader bundle {bundle} is missing: run pnpm --dir apps/cli/fe-reader build.");
        Assert.True(Directory.Exists(corpus), $"The corpus folder {corpus} is missing.");
        var node = NodeFrontendReader.FindNode() ?? throw new InvalidOperationException("Node.js is not on the path, so the reader cannot run.");

        var cases = Directory.EnumerateDirectories(corpus)
            .Where(folder => File.Exists(Path.Combine(folder, "request.json"))
                && File.Exists(Path.Combine(folder, "expected.json")))
            .OrderBy(folder => folder, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(cases);

        foreach (var folder in cases)
        {
            var request = File.ReadAllText(Path.Combine(folder, "request.json"))
                .Replace("{case}", folder.Replace(Path.DirectorySeparatorChar, '/'), StringComparison.Ordinal);
            var expected = File.ReadAllText(Path.Combine(folder, "expected.json"))
                .Replace("{case}", folder.Replace(Path.DirectorySeparatorChar, '/'), StringComparison.Ordinal);

            var start = new System.Diagnostics.ProcessStartInfo(node)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = folder,
            };

            start.ArgumentList.Add(bundle);

            using var process = System.Diagnostics.Process.Start(start)!;
            process.StandardInput.Write(request);
            process.StandardInput.Close();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds), folder);
            Assert.True(process.ExitCode == 0, $"{folder}: {error}");

            using var actualJson = JsonDocument.Parse(output);
            using var expectedJson = JsonDocument.Parse(expected);

            Assert.True(
                FactsEqual(expectedJson.RootElement, actualJson.RootElement),
                $"{folder}\nExpected: {expectedJson.RootElement}\nActual: {actualJson.RootElement}");
        }
    }

    static bool FactsEqual(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != JsonValueKind.Object || actual.ValueKind != JsonValueKind.Object)
        {
            return JsonEqual(expected, actual);
        }

        var expectedProps = expected.EnumerateObject()
            .Where(property => property.Name is not ("typescript" or "svelte"))
            .ToList();
        var actualProps = actual.EnumerateObject()
            .Where(property => property.Name is not ("typescript" or "svelte"))
            .ToList();

        return expectedProps.All(property =>
                actual.TryGetProperty(property.Name, out var value) && JsonEqual(property.Value, value))
            && actualProps.All(property => expected.TryGetProperty(property.Name, out _));
    }

    static bool JsonEqual(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            return false;
        }

        return expected.ValueKind switch
        {
            JsonValueKind.Object => expected.EnumerateObject().All(property =>
                actual.TryGetProperty(property.Name, out var value) && JsonEqual(property.Value, value))
                && actual.EnumerateObject().All(property => expected.TryGetProperty(property.Name, out _)),
            JsonValueKind.Array => expected.GetArrayLength() == actual.GetArrayLength()
                && expected.EnumerateArray().Zip(actual.EnumerateArray()).All(pair => JsonEqual(pair.First, pair.Second)),
            JsonValueKind.String => expected.GetString() == actual.GetString(),
            JsonValueKind.Number => expected.GetRawText() == actual.GetRawText(),
            _ => true,
        };
    }
}
