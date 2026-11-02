using System.Text.Json.Nodes;
using LiteSpec.Book;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book reads a record's risk classes by name, with no tier.</summary>
public sealed class BookModelTests
{
    /// <summary>A record with risk reads its risk classes in model order.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Risk_Classes_Read_By_Name()
    {
        Assert.Equal(["money"], BookModel.RiskClassesOf(Record("""["money"]""")));
        Assert.Equal(["money", "personal-data"], BookModel.RiskClassesOf(Record("""["money","personal-data"]""")));
    }

    /// <summary>Every risk class reads by name, including physical access.</summary>
    /// <param name="risk">The risk value on the record.</param>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Theory]
    [InlineData("money")]
    [InlineData("physical-access")]
    [InlineData("personal-data")]
    [InlineData("access")]
    public void Risk_Class_Reads_By_Name(string risk)
    {
        Assert.Equal([risk], BookModel.RiskClassesOf(Record($"[\"{risk}\"]")));
    }

    /// <summary>Without risk, a record reads no risk class and shows nothing.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Risk_Without_Risk_Reads_None()
    {
        Assert.Empty(BookModel.RiskClassesOf(Record("[]")));
    }

    static JsonObject Record(string risk)
    {
        return new JsonObject
        {
            ["type"] = "requirement",
            ["id"] = "requirement/borrowing.max-wait",
            ["fields"] = (JsonObject)JsonNode.Parse($"{{\"risk\": {risk}}}")!,
        };
    }
}
