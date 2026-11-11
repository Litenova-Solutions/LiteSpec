using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Tests.Model.Tables;

/// <summary>Proves page records carry their explanation text while entries carry none.</summary>
public sealed class BodyTests
{
    /// <summary>Pages write their explanation; entries write none.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void PagesWriteBodiesAndEntriesWriteNone()
    {
        using var temp = ModelFixture.Prepare("small");

        using var document = Build(temp.Path);

        Assert.Equal(
            "A loan lends one copy to one reader.",
            Record(document, "aggregate/lending.loan").GetProperty("explanation").GetString());
        Assert.Equal(
            "A reader borrows an available copy.",
            Record(document, "use-case/lending.borrow-copy").GetProperty("explanation").GetString());
        Assert.False(Record(document, "rule/loan.has-one-borrower").TryGetProperty("explanation", out _));
        Assert.False(Record(document, "result/borrow-copy.borrowed").TryGetProperty("explanation", out _));
    }

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static JsonElement Record(JsonDocument document, string id) =>
        document.RootElement.GetProperty("records").EnumerateArray()
            .First(record => record.GetProperty("id").GetString() == id);
}
