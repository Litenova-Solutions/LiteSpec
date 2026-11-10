using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Model.Record;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>
///     Proves <c>model query</c> writes an apostrophe and an angle bracket as themselves. The query
///     writes through the same serializer options as every other command, so its output reads the
///     same as <c>model check</c> output and a reader does not see escaped text.
/// </summary>
public sealed class QueryEscapingTests
{
    /// <summary>The Query Leaves An Apostrophe And An Angle Bracket Unescaped.</summary>
    [Fact]
    public void QueryLeavesAnApostropheAndAnAngleBracketUnescaped()
    {
        using var temp = TestCli.Temp("query-escaping");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "result", "result/loans.refused", Path.Combine(temp.Path, "loans.yaml"),
            """{"status": "reviewed", "outcome": "refusal", "meaning": "The reader's <loan> is refused."}"""));

        var output = Querier.Query(model, "result/loans.refused");

        Assert.NotNull(output);
        Assert.Contains("The reader's <loan> is refused.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0027", output, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u003C", output, StringComparison.Ordinal);
    }

    /// <summary>Builds one in-memory record from its JSON body.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="json">The record body.</param>
    /// <returns>The record.</returns>
    static Record Rec(string type, string id, string file, string json)
    {
        using var document = JsonDocument.Parse(json);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = file,
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
